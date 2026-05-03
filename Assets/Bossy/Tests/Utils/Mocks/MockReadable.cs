using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Execution;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// A mock readable for testing.
    /// </summary>
    internal class MockReadable : IReadable
    {
        private int _idx;
        private bool _infinite;
        private List<object> _queue;
        
        /// <summary>
        /// Creates a finite readable source that gives the items presented in order.
        /// </summary>
        public MockReadable(List<object> queue)
        {
            _queue = queue;
        }

        /// <summary>
        /// Creates an infinite source that always produces the integer 1.
        /// </summary>
        public MockReadable()
        {
            _infinite = true;
        }
        
        public async Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            if (!_infinite) return _idx >= _queue.Count ? CloseWriterSentinel.Object : _queue[_idx++];
            
            await Task.Yield();
            return 1;
        }
    }
}