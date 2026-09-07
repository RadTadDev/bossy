using Bossy.Command;
using Bossy.Schema;

namespace Bossy.Execution
{
    /// <summary>
    /// Allows writing to a command graph node after its creation.
    /// </summary>
    public interface ICommandGraphNodeWriter
    {
        /// <summary>
        /// Adds a link to specify how to run the next command.
        /// </summary>
        /// <param name="link">The link to use.</param>
        public void AddLink(CommandGraphLink link);    
    }
    
    /// <summary>
    /// A single command graph node.
    /// </summary>
    public class CommandGraphNode : ICommandGraphNodeWriter
    {
        /// <summary>
        /// The command to run at this step.
        /// </summary>
        public ICommand Command { get; }


        /// <summary>
        /// The schema for this command.
        /// </summary>
        public CommandSchema Schema { get; }
        
        /// <summary>
        /// The link to the next command.
        /// </summary>
        public CommandGraphLink Link { get; private set; } = CommandGraphLink.None;

        /// <summary>
        /// The context for this command.
        /// </summary>
        public CommandContext Context;

        /// <summary>
        /// Creates a new command graph node.
        /// </summary>
        /// <param name="command">The command to execute at this step.</param>
        /// <param name="schema">The schema for this command.</param>
        public CommandGraphNode(ICommand command, CommandSchema schema)
        {
            Command = command;
            Schema = schema;
        }
        
        void ICommandGraphNodeWriter.AddLink(CommandGraphLink link)
        {
            Link = link;
        }
    }
}