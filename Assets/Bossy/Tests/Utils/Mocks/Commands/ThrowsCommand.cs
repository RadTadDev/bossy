using System;
using Bossy.Command;
using Bossy.Shell;

namespace Bossy.Tests.Utils.Commands
{
    internal class ThrowsCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            throw new ArgumentException("This throws");
        }
    }
}