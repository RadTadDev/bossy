using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Frontend;
using Bossy.Utils;

namespace Bossy.Execution
{
    /// <summary>
    /// Executes a command graph.
    /// </summary>
    internal class CommandExecutor
    {
        private readonly Session _session;
        private readonly BossyContext _context;

        /// <summary>
        /// Creates a new executor.
        /// </summary>
        /// <param name="session">The session that owns this executor.</param>
        /// <param name="context">The Bossy context.</param>
        public CommandExecutor(Session session, BossyContext context)
        {
            _session = session;
            _context = context;
        }

        /// <summary>
        /// Executes a command graph.
        /// </summary>  
        /// <param name="graph">The graph to execute.</param>
        /// <param name="session">The session executing this.</param>
        /// <param name="token">The token controlling cancellation.</param>
        /// <param name="input">The default input stream.</param>
        /// <param name="output">The default output stream.</param>
        public async Task ExecuteAsync(CommandGraph graph, Session session, CancellationToken token, IReadable input = null, IWriteable output = null)
        {
            if (graph.IsEmpty) return;

            if (graph.Windowed)
            {
                _session.CreateCommandSession(graph);
                return;
            }
            
            var groups = GroupCommands(graph);

            var previousStatus = CommandStatus.Ok;
            var previousLink = CommandGraphLink.Then;

            input ??= session.Bridge;
            output ??= session.Bridge;

            var defaultContext = new CommandContext(session, _context, input, output, true, token);
            defaultContext.SetCapabilitySourcer(session.Bridge.GetCapabilities);

            foreach (var group in groups)
            {
                if (!Continue(previousStatus, previousLink))
                {
                    break;
                }

                try
                {
                    // 1. Ensure all prelaunch hooks pass
                    var failure = group.Nodes.Select(n => RunHooks(n.Command)).FirstOrDefault(r => !r.Execute);
                    if (failure != null)
                    {
                        output.Write($"Command execution cancelled: {failure.Message}");
                        
                        // Note: Use error here rather than canceled so other commands react appropriately
                        previousStatus = CommandStatus.Error;
                        previousLink = group.Nodes.Last().Link;
                        
                        continue;
                    }
                
                    // 2. Install all bindings
                    var bindingFailure = group.Nodes.Select(n => InstallBindings(n.Command)).FirstOrDefault(r => !r.Success);
                    if (bindingFailure != null)
                    {
                        output.Write(Format.Error(bindingFailure.Message));
                        
                        previousStatus = CommandStatus.Error;
                        previousLink = group.Nodes.Last().Link;
                        
                        continue;
                    }
                    
                    // 3. Create a view if needed
                    if (group.View != null)
                    {
                        session.Bridge.PushContent(group.View);
                    }
                    
                    // 4. Execute
                    var task = group.Nodes.Count == 1
                        ? group.Nodes[0].Command.ExecuteAsync(defaultContext)
                        : BuildPipeline(group.Nodes, session, defaultContext);

                    // 5. Update bookkeeping
                    previousLink = group.Nodes.Last().Link;
                    previousStatus = await task;
                }
                catch (OperationCanceledException)
                {
                    output.Write("Task cancelled");
                    previousStatus = CommandStatus.Cancelled;
                }
                catch (BossyStreamClosedException)
                {
                    output.Write(Format.Warning("Command failed after reading from a closed input stream."));
                    previousStatus = CommandStatus.Error;
                }
                catch (BossyNotAdaptableException e)
                {
                    output.Write(Format.Error(e.Message));
                    previousStatus = CommandStatus.Error;
                }
                catch (Exception e)
                {
                    output.Write(Format.Error(e.Message));
                    previousStatus = CommandStatus.Error;
                    Log.Exception(e);
                }
                finally
                {
                    if (group.View != null)
                    {
                        session.Bridge.PopContent();
                    }
                }
            }
        }

        private static PrelaunchResult RunHooks(ICommand command)
        {
            var hooks = command.GetType().GetCustomAttributes<PrelaunchHookAttribute>();

            var failure = hooks.Select(h => h.OnPrelaunch(command)).FirstOrDefault(h => !h.Execute);
            
            return failure ?? PrelaunchResult.Allow();
        }

