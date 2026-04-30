using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    public class GuiContentView : IContentView
    {
        public Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public void Write(object value)
        {
            throw new NotImplementedException();
        }

        public void SetSignaler(Signaler signaler)
        {
            throw new NotImplementedException();
        }

        public VisualElement CreateView()
        {
            throw new NotImplementedException();
        }

        public void Focus()
        {
            throw new NotImplementedException();
        }

        public void Defocus()
        {
            throw new NotImplementedException();
        }
    }
}