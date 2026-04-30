using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Shell;
using Bossy.Utils;

namespace Bossy.Runtime.Command.Library
{
    [Command("consume", "sda")]
    public class ConsumerCommand : ICommand
    {
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            await foreach (var line in ctx.ReadAllAsync<string>())
            {
                ctx.Write("consumer: " + line);
                Log.Info("HERE");
            }
            
            return CommandStatus.Ok;
        }
    }
}