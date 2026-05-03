using System;
using System.Collections.Generic;
using Bossy.Schema.Registry;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// A dummy command discoverer that returns the same list given on creation.
    /// </summary>
    internal class MockCommandDiscoverer : ICommandDiscoverer
    {
        private readonly List<Type> _types;

        /// <summary>
        /// Creates a mock command discoverer.
        /// </summary>
        /// <param name="types">The command types to discover.</param>
        public MockCommandDiscoverer(List<Type> types)
        {
            _types = types;
        }
        
        public IReadOnlyList<Type> GetAllCommandTypes()
        {
            return _types;
        }
    }
}