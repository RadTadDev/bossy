using Bossy.Command;

namespace Bossy.Runtime.Command.Library
{
    [Command("print", "Prints text to the screen.")]
    public class PrintCommand : SimpleCommand
    {
        [Switch('d', "actually dont")] private bool dont;
        
        [Variadic("The text to print.")] 
        private string[] _line;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            if (!dont)
            {
                ctx.Write(string.Join(" ", _line));
            }

            return CommandStatus.Ok;
        }
    }
}