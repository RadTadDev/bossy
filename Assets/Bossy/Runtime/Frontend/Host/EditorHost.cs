#if UNITY_EDITOR

using System;
using System.Linq;
using Bossy.Settings;
using UnityEditor;
using UnityEngine;

namespace Bossy.Frontend
{
    /// <summary>
    /// Hosts graphics in an editor window.
    /// </summary>
    internal class EditorHost : EditorWindow, IHost
    {
        public IHostController Controller => _controller;
        
        public SessionSpace Space { get; private set; }

        private HostManager _manager;
        private EditorHostController _controller;

        private static EditorWindow _lastFocused;

        public void Initialize(HostManager manager, BossyInputSettings settings, Action<FrontendType, SessionSpace> createNewSession, SessionSpace space)
        {
            EditorApplication.update += TrackFocus;
            
            _manager = manager;
            Space = space;
            _controller = new EditorHostController(settings, createNewSession, rootVisualElement);
        }

        public void Reveal()
        {
            Focus();
            _controller.Show();
        }


        public void Hide()
        {
            _controller.Hide();
            
            if (_lastFocused != null)
            {
                _lastFocused.Focus();
            }
            else if (HasOpenInstances<SceneView>())
            {
                FocusWindowIfItsOpen<SceneView>();
            }
            else
            {
                var window = Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w => !typeof(EditorHost).IsAssignableFrom(w.GetType()));

                if (window != null)
                {
                    window.Focus();
                }
            }
        }

        private void OnFocus()
        {
            // This can be null since there is a moment between creation and initialize. 
            // The manager always focuses on initialize so it is okay to miss this message.
            _manager?.NotifyFocusTaken(this);
            _controller?.Show();
        }

        private void OnLostFocus()
        {
            _controller?.Hide();
            _manager?.NotifyFocusLost(this, true);
        }
        
        private void TrackFocus()
        {
            if (focusedWindow != null && focusedWindow is not EditorHost)
            {
                _lastFocused = focusedWindow;
            }
        }
        
        private void OnDestroy()
        {
            EditorApplication.update -= TrackFocus;
            _manager?.RequestClose(this, true);
        }
    }
}

#endif