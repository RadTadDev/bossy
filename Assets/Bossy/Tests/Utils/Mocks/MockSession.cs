using Bossy.Execution;
using Bossy.Frontend;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// Creates sessions for tests. Their history is kept in memory and never saved.
    /// </summary>
    internal static class MockSession
    {
        /// <summary>
        /// Creates a session with an in-memory history and empty aliases.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        /// <param name="permissions">The permissions manager.</param>
        /// <param name="endpoint">The endpoint to read from and write to.</param>
        /// <param name="host">Where the session's UI lives, or null if it has none.</param>
        /// <param name="isWindowed">True if the session should act as if it runs in a command window.</param>
        /// <returns>The session.</returns>
        public static Session Create(BossyContext context, BossyPermissions permissions, IEndpoint endpoint, ISessionHost host = null, bool isWindowed = false)
        {
            return new Session(context, permissions, endpoint, host, new CommandHistory(null), new(), isWindowed);
        }
    }
}
