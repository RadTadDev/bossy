using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Execution;

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