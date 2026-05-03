using System;
using System.Collections.Generic;
using System.Linq;
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

            var status = CommandStatus.Ok;
            
            foreach (var group in groups)
            {
                if (!Continue(previousStatus, previousLink))
                {
                    break;
                }

                try
                {
                    if (group.View != null)
                    {
                        session.Bridge.PushContent(group.View);
                    }
                    
                    var task = group.Commands.Count == 1
                        ? group.Commands[0].Command.ExecuteAsync(defaultContext)
                        : BuildPipeline(group.Commands, session, defaultContext);

                    previousLink = group.Commands.Last().Link;
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
    }
}