using Bossy.Command;
using Bossy.Execution;

namespace Bossy.Runtime.Command.Library
{
    [Command("print", "Prints text to the screen.")]
    public class PrintCommand : SimpleCommand
    {
        [Variadic("The text to print.")] 
        private string[] _line;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write(string.Join(" ", _line));
            return CommandStatus.Ok;
        }
    }
}