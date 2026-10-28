using System;
using Bossy.Command;

namespace Bossy.Tests.Utils.Commands
{
    internal class ThrowsCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            throw new ArgumentException("Expected test exception: This throws");
        }
    }
}