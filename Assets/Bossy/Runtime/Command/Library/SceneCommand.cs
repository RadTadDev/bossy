using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Command;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Bossy.Frontend.Autocomplete;
using UnityEngine;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Bossy.Runtime.Command.Library
{
    
    [Command("scene", "Commands for inspecting and manipulating Unity scenes.")]
    public class SceneCommand : ICommand
    {
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            await ctx.ExecuteAsync("help scene");
            return CommandStatus.Ok;
        }
        
        public static List<string> SuggestLoadableScenes()
        {
            if (Application.isPlaying)
            {
                return Enumerable
                    .Range(0, SceneManager.sceneCountInBuildSettings)
                    .Select(i => SceneManager.GetSceneByBuildIndex(i).name)
                    .Distinct()
                    .ToList();
            }

#if UNITY_EDITOR
            return AssetDatabase.FindAssets("t:Scene", new [] { "Assets" } )
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(Path.GetFileNameWithoutExtension)
                .Distinct()
                .ToList();

#else
            return new List<string>();
#endif
        }
        
        public static List<string> SuggestLoadedScenes()
        {
            return Enumerable.Range(0, SceneManager.sceneCount)
                .Select(i => SceneManager.GetSceneAt(i).name).ToList();
        }
    }
    
    [Command("load", "Loads a scene", typeof(SceneCommand))]
    public class SceneLoadCommand : ICommand
    {
        [Suggest(nameof(SceneCommand.SuggestLoadableScenes), typeof(SceneCommand))]
        [Positional(0, "The name of the scene to load.")]
        private string _name;

        [Switch('a', "Whether to load the scene additively.")]
        private bool _additive;
        
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            if (ctx.IsEditMode())
            {
#if UNITY_EDITOR
                var path = await GetEditorPath(ctx);

                if (path == null)
                {
                    ctx.WriteError($"Could not find a scene with the name {_name}");
                    return CommandStatus.Error;
                }
                
                if (!_additive)
                {
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                }
                
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                
                var scene = EditorSceneManager.OpenScene(path, _additive ? OpenSceneMode.Additive : OpenSceneMode.Single);

                if (scene.IsValid()) return CommandStatus.Ok;
                
                ctx.WriteError($"No scene available with name {_name}");
                return CommandStatus.Error;
#else
                return CommandStatus.Error;
#endif
            }

            var sceneList = Enumerable
                .Range(0, SceneManager.sceneCountInBuildSettings)
                .Select(i => SceneManager.GetSceneByBuildIndex(i).name).ToList();
            
            if (!sceneList.Distinct().Contains(_name))
            {
                ctx.WriteError($"Could not find a scene with the name {_name}");
                return CommandStatus.Error;
            }

            SceneManager.LoadScene(_name, _additive ? LoadSceneMode.Additive : LoadSceneMode.Single);
            ctx.Write($"Loaded scene {_name}");
            
            await Task.CompletedTask;
            return CommandStatus.Ok;
        }
        
