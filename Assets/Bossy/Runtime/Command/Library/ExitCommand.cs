using Bossy.Command;
using Bossy.Session;
using UnityEditor;
using UnityEngine;

namespace Bossy.Runtime.Command.Library
{
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