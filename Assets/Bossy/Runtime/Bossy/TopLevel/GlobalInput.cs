using System;
using UnityEngine;
using System.Linq;
using Bossy.Settings;
using Bossy.Utils;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using Bossy.Utils.Editor;
using UnityEditor.ShortcutManagement;
#endif

namespace Bossy
{
    internal class GlobalInput
    {
        /// <summary>
        /// Invoked when a global toggle session input is used.
        /// </summary>
        public Action<SessionSpace> OnToggleMainHost { get; set; }

        private const string ToggleId = "Bossy Console/Toggle Main Window";
        
        private const string ProfileName = "bossy-shortcut-profile";
        
        private static Action<SessionSpace> _toggleBus;
        
        private readonly BossyInputSettings _settings;
        
        /// <summary>
        /// Create a new global input listener which raises an event when the user presses the super key.
        /// </summary>
        /// <param name="settings">The Bossy input settings.</param>
        public GlobalInput(BossyInputSettings settings)
        {
            _settings = settings;

#if UNITY_EDITOR
            SetEditorShortcuts();
#endif
            
            BossyRuntime.SetInputPoll(PollRuntimeInput);
            
            _toggleBus -= RaiseEvent;
            _toggleBus += RaiseEvent;
        }
        
#if UNITY_EDITOR
        [Shortcut(ToggleId, KeyCode.Slash)]
        private static void ToggleMainWindow()
        {
            var space = Application.isPlaying ? SessionSpace.Runtime : SessionSpace.Edit;
            _toggleBus?.Invoke(space);
        }

        private void SetEditorShortcuts()
        {
            var manager = ShortcutManager.instance;
            var active = manager.activeProfileId;

            if (manager.IsProfileReadOnly(active))
            {
                if (!manager.GetAvailableProfileIds().Contains(ProfileName))
                {
                    Log.Info($"Created shortcut profile for Bossy: {ProfileName}");
                    manager.CreateProfile(ProfileName);
                }
                manager.activeProfileId = ProfileName;
            }

            var toggleBinding = new ShortcutBinding(_settings.ToggleMainHost.BossyToUnity());
            
            ShortcutManager.instance.RebindShortcut(ToggleId, toggleBinding);
        }
#endif
        
        private void PollRuntimeInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (!keyboard[_settings.ToggleMainHost.KeyCode.ToKey()].wasPressedThisFrame) return;
            
            var mods = _settings.ToggleMainHost.Modifiers;
            if (mods.HasFlag(KeyModifiers.Alt) && !keyboard.altKey.isPressed) return;
            if (mods.HasFlag(KeyModifiers.Shift) && !keyboard.shiftKey.isPressed) return;
            if (mods.HasFlag(KeyModifiers.Control) && !keyboard.ctrlKey.isPressed) return;

            _toggleBus?.Invoke(SessionSpace.Runtime);
        }

        private void RaiseEvent(SessionSpace space) => OnToggleMainHost?.Invoke(space);
    }
}