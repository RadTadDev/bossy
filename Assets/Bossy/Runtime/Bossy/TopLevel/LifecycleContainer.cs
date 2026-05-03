using System;
using System.Threading.Tasks;
using Bossy.Frontend;
using Bossy.Execution;
using Bossy.Utils;

namespace Bossy
{
    internal class LifecycleContainer
    {
        private bool _alive = true;
        
        private readonly Session _session;
        private readonly SessionViewer _viewer;

        public LifecycleContainer(Session session, SessionViewer viewer)
        {
            _session = session;
            _viewer = viewer;
        }
        
        public void Start(CommandGraph graph = null)
        {
            var task = graph == null ? _session.RunAsync() : _session.RunGraphAsync(graph);
            _ = ObserveExceptions(task);
        }
        
        public void Close()
        {
            if (!_alive) return;

            ((IWriteable)_session.Bridge).CloseWriter();
            _viewer.Dispose();
            _session.Dispose();
            _alive = false;
        }
        
        public void CancelCommand()
        {
            _session.CancelCommand();
        }

        private async Task ObserveExceptions(Task task)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected when session is canceled
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }
        }
    }
}