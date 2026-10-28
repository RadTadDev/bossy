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
        private readonly BossyContext _context;
        private readonly BossyPermissions _permissions;
        private readonly IExecutionListener _listener;

        /// <summary>
        /// Creates a new executor.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        /// <param name="permissions">The current permissions.</param>
        /// <param name="listener">Notified as each command group starts and finishes.</param>
        public CommandExecutor(BossyContext context, BossyPermissions permissions, IExecutionListener listener)
        {
            _context = context;
            _permissions = permissions;
            _listener = listener;
        }

        /// <summary>
        /// Executes a command graph.
        /// </summary>
        /// <param name="graph">The graph to execute.</param>
        /// <param name="session">The session executing this.</param>
        /// <param name="token">The token controlling cancellation.</param>
        /// <param name="input">The default input stream.</param>
        /// <param name="output">The default output stream.</param>
        public async Task ExecuteAsync(CommandGraph graph, Session session, CancellationToken token, IReadable input, IWriteable output)
        {
            if (graph.IsEmpty) return;

            var context = new CommandContext(session, _context, input, output, true, token);
            var groups = GroupCommands(graph, context);

            if (!HasPermissions(groups, output)) return;

            var status = CommandStatus.Ok;
            var link = CommandGraphLink.Then;

            foreach (var group in groups)
            {
                if (!Continue(status, link)) break;

                status = await RunGroupAsync(group, session, context, output);
                link = group.Nodes[^1].Link;
            }
        }

        /// <summary>
        /// Checks every command in the graph against the permissions. Writes the reason when one is denied.
        /// </summary>
        private bool HasPermissions(IEnumerable<CommandGroup> groups, IWriteable output)
        {
            var denied = groups.SelectMany(group => group.Nodes).FirstOrDefault(node => !_permissions.CanExecute(node.Schema));
            if (denied == null) return true;

            output.Write(Format.Error($"You do not have permission to execute command: {denied.Schema.Name}. Use the command 'login' to login."));
            return false;
        }

        /// <summary>
        /// Runs one group. A group that is skipped because its hooks or bindings failed still writes the reason,
        /// but it never starts, so the listener is not told about it.
        /// </summary>
        private async Task<CommandStatus> RunGroupAsync(CommandGroup group, Session session, CommandContext context, IWriteable output)
        {
            var status = CommandStatus.Error;
            var started = false;

            try
            {
                if (!await PrepareGroupAsync(group, context, output)) return status;

                started = true;
                _listener.OnGroupStarted(group);

                status = await ExecuteNodesAsync(group, session, context);
                return status;
            }
            catch (OperationCanceledException)
            {
                status = CommandStatus.Cancelled;
                return status;
            }
            catch (BossyStreamClosedException)
            {
                output.Write(Format.Warning("Command failed after reading from a closed input stream."));
                return status;
            }
            catch (BossyNotAdaptableException e)
            {
                output.Write(Format.Error(e.Message));
                return status;
            }
            catch (Exception e)
            {
                output.Write(Format.Error(e.Message));
                Log.Exception(e);
                return status;
            }
            finally
            {
                foreach (var node in group.Nodes)
                {
                    node.Context.GetWriter().CloseWriter();
                }

                if (status is CommandStatus.Cancelled)
                {
                    output.Write("Task cancelled");
                }

                if (started)
                {
                    _listener.OnGroupFinished(group);
                }
            }
        }

        /// <summary>
        /// Runs the prelaunch hooks, then installs the bindings, for every command in a group.
        /// </summary>
        /// <returns>True if the group can run. False means the reason was written.</returns>
        private async Task<bool> PrepareGroupAsync(CommandGroup group, CommandContext context, IWriteable output)
        {
            foreach (var node in group.Nodes)
            {
                var hookResult = await RunHooksAsync(node.Command, context);
                if (!hookResult.Execute)
                {
                    output.Write($"{Format.Color("Command cancelled", Format.Yellow)}: {hookResult.Message}");
                    return false;
                }
            }

            foreach (var node in group.Nodes)
            {
                var binding = InstallBindings(node.Command);
                if (!binding.Success)
                {
                    output.Write(Format.Error(binding.Message));
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Runs a group's commands, either alone or as a pipeline.
        /// </summary>
        private Task<CommandStatus> ExecuteNodesAsync(CommandGroup group, Session session, CommandContext context)
        {
            return group.Nodes.Count == 1
                ? group.Nodes[0].Command.ExecuteAsync(context)
                : RunPipelineAsync(group.Nodes, session, context);
        }

        private static async Task<PrelaunchResult> RunHooksAsync(ICommand command, CommandContext context)
        {
            var hooks = command.GetType().GetCustomAttributes<PrelaunchHookAttribute>();

            foreach (var hook in hooks)
            {
                var result = await hook.OnPrelaunch(command, context);
                if (!result.Execute) return result;
            }

            return PrelaunchResult.Allow();
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
                    return InstallBindingResult.Error($"Could not resolve binding for type \"{field.FieldType.GetFriendlyName()}\". Make sure to " +
                                                      $"include an {nameof(IBossyBinder)} when you create the console and register an instance of this type.");
                }

                if (instance == null)
                {
                    return InstallBindingResult.Error($"Instance registered to Bossy for type \"{field.FieldType.GetFriendlyName()}\" was null.");
                }

                field.SetValue(command, instance);
            }

            return InstallBindingResult.Ok();
        }

        /// <summary>
        /// Runs piped commands concurrently. Each command reads from the previous one's pipe and writes to the next.
        /// </summary>
        private async Task<CommandStatus> RunPipelineAsync(IReadOnlyList<CommandGraphNode> nodes, Session session, CommandContext defaultContext)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(defaultContext.CancellationToken);

            var pipes = Enumerable.Range(0, nodes.Count - 1).Select(_ => new AsyncPipe()).ToList();

            async Task<CommandStatus> RunNode(CommandGraphNode node, IReadable reader, IWriteable writer)
            {
                var context = new CommandContext(session, _context, reader, writer, false, cts.Token);
                node.Context = context;

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
                    // Closing the pipe is what tells the next command its input has ended
                    writer.CloseWriter();
                }
            }

            var tasks = nodes.Select((node, i) =>
            {
                var reader = i == 0 ? defaultContext.GetReader() : pipes[i - 1];
                var writer = i == nodes.Count - 1 ? defaultContext.GetWriter() : pipes[i];
                return RunNode(node, reader, writer);
            }).ToList();

            var results = await Task.WhenAll(tasks);

            return results.Any(r => r is CommandStatus.Error or CommandStatus.Cancelled)
                ? results.First(r => r is CommandStatus.Error or CommandStatus.Cancelled)
                : CommandStatus.Ok;
        }

        /// <summary>
        /// Splits the graph into groups. Nodes linked by pipes share a group, and every other node ends one.
        /// </summary>
        private static List<CommandGroup> GroupCommands(CommandGraph graph, CommandContext context)
        {
            var groups = new List<CommandGroup>();
            var current = new List<CommandGraphNode>();

            foreach (var node in graph.ToArray())
            {
                // Windowed commands and pipelines replace this when they run
                node.Context = context;
                current.Add(node);

                if (node.Link is CommandGraphLink.Pipe) continue;

                groups.Add(new CommandGroup(current, node.Command as ICommandView));
                current = new List<CommandGraphNode>();
            }

            // A pipeline left open at the end of the graph still forms a group
            if (current.Count > 0)
            {
                groups.Add(new CommandGroup(current, current[^1].Command as ICommandView));
            }

            return groups;
        }

        /// <summary>
        /// Tells whether the next group should run, given how the previous one ended and how it was linked.
        /// </summary>
        private static bool Continue(CommandStatus status, CommandGraphLink link)
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
