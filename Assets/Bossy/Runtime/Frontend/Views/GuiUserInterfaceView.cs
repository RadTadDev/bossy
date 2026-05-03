using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    public class GuiUserInterfaceView : IUserInterfaceView
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

        public void OnFocus()
        {
            throw new NotImplementedException();
        }

        public void OnDefocus()
        {
            throw new NotImplementedException();
        }

        public void OnCommandCanceled()
        {
            throw new NotImplementedException();
        }
    }
}