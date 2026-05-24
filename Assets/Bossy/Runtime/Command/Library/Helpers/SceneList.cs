using System.Collections.Generic;
using UnityEngine;

namespace Bossy.Runtime.Command.Library
{
    /// <summary>
    /// A helper for the scene command. This caches all scene names in the build scenes list
    /// so that we can have autocomplete at runtime.
    /// </summary>
    public class SceneList : ScriptableObject
    {
        /// <summary>
        /// The scenes in the build list.
        /// </summary>
        public List<string> Scenes;
    }
}