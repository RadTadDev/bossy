using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bossy.FrontEnd;
using Bossy.Utils;

namespace Bossy.Shell
{
    /// <summary>
    /// Executes a command graph.
    /// </summary>
    internal class CommandExecutor
    {
        private readonly SessionManager _sessionManager;

        /// <summary>
        /// Creates a new executor.
        /// </summary>
        /// <param name="sessionManager">The shell.</param>
        public CommandExecutor(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Executes a command graph.
        /// </summary>
        /// <param name="graph">The graph to execute.</param>
        /// <param name="userInterface">The user interface executing this.</param>
        /// <param name="defaultInput">The default input stream.</param>
        /// <param name="defaultOutput">The default output stream.</param>
        /// <param name="token">The token controlling cancellation.</param>
        public async Task ExecuteAsync(CommandGraph graph, IUserInterface userInterface, IReadable defaultInput, IWriteable defaultOutput, CancellationToken token)
        {
            if (graph.IsEmpty) return;

            var groups = GroupCommands(graph);

            var previousStatus = CommandStatus.Ok;
            var previousLink = CommandGraphLink.Then;

            var defaultContext = new CommandContext(_sessionManager, userInterface, defaultInput, defaultOutput, true, token);

            foreach (var group in groups)
            {
                if (!Continue(previousStatus, previousLink))
                {
                    break;
                }

                try
                {
                    var task = group.Count == 1
                        ? group[0].Command.ExecuteAsync(defaultContext)
                        : BuildPipeline(group, defaultContext);

                    previousLink = group.Last().Link;
                    previousStatus = await task;
                }
                catch (OperationCanceledException)
                {
                    defaultOutput.Write("Task cancelled");
                    previousStatus = CommandStatus.Cancelled;
                }
                catch (BossyStreamClosedException)
                {
                    defaultOutput.Write("Command failed after reading from a closed input stream.");
                    previousStatus = CommandStatus.Error;
                }
                catch (BossyNotAdaptableException e)
                {
                    defaultOutput.Write(e.Message);
                    previousStatus = CommandStatus.Error;
                }
                catch (Exception e)
                {
                    defaultOutput.Write(e.Message);
                    previousStatus = CommandStatus.Error;
                }
            }
        }

        private async Task<CommandStatus> BuildPipeline(List<CommandGraphNode> group, CommandContext defaultContext)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(defaultContext.CancellationToken);

            var pipes = Enumerable.Range(0, group.Count - 1).Select(_ => new AsyncPipe()).ToList();

            async Task<CommandStatus> RunNode(CommandGraphNode node, IReadable reader, IWriteable writer)
            {
                var context = new CommandContext(_sessionManager, defaultContext.UserInterface, reader, writer, false, cts.Token);
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
                    writer.Close();
                }
            }

            var tasks = group.Select((node, i) =>
            {
                var reader = i == 0 ? defaultContext.InputStream : pipes[i - 1];
                var writer = i == group.Count - 1 ? defaultContext.OutputStream : pipes[i];
                return RunNode(node, reader, writer);
            }).ToList();

            // Execution suspends here until this pipeline task is awaited...
            var results = await Task.WhenAll(tasks);

            return results.Any(r => r is CommandStatus.Error or CommandStatus.Cancelled)
                ? results.First(r => r is CommandStatus.Error or CommandStatus.Cancelled)
                : CommandStatus.Ok;
        }

        private List<List<CommandGraphNode>> GroupCommands(CommandGraph graph)
        {
            List<List<CommandGraphNode>> groups = new();
            List<CommandGraphNode> current = new();

            foreach (var node in graph.ToArray())
            {
                current.Add(node);

                if (node.Link is not CommandGraphLink.Pipe)
                {
                    groups.Add(current);
                    current = new List<CommandGraphNode>();
                }
            }
            
            if (current.Count > 0)
            {
                groups.Add(current);
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