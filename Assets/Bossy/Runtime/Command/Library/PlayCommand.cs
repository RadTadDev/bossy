#if UNITY_EDITOR

using System.Linq;
using Bossy.Command;
using UnityEditor;
using UnityEngine;

namespace Bossy.Runtime.Command.Library
{
    [RestrictPlatform(Platform.EditMode)]
    [Command("play", "Enters play mode.")]
    public class PlayCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            EditorApplication.isPlaying = true;

            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            var gameView = Resources.FindObjectsOfTypeAll(gameViewType).FirstOrDefault() as EditorWindow;

            if (gameView == null)
            {
                return CommandStatus.Error;
            }
            
            ctx.Write("Focusing game window");
            gameView.Focus();

            return CommandStatus.Ok;
        }
    }
}

#endif   
