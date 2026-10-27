#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// The dockable editor window that shows the editor console.
    /// </summary>
    internal sealed class ConsoleWindow : BossyEditorWindow
    {
        private const string NotStartedMessage =
            "Bossy has not started. Build a BossyConsole from a startup script, as shown in the README's Quick Start.";

        protected override void CreateGUI()
        {
            base.CreateGUI();
            titleContent = new GUIContent("Bossy");

            if (EditorConsole.Active != null)
            {
                // The window may have been created after the console started, such as from the menu
                EditorConsole.Active.AttachWindow(this);
            }
            else
            {
                // Replaced when the console starts, for example shortly after a domain reload
                rootVisualElement.Add(new Label(NotStartedMessage) { style = { whiteSpace = WhiteSpace.Normal } });
            }
        }

        private void OnDestroy()
        {
            EditorConsole.Active?.DetachWindow(this);
        }
    }
}

#endif
