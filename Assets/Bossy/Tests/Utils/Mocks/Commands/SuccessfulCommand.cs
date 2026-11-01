namespace RadTadDev.Bossy.Tests
{
    internal class SuccessfulCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            return CommandStatus.Ok;
        }
    }
}