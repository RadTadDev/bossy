using System.Collections.Generic;
using Bossy.Execution;
using Bossy.Frontend;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// A session host that records the calls made to it.
    /// </summary>
    internal class MockSessionHost : ISessionHost
    {
        /// <summary>
        /// Every call made, in order, such as "push" or "pop".
        /// </summary>
        public IReadOnlyList<string> Calls => _calls;

        /// <summary>
        /// The graph passed to the last command window that was opened.
        /// </summary>
        public CommandGraph OpenedGraph { get; private set; }
        
        private readonly List<string> _calls = new();

        public void PushView(ICommandView view)
        {
            _calls.Add("push");
        }

        public void PopView()
        {
            _calls.Add("pop");
        }

        public void OpenCommandWindow(Session template, CommandGraph graph, IReadable reader, IWriteable writer)
        {
            _calls.Add("open");
            OpenedGraph = graph;
        }
    }
}
