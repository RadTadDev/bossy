#if UNITY_EDITOR

using UnityEditor;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// Base for Bossy's editor windows. Displays a session view and handles the toggle key inside the window.
    /// </summary>
    internal abstract class BossyEditorWindow : EditorWindow
    {
        /// <summary>
        /// The session view being displayed, or null.
        /// </summary>
        protected SessionView View { get; private set; }

        /// <summary>
        /// Displays a session view in this window.
        /// </summary>
        /// <param name="view">The view to display.</param>
        public void Display(SessionView view)
        {
            View = view;

            rootVisualElement.Clear();
            rootVisualElement.Add(view.Root);

            if (hasFocus)
            {
                view.Focus();
            }
        }

        protected virtual void CreateGUI()
        {
            BossyFont.ApplyTo(rootVisualElement);
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        protected virtual void OnFocus()
        {
            View?.Focus();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!ToggleKey.Matches(evt)) return;

            var focused = rootVisualElement.focusController?.focusedElement;
            if (ToggleKey.ShouldType(focused)) return;

            ToggleKey.Suppress(evt, rootVisualElement);

            // Unity does not run shortcuts while text is being edited, so the key is handled here instead
            if (ToggleKey.IsKeyPress(evt) && ToggleKey.IsEditingText(focused))
            {
                EditorConsole.Active?.Toggle();
            }
        }
    }
}

#endif
