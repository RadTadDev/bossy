using System;

namespace Bossy.Frontend
{
    /// <summary>
    /// Allows sending events when default behavior is hidden by the specific UI controls.
    /// </summary>
    public class Signaler
    {
        private Action _releaseFocus;
        private Bridge _bridge;
        
        /// <summary>
        /// Creates a new signaler.
        /// </summary>
        /// <param name="releaseFocus">An action to call when the view wants to release focus.</param>
        /// <param name="bridge">The bridge to the session backend.</param>
        public Signaler(Action releaseFocus, Bridge bridge)
        {
            _releaseFocus = releaseFocus;
            _bridge = bridge;
        }

        /// <summary>
        /// Signals this view wants to release focus.
        /// </summary>
        public void ReleaseFocus()
        {
            _releaseFocus?.Invoke();
        }

        /// <summary>
        /// Signals this view wants to cancel a command.
        /// </summary>
        public void CancelCommand()
        {
            _bridge.RequestCancelCommand();
        }
    }
}