using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Execution;
using Bossy.Frontend;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// A session frontend that records what is written to it. Reads return the given input in order, one item each,
    /// and are cancelled once it runs out, which ends a session's read loop.
    /// </summary>
    internal class MockEndpoint : IVisualEndpointCapabilities
    {
        /// <summary>
        /// The output log.
        /// </summary>
        public IReadOnlyList<object> Log => _log;

        /// <summary>
        /// How many times capabilities were reset.
        /// </summary>
        public int ResetCount { get; private set; }

        private readonly List<object> _log = new();
        private readonly Queue<object> _input;

        /// <summary>
        /// Creates a mock endpoint.
        /// </summary>
        /// <param name="input">What reads return, in order.</param>
        public MockEndpoint(params object[] input)
        {
            _input = new Queue<object>(input);
        }

        public void Write(object value)
        {
            _log.Add(value);
        }

        public Task<object> ReadAsync(ReadRequest request, CancellationToken token)
        {
            return _input.Count > 0
                ? Task.FromResult(_input.Dequeue())
                : Task.FromCanceled<object>(new CancellationToken(true));
        }

        public void ResetCapabilities()
        {
            ResetCount++;
        }
    }
}
