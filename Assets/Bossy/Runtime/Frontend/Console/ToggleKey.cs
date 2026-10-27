using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// The key that shows and hides the console, and the rule for when it types instead.
    /// </summary>
    /// <remarks>
    /// The key toggles the console everywhere except while editing text that is not empty, where it types
    /// its character. That keeps paths such as Assets/Scenes typeable. Hardcoded until hotkeys become settings.
    /// </remarks>
    internal static class ToggleKey
    {
        /// <summary>
        /// The toggle key.
        /// </summary>
        public const KeyCode Key = KeyCode.Slash;

        /// <summary>
        /// The character the toggle key types.
        /// </summary>
        public const char Character = '/';

        /// <summary>
        /// Tells if a key event comes from the toggle key. Unity can deliver one key press as two events,
        /// one with the key code and one with the typed character, so both count.
        /// </summary>
        /// <param name="evt">The key event.</param>
        /// <returns>True if the event comes from the toggle key.</returns>
        public static bool Matches(KeyDownEvent evt) => IsKeyPress(evt) || evt.character == Character;

        /// <summary>
        /// Tells if a key event is the key press itself rather than the character it types.
        /// </summary>
        /// <param name="evt">The key event.</param>
        /// <returns>True if the event is the toggle key press.</returns>
        public static bool IsKeyPress(KeyDownEvent evt) => evt.keyCode == Key && !HasModifiers(evt);

        /// <summary>
        /// Tells if the toggle key should type its character into the focused element instead of toggling.
        /// </summary>
        /// <param name="focused">The element with keyboard focus, or null.</param>
        /// <returns>True if the key should type.</returns>
        public static bool ShouldType(Focusable focused)
        {
            var field = GetTextField(focused);
            return field != null && !string.IsNullOrEmpty(field.value);
        }

        /// <summary>
        /// Tells if the focused element is part of a text field.
        /// </summary>
        /// <param name="focused">The element with keyboard focus, or null.</param>
        /// <returns>True if text is being edited.</returns>
        public static bool IsEditingText(Focusable focused) => GetTextField(focused) != null;

        /// <summary>
        /// Stops a toggle key event from reaching the focused element, so it is not typed.
        /// </summary>
        /// <param name="evt">The key event.</param>
        /// <param name="root">The root the event is being handled on.</param>
        public static void Suppress(KeyDownEvent evt, VisualElement root)
        {
            root.focusController?.IgnoreEvent(evt);
            evt.StopImmediatePropagation();
        }

        // Shift is included so shift + slash still types a question mark
        private static bool HasModifiers(KeyDownEvent evt) => evt.shiftKey || evt.ctrlKey || evt.altKey || evt.commandKey;

        private static TextField GetTextField(Focusable focused)
        {
            // Focus usually lands on an element inside the text field rather than the field itself
            for (var element = focused as VisualElement; element != null; element = element.parent)
            {
                if (element is TextField field)
                {
                    return field;
                }
            }

            return null;
        }
    }
}
