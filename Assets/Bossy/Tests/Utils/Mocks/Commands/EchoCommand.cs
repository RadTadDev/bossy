using System.Threading.Tasks;

namespace RadTadDev.Bossy.Tests
{
    internal class EchoCommand : ICommand
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

