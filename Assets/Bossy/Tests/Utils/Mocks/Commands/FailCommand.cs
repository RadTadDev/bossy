using Bossy.Command;

namespace Bossy.Tests.Utils.Commands
{
    internal class FailCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            return CommandStatus.Error;
        }
    }
}