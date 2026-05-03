using Bossy.Command;
using Bossy.Frontend;
using Bossy.Session;

namespace Bossy.Runtime.Command.Library
{
    [Command("hist", "Gets command history.")]
    public class HistoryCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            if (ctx.Capabilities is not IHistorical history)
            {
                ctx.Write("This front end does not support history.");
                return CommandStatus.Error;
            }

            Formatter.Enumerate(history.GetHistory(), ctx);
            
            return CommandStatus.Ok;
        }
    }
    
    [Command("clears", "")]
    public class ClearHistoryCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            if (ctx.Capabilities is not IHistorical history)
            {
                ctx.Write("This front end does not support history.");
                return CommandStatus.Error;
            }

            history.ClearHistory();
            
            return CommandStatus.Ok;
        }
    }
}