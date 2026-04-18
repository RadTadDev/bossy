using System.Threading;

namespace Bossy.FrontEnd.UI
{
    /// <summary>
    /// A command line interface.
    /// </summary>
    public interface ICommandLineGui
    {
        /// <summary>
        /// Gets a cancellation token that closes the session when canceled.
        /// </summary>
        /// <returns>The token.</returns>
        public CancellationToken GetSessionToken();
        
        /// <summary>
        /// Gets a cancellation token that closes the command when canceled.
        /// </summary>
        /// <returns>The token.</returns>
        public CancellationToken GetCommandToken();
        

    }
}