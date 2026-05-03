using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Bossy.Execution
{
    /// <summary>
    /// Allows observing pipe IO.
    /// </summary>
    public class ObservablePipe : IOHandler
    {
        private readonly SemaphoreSlim _signal = new(0);
        private readonly ConcurrentQueue<object> _queue = new();

        private readonly Action<object> _onWrite;
        private readonly Action<object> _onRead;
        
        public ObservablePipe(Action<object> onWrite = null, Action<object> onRead = null)
        {
            _onWrite = onWrite;
            _onRead = onRead;
        }

        public async Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            await _signal.WaitAsync(token);
            var result = _queue.TryDequeue(out var obj) ? obj : null;
            _onRead?.Invoke(result);
            return result;
        }

        public void Write(object value)
        {
            _queue.Enqueue(value);
            _onWrite?.Invoke(value);
            _signal.Release();
        }

        public void CloseWriter()
        {
            _signal.Release();
            Write(CloseWriterSentinel.Object);
        }
    }
}