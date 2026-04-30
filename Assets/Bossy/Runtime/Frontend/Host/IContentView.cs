using Bossy.Shell;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    public interface IContentView : IOHandler, IFrontEndCapabilities
    {
        public void SetSignaler(Signaler signaler);
        public VisualElement CreateView();

        /// <summary>
        /// Called when this view is being focused.
        /// </summary>
        public void Focus();

        /// <summary>
        /// Called when this view is being defocused.
        /// </summary>
        public void Defocus();
    }
}