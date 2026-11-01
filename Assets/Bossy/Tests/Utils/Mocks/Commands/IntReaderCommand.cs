using System.Threading.Tasks;

namespace RadTadDev.Bossy.Tests
{
    internal class IntReaderCommand : ICommand
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