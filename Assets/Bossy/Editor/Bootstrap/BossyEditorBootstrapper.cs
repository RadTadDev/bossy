using UnityEditor;

namespace Bossy.Editor
{
    /// <summary>
    /// Allows editor access to the single Bossy instance.
    /// </summary>
    [InitializeOnLoad]
    internal static class BossyEditorBootstrapper
    {
        /// <summary>
        /// The single Bossy instance.
        /// </summary>
        public static BossyConsole EditorInstance;

        /// <summary>
        /// True if the bossy instance has been set.
        /// </summary>
        public static bool IsInitialized => EditorInstance != null;
        
        static BossyEditorBootstrapper()
        {
            if (BossyConsole.IsInitialized)
            {
                EditorInstance = BossyConsole.Instance;
            }
            else
            {
                BossyConsole.OnInitialize += SetInstance;
            }
        }

        private static void SetInstance(BossyConsole created)
        {
            if (EditorInstance == null)
            {
                EditorInstance = created;
            }
        }
    }
}