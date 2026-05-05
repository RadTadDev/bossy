#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Bossy.Frontend
{
    /// <summary>
    /// Automatically closes all windowed commands that were running so they don't attempt
    /// to wake back up after domain reload as full graphics hosts.
    /// </summary>
    [InitializeOnLoad]
    public static class EditorWindowCloser
    {
        static EditorWindowCloser()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
        }

        private static void OnBeforeReload()
        {
            var windows = Resources.FindObjectsOfTypeAll<EditorHost>();

            foreach (var window in windows)
            {
                if (window.Space is SessionSpace.EditCommand)
                {
                    window.Close();
                }
            }
        }
    }
}

#endif