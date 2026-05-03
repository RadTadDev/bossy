using Bossy.Session;
using System.Collections.Generic;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// Creates an inspectable write target for testing.
    /// </summary>
    internal class MockWriteable : IWriteable
    {
        /// <summary>
        /// The output log.
        /// </summary>
        public IReadOnlyList<object> Log => _log; 
        
        private readonly List<object> _log = new();
        
        public void Write(object value)
        {
            _log.Add(value);
        }
    }
}