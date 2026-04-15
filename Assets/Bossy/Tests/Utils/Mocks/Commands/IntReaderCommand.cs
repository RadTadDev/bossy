using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Shell;

namespace Bossy.Tests.Utils.Commands
{
    public class IntReaderCommand : ICommand
    {
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            await foreach (var _ in ctx.ReadAllAsync<int>())
            {
                
            }

            return CommandStatus.Ok;
        }
    }
}