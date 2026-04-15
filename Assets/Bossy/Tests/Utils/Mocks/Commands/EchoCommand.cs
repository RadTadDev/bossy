using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Shell;

namespace Bossy.Tests.Utils.Commands
{
    public class EchoCommand : ICommand
    {
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            await foreach (var obj in ctx.ReadAllAsync<object>())
            {
                ctx.Write(obj);
            }

            return CommandStatus.Ok;
        }
    }
}