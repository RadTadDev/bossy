using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Shell;

namespace Bossy.Runtime.Command.Library
{
    [Command("produce", "Producer Command")]
    public class ProducerCommand : ICommand
    {
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            while (true)
            {
                ctx.Write("fish");   
                await ctx.Yield();
            }
            
            // ReSharper disable once FunctionNeverReturns
        }
    }
}