using Bossy.Command;
using Bossy.Execution;

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