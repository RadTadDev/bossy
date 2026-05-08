using Bossy.Command;

#if UNITY_EDITOR
using UnityEditor;
#else
using UnityEngine;
#endif

namespace Bossy.Runtime.Command.Library
{
    [RestrictPlatform(Platform.Runtime)]
    [Command("exit", "Exits the game.")]
    public class ExitCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return CommandStatus.Ok;
        }
    }
}