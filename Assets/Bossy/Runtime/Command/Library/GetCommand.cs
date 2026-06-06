using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Command;
using Bossy.Frontend.Autocomplete;
using Bossy.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bossy.Runtime.Command.Library
{
    [Command("get", "Get generic data.")]
    public class GetCommand : SimpleCommand
    {
        [Switch('n', "Display name")] 
        private string _name = string.Empty;
        
        [Suggest(nameof(SuggestGameObjectName))]
        [Positional(0, "Object name in scene")]
        private string _objectName;
        
        [Suggest(nameof(SuggestComponents))]
        [Positional(1, "Component type on the object.")]
        private Type _componentType;
        
        [Suggest(nameof(SuggestDataPath))]
        [Variadic("The path to the data to get.")]
        private string[] _dataPath;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            var gameObject = GameObject.Find(_objectName);
            
            if (gameObject == null)
            {
                ctx.WriteError($"No game object with the name '{_objectName}'");
                return CommandStatus.Error;
            }

            if (!gameObject.TryGetComponent(_componentType, out var component))
            {
                ctx.WriteError($"No component with type '{_componentType.GetFriendlyName()}' on object '{_objectName}'");
                return CommandStatus.Error;
            }

            object obj = component;
            var errorPath = _componentType.GetFriendlyName();
            
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            foreach (var part in _dataPath)
            {
                errorPath += $".{part}";
    
                var type = obj.GetType();
                var field    = type.GetField(part, bindingFlags);
                var property = field == null ? type.GetProperty(part, bindingFlags) : null;

                obj = field?.GetValue(obj) ?? property?.GetValue(obj);

                if (obj == null)
                {
                    ctx.WriteError($"No data member '{errorPath}' or the item was null");
                    return CommandStatus.Error;
                }
            }

            ctx.Write($"({(_name == string.Empty ? errorPath : _name)}) = {obj}");
            
            return CommandStatus.Ok;
        }

        public static string[] SuggestGameObjectName()
        {
            var names = new HashSet<string>();

            for (int i = 0; i < SceneManager.loadedSceneCount; i++)
            {
                foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                    CollectNames(root.transform, names);
            }

            return names.ToArray();
        }
        
        private static void CollectNames(Transform transform, HashSet<string> names)
        {
            names.Add(transform.name);
    
            for (int i = 0; i < transform.childCount; i++)
                CollectNames(transform.GetChild(i), names);
        }

        public static string[] SuggestComponents(SuggestionContext context)
        {
            if (context.AutocompleteContext.TokensSoFar.Count < 2) return Array.Empty<string>();
            
            var go = GameObject.Find(context.AutocompleteContext.TokensSoFar[1]);

            if (go == null) return Array.Empty<string>();
            
            return go.GetComponents<Component>()
                .Select(c => c.GetType().GetFriendlyName())
                .ToArray();
        }


        public static string[] SuggestDataPath(SuggestionContext context)
        {
            if (context.AutocompleteContext.TokensSoFar.Count < 3) return Array.Empty<string>();

            var go = GameObject.Find(context.AutocompleteContext.TokensSoFar[1]);
            if (go == null) return Array.Empty<string>();
            
            var components = SuggestComponents(context);
            if (!components.Contains(context.AutocompleteContext.TokensSoFar[2])) return Array.Empty<string>();

            var componentType = TypeExtensions.GetTypeFromName(context.AutocompleteContext.TokensSoFar[2]);
            if (componentType == null) return Array.Empty<string>();

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            // No path token yet - suggest top-level fields
            if (context.AutocompleteContext.TokensSoFar.Count < 4)
            {
                return GetMemberNames(componentType, bindingFlags);
            }

            var path = context.AutocompleteContext.TokensSoFar.Skip(3).ToList();
            
            var current = componentType;
            foreach (var part in path)
            {
                var member = current.GetField(part, bindingFlags) ?? (MemberInfo)current.GetProperty(part, bindingFlags);
                if (member == null) return Array.Empty<string>();

                current = member switch
                {
                    FieldInfo f    => f.FieldType,
                    PropertyInfo p => p.PropertyType,
                    _              => null
                };

                if (current == null) return Array.Empty<string>();
            }

            return GetMemberNames(current, bindingFlags);
        }

        private static string[] GetMemberNames(Type type, BindingFlags flags)
        {
            var members = type.GetMembers(flags);
            var names = new List<string>(members.Length);

            foreach (var member in members)
            {
                switch (member)
                {
                    case FieldInfo { IsStatic: false, IsSpecialName: false } f 
                        when !f.Name.StartsWith('<'):
                        names.Add(f.Name);
                        break;
                    case PropertyInfo p 
                        when !(p.Name == "Item" && p.GetIndexParameters().Length > 0)
                             && !p.IsSpecialName
                             && !(IsStatic(p) && p.SetMethod == null):
                        names.Add(p.Name);
                        break;
                }
            }

            return names.ToArray();
        }

        private static bool IsStatic(PropertyInfo p) => p.GetMethod?.IsStatic ?? p.SetMethod?.IsStatic ?? false;
    }
}