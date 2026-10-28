using Bossy.Command;
using Bossy.Execution;

namespace Bossy.Tests.Utils.Commands
{
    /// <summary>
    /// Writes a fixed reply. Useful when a test cannot hold the command instance, such as one the parser creates.
    /// </summary>
    internal class PingCommand : SimpleCommand
    {
        /// <summary>
        /// What this command writes.
        /// </summary>
        public const string Reply = "pong";

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write(Reply);
            return CommandStatus.Ok;
        }
    }
}
