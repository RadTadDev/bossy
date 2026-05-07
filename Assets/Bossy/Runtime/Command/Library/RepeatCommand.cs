using System.Threading.Tasks;
using Bossy.Command;

namespace Bossy.Runtime.Command.Library
{
    [Command("repeat", "Repeats input as output.")]
    public class RepeatCommand : ICommand
    {
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            await foreach (var line in ctx.ReadAllAsync<object>())
            {
                ctx.Write(line);
            }
            
            return CommandStatus.Ok;
        }
    }
}