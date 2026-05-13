#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bossy.Command;
using Bossy.Frontend.Autocomplete;

namespace Bossy.Runtime.Command.Library
{
    [RestrictPlatform(Platform.Editor)]
    [Command("open", "Opens a file to edit in the external editor.")]
    public class OpenCommand : SimpleCommand
    {
        [Suggest(nameof(Suggest))]
        [EndsWith(".cs")]   
        [Positional(0, "The script file name to open.")]
        private string _fileName;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            var guids = AssetDatabase.FindAssets(_fileName.Replace(".cs", "") + " t:Script");
            
            if (guids.Length == 0)
            {
                ctx.Write($"No script named '{_fileName}' found.");
                return CommandStatus.Error;
            }

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            AssetDatabase.OpenAsset(asset);

            return CommandStatus.Ok;
        }

        private static IEnumerable<string> Suggest()
        {
            return AssetDatabase.FindAssets("t:Script", new [] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(Path.GetFileName);
        }
    }
}

#endif
