using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    internal class SessionViewer : IDisposable
    {
        public readonly Bridge Bridge;
        
        private readonly Stack<IContentView> _contentStack = new();

        public SessionViewer(Bridge bridge, IContentView initialView)
        {
            Bridge = bridge;
            
            PushView(initialView);
        }

        public void InitializeView(VisualElement root)
        {
            if (_contentStack.Count == 0)
            {
                throw new InvalidOperationException("Cannot create view without content stack");
            }

            root.Add(_contentStack.Peek().CreateView());
        }
        
        public void Dispose()
        {
            // TODO release managed resources here
        }

        public void Focus()
        {
            _contentStack.Peek().Focus();
        }

        public void Defocus()
        {
            _contentStack.Peek().Defocus();
        }
        
        private void PushView(IContentView view)
        {
            _contentStack.Push(view);
            Bridge.SetCurrentView(view);
        }
    }
}