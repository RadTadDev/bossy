using System;

namespace Bossy.Frontend
{
    public class Signaler
    {
        private Action _releaseFocus;
        private Bridge _bridge;
        
        public Signaler(Action releaseFocus, Bridge bridge)
        {
            _releaseFocus = releaseFocus;
            _bridge = bridge;
        }

        public void ReleaseFocus()
        {
            _releaseFocus?.Invoke();
        }

        public void CancelCommand()
        {
            _bridge.RequestCancelCommand();
        }
    }
}