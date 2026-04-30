using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Utils
{
    /// <summary>
    /// Represents modifier keys for use in a shortcut binding.
    /// </summary>
    [Flags]
    [Serializable]
    public enum KeyModifiers
    {
        /// <summary>
        /// No modifier keys.
        /// </summary>
        None = 0,
        /// <summary>
        /// Alt key (or Option key on macOS).
        /// </summary>
        Alt = 1,
        /// <summary>
        /// Shift key.
        /// </summary>
        Shift = 2,
        /// <summary>
        /// Marks that the Control key modifier is part of the key combination. Resolves to control key on Windows, macOS, and Linux.
        /// </summary>
        Control = 4,
    }
    
    /// <summary>
    /// A Key plus modifiers combination.
    /// </summary>
    [Serializable]
    public class KeyCombination
    {
        /// <summary>
        /// Creates a new key combination.
        /// </summary>
        /// <param name="keyCode">The key.</param>
        /// <param name="keyModifiers">The modifiers.</param>
        public KeyCombination(KeyCode keyCode, KeyModifiers keyModifiers)
        {
            KeyCode = keyCode;
            Modifiers = keyModifiers;
        }        
        
        /// <summary>
        /// The key being pressed.
        /// </summary>
        public KeyCode KeyCode;
        
        /// <summary>
        /// The modifiers attached.
        /// </summary>
        public KeyModifiers Modifiers;

        public bool IsAsserted(Event evt)
        {
            if (evt.keyCode != KeyCode) return false;
            
            if (Modifiers.HasFlag(KeyModifiers.Alt) && !evt.alt) return false;
            if (Modifiers.HasFlag(KeyModifiers.Shift) && !evt.shift) return false;
            if (Modifiers.HasFlag(KeyModifiers.Control) && !evt.control) return false;

            return true;
        }
        
        public bool IsAsserted(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode) return false;
            
            if (Modifiers.HasFlag(KeyModifiers.Alt) && !evt.altKey) return false;
            if (Modifiers.HasFlag(KeyModifiers.Shift) && !evt.shiftKey) return false;
            if (Modifiers.HasFlag(KeyModifiers.Control) && !evt.ctrlKey) return false;

            return true;
        }
    }
}