using Bossy.Execution;

namespace Bossy.Frontend
{
    /// <summary>
    /// An endpoint with a user interface. Commands can test for optional capabilities, such as
    /// <see cref="IClearable"/>, by casting <see cref="Command.SimpleContext.Endpoint"/>.
    /// </summary>
    public interface IVisualCapabilities : IEndpoint
    {
        /// <summary>
        /// Resets capabilities a command changed for its run, such as the prompt header.
        /// Called after each command group finishes.
        /// </summary>
        public void ResetCapabilities();
    }
}
