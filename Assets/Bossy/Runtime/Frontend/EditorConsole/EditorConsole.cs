#if UNITY_EDITOR

using System;
using Bossy.Execution;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Bossy.Frontend
{
    /// <summary>
    /// The console in the editor: one dockable console window with its own session, plus any command windows.
    /// </summary>
    internal sealed class EditorConsole : IDisposable
    {
        private const string ShortcutId = "Bossy/Toggle Console";

        /// <summary>
        /// The running editor console. Unity creates windows and runs shortcuts on its own,
        /// so they find the console here.
        /// </summary>
        public static EditorConsole Active { get; private set; }

        /// <summary>
        /// Invoked when the console is shown or hidden.
        /// </summary>
        public event Action<ConsoleVisibility> VisibilityChanged;

        private readonly SessionFactory _sessionFactory;

        private SessionView _consoleView;
        private ConsoleWindow _consoleWindow;
        private EditorWindow _lastOtherWindow;
        private bool _hiding;

        /// <summary>
        /// Creates the editor console and attaches it to the console window that is already open, closing any others.
        /// </summary>
        /// <param name="sessionFactory">Creates sessions for the console and command windows.</param>
        public EditorConsole(SessionFactory sessionFactory)
        {
            // A console built again, such as when domain reload is disabled, replaces the previous one
            Active?.Dispose();
            Active = this;

            _sessionFactory = sessionFactory;

            EditorWindow.windowFocusChanged += TrackLastOtherWindow;
            AssemblyReloadEvents.beforeAssemblyReload += CommandWindow.CloseAll;

            /*
             * When the editor console is created, it's possible that existing windows have already run CreateGUI.
             * By searching for them here, we can attach to them no matter when the console is created.
             */
            foreach (var window in Resources.FindObjectsOfTypeAll<ConsoleWindow>())
            {
                AttachWindow(window);
            }
        }
        
        /// <summary>
        /// Ends the console session and stops listening to the editor.
        /// </summary>
        public void Dispose()
        {
            EditorWindow.windowFocusChanged -= TrackLastOtherWindow;
            AssemblyReloadEvents.beforeAssemblyReload -= CommandWindow.CloseAll;

            CloseConsoleSession();

            if (Active == this)
            {
                Active = null;
            }
        }
        
        /// <summary>
        /// Displays the console session in a console window, starting the session if needed.
        /// Only one console window is supported, so any other console window is closed.
        /// </summary>
        /// <param name="window">The window.</param>
        public void AttachWindow(ConsoleWindow window)
        {
            if (_consoleWindow != null && _consoleWindow != window)
            {
                _consoleWindow.Close();
            }

            _consoleWindow = window;

            if (_consoleView == null)
            {
                _consoleView = _sessionFactory.CreateConsole(OpenCommandWindow);
                _consoleView.Start();
            }

            window.Display(_consoleView);
        }

        /// <summary>
        /// Called when a console window is destroyed. Ends the console session unless another window takes it over.
        /// </summary>
        /// <param name="window">The window being destroyed.</param>
        public void DetachWindow(ConsoleWindow window)
        {
            // An older instance of a window that was recreated
            if (window != _consoleWindow) return;

            _consoleWindow = null;

            // Unity sometimes destroys and recreates a window, for example when maximizing it. Waiting
            // until the next update lets the recreated window take over the session instead of starting a new one.
            EditorApplication.update -= CloseSessionIfNotReplaced;
            EditorApplication.update += CloseSessionIfNotReplaced;
        }

        private void CloseSessionIfNotReplaced()
        {
            EditorApplication.update -= CloseSessionIfNotReplaced;

            if (_consoleWindow == null)
            {
                CloseConsoleSession();
            }
        }

        /// <summary>
        /// Shows the console if it does not have focus, otherwise hides it.
        /// </summary>
        public void Toggle()
        {
            if (IsFocused)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Opens the console window if needed and gives it focus.
        /// </summary>
        public void Show()
        {
            // Opening the window attaches it through ConsoleWindow.CreateGUI
            var window = EditorWindow.GetWindow<ConsoleWindow>();
            window.Focus();

            VisibilityChanged?.Invoke(ConsoleVisibility.Shown);
        }

        /// <summary>
        /// Gives focus back to the window that had it before the console. The console window stays open.
        /// </summary>
        private void Hide()
        {
            if (_hiding || !IsFocused) return;

            // Deferred so the key event that hid the console finishes inside the console first
            _hiding = true;
            EditorApplication.delayCall += FocusLastOtherWindow;

            VisibilityChanged?.Invoke(ConsoleVisibility.Hidden);
        }
        

        private bool IsFocused => _consoleWindow != null && EditorWindow.focusedWindow == _consoleWindow;

        
        [Shortcut(ShortcutId, ToggleKey.Key)]
        private static void OnToggleShortcut() => Active?.HandleToggleShortcut();

        
        [MenuItem("Window/Bossy Console")]
        private static void OpenFromMenu()
        {
            if (Active == null)
            {
                EditorWindow.GetWindow<ConsoleWindow>();
                return;
            }

            Active.Show();
        }
        
        
        private void HandleToggleShortcut()
        {
            // While playing with the Game view focused, the runtime console owns the key. This is the
            // rule for choosing between the editor and runtime consoles, and may become a setting.
            if (EditorApplication.isPlaying && IsGameView(EditorWindow.focusedWindow))
            {
                return;
            }

            Toggle();
        }

        // The Game view type is internal to Unity, so it is matched by name
        private static bool IsGameView(EditorWindow window) => window != null && window.GetType().FullName == "UnityEditor.GameView";

        private void OpenCommandWindow(Session template, CommandGraph graph, IReadable reader, IWriteable writer)
        {
            var view = _sessionFactory.CreateCommandWindow(template, graph, OpenCommandWindow);
            CommandWindow.Open(view);
            view.StartGraph(graph, reader, writer);
        }

        private void CloseConsoleSession()
        {
            _consoleView?.Dispose();
            _consoleView = null;
        }

        private void TrackLastOtherWindow()
        {
            var window = EditorWindow.focusedWindow;

            if (window != null && window is not BossyEditorWindow)
            {
                _lastOtherWindow = window;
            }
        }

        private void FocusLastOtherWindow()
        {
            _hiding = false;

            if (_lastOtherWindow != null)
            {
                _lastOtherWindow.Focus();
            }
            else
            {
                EditorWindow.FocusWindowIfItsOpen<SceneView>();
            }
        }
    }
}

#endif
