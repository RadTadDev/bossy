using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bossy.Command;
using Bossy.Frontend.Autocomplete;
using UnityEditor;

namespace Bossy.Runtime.Command.Library
{
    [Command("open", "Opens a file to edit in the external editor.")]
    public class OpenCommand : SimpleCommand
    {
        [Suggest(nameof(Suggest))]
        [Switch('f', "The script file name to open.")]
        private string _fileName;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            if (!_fileName.EndsWith(".cs"))
            {
                ctx.Write("Only .cs files are supported.");
                return CommandStatus.Error;
            }

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