using System.Threading.Tasks;

namespace Bossy.Command
{
    /// <summary>
    /// A synchronous, runnable command.
    /// </summary>
    public abstract class SimpleCommand : ICommand
    {
        public Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            return Task.FromResult(Execute());
        }

        // TODO: Probably need to pass in a new type of command context which also may require
        // TODO: making a base class (likely the simple is the base, then the above inherits it)
        
        /// <summary>
        /// Defines how a command behaves when executed.
        /// </summary>
        /// <returns>The execution status.</returns>
        protected abstract CommandStatus Execute();
    }
}