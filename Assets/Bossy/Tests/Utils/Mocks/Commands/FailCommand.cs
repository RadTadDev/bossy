namespace RadTadDev.Bossy.Tests
{
    internal class FailCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            return CommandStatus.Error;
        }
    }
}