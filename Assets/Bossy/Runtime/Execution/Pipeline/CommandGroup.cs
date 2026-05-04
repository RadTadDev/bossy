using System.Collections.Generic;
using Bossy.Frontend;

namespace Bossy.Execution
{
    /// <summary>
    /// A group of commands executed together.
    /// </summary>
    public class CommandGroup
    {
        /// <summary>
        /// The commands to run.
        /// </summary>
        public IReadOnlyList<CommandGraphNode> Commands { get; }
        
        /// <summary>
        /// The view if the visible command defines one.
        /// </summary>
        public IContentView View { get; }
        
        /// <summary>
        /// Creates a new command group.
        /// </summary>
        /// <param name="commands">The commands to run.</param>
        /// <param name="view">The view if the visible command defines one.</param>
        public CommandGroup(List<CommandGraphNode> commands, IContentView view)
        {
            Commands = commands;
            View = view;
        }
    }
}