using System.Threading.Tasks;
using Bossy.Shell;

namespace Bossy.Command
{
    /// <summary>
    /// A simple and synchronous command.
    /// </summary>
    public abstract class SimpleCommand : ICommand
    {
        public Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            return Task.FromResult(Execute(ctx));
        }

        /// <summary>
        /// Defines how a command behaves when executed.
        /// </summary>
        /// <returns>The execution status.</returns>
        /// <param name="ctx">The context object holding information and utility.</param>
        protected abstract CommandStatus Execute(SimpleContext ctx);
    }
}