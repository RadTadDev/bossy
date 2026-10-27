using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// A frontend drawn with UI Toolkit, which can be shown in an editor window or at runtime.
    /// </summary>
    internal interface IVisualEndpoint : ISessionEndpoint
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
        /// Called when the user presses the cancel keys, after the running command was cancelled.
        /// </summary>
        public void OnCancelPressed();
    }
}
