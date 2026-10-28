using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// Applies the Bossy look: the theme stylesheet, its root class and the Bossy font.
    /// Also holds every USS class name used from code, which must match BossyTheme.uss.
    /// </summary>
    internal static class BossyTheme
    {
        /// <summary>
        /// The class that defines the theme variables. Elements inside it can use them.
        /// </summary>
        public const string RootClass = "bossy-root";

        /// <summary>
        /// The root of the runtime overlay.
        /// </summary>
        public const string OverlayClass = "bossy-overlay";

        /// <summary>
        /// The runtime console panel while it is hidden. Its transition slides the panel out.
        /// </summary>
        public const string RuntimeConsoleHiddenClass = "bossy-runtime-console--hidden";

        /// <summary>
        /// A runtime command window as it opens. Its transition fades the window in.
        /// </summary>
        public const string WindowEnteringClass = "bossy-window--entering";

        /// <summary>
        /// A command's view, shown on top of the console while the command runs.
        /// </summary>
        public const string CommandViewClass = "bossy-command-view";

        /// <summary>
        /// A command view as it is shown. Its transition animates the view in.
        /// </summary>
        public const string CommandViewEnteringClass = "bossy-command-view--entering";

        /// <summary>
        /// A line of CLI output.
        /// </summary>
        public const string CliLineClass = "bossy-cli__line";

        /// <summary>
        /// The CLI input row while its text field has focus.
        /// </summary>
        public const string CliInputRowFocusedClass = "bossy-cli__input-row--focused";

        /// <summary>
        /// The CLI autocomplete container while it shows suggestions.
        /// </summary>
        public const string CliSuggestionsVisibleClass = "bossy-cli__suggestions--visible";

        /// <summary>
        /// An autocomplete suggestion.
        /// </summary>
        public const string CliSuggestionClass = "bossy-cli__suggestion";

        /// <summary>
        /// The autocomplete suggestion currently selected by cycling.
        /// </summary>
        public const string CliSuggestionSelectedClass = "bossy-cli__suggestion--selected";

        /// <summary>
        /// An autocomplete suggestion that is a hint rather than a completion.
        /// </summary>
        public const string CliSuggestionHintClass = "bossy-cli__suggestion--hint";

        /// <summary>
        /// An autocomplete suggestion that reports an error.
        /// </summary>
        public const string CliSuggestionErrorClass = "bossy-cli__suggestion--error";

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
