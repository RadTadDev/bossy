using Bossy.Command;
using Bossy.Execution;
using Bossy.Frontend;
using UnityEngine.UIElements;

namespace Bossy.Tests.Utils.Commands
{
    internal class ViewCommand : SimpleCommand, ICommandView
    {
        public VisualElement CreateView() => new();
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            return CommandStatus.Ok;
        }
    }
}
