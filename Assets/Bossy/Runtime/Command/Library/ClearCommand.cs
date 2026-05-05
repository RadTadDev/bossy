using Bossy.Command;
using Bossy.Frontend;
using Bossy.Execution;

namespace Bossy.Runtime.Command.Library
{
    [Command("clear", "Clears the screen.")]
    public class ClearCommand : SimpleCommand
    {
        [Bind] private string mystring;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            if (ctx.Capabilities is IClearable clearable)
            {
                clearable.Clear();
            }
            
            return CommandStatus.Ok;
        }
    }
}