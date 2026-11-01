using System.Threading.Tasks;

namespace RadTadDev.Bossy
{
    [Command("repeat", "Repeats input as output.")]
    public class RepeatCommand : ICommand
    {
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            if (ctx.Endpoint is IModifiablePromptHeader prompt)
            {
                prompt.SetPromptHeader("Repeat");   
            }
            
            await foreach (var line in ctx.ReadAllAsync<object>())
            {
                ctx.Write(line);
            }

            return CommandStatus.Ok;
        }
    }
}