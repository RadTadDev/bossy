using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// A command with its own UI. While the command runs, its view is shown on top of the console,
    /// and it is removed when the command finishes.
    /// </summary>
    /// <remarks>
    /// The console handles focus, the toggle key and cancelling, so the view only needs to build its UI.
    /// Focus goes to the first focusable element in the view when it is shown.
    /// </remarks>
    public interface ICommandView
    {
        /// <summary>
        /// Creates the UI for this command.
        /// </summary>
        /// <returns>The UI root.</returns>
        public VisualElement CreateView();
    }
}
