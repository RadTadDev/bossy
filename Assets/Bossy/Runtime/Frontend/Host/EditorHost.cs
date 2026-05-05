#if UNITY_EDITOR

using System;
using Bossy.Settings;
using UnityEditor;

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
            else
            {
                FocusWindowIfItsOpen<SceneView>();
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