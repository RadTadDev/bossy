using UnityEngine;

#if ENABLE_INPUT_SYSTEM && BOSSY_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Bossy.Frontend
{
    /// <summary>
    /// Reads the toggle key at runtime with whichever input backend the project uses.
    /// </summary>
    internal static class RuntimeToggleInput
    {
        /// <summary>
        /// Tells if the toggle key was pressed this frame without modifiers.
        /// </summary>
        /// <returns>True if the toggle key was pressed.</returns>
        public static bool WasPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM && BOSSY_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;

            // Must match ToggleKey.Key
            return keyboard.slashKey.wasPressedThisFrame
                && !keyboard.shiftKey.isPressed
                && !keyboard.ctrlKey.isPressed
                && !keyboard.altKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(ToggleKey.Key)
                && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)
                && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)
                && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt);
#else
            // The project uses the Input System but the package is missing, so there is nothing to read
            return false;
#endif
        }
    }
}
