using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bossy.Execution;
using Bossy.Settings;
using Bossy.Utils;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// The UI for one session: its frontend, with any running command's view stacked on top.
    /// Editor windows and runtime panels display a session view by adding its root.
    /// </summary>
    internal sealed class SessionView : ISessionHost, IDisposable
    {
        /// <summary>
        /// Opens a command window for a graph. Supplied by the editor or runtime console that owns this view.
        /// </summary>
        public delegate void CommandWindowOpener(Session template, CommandGraph graph, IReadable reader, IWriteable writer);

        /// <summary>
        /// The root of this view. Add it to a window or panel to display the session.
        /// </summary>
        public VisualElement Root { get; }

        /// <summary>
        /// The session shown by this view.
        /// </summary>
        public Session Session { get; }

        /// <summary>
        /// The title for a window or panel showing this view.
        /// </summary>
        public string Title { get; }

        private const string CommandViewClass = "bossy-command-view";
        private const string CommandViewEnteringClass = "bossy-command-view--entering";

        private readonly IVisualEndpoint _endpoint;
        private readonly CommandWindowOpener _openCommandWindow;
        private readonly Stack<VisualElement> _commandViews = new();
        private bool _disposed;

        /// <summary>
        /// Creates a new session view.
        /// </summary>
        /// <param name="title">The title for a window or panel showing this view.</param>
        /// <param name="endpoint">The frontend to show.</param>
        /// <param name="createSession">Creates the session given this view as its host.</param>
        /// <param name="openCommandWindow">Opens command windows for this session.</param>
        /// <param name="inputSettings">The input settings.</param>
        public SessionView(string title, IVisualEndpoint endpoint, Func<ISessionHost, Session> createSession, CommandWindowOpener openCommandWindow, BossyInputSettings inputSettings)
        {
            Title = title;
            _endpoint = endpoint;
            _openCommandWindow = openCommandWindow;

            Root = new VisualElement { name = "session-view" };
            Root.style.flexGrow = 1;
            Root.Add(endpoint.Root);

            Root.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (!inputSettings.CancelCommand.IsAsserted(evt)) return;

                Session?.CancelCommand();
                _endpoint?.OnCancelPressed();
                evt?.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);

            Session = createSession(this);
        }

        /// <summary>
        /// Starts the session's read and execute loop.
        /// </summary>
        public void Start()
        {
            _ = ObserveExceptions(Session.RunAsync());
        }

        /// <summary>
        /// Runs a single graph, as a command window does.
        /// </summary>
        /// <param name="graph">The graph to run.</param>
        /// <param name="reader">An override reader, or null to read from this view.</param>
        /// <param name="writer">An override writer, or null to write to this view.</param>
        public void StartGraph(CommandGraph graph, IReadable reader, IWriteable writer)
        {
            // The graph is already in its own window, so it must not open another one when it runs
            graph.Windowed = false;
            _ = ObserveExceptions(Session.RunGraphAsync(graph, reader, writer));
        }

        /// <summary>
        /// Gives keyboard focus to the view on top.
        /// </summary>
        public void Focus()
        {
            if (_commandViews.TryPeek(out var commandView))
            {
                commandView.Focus();
            }
            else
            {
                _endpoint.Focus();
            }
        }

        public void PushView(ICommandView view)
        {
            var root = view.CreateView();

            // Command views are placed on top of the frontend rather than replacing it, because removing
            // or hiding the frontend breaks UI Toolkit's focus tracking for it.
            root.AddToClassList(CommandViewClass);
            root.delegatesFocus = true;
            Animations.In(root, CommandViewEnteringClass);

            _commandViews.Push(root);
            Root.Add(root);
            Focus();
        }

        public void PopView()
        {
            if (!_commandViews.TryPop(out var root)) return;

            root.RemoveFromHierarchy();
            Focus();
        }

        public void OpenCommandWindow(Session template, CommandGraph graph, IReadable reader, IWriteable writer)
        {
            _openCommandWindow(template, graph, reader, writer);
        }

        /// <summary>
        /// Ends the session and clears the view.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Session.Dispose();

            while (_commandViews.TryPop(out var root))
            {
                root.RemoveFromHierarchy();
            }
        }

        private static async Task ObserveExceptions(Task task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected when the session is closed
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}
