using System;
using System.Collections.Generic;
using Bossy.Execution;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bossy.Frontend
{
    /// <summary>
    /// The console at runtime, in builds and in play mode: one console session in the overlay, plus any command windows.
    /// Everything is created when the runtime starts and destroyed when play mode ends.
    /// </summary>
    internal sealed class RuntimeConsole : IDisposable
    {
        // Each new command window opens this far below and to the right of the previous one
        private const float WindowCascadeOffset = 24f;
        private static readonly Vector2 FirstWindowPosition = new(40f, 40f);

        /// <summary>
        /// Invoked when the console is shown (true) or hidden (false).
        /// </summary>
        public event Action<bool> VisibilityChanged;

        private readonly SessionFactory _sessions;
        private readonly List<RuntimeCommandWindow> _commandWindows = new();

        private RuntimeOverlay _overlay;
        private SessionView _consoleView;
        private bool _visible;

        /// <summary>
        /// Creates the runtime console. It starts now if the runtime is already running, otherwise when it starts.
        /// </summary>
        /// <param name="sessions">Creates sessions for the console and command windows.</param>
        public RuntimeConsole(SessionFactory sessions)
        {
            _sessions = sessions;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (EditorApplication.isPlaying)
            {
                Start();
            }
#else
            // Bossy may be built before the first scene has loaded, when objects cannot be created yet
            if (SceneManager.GetActiveScene().isLoaded)
            {
                Start();
            }
            else
            {
                SceneManager.sceneLoaded += StartOnFirstScene;
            }
#endif
        }

        /// <summary>
        /// Shows the console if it is hidden, otherwise hides it.
        /// </summary>
        public void Toggle()
        {
            if (_visible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Shows the console, starting its session the first time.
        /// </summary>
        public void Show()
        {
            if (_overlay == null) return;

            if (_consoleView == null)
            {
                _consoleView = _sessions.CreateConsole(OpenCommandWindow);
                _overlay.ConsoleLayer.Add(_consoleView.Root);
                _consoleView.Start();
            }

            _overlay.ConsoleLayer.style.display = DisplayStyle.Flex;
            _visible = true;
            _consoleView.Focus();

            VisibilityChanged?.Invoke(true);
        }

        /// <summary>
        /// Hides the console. Its session and any command windows keep running.
        /// </summary>
        public void Hide()
        {
            if (_overlay == null || !_visible) return;

            _overlay.ConsoleLayer.style.display = DisplayStyle.None;
            _visible = false;

            // Release keyboard focus so the game's typing does not go into a hidden field
            FocusedElement?.Blur();

            VisibilityChanged?.Invoke(false);
        }

        /// <summary>
        /// Stops the runtime console and stops listening for the runtime to start.
        /// </summary>
        public void Dispose()
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#else
            SceneManager.sceneLoaded -= StartOnFirstScene;
#endif
            Stop();
        }

        private Focusable FocusedElement => _overlay.Root.focusController?.focusedElement;

#if UNITY_EDITOR
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Start();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Stop();
            }
        }
#else
        private void StartOnFirstScene(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= StartOnFirstScene;
            Start();
        }
#endif

        private void Start()
        {
            if (_overlay != null) return;

            _overlay = RuntimeOverlay.Create();
            _overlay.Updated += PollToggleKey;
            _overlay.Root.RegisterCallback<KeyDownEvent>(SuppressToggleKey, TrickleDown.TrickleDown);
        }

        private void Stop()
        {
            foreach (var window in _commandWindows)
            {
                window.Dispose();
            }
            _commandWindows.Clear();

            _consoleView?.Dispose();
            _consoleView = null;
            _visible = false;

            if (_overlay != null)
            {
                Object.Destroy(_overlay.gameObject);
                _overlay = null;
            }
        }

        private void PollToggleKey()
        {
            if (!RuntimeToggleInput.WasPressedThisFrame()) return;

            // While editing text that is not empty, the key types instead
            if (ToggleKey.ShouldType(FocusedElement)) return;

            Toggle();
        }

        private void SuppressToggleKey(KeyDownEvent evt)
        {
            // The key is acted on in PollToggleKey. This only stops it from also being typed.
            if (ToggleKey.Matches(evt) && !ToggleKey.ShouldType(FocusedElement))
            {
                ToggleKey.Suppress(evt, _overlay.Root);
            }
        }

        private void OpenCommandWindow(Session template, CommandGraph graph, IReadable reader, IWriteable writer)
        {
            if (_overlay == null) return;

            var view = _sessions.CreateCommandWindow(template, graph, OpenCommandWindow);

            RuntimeCommandWindow window = null;
            window = new RuntimeCommandWindow(view, () => CloseCommandWindow(window));
            window.MoveTo(FirstWindowPosition + Vector2.one * (WindowCascadeOffset * _commandWindows.Count));

            _commandWindows.Add(window);
            _overlay.WindowLayer.Add(window.Root);

            view.Start(graph, reader, writer);
            view.Focus();
        }

        private void CloseCommandWindow(RuntimeCommandWindow window)
        {
            window.Dispose();
            _commandWindows.Remove(window);
        }
    }
}
