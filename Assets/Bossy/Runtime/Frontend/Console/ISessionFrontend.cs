using Bossy.Execution;

namespace Bossy.Frontend
{
    /// <summary>
    /// The side of a session that a user interacts with. The session reads commands and input from it and
    /// writes output to it. The CLI is one frontend; a GUI or a voice interface could be others.
    /// </summary>
    /// <remarks>
    /// Commands can test a frontend for optional capabilities, such as <see cref="IClearable"/>, by casting it.
    /// </remarks>
    public interface ISessionFrontend : IOHandler, IFrontEndCapabilities
    {
        /// <summary>
        /// Resets capabilities a command changed for its run, such as the prompt header.
        /// Called after each command group finishes.
        /// </summary>
        public void ResetCapabilities();

        /// <summary>
        /// A frontend stays open for the whole session, so closing it as a command's output does nothing.
        /// </summary>
        void IWriteable.CloseWriter() { }
    }
}
