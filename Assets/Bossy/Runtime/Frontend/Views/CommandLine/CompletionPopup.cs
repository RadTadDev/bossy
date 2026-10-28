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
        private readonly Label _failure;
        private readonly VisualElement _status;
        private readonly ListView _rows;
        private readonly List<Completion> _completions = new();

        /// <summary>
        /// Creates a popup inside the CLI's autocomplete container.
        /// </summary>
        /// <param name="container">The container, holding the failure tab, status line and list elements from BossyCli.uxml.</param>
        public CompletionPopup(VisualElement container)
        {
            _container = container;
            _failure = container.Q<Label>("completion-failure");
            _status = container.Q("completion-status");
            _rows = container.Q<ListView>("completion-list");

            _failure.pickingMode = PickingMode.Ignore;
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
            ShowFailure(list.Result.SuggestionFailure);

            _completions.Clear();
            _completions.AddRange(list.Result.Completions);
            _status.EnableInClassList(BossyTheme.CliStatusDividedClass, _completions.Count > 0);
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
            ShowFailure(null);
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
        /// Gets the rich text for the tab that reports a failing suggestion function.
        /// </summary>
        /// <param name="failure">The failure.</param>
        /// <returns>The rich text.</returns>
        internal static string FailureText(SuggestionFailure failure)
        {
            var subject = failure.IsValidator ? "A validator for" : "Suggestions for";
            return $"{subject} {Format.Bold(Literal(failure.Argument.Name))} failed: {Literal(failure.Message)}";
        }

        /// <summary>
        /// Gets the rich text for an argument's constraints, with the one the value at the cursor fails in red.
        /// The rest of the line keeps its color, since a value being typed fails most constraints on the way.
        /// </summary>
        /// <param name="hint">The hint about the argument.</param>
        /// <returns>The rich text.</returns>
        internal static string ConstraintsText(ArgumentHint hint)
        {
            var parts = new List<string>();
            for (var i = 0; i < hint.Constraints.Count; i++)
            {
                var constraint = Literal(hint.Constraints[i]);
                parts.Add(i == hint.FailedConstraint ? Format.Color(constraint, Format.Red) : constraint);
            }

            return string.Join(", ", parts);
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
         * its validators' constraints, whether the value is invalid, and the description, which is clipped to the
         * width that is left.
         * The whole line turns red for an error or an invalid value. A value that parses but fails a validator
         * only turns that constraint red, since it is likely still being typed.
         */
        private void ShowStatus(CompletionResult result)
        {
            var hint = result.Argument;

            _status.Clear();
            _status.style.display = result.MessageKind == CompletionMessageKind.None ? DisplayStyle.None : DisplayStyle.Flex;
            _status.EnableInClassList(BossyTheme.CliStatusErrorStateClass,
                result.MessageKind == CompletionMessageKind.Error || hint?.InvalidValue != null);

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

            if (hint.Constraints.Count > 0)
            {
                AddStatusPart(ConstraintsText(hint), BossyTheme.CliStatusMutedClass);
            }

            // A validator that does not describe itself has no constraint to color, so its message is shown instead
            if (hint.ValidationError != null && hint.FailedConstraint < 0)
            {
                AddStatusPart(Literal(hint.ValidationError), BossyTheme.CliStatusErrorClass);
            }

            if (hint.InvalidValue != null)
            {
                AddStatusPart("invalid", BossyTheme.CliStatusErrorClass);
            }

            if (!string.IsNullOrWhiteSpace(argument.Description))
            {
                AddStatusPart(Literal(argument.Description), BossyTheme.CliStatusMutedClass, BossyTheme.CliStatusClipClass);
            }
        }

        /*
         * The tab is shown on the next tick, so that its hidden style has been applied once and the change to the
         * visible style runs its slide transition, even when the popup opens in the same frame.
         */
        private void ShowFailure(SuggestionFailure failure)
        {
            if (failure == null)
            {
                _failure.RemoveFromClassList(BossyTheme.CliSuggestionFailureVisibleClass);
                return;
            }

            _failure.text = FailureText(failure);
            _failure.schedule.Execute(() =>
            {
                if (List.Result.SuggestionFailure != null)
                {
                    _failure.AddToClassList(BossyTheme.CliSuggestionFailureVisibleClass);
                }
            });
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
