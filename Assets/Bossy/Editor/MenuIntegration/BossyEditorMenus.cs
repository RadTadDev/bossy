using Bossy.Editor;
using Bossy.FrontEnd;
using Bossy.Utils;
using UnityEditor;

namespace UnityEngine.TestTools.TestRunner
{
    public static class BossyEditorMenus
    {
        [MenuItem("Bossy/Open Console")]
        private static void OpenConsole()
        {
            if (BossyEditorBootstrapper.IsInitialized)
            {
                BossyEditorBootstrapper.EditorInstance.CreateSession(UserInterfaceType.EDITOR_CommandLine);
            }
            else
            {
                Log.Error("Cannot open a session window because the console has not been created. " +
                          "Create a new Bossy() object and store it anywhere to fix this.");
            }
        }
    }   
}