using Bossy.Command;
using Bossy.Shell;

namespace Bossy.Tests.Utils.Commands
{
    /// <summary>
    /// Tracks whether this command was called. Useful for testing command graphs.
    /// </summary>
    public class TrackingCommand : SimpleCommand
    {
        /// <summary>
        /// True if this command was called.
        /// </summary>
        public bool WasCalled { get; private set; }
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            WasCalled = true;
            return CommandStatus.Ok;
        }
    }
}