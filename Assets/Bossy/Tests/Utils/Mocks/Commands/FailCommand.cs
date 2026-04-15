using Bossy.Command;
using Bossy.Shell;

namespace Bossy.Tests.Utils.Commands
{
    public class FailCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            return CommandStatus.Error;
        }
    }
}