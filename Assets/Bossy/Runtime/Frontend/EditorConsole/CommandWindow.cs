#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Bossy.Frontend
{
    /// <summary>
    /// An editor window that runs one command graph in its own session. Closing it ends the session.
    /// </summary>
    internal sealed class CommandWindow : BossyEditorWindow
    {
        /// <summary>
        /// Opens a new command window for a session view.
        /// </summary>
        /// <param name="view">The view to display.</param>
        public static void Open(SessionView view)
        {
            var window = CreateWindow<CommandWindow>(view.Title);
            window.Display(view);
        }

        /// <summary>
        /// Closes every command window. Their sessions cannot survive a domain reload.
        /// </summary>
        public static void CloseAll()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<CommandWindow>())
            {
                window.Close();
            }
        }

        protected override void CreateGUI()
        {
            base.CreateGUI();

            // A window Unity restores without a session, such as after a domain reload, has nothing to show
            EditorApplication.delayCall += CloseIfEmpty;
        }

        private void CloseIfEmpty()
        {
            if (this != null && View == null)
            {
                Close();
            }
        }

        private void OnDestroy()
        {
            View?.Dispose();
        }
    }
}

#endif
