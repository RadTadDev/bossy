using System.Threading;
using Bossy.FrontEnd.UI;
using UnityEditor;
using UnityEngine.UIElements;

namespace Bossy.Editor.UI
{
    public class EditorCommandLineGUI : EditorWindow, ICommandLineGui
    {
        private readonly CancellationTokenSource _sessionTokenSource = new();
        
        /// <summary>
        /// Creates a new editor Bossy CLI window.
        /// </summary>
        /// <returns>The command line interface window.</returns>
        public static ICommandLineGui Create()
        {
            var window = GetWindow<EditorCommandLineGUI>();
            window.name = "Bossy CLI";
            return window;
        }
        
        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            var root = rootVisualElement;

            // VisualElements objects can contain other VisualElement following a tree hierarchy
            var label = new Label("Hello World!");
            root.Add(label);

            // Create button
            var button = new Button
            {
                name = "button",
                text = "Button"
            };
            root.Add(button);

            // Create toggle
            var toggle = new Toggle
            {
                name = "toggle",
                label = "Toggle"
            };
            root.Add(toggle);
        }

        public void OnDestroy()
        {
            _sessionTokenSource.Cancel();
        }
        
        public CancellationToken GetSessionToken() => _sessionTokenSource.Token;

        public CancellationToken GetCommandToken()
        {
            throw new System.NotImplementedException();
        }
    }
}