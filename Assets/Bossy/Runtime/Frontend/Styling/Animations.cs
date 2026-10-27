using System;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// Plays the theme's USS transitions by adding and removing state classes.
    /// </summary>
    internal static class Animations
    {
        /// <summary>
        /// Animates an element from the state a class describes to its normal state, such as fading in.
        /// </summary>
        /// <param name="element">The element. It should be in a visible hierarchy or about to be added to one.</param>
        /// <param name="fromClass">The class that describes the starting state.</param>
        public static void In(VisualElement element, string fromClass)
        {
            element.AddToClassList(fromClass);

            // A transition only plays once the element has been laid out in its starting state
            EventCallback<GeometryChangedEvent> onLaidOut = null;
            onLaidOut = _ =>
            {
                element.UnregisterCallback(onLaidOut);
                element.RemoveFromClassList(fromClass);
            };
            element.RegisterCallback(onLaidOut);
        }

        /// <summary>
        /// Animates an element to the state a class describes, such as fading out, then runs an action.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="toClass">The class that describes the end state.</param>
        /// <param name="durationMs">How long the transition takes. Must match the theme.</param>
        /// <param name="onFinished">Runs when the transition has finished.</param>
        public static void Out(VisualElement element, string toClass, long durationMs, Action onFinished)
        {
            element.AddToClassList(toClass);
            element.schedule.Execute(onFinished).StartingIn(durationMs);
        }
    }
}