        private InstallBindingResult InstallBindings(ICommand command)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

            var bindableFields = command.GetType().GetFields(flags)
                .Where(f => f.GetCustomAttribute<BindAttribute>() != null);

            foreach (var field in bindableFields)
            {
                if (!_context.Binder.TryGet(field.FieldType, out var instance))
                {
                    return InstallBindingResult.Error($"Could not resolve binding for type \"{field.FieldType.GetFriendlyName()}\"");
                }
                field.SetValue(command, instance);
            }

            return InstallBindingResult.Ok();
        }
        
        private async Task<CommandStatus> BuildPipeline(IReadOnlyList<CommandGraphNode> group, Session session, CommandContext defaultContext)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(defaultContext.CancellationToken);

            var pipes = Enumerable.Range(0, group.Count - 1).Select(_ => new AsyncPipe()).ToList();

            async Task<CommandStatus> RunNode(CommandGraphNode node, IReadable reader, IWriteable writer)
            {
                var context = new CommandContext(session, _context, reader, writer, false, cts.Token);
                try
                {
                    var status = await node.Command.ExecuteAsync(context);
                    if (status == CommandStatus.Error)
                    {
                        cts.Cancel();
                    }
                    return status;
                }
                catch (OperationCanceledException e)
                {
                    return e.CancellationToken == cts.Token && !defaultContext.CancellationToken.IsCancellationRequested
                        ? CommandStatus.Error
                        : CommandStatus.Cancelled;
                }
                catch
                {
                    cts.Cancel();
                    throw;
                }
                finally
                {
                    writer.CloseWriter();
                }
            }

            var tasks = group.Select((node, i) =>
            {
                var reader = i == 0 ? defaultContext.GetReader() : pipes[i - 1];
                var writer = i == group.Count - 1 ? defaultContext.GetWriter() : pipes[i];
                return RunNode(node, reader, writer);
            }).ToList();

            // Execution suspends here until this pipeline task is awaited...
            var results = await Task.WhenAll(tasks);

            return results.Any(r => r is CommandStatus.Error or CommandStatus.Cancelled)
                ? results.First(r => r is CommandStatus.Error or CommandStatus.Cancelled)
                : CommandStatus.Ok;
        }

        private List<CommandGroup> GroupCommands(CommandGraph graph)
        {
            List<CommandGroup> groups = new();
            List<CommandGraphNode> current = new();
            CommandGraphNode last = null;
            
            foreach (var node in graph.ToArray())
            {
                last = node;
                
                current.Add(node);

                if (node.Link is CommandGraphLink.Pipe) continue;
                
                groups.Add(new CommandGroup(current, node.Command as IContentView));
                current = new List<CommandGraphNode>();
            }
            
            if (current.Count > 0)
            {
                groups.Add(new CommandGroup(current, last?.Command as IContentView));
            }

            return groups;
        }

        private bool Continue(CommandStatus status, CommandGraphLink link)
        {
            if (status == CommandStatus.Cancelled) return false;
            if (link is CommandGraphLink.None) return false;
            if (link is CommandGraphLink.Then or CommandGraphLink.Pipe) return true;

            if (status is CommandStatus.Ok)
            {
                return link is CommandGraphLink.And;
            }

            return link is CommandGraphLink.Or;
        }
        
        private class InstallBindingResult
        {
            /// <summary>
            /// Whether bindings were installed correctly.
            /// </summary>
            public readonly bool Success;
            
            /// <summary>
            /// The failure message if not successful.
            /// </summary>
            public readonly string Message;

            private InstallBindingResult(bool success, string message)
            {
                Success = success;
                Message = message;
            }

            /// <summary>
            /// Creates a successful result.
            /// </summary>
            /// <returns>The result.</returns>
            public static InstallBindingResult Ok()
            {
                return new InstallBindingResult(true, string.Empty);
            }

            /// <summary>
            /// Creates a failed result.
            /// </summary>
            /// <param name="message">The failure message.</param>
            /// <returns>The result.</returns>
            public static InstallBindingResult Error(string message)
            {
                return new InstallBindingResult(false, message);
            }
        }
    }
}