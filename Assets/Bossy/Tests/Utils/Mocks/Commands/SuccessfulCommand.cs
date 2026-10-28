using Bossy.Command;

namespace Bossy.Tests.Utils.Commands
{
    internal class SuccessfulCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            return CommandStatus.Ok;
        }
    }
}