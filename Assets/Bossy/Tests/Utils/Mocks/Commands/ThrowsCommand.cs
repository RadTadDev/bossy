using System;

namespace RadTadDev.Bossy.Tests
{
    internal class ThrowsCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            throw new ArgumentException("Expected test exception: This throws");
        }
    }
}