using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Bossy.Utils;

namespace Bossy.Execution
{
    /// <summary>
    /// A standard asynchronous communication stream.
    /// </summary>
    public class AsyncPipe : IOHandler
    {
        private readonly SemaphoreSlim _signal = new(0);
        private readonly ConcurrentQueue<object> _queue = new();

        public async Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            await _signal.WaitAsync(token);

            return _queue.TryDequeue(out var obj) ? obj : throw new InvalidOperationException("Pipe read after being closed! A pipe should not outlive its command.");
        }
        
        public void Write(object value)
        {
            _queue.Enqueue(value);
            _signal.Release();
        }

        public void CloseWriter()
        {
            _signal.Release();
            Write(CloseWriterSentinel.Object);
        }
    }
}