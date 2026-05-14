using System.Threading.Tasks;
using Bossy.Command;

namespace Bossy.Runtime.Command.Library
{
    [Command("print", "Prints text to the screen.")]
    public class PrintCommand : ICommand
    {
        [Variadic("The text to print.")] 
        private string[] _line;
        
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            if (_line.Length > 0)
            {
                ctx.Write(string.Join(" ", _line));
            }
            else
            {
                await foreach (var line in ctx.ReadAllAsync<object>())
                {
                    ctx.Write(line);
                }
            }            

            return CommandStatus.Ok;
        }
    }
}