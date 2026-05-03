using System;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Execution;

namespace Bossy.Frontend
{
    /// <summary>
    /// Base class for front end objects.
    /// </summary>
    public sealed class Bridge : IOHandler
    {
        private readonly Action<Bridge> _requestSessionClose;
        private readonly Action<Bridge> _requestCommandCancel;

        public event Action<IContentView> OnPushContent;
        public event Action OnPopContent;

        private IUserInterfaceView _ui;
        
        public Bridge(Action<Bridge> requestSessionClose, Action<Bridge> requestCommandCancel)
        {
            _requestSessionClose = requestSessionClose;
            _requestCommandCancel = requestCommandCancel;
        }

        public void SetUIView(IUserInterfaceView view)
        {
            _ui = view;
        }

        public void Write(object value)
        {
            if (value == CloseWriterSentinel.Object)
            {
                return;
            }
            
            _ui.Write(value);
        } 

        public Task<object> ReadAsync(Type requestedType, CancellationToken token) => _ui.ReadAsync(requestedType, token);

        public IFrontEndCapabilities GetCapabilities() => _ui;
        
        public void RequestCloseSession() => _requestSessionClose?.Invoke(this);

        public void RequestCancelCommand()
        {
            _requestCommandCancel?.Invoke(this);
            _ui.OnCommandCanceled();
        } 

        public void PushContent(IContentView view)
        {
            OnPushContent?.Invoke(view);
        }

        public void PopContent()
        {
            OnPopContent?.Invoke();
        }
    }
}