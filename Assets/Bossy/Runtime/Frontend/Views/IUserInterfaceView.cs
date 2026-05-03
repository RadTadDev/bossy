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
    }
}