using System;
using System.Threading.Tasks;
using UnityEditor;

namespace Bossy.Command
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RestrictPlatformAttribute : PrelaunchHookAttribute
    {
        private readonly Platform _platform;
        
        public RestrictPlatformAttribute(Platform platform)
        {
            _platform = platform;
        }
        
        public override Task<PrelaunchResult> OnPrelaunch(ICommand command, CommandContext ctx)
        {
#if UNITY_EDITOR
            if (_platform is Platform.Build) goto Failure;
            if (!EditorApplication.isPlaying && _platform is Platform.Runtime) goto Failure;
            if (EditorApplication.isPlaying && _platform is Platform.EditMode) goto Failure;
#else
            if (Platform is Platform.Editor or Platform.EditMode) goto Failure;
#endif

            return Task.FromResult(PrelaunchResult.Allow());
            
            Failure:
            return Task.FromResult(PrelaunchResult.Deny("This command is not available at this time."));
        }
    }
}