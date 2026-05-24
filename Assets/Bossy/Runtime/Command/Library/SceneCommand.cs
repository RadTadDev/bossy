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
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
#endif

namespace Bossy.Runtime.Command.Library
{
    
#if UNITY_EDITOR
    [InitializeOnLoad]
    internal static class SceneCaching
    {
        static SceneCaching() => EditorApplication.delayCall += RefreshScenes;
        
        public static void RefreshScenes()
        {
            EditorApplication.delayCall -= RefreshScenes;
            
            var list = AssetDatabase.LoadAssetAtPath<SceneList>("Assets/Bossy/Runtime/Resources/SceneCommandList.asset");
            
            if (list == null) return;
        
            list.Scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => Path.GetFileNameWithoutExtension(s.path))
                .ToList();
        
            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssets();
        }
    }
    
    internal class SceneCachingPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => SceneCaching.RefreshScenes();
    }
#endif
    
    [Command("scene", "Commands for inspecting and manipulating Unity scenes.")]
    public class SceneCommand : ICommand
    {
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            await ctx.ExecuteAsync("help scene");
            return CommandStatus.Ok;
        }
    }
    
    [Command("load", "Loads a scene", typeof(SceneCommand))]
    public class SceneLoadCommand : ICommand
    {
        [Suggest(nameof(Suggest))]
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

            var sceneList = Resources.Load<SceneList>("SceneCommandList");
                
            if (sceneList is null || !sceneList.Scenes.Distinct().Contains(_name))
            {
                ctx.WriteError($"Could not find a scene with the name {_name}");
                return CommandStatus.Error;
            }

            SceneManager.LoadScene(_name, _additive ? LoadSceneMode.Additive : LoadSceneMode.Single);

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
        
        private static List<string> Suggest()
        {
            if (Application.isPlaying)
            {
                var sceneList = Resources.Load<SceneList>("SceneCommandList");
                
                if (sceneList == null)
                {
                    return new List<string>();
                }

                return sceneList.Scenes.Distinct().ToList();
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
    }
    
    [Command("unload", "Unloads a scene", typeof(SceneCommand))]
    public class SceneUnloadCommand : SimpleCommand
    {
        [Suggest(nameof(Suggest))]
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

            try
            {
                SceneManager.UnloadSceneAsync(_name);
                ctx.Write($"Closed scene {_name}");
            }
            catch (Exception)
            {
                ctx.WriteError($"No open scene named {_name}");
            }
            
            
            return CommandStatus.Ok;
        }

        private static List<string> Suggest()
        {
            return Enumerable.Range(0, SceneManager.sceneCount)
                .Select(i => SceneManager.GetSceneAt(i).name).ToList();
        }
    }
    
    [Command("list", "Lists all loaded scenes",  typeof(SceneCommand))]
    public class SceneListCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write("Loaded scenes:");
            
            foreach (var name in Enumerable.Range(0, SceneManager.sceneCount).Select(i => SceneManager.GetSceneAt(i).name))
            {
                ctx.Write(name);
            }
            
            return CommandStatus.Ok;
        }
    }
}