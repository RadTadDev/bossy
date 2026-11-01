#if UNITY_EDITOR
using UnityEditor;

namespace RadTadDev.Bossy
{
    [RestrictPlatform(Platform.EditMode)]
    [Command("build", "Opens the build settings.")]
    public class BuildCommand : SimpleCommand
    {
        [Switch('r', "Whether to run the application after building.")]
        private bool _run;

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            BuildPlayerWindow.ShowBuildPlayerWindow();
            return CommandStatus.Ok;
        }
    }
}

#endif
