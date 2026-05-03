using System;
using System.Collections.Generic;
using Bossy.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    internal class SessionViewer : IDisposable
    {
        public readonly Bridge Bridge;
        
        private VisualElement _root;
        private readonly IUserInterfaceView _userInterface;
        
        private readonly Stack<ViewState> _contentStack = new();
        
        public SessionViewer(Bridge bridge, IUserInterfaceView userInterface)
        {
            Bridge = bridge;

            Bridge.OnPushContent += PushContent;
            Bridge.OnPopContent += PopContent;

            _userInterface = userInterface;
            Bridge.SetUIView(_userInterface);
        }

        public void SetRootAndInitializeView(VisualElement root)
        {
            _root = root;
            PushContent(_userInterface);
        }

        public void Focus()
        {
            if (_contentStack.TryPeek(out var current))
            {
                current.Content.OnFocus();
            }
        }

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
            // TODO release managed resources here
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