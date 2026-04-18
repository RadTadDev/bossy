using Bossy.Command;
using Bossy.Shell;

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