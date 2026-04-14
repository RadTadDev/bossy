using System.Threading.Tasks;
using Bossy.Shell;

namespace Bossy.Command
{
    /// <summary>
    /// A runnable command.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Defines how a command behaves when executed.
        /// </summary>
        /// <param name="ctx">The context object holding information and utility.</param>
        /// <returns>The execution status.</returns>
        public Task<CommandStatus> ExecuteAsync(CommandContext ctx);
    }
}