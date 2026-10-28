using System;
using System.Collections.Generic;
using System.Text;
using Bossy.Command;
using Bossy.Frontend.Autocomplete;
using Bossy.Utils;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// Shows a <see cref="CompletionList"/> above the CLI input: a status line, then a virtualized list of rows.
    /// </summary>
    internal class CompletionPopup
    {
        /// <summary>
        /// True while the popup is shown.
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// The list being shown. Empty while the popup is closed.
        /// </summary>
        public CompletionList List { get; private set; } = CompletionList.Empty;

        // The list is virtualized with a fixed row height, which must fit the row style in BossyTheme.uss
        private const int RowHeight = 24;
        private const int VisibleRows = 8;

        private readonly VisualElement _container;
        private readonly VisualElement _status;
        private readonly ListView _rows;
        private readonly List<Completion> _completions = new();

        /// <summary>
        /// Creates a popup inside the CLI's autocomplete container.
        /// </summary>
        /// <param name="container">The container, holding the status line and list elements from BossyCli.uxml.</param>
        public CompletionPopup(VisualElement container)
        {
            _container = container;
            _status = container.Q("completion-status");
            _rows = container.Q<ListView>("completion-list");

            SetUpRows();
        }

        /// <summary>
        /// Shows a list, or closes the popup if the list has nothing to show.
        /// </summary>
        /// <param name="list">The list to show.</param>
        public void Show(CompletionList list)
        {
            if (list.Result.IsEmpty)
            {
                Close();
                return;
            }

            List = list;
            IsOpen = true;
            _container.AddToClassList(BossyTheme.CliSuggestionsVisibleClass);

            ShowStatus(list.Result);

            _completions.Clear();
            _completions.AddRange(list.Result.Completions);
            _rows.style.display = _completions.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _rows.style.height = Math.Min(_completions.Count, VisibleRows) * RowHeight;
            _rows.RefreshItems();
            _rows.ScrollToItem(0);
        }

        /// <summary>
        /// Moves the highlighted row and keeps it in view.
        /// </summary>
        /// <param name="delta">How many rows to move. Positive moves down.</param>
        public void MoveHighlight(int delta)
        {
            List.Move(delta);
            _rows.RefreshItems();
            _rows.ScrollToItem(List.Highlighted);
        }

        /// <summary>
        /// Hides the popup and forgets its list.
        /// </summary>
        public void Close()
        {
            IsOpen = false;
            List = CompletionList.Empty;
            _container.RemoveFromClassList(BossyTheme.CliSuggestionsVisibleClass);
            _status.Clear();
            _completions.Clear();
            _rows.RefreshItems();
        }

        /// <summary>
        /// Marks up a completion's display text with its matched runs highlighted. Everything else is shown as
        /// typed, so text such as "&lt;float&gt;" is never read as a rich text tag.
        /// </summary>
        /// <param name="text">The display text.</param>
        /// <param name="matches">The matched runs, in order.</param>
        /// <returns>The rich text.</returns>
        internal static string HighlightMatches(string text, IReadOnlyList<MatchRange> matches)
        {
            var builder = new StringBuilder();
            var index = 0;

            foreach (var match in matches)
            {
                if (match.Start < index || match.End > text.Length) continue;

                builder.Append(Literal(text[index..match.Start]));
                builder.Append(Format.Bold(Format.Color(Literal(text[match.Start..match.End]), Format.Accent)));
                index = match.End;
            }

            builder.Append(Literal(text[index..]));
            return builder.ToString();
        }

        /// <summary>
        /// Gets the short tag shown for an argument's kind. Positionals have none.
        /// </summary>
        /// <param name="attribute">The argument's attribute.</param>
        /// <returns>The tag, or null.</returns>
        internal static string KindTag(ArgumentAttribute attribute)
        {
            return attribute switch
            {
                OptionalAttribute => "opt",
                SwitchAttribute   => "switch",
                VariadicAttribute => "var",
                _                 => null
            };
        }

        private void SetUpRows()
        {
            _rows.itemsSource = _completions;
            _rows.selectionType = SelectionType.None;
            _rows.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            _rows.fixedItemHeight = RowHeight;
            _rows.focusable = false;

            _rows.makeItem = () =>
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList(BossyTheme.CliSuggestionClass);

                var value = new Label { pickingMode = PickingMode.Ignore };
                value.AddToClassList(BossyTheme.CliSuggestionValueClass);

                var description = new Label { pickingMode = PickingMode.Ignore };
                description.AddToClassList(BossyTheme.CliSuggestionDescriptionClass);

                row.Add(value);
                row.Add(description);
                return row;
            };

            _rows.bindItem = (element, i) =>
            {
                var completion = _completions[i];
                element.Q<Label>(className: BossyTheme.CliSuggestionValueClass).text = HighlightMatches(completion.DisplayText, completion.MatchRanges);
                element.Q<Label>(className: BossyTheme.CliSuggestionDescriptionClass).text = Literal(completion.Description);

                element.EnableInClassList(BossyTheme.CliSuggestionSelectedClass, i == List.Highlighted);
                element.EnableInClassList(BossyTheme.CliSuggestionSwitchClass, completion.Kind == CompletionKind.Switch);
                element.EnableInClassList(BossyTheme.CliSuggestionAliasClass, completion.Kind == CompletionKind.Alias);
            };
        }

        /*
         * Errors replace the list, so they are the whole status line. A hint about an argument is laid out in
         * pieces: the name and type, progress through a value that takes several tokens, the argument's kind,
         * whether the value is invalid, and the description, which is clipped to the width that is left.
         */
        private void ShowStatus(CompletionResult result)
        {
            _status.Clear();
            _status.style.display = result.MessageKind == CompletionMessageKind.None ? DisplayStyle.None : DisplayStyle.Flex;

            var hint = result.Argument;
            if (result.MessageKind != CompletionMessageKind.Hint || hint == null)
            {
                var kindClass = result.MessageKind switch
                {
                    CompletionMessageKind.Error => BossyTheme.CliStatusErrorClass,
                    CompletionMessageKind.Ready => BossyTheme.CliStatusReadyClass,
                    _                           => BossyTheme.CliStatusHintClass
                };
                AddStatusPart(Literal(result.Message), kindClass, BossyTheme.CliStatusClipClass);
                return;
            }

            var argument = hint.Argument;
            AddStatusPart($"{Format.Bold(Literal(argument.Name))} {Literal($"<{argument.Type.GetFriendlyName()}>")}", BossyTheme.CliStatusHintClass);

            if (hint.HasProgress)
            {
                AddStatusPart($"{hint.TokenNumber}/{hint.TokensDesired}", BossyTheme.CliStatusMutedClass);
            }

            var tag = KindTag(argument.ArgumentAttribute);
            if (tag != null)
            {
                AddStatusPart(tag, BossyTheme.CliStatusMutedClass);
            }

            if (hint.InvalidValue != null)
            {
                AddStatusPart("invalid", BossyTheme.CliStatusWarningClass);
            }

            if (hint.SuggestionError != null)
            {
                AddStatusPart(Literal($"Suggestions failed: {hint.SuggestionError}"), BossyTheme.CliStatusErrorClass, BossyTheme.CliStatusClipClass);
            }

            if (!string.IsNullOrWhiteSpace(argument.Description))
            {
                AddStatusPart(Literal(argument.Description), BossyTheme.CliStatusMutedClass, BossyTheme.CliStatusClipClass);
            }
        }

        private void AddStatusPart(string text, params string[] classes)
        {
            var part = new Label(text) { pickingMode = PickingMode.Ignore };
            part.AddToClassList(BossyTheme.CliStatusPartClass);

            foreach (var modifier in classes)
            {
                part.AddToClassList(modifier);
            }

            _status.Add(part);
        }

        private static string Literal(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : $"<noparse>{text}</noparse>";
        }
    }
}
