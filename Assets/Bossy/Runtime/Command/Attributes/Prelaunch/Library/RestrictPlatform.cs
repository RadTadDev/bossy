using UnityEditor;

namespace Bossy.Command
{
    public class RestrictPlatform : PrelaunchHookAttribute
    {
        public readonly Platform Platform;
        
        public RestrictPlatform(Platform platform)
        {
            Platform = platform;
        }
        
        public override PrelaunchResult OnPrelaunch(ICommand command)
        {
#if UNITY_EDITOR
            if (Platform is Platform.Build) goto Failure;
            if (!EditorApplication.isPlaying && Platform is Platform.Runtime) goto Failure;
            if (EditorApplication.isPlaying && Platform is Platform.EditMode) goto Failure;
#else
            if (Platform is Platform.Editor or Platform.EditMode) goto Failure;
#endif

            return PrelaunchResult.Allow();
            
            Failure:
            return PrelaunchResult.Deny("This command is not available at this time.");
        }
    }
}