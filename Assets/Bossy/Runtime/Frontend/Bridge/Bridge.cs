using System;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Shell;

namespace Bossy.Frontend
{
    /// <summary>
    /// Base class for front end objects.
    /// </summary>
    public sealed class Bridge : IOHandler
    {
        private readonly Action<Bridge> _requestSessionClose;
        private readonly Action<Bridge> _requestCommandCancel;

        private IOHandler _ioHandler;
        private IFrontEndCapabilities _capabilities;
        
        public Bridge(Action<Bridge> requestSessionClose, Action<Bridge> requestCommandCancel)
        {
            _requestSessionClose = requestSessionClose;
            _requestCommandCancel = requestCommandCancel;
        }

        public void SetCurrentView(IContentView view)
        {
            _ioHandler = view;
            _capabilities = view;
        }

        public void Write(object value) => _ioHandler.Write(value);

        public Task<object> ReadAsync(Type requestedType, CancellationToken token) => _ioHandler.ReadAsync(requestedType, token);

        public IFrontEndCapabilities GetCapabilities() => _capabilities;
        
        public void RequestCloseSession() => _requestSessionClose?.Invoke(this);

        public void RequestCancelCommand() => _requestCommandCancel?.Invoke(this);
    }
}