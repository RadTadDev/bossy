using Bossy.Command;

namespace Bossy.Runtime.Command.Library
{
    [Command("hist", "Gets command history.")]
    public class HistoryCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            Format.Enumerate(ctx.Session.History.Lines, ctx);
            
            return CommandStatus.Ok;
        }
    }
    
    [Confirm]
    [Command("clear", "Clears all history.", typeof(HistoryCommand))]
    public class ClearHistoryCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Session.History.Clear();
            
            return CommandStatus.Ok;
        }
    }
}
