using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// A session frontend that records what is written to it and never provides input.
    /// </summary>
    internal class MockFrontend : ISessionFrontend
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
        
        public void Write(object value)
        {
            _log.Add(value);
        }

        public Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            return Task.FromCanceled<object>(new CancellationToken(true));
        }

        public void ResetCapabilities()
        {
            ResetCount++;
        }
    }
}
