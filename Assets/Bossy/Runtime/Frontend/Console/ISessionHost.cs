using Bossy.Execution;

namespace Bossy.Frontend
{
    /// <summary>
    /// Where a session's UI lives. Lets a session show command views and open command windows
    /// without knowing whether it runs in the editor or at runtime.
    /// </summary>
    internal interface ISessionHost
    {
        /// <summary>
        /// Shows a command's view on top of the session's current view.
        /// </summary>
        /// <param name="view">The command view to show.</param>
        public void PushView(ICommandView view);

        /// <summary>
        /// Removes the most recently shown command view.
        /// </summary>
        public void PopView();

        /// <summary>
        /// Runs a command graph in a new command window with its own session.
        /// </summary>
        /// <param name="template">The session the new one is cloned from.</param>
        /// <param name="graph">The graph to run.</param>
        /// <param name="reader">An override reader, or null to read from the new window.</param>
        /// <param name="writer">An override writer, or null to write to the new window.</param>
        public void OpenCommandWindow(Session template, CommandGraph graph, IReadable reader, IWriteable writer);
    }
}
