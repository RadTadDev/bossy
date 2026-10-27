using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// A draggable, resizable window in the runtime overlay that runs one command graph in its own session.
    /// It stays open until it is closed, which ends its session.
    /// </summary>
    internal sealed class RuntimeCommandWindow : IDisposable
    {
        private const float MinWidth = 200f;
        private const float MinHeight = 150f;
        
        // Matches the window's transition in BossyTheme.uss
        private const string EnteringClass = "bossy-window--entering";
        private const long FadeDurationMs = 140;

        /// <summary>
        /// The root of the window.
        /// </summary>
        public VisualElement Root { get; }

        private readonly SessionView _view;

        /// <summary>
        /// Creates a command window.
        /// </summary>
        /// <param name="view">The session view to display.</param>
        /// <param name="closeRequested">Invoked when the user closes the window.</param>
        public RuntimeCommandWindow(SessionView view, Action closeRequested)
        {
            _view = view;

            Root = ContentViewUtility.GetRootFromUxml("BossyRuntimeCommandWindow");
            Root.RemoveFromClassList(EnteringClass);
            Animations.In(Root, EnteringClass);
            Root.Q<Label>("title").text = view.Title;
            Root.Q<Button>("button-close").clicked += closeRequested;
            Root.Q("content-area").Add(view.Root);

            // Bring the window to the front when it is clicked
            Root.RegisterCallback<PointerDownEvent>(_ => Root.BringToFront(), TrickleDown.TrickleDown);

            WireDrag(Root.Q("title-handle"));
            WireResize(Root.Q("drag-handle"));
        }

        /// <summary>
        /// Moves the window to a position in its layer.
        /// </summary>
        /// <param name="position">The top left corner.</param>
        public void MoveTo(Vector2 position)
        {
            Root.style.left = position.x;
            Root.style.top = position.y;
        }

        /// <summary>
        /// Ends the window's session and fades the window out.
        /// </summary>
        public void Dispose()
        {
            _view.Dispose();
            
            // Without a panel the fade cannot run, so remove it straight away
            if (Root.panel == null)
            {
                Root.RemoveFromHierarchy();
                return;
            }
            
            Root.pickingMode = PickingMode.Ignore;
            Animations.Out(Root, EnteringClass, FadeDurationMs, Root.RemoveFromHierarchy);
        }

        private void WireDrag(VisualElement titleBar)
        {
            Vector2 startPointer = default;
            Vector2 startPosition = default;
            var dragging = false;

            titleBar.RegisterCallback<PointerDownEvent>(evt =>
            {
                // The close button sits in the title bar and needs the pointer for its click
                if (evt.target is Button) return;
                
                dragging = true;
                startPointer = evt.position;
                startPosition = new Vector2(Root.resolvedStyle.left, Root.resolvedStyle.top);
                titleBar.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });

            titleBar.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!dragging) return;

                // Keep the window inside its layer
                var bounds = Root.parent.layout;
                var delta = (Vector2)evt.position - startPointer;
                Root.style.left = Mathf.Clamp(startPosition.x + delta.x, 0, bounds.width - Root.resolvedStyle.width);
                Root.style.top = Mathf.Clamp(startPosition.y + delta.y, 0, bounds.height - Root.resolvedStyle.height);
                evt.StopPropagation();
            });

            titleBar.RegisterCallback<PointerUpEvent>(evt =>
            {
                dragging = false;
                titleBar.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            });
        }

        private void WireResize(VisualElement handle)
        {
            Vector2 startPointer = default;
            Vector2 startSize = default;
            var resizing = false;

            handle.RegisterCallback<PointerDownEvent>(evt =>
            {
                resizing = true;
                startPointer = evt.position;
                startSize = new Vector2(Root.resolvedStyle.width, Root.resolvedStyle.height);
                handle.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });

            handle.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!resizing) return;

                // Keep the window inside its layer
                var bounds = Root.parent.layout;
                var delta = (Vector2)evt.position - startPointer;
                var maxWidth = bounds.width - Root.resolvedStyle.left;
                var maxHeight = bounds.height - Root.resolvedStyle.top;

                Root.style.width = Mathf.Clamp(startSize.x + delta.x, MinWidth, maxWidth);
                Root.style.height = Mathf.Clamp(startSize.y + delta.y, MinHeight, maxHeight);
                evt.StopPropagation();
            });

            handle.RegisterCallback<PointerUpEvent>(evt =>
            {
                resizing = false;
                handle.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            });
        }
    }
}
