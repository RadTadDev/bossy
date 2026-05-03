using Bossy.Command;
using Bossy.Session;
using UnityEditor;

namespace Bossy.Runtime.Command.Library
{
    [Command("play", "Enters play mode.")]
    public class PlayCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = true;            
#endif   
            
            return CommandStatus.Ok;
        }
    }
}