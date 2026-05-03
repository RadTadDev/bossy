using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// A viewable object.
    /// </summary>
    public interface IContentView
    {
        public VisualElement CreateView();
        
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