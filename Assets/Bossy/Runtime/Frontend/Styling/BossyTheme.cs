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
        /// A row in the autocomplete list.
        /// </summary>
        public const string CliSuggestionClass = "bossy-cli__suggestion";

        /// <summary>
        /// The highlighted row in the autocomplete list, which the accept key applies.
        /// </summary>
        public const string CliSuggestionSelectedClass = "bossy-cli__suggestion--selected";

        /// <summary>
        /// An autocomplete row that completes a switch name.
        /// </summary>
        public const string CliSuggestionSwitchClass = "bossy-cli__suggestion--switch";

        /// <summary>
        /// An autocomplete row that completes an alias.
        /// </summary>
        public const string CliSuggestionAliasClass = "bossy-cli__suggestion--alias";

        /// <summary>
        /// The value shown in an autocomplete row.
        /// </summary>
        public const string CliSuggestionValueClass = "bossy-cli__suggestion-value";

        /// <summary>
        /// The dimmed description on the right of an autocomplete row.
        /// </summary>
        public const string CliSuggestionDescriptionClass = "bossy-cli__suggestion-description";

        /// <summary>
        /// A piece of the autocomplete status line, above the list.
        /// </summary>
        public const string CliStatusPartClass = "bossy-cli__status-part";

        /// <summary>
        /// A status line piece showing a hint.
        /// </summary>
        public const string CliStatusHintClass = "bossy-cli__status-part--hint";

        /// <summary>
        /// A status line piece saying the line is ready to run.
        /// </summary>
        public const string CliStatusReadyClass = "bossy-cli__status-part--ready";

        /// <summary>
        /// A status line piece showing an error.
        /// </summary>
        public const string CliStatusErrorClass = "bossy-cli__status-part--error";

        /// <summary>
        /// A status line piece warning that the typed value is invalid.
        /// </summary>
        public const string CliStatusWarningClass = "bossy-cli__status-part--warning";

        /// <summary>
        /// A dimmed status line piece, such as value progress or the argument kind.
        /// </summary>
        public const string CliStatusMutedClass = "bossy-cli__status-part--muted";

        /// <summary>
        /// A status line piece that shrinks and ends in an ellipsis when the line runs out of room.
        /// </summary>
        public const string CliStatusClipClass = "bossy-cli__status-part--clip";

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
