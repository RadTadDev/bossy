using Bossy.Command;

namespace Bossy.Shell
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
        /// The link to the next command.
        /// </summary>
        public CommandGraphLink Link { get; private set; } = CommandGraphLink.None;

        /// <summary>
        /// Creates a new command graph node.
        /// </summary>
        /// <param name="command">The command to execute at this step.</param>
        public CommandGraphNode(ICommand command)
        {
            Command =  command;
        }
        
        void ICommandGraphNodeWriter.AddLink(CommandGraphLink link)
        {
            Link = link;
        }
    }
}