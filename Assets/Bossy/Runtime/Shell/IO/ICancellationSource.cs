using System.Threading;

namespace Bossy.Shell
{
    public interface ICancellationSource
    {
        /// <summary>
        /// Gets a session-scoped cancellation token.
        /// </summary>
        /// <returns>The token.</returns>
        public CancellationToken GetSessionToken();

        /// <summary>
        /// Gets a command-scoped cancellation token.
        /// </summary>
        /// <returns>The token.</returns>
        public CancellationToken GetCommandToken();
    }
}