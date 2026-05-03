using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// A viewable object.
    /// </summary>
    public interface IContentView
    {
        /// <summary>
        /// Creates the UI for this view.
        /// </summary>
        /// <returns>The UI root.</returns>
        public VisualElement CreateView();
        
        /// <summary>
        /// A signaler to send events.
        /// </summary>
        /// <param name="signaler"></param>
        public void SetSignaler(Signaler signaler);

        /// <summary>
        /// Called when this view is being focused.
        /// </summary>
        public void OnFocus();

        /// <summary>
        /// Called when this view is being defocused.
        /// </summary>
        public void OnDefocus();
    }
}