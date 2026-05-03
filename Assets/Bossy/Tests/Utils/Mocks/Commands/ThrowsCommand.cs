using System;
using Bossy.Command;
using Bossy.Execution;

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