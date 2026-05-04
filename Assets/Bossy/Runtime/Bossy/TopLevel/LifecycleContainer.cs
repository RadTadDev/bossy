using System;
using System.Threading.Tasks;
using Bossy.Frontend;
using Bossy.Execution;
using Bossy.Utils;

namespace Bossy
{
    /// <summary>
    /// A state container for a single full stack session.
    /// </summary>
    internal class LifecycleContainer
    {
        private bool _alive = true;
        
        private readonly Session _session;
        private readonly SessionViewer _viewer;

        /// <summary>
        /// Creates a new container.
        /// </summary>
        /// <param name="session">The session being spun up.</param>
        /// <param name="viewer">The view associated with this session.</param>
        public LifecycleContainer(Session session, SessionViewer viewer)
        {
            _session = session;
            _viewer = viewer;
        }
        
        /// <summary>
        /// Starts the session loop.
        /// </summary>
        /// <param name="graph">A graph to execute if running in command mode.</param>
        public void Start(CommandGraph graph = null)
        {
            var task = graph == null ? _session.RunAsync() : _session.RunGraphAsync(graph);
            _ = ObserveExceptions(task);
        }
        
        /// <summary>
        /// Closes the session and all associated resources.
        /// </summary>
        public void Close()
        {
            if (!_alive) return;

            ((IWriteable)_session.Bridge).CloseWriter();
            _viewer.Dispose();
            _session.Dispose();
            _alive = false;
        }
        
        /// <summary>
        /// Cancels the current command.
        /// </summary>
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