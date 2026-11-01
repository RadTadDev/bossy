using UnityEngine.UIElements;

namespace RadTadDev.Bossy.Tests
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
