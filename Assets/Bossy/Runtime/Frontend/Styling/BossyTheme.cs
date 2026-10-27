using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// Applies the Bossy look: the theme stylesheet, its root class and the Bossy font.
    /// </summary>
    internal static class BossyTheme
    {
        /// <summary>
        /// The class that defines the theme variables. Elements inside it can use them.
        /// </summary>
        public const string RootClass = "bossy-root";

        private const string StyleSheetPath = "BossyTheme";

        private static StyleSheet _styleSheet;

        /// <summary>
        /// Applies the theme to the root of a Bossy window or overlay.
        /// </summary>
        /// <param name="root">The root element.</param>
        public static void ApplyTo(VisualElement root)
        {
            if (_styleSheet == null)
            {
                _styleSheet = Resources.Load<StyleSheet>(StyleSheetPath);
                if (_styleSheet == null)
                {
                    throw new InvalidOperationException($"Bossy could not find its theme at Resources/{StyleSheetPath}.");
                }
            }

            if (!root.styleSheets.Contains(_styleSheet))
            {
                root.styleSheets.Add(_styleSheet);
            }

            root.AddToClassList(RootClass);
            BossyFont.ApplyTo(root);
        }
    }
}
