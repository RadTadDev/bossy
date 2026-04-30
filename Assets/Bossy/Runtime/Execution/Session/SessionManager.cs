namespace Bossy.Shell
{
    internal class SessionManager
    {
        private LifecycleManager _lifecycle;

        public SessionManager(LifecycleManager lifecycle)
        {
            _lifecycle = lifecycle;
        }
    }
}