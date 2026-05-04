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
        /// <summary>
        /// Invoked when the backend has request that content be pushed.
        /// </summary>
        public event Action<IContentView> OnPushContent;
        
        /// <summary>
        /// Invoked when the backend has requested that content be popped.
        /// </summary>
        public event Action OnPopContent;
        
        private readonly Action<Bridge> _requestSessionClose;
        private readonly Action<Bridge> _requestCommandCancel;

        private IUserInterfaceView _ui;
        
        /// <summary>
        /// Creates a new bridge.
        /// </summary>
        /// <param name="requestSessionClose">The hook to request a session close.</param>
        /// <param name="requestCommandCancel">The hook to request canceling a command.</param>
        public Bridge(Action<Bridge> requestSessionClose, Action<Bridge> requestCommandCancel)
        {
            _requestSessionClose = requestSessionClose;
            _requestCommandCancel = requestCommandCancel;
        }

        /// <summary>
        /// Sets the UI viewing this session.
        /// </summary>
        /// <param name="view">The UI.</param>
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

        /// <summary>
        /// Gets the front end capabilities.
        /// </summary>
        /// <returns>The capabilities.</returns>
        public IFrontEndCapabilities GetCapabilities() => _ui;
        
        /// <summary>
        /// Request for the current session to be closed.
        /// </summary>
        public void RequestCloseSession() => _requestSessionClose?.Invoke(this);

        /// <summary>
        /// Request for the current command to be canceled.
        /// </summary>
        public void RequestCancelCommand()
        {
            _requestCommandCancel?.Invoke(this);
            _ui.OnCommandCanceled();
        } 

        /// <summary>
        /// Indicate that content should be pushed to the view stack.
        /// </summary>
        /// <param name="view">The content view to push.</param>
        public void PushContent(IContentView view)
        {
            OnPushContent?.Invoke(view);
        }

        /// <summary>
        /// Indicate that content should be popped from the view stack.
        /// </summary>
        public void PopContent()
        {
            OnPopContent?.Invoke();
        }
    }
}