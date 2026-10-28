using System.Collections.Generic;
using Bossy.Execution;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// An execution listener that records when each command group starts and finishes.
    /// </summary>
    internal class MockExecutionListener : IExecutionListener
    {
        /// <summary>
        /// Every call made, in order, such as "start" or "finish".
        /// </summary>
        public IReadOnlyList<string> Calls => _calls;

        private readonly List<string> _calls = new();

        public void OnGroupStarted(CommandGroup group)
        {
            _calls.Add("start");
        }

        public void OnGroupFinished(CommandGroup group)
        {
            _calls.Add("finish");
        }
    }
}
