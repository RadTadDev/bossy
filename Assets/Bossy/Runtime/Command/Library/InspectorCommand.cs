#if UNITY_EDITOR

using Bossy.Command;
using UnityEditor;

namespace Bossy.Runtime.Command.Library
{
    [Command("ilock", "Toggles the inspector lock.")]
    public class InspectorCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ActiveEditorTracker.sharedTracker.isLocked = !ActiveEditorTracker.sharedTracker.isLocked;
            ActiveEditorTracker.sharedTracker.ForceRebuild();

            return CommandStatus.Ok;
        }
    }
}

#endif