#if UNITY_EDITOR
        private async Task<string> GetEditorPath(CommandContext ctx)
        {
            var paths = AssetDatabase.FindAssets("t:Scene", new []{ "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetFileNameWithoutExtension(path) == _name)
                .ToList();

            if (paths.Count == 0)
            {
                return null;
            }

            string path;
                
            if (paths.Count > 1)
            {
                var chop = GetCommonPrefixLength(paths);
                    
                path = await ctx.PromptWithOptions(paths.Select(p => new Option<string>(p, p[chop..])));
            }
            else
            {
                path = paths.First();
            }
            
            return path;
        }
#endif
        
        private static int GetCommonPrefixLength(IEnumerable<string> paths)
        {
            var split = paths.Select(p => p.Split('/')).ToList();

            int firstDiff = 0;
            for (int i = 0; i < split.Min(p => p.Length); i++)
            {
                if (split.Select(p => p[i]).Distinct().Count() > 1)
                {
                    firstDiff = i;
                    break;
                }
            }

            // +1 for the slash after the last common segment, -1 to go one level higher
            return split.First().Take(Math.Max(0, firstDiff - 1)).Sum(p => p.Length + 1);
        }
    }
    
    [Command("unload", "Unloads a scene", typeof(SceneCommand))]
    public class SceneUnloadCommand : SimpleCommand
    {
        [Suggest(nameof(SceneCommand.SuggestLoadedScenes), typeof(SceneCommand))]
        [Positional(0, "The name of the scene to unload.")]
        private string _name;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            if (ctx.IsEditMode())
            {
#if UNITY_EDITOR
                
                var scene = SceneManager.GetSceneByName(_name);

                if (!scene.IsValid() || !scene.isLoaded)
                {
                    ctx.WriteError($"Could not find a loaded scene with the name {_name}");
                    return CommandStatus.Error;
                }
                
                EditorSceneManager.CloseScene(scene, true);
                ctx.Write($"Closed scene {_name}");
                
                return CommandStatus.Ok;
#else
                return CommandStatus.Error;
#endif
            }
            
            var sceneList = Enumerable
                .Range(0, SceneManager.sceneCount)
                .Select(i => SceneManager.GetSceneAt(i).name).ToList();

            if (sceneList.Count == 1)
            {
                ctx.WriteError("Cannot unload the only open scene!");
                return CommandStatus.Error;
            }
            
            if (!sceneList.Distinct().Contains(_name))
            {
                ctx.WriteError($"Could not find a loaded scene with the name {_name}");
                return CommandStatus.Error;
            }

            SceneManager.UnloadSceneAsync(_name);

            ctx.Write($"Unloaded scene {_name}");
            
            return CommandStatus.Ok;
        }
    }
    
    [Command("list", "Lists all loaded scenes",  typeof(SceneCommand))]
    public class SceneListCommand : SimpleCommand
    {
        [Switch('a', "List all scenes available")]
        private bool _all;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write(Format.Color("=== Loaded scenes ===", Format.LightBlue));
            
            foreach (var name in Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).name))
            {
                if (SceneManager.GetActiveScene().name == name)
                {
                    ctx.Write($"{ Format.Color(name, Format.Green)}*");
                }
                else
                {
                    ctx.Write($"{ Format.Color(name, Format.Yellow)}");
                }
            }

            if (_all)
            {
                ctx.Write(Format.Color("=== Build List ===", Format.LightBlue));
                
                var sceneList = Enumerable
                    .Range(0, SceneManager.sceneCountInBuildSettings)
                    .Select(i => SceneManager.GetSceneByBuildIndex(i).name)
                    .Distinct()
                    .ToList();

                foreach (var name in sceneList)
                {
                    ctx.Write(Format.Color(name, Format.Gray));
                }
            }
            
            return CommandStatus.Ok;
        }
    }
    
    [Command("get-active", "Gets the active scene",  typeof(SceneCommand))]
    public class SceneGetActiveCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write(Format.Color(SceneManager.GetActiveScene().name + "*", Format.Green));
            
            return CommandStatus.Ok;
        }
    }
    
    [Command("set-active", "Sets the active scene",  typeof(SceneCommand))]
    public class SceneSetActiveCommand : SimpleCommand
    {
        [Suggest(nameof(SceneCommand.SuggestLoadedScenes), typeof(SceneCommand))]
        [Positional(0, "The name of the scene to set active.")]
        private string _name;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            var sceneList = Enumerable
                .Range(0, SceneManager.sceneCount)
                .Select(i => SceneManager.GetSceneAt(i).name).ToList();

            if (!sceneList.Distinct().Contains(_name))
            {
                ctx.WriteError($"Could not find a loaded scene with the name {_name}");
                return CommandStatus.Error;
            }

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(_name));
            return CommandStatus.Ok;
        }
    }
}