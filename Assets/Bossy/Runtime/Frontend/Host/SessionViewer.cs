using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// A viewer that helps to render sessions.
    /// </summary>
    internal class SessionViewer : IDisposable
    {
        /// <summary>
        /// The bridge to the session backend.
        /// </summary>
        public readonly Bridge Bridge;
        
        private VisualElement _root;
        private readonly IUserInterfaceView _userInterface;
        
        private readonly Stack<ViewState> _contentStack = new();
        
        /// <summary>
        /// Creates a new viewer.
        /// </summary>
        /// <param name="bridge">The bridge to the session backend.</param>
        /// <param name="userInterface">The user interface to render.</param>
        public SessionViewer(Bridge bridge, IUserInterfaceView userInterface)
        {
            Bridge = bridge;

            Bridge.OnPushContent += PushContent;
            Bridge.OnPopContent += PopContent;

            _userInterface = userInterface;
            Bridge.SetUIView(_userInterface);
        }

        /// <summary>
        /// Sets the root visual element this viewer docks to and initializes the UI.
        /// </summary>
        /// <param name="root"></param>
        public void SetRootAndInitializeView(VisualElement root)
        {
            _root = root;
            PushContent(_userInterface);
        }

        /// <summary>
        /// Focuses this viewer.
        /// </summary>
        public void Focus()
        {
            if (_contentStack.TryPeek(out var current))
            {
                current.Content.OnFocus();
            }
        }

        /// <summary>
        /// Defocuses this viewer.
        /// </summary>
        public void Defocus()
        {
            if (_contentStack.TryPeek(out var current))
            {
                current.Content.OnDefocus();
            }
        }

        private void PushContent(IContentView view)
        {
            /*
             * NOTE: Due to what is apparently a Unity UI-Toolkit bug, we
             * cannot just remove or hide the current UI because focus stops being tracked.
             * Instead, we just paste the new UI overtop.
             */
            
            if (_contentStack.TryPeek(out var current))
            {
                current.Content.OnDefocus();
            }
            
            var root = view.CreateView();
            root.style.position = Position.Absolute;
            root.style.top = 0;
            root.style.left = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            
            const float value = 55f / 255;
            root.style.backgroundColor = new Color(value, value, value, 1);
            
            _contentStack.Push(new ViewState
            {
                Root = root,
                Content = view,
            });

            _root.Add(root);
            view.OnFocus();
        }

        private void PopContent()
        {
            if (!_contentStack.TryPop(out var popped))
            {
                return;
            }

            popped.Content.OnDefocus();
            _root.Remove(popped.Root);
            
            if (!_contentStack.TryPeek(out var current))
            {
                return;
            }
            
            // _root.Add(current.Root);
            current.Content.OnFocus();
        }
        
        public void Dispose()
        {
            while (_contentStack.TryPop(out var popped))
            {
                popped.Content.OnDefocus();
            }
        }

        private struct ViewState
        {
            /// <summary>
            /// The root of the view.
            /// </summary>
            public VisualElement Root;
            
            /// <summary>
            /// The view.
            /// </summary>
            public IContentView Content;
        }
    }
}