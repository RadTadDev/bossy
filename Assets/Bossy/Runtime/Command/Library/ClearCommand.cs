namespace RadTadDev.Bossy
{
    [Command("clear", "Clears the screen.")]
    public class ClearCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            if (ctx.Endpoint is IClearable clearable)
            {
                clearable.Clear();
            }
            
            return CommandStatus.Ok;
        }
    }
}