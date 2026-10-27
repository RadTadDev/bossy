using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// A visual endpoint drawn with UI Toolkit, which can be shown in an editor window or at runtime.
    /// </summary>
    internal interface IVisualEndpointEndpoint : IVisualEndpointCapabilities
    {
        /// <summary>
        /// The root of the frontend's UI.
        /// </summary>
        public VisualElement Root { get; }

        /// <summary>
        /// Gives keyboard focus to the frontend's main input.
        /// </summary>
        public void Focus();

        /// <summary>
        /// Called when the user presses the cancel keys, after the running command was canceled.
        /// </summary>
        public void OnCancelPressed();
    }
}
