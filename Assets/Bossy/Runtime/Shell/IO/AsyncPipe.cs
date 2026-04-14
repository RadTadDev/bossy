using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace Bossy.Shell
{
    /// <summary>
    /// A standard asynchronous communication stream.
    /// </summary>
    public class AsyncPipe : IReadable, IWriteable
    {
        private readonly SemaphoreSlim _signal = new(0);
        private readonly ConcurrentQueue<object> _queue = new();

        public async Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            await _signal.WaitAsync(token);
            _queue.TryDequeue(out var obj);
            return obj;
        }

        public void Write(object value)
        {
            _queue.Enqueue(value);
            _signal.Release();
        }
    }
}