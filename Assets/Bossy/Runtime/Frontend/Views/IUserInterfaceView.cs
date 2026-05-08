using Bossy.Execution;

namespace Bossy.Frontend
{
    /// <summary>
    /// A front end object that a user interacts with to access sessions.
    /// </summary>
    public interface IUserInterfaceView : IOHandler, IContentView, IFrontEndCapabilities
    {
        /// <summary>
        /// Called when a command has been canceled.
        /// </summary>
        public void OnCommandCanceled();

        /// <summary>
        /// Resets any dynamic capabilities once the command using them is popped.
        /// </summary>
        public void ResetCapabilities();
    }
}