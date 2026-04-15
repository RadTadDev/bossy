using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Utils;
using Codice.CM.Common.UndoCheckOut;
using UnityEngine.Analytics;

namespace Bossy.Shell
{
    /// <summary>
    /// Executes a command graph.
    /// </summary>
    public class CommandExecutor
    {
        /// <summary>
        /// Executes a command graph.
        /// </summary>
        /// <param name="graph">The graph to execute.</param>
        /// <param name="token">The token controlling cancellation.</param>
        public async Task ExecuteAsync(CommandGraph graph, CancellationToken token)
        {
            var writer = graph.MainWriter;
            var groups = GroupCommands(graph);
            
            var previousStatus = CommandStatus.Ok;
            var previousLink = CommandGraphLink.Then;
            
            foreach (var group in groups)
            {
                if (!Continue(previousStatus, previousLink)) break;
                
                var context = new CommandContext();
                
                if (group.Count == 1)
                {
                    previousStatus = await RunTaskAsync(group[0].Command.ExecuteAsync(context));
                }
            }
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

            if (groups.Count <= 0)
            {
                groups.Add(current);
            }
            
            return groups;
        }
        
        private async Task<CommandStatus> RunTaskAsync(Task<CommandStatus> task, IWriteable errorWriter)
        {
            try
            {
                return await task;
            }
            catch (OperationCanceledException)
            {
                // Command or session canceled
                return CommandStatus.Cancelled;
            }
            catch (BossyNotAdaptableException e)
            {
                // Bad type adaptation
                errorWriter.Write(e.Message);
                return CommandStatus.Error;
            }
            catch (Exception e)
            {
                errorWriter.Write(e.Message);
                return CommandStatus.Error;
            }
        }

        private bool Continue(CommandStatus status, CommandGraphLink link)
        {
        }
    }
}