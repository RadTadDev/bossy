using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Execution;
using Bossy.Frontend.Autocomplete;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// The default command line frontend.
    /// </summary>
    internal class CliView : IVisualEndpoint,
        IClearable,
        IModifiableOutputBuffer,
        IModifiablePromptHeader
    {
        /// <summary>
        /// When the input line is shown.
        /// </summary>
        public enum InputVisibility
        {
            /// <summary>
            /// Always shown, as in the main console.
            /// </summary>
            Always,

            /// <summary>
            /// Only shown while a command asks for input, as in command windows.
            /// </summary>
            WhenReading
        }

        public VisualElement Root { get; }

        private readonly BossyContext _context;
        private readonly InputVisibility _inputVisibility;

        // Output
        private readonly ListView _output;
        private readonly List<string> _outputBuffer = new() { string.Empty };

        // Input
        private readonly VisualElement _inputRow;
        private readonly TextField _input;
        private TaskCompletionSource<object> _readSource;
        private bool _reading;
        private bool _requestingCommand;

        // History
        private readonly CommandHistory _history;
        private int _historyIndex;

        // Autocomplete
        private readonly AutocompleteEngine _autocomplete;
        private readonly VisualElement _autocompleteContainer;
        private readonly List<Label> _completionLabels = new();
        private CompletionResult _completions = CompletionResult.Empty;
        private string _completedLine = string.Empty;
        private int _suggestionIndex;
        private bool _cyclingSuggestions;

        // Prompt header
        private readonly Label _promptHeaderElement;
        private string _promptHeader = string.Empty;

        /// <summary>
        /// Creates a CLI frontend.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        /// <param name="inputVisibility">When the input line is shown.</param>
        /// <param name="history">The history to browse with the history keys.</param>
        /// <param name="aliases">The session's aliases, so autocomplete can suggest and expand them.</param>
        public CliView(BossyContext context, InputVisibility inputVisibility, CommandHistory history, Dictionary<string, string> aliases)
        {
            _context = context;
            _inputVisibility = inputVisibility;
            _history = history;
            _historyIndex = _history.Lines.Count;
            _autocomplete = new AutocompleteEngine(_context, context.Settings.BossyCliSettings.ToOperatorList(), aliases);

            Root = ContentViewUtility.GetRootFromUxml("BossyCli");

            _promptHeaderElement = Root.Q<Label>("prompt-label");
            _autocompleteContainer = Root.Q("auto-complete");
            _inputRow = Root.Q("input-row");
            _input = Root.Q<TextField>("input-field");
            _output = Root.Q<ListView>("output-list");

            SetUpInput();
            SetUpOutput();
            SetInputEnabled(false);
        }

        public void Focus()
        {
            if (_reading)
            {
                FocusInput();
            }
            else
            {
                // Keep focus inside the view while a command runs so the cancel keys still arrive
                _inputRow.Focus();
            }
        }

        public void OnCancelPressed()
        {
            SetInput(string.Empty);
            ClearAutocomplete();
        }

        public void Write(object value)
        {
            _outputBuffer.Add(value.ToString());
            _output.RefreshItems();
            _output.ScrollToItem(_outputBuffer.Count - 1);
        }

        public async Task<object> ReadAsync(ReadRequest request, CancellationToken token)
        {
            _requestingCommand = request.RequestedType == typeof(CommandGraph);
            _readSource = new TaskCompletionSource<object>();

            // Choices are numbered from 1, and the reader maps the typed number back to the choice
            for (var i = 0; request.Choices != null && i < request.Choices.Count; i++)
            {
                Write($"{i + 1}: {request.Choices[i].Display}");
            }

            // Start browsing from the end, after anything the last command added to the history
            _historyIndex = _history.Lines.Count;

            SetInputEnabled(true);
            FocusInput();

            using var registration = token.Register(() => _readSource.TrySetCanceled());

            return await _readSource.Task;
        }

        public void ResetCapabilities()
        {
            ResetHeader();
        }

        public void Clear()
        {
            ClearAutocomplete();
            _outputBuffer.Clear();
            _outputBuffer.Add(string.Empty);
            _output.RefreshItems();
        }

        public void Overwrite(object value)
        {
            _outputBuffer[^1] = value.ToString();
            _output.RefreshItems();
            _output.ScrollToItem(_outputBuffer.Count - 1);
        }

        public void SetPromptHeader(string header)
        {
            _promptHeader = header;
            _promptHeaderElement.text = $"{_promptHeader}>";
        }

        public void ResetHeader()
        {
            _promptHeader = string.Empty;
            _promptHeaderElement.text = ">";
        }

        private void SetUpInput()
        {
            _input.selectAllOnFocus = false;
            _input.selectAllOnMouseUp = false;
            _inputRow.focusable = true;

            _input.RegisterCallback<FocusInEvent>(_ => _inputRow.AddToClassList(BossyTheme.CliInputRowFocusedClass));
            _input.RegisterCallback<FocusOutEvent>(_ => _inputRow.RemoveFromClassList(BossyTheme.CliInputRowFocusedClass));

            var input = _context.Settings.BossyInputSettings;
            _input.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (input.SubmitCommand.IsAsserted(evt))
                {
                    Submit();
                }
                else if (input.HistoryBack.IsAsserted(evt))
                {
                    HistoryBack();
                }
                else if (input.HistoryForward.IsAsserted(evt))
                {
                    HistoryForward();
                }
                else if (input.CycleSuggestions.IsAsserted(evt))
                {
                    CycleSuggestions();
                    evt.StopImmediatePropagation();
                }
                else
                {
                    // Reset the history index on any normal character press
                    _historyIndex = _history.Lines.Count;
                }
            }, TrickleDown.TrickleDown);

            _input.RegisterValueChangedCallback(OnValueUpdated);
        }

        private void SetUpOutput()
        {
            _output.itemsSource = _outputBuffer;
            _output.selectionType = SelectionType.None;
            _output.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _output.makeItem = () =>
            {
                var line = new Label { pickingMode = PickingMode.Ignore };
                line.AddToClassList(BossyTheme.CliLineClass);
                return line;
            };
            _output.bindItem = (element, i) => ((Label)element).text = _outputBuffer[i];
        }

        private void SetInputEnabled(bool enabled)
        {
            _reading = enabled;
            _input.focusable = enabled;
            _input.SetEnabled(enabled);

            var visible = enabled || _inputVisibility == InputVisibility.Always;
            _inputRow.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Submit()
        {
            var line = _input.value;

            Write($"{Format.Color($"{_promptHeader}>", Format.Accent)} {line}");

            SetInput(string.Empty);
            ClearAutocomplete();

            // Nothing runs, so keep reading. Unity's text field can end editing on enter, so focus it again.
            // Other lines are handed over as typed; the session parses them.
            if (_requestingCommand && string.IsNullOrWhiteSpace(line))
            {
                Write(string.Empty);
                FocusInput();
                return;
            }

            SetInputEnabled(false);
            _inputRow.Focus();
            _readSource.TrySetResult(line);
        }

        private void FocusInput()
        {
            // Focus on the next tick; focusing during layout or event handling does not always stick
            _input.schedule.Execute(() =>
            {
                _input.Focus();
                MoveCursorToEnd();
            });
        }

        private void SetInput(string value)
        {
            _input.value = value;
            _input.schedule.Execute(MoveCursorToEnd);
        }

        private void SetInput(string value, int cursorIndex)
        {
            _input.value = value;
            _input.schedule.Execute(() =>
            {
                _input.cursorIndex = cursorIndex;
                _input.selectIndex = cursorIndex;
            });
        }

        private void MoveCursorToEnd()
        {
            _input.cursorIndex = _input.value.Length;
            _input.selectIndex = _input.value.Length;
        }

        private void HistoryBack()
        {
            if (_historyIndex == 0 || _history.Lines.Count == 0) return;

            ClearAutocomplete();

            _historyIndex--;

            SetInput(_history.Lines[_historyIndex]);
        }

        private void HistoryForward()
        {
            if (_historyIndex >= _history.Lines.Count - 1 || _history.Lines.Count == 0) return;

            ClearAutocomplete();

            _historyIndex++;

            SetInput(_history.Lines[_historyIndex]);
        }

        private void OnValueUpdated(ChangeEvent<string> evt)
        {
            if (!_requestingCommand || !_reading || _cyclingSuggestions) return;

            if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                ClearAutocomplete();
                return;
            }

            ShowCompletions(evt.newValue, _input.cursorIndex);
        }

        private void ShowCompletions(string line, int cursorIndex)
        {
            ClearAutocomplete();

            var result = _autocomplete.Complete(line, cursorIndex);
            if (result.IsEmpty)
            {
                return;
            }

            // Completions replace spans of the line they were made for, so keep it while they are cycled
            _completions = result;
            _completedLine = line;
            _autocompleteContainer.AddToClassList(BossyTheme.CliSuggestionsVisibleClass);

            if (result.MessageKind != CompletionMessageKind.None)
            {
                var modifier = result.MessageKind == CompletionMessageKind.Error
                    ? BossyTheme.CliSuggestionErrorClass
                    : BossyTheme.CliSuggestionHintClass;
                var description = result.Argument?.Argument.Description;
                var message = string.IsNullOrWhiteSpace(description)
                    ? result.Message
                    : $"{result.Message}  {Format.Color(description, Format.Gray)}";
                _autocompleteContainer.Add(MakeSuggestionLabel(message, modifier));
            }

            foreach (var completion in result.Completions)
            {
                var text = string.IsNullOrEmpty(completion.Description)
                    ? completion.DisplayText
                    : $"{completion.DisplayText}  {Format.Color(completion.Description, Format.Gray)}";
                var label = MakeSuggestionLabel(text, null);

                _completionLabels.Add(label);
                _autocompleteContainer.Add(label);
            }
        }

        private static Label MakeSuggestionLabel(string text, string modifierClass)
        {
            var label = new Label(text);
            label.AddToClassList(BossyTheme.CliSuggestionClass);

            if (modifierClass != null)
            {
                label.AddToClassList(modifierClass);
            }

            return label;
        }

        private void CycleSuggestions()
        {
            // If autocomplete is not open, try opening it
            if (_autocompleteContainer.childCount == 0)
            {
                ShowCompletions(_input.value, _input.cursorIndex);
                return;
            }

            // Messages cannot be applied
            var count = _completionLabels.Count;
            if (count == 0)
            {
                return;
            }

            _completionLabels[(_suggestionIndex - 1 + count) % count].RemoveFromClassList(BossyTheme.CliSuggestionSelectedClass);
            _completionLabels[_suggestionIndex].AddToClassList(BossyTheme.CliSuggestionSelectedClass);

            // Changing the input would otherwise recompute the suggestions being cycled. The change event
            // can arrive after this key event finishes, so the flag is cleared on the next tick.
            var completion = _completions.Completions[_suggestionIndex];
            _cyclingSuggestions = true;
            SetInput(completion.ApplyTo(_completedLine), completion.CursorAfterApply);
            _input.schedule.Execute(() => _cyclingSuggestions = false);

            _suggestionIndex = (_suggestionIndex + 1) % count;
        }

        private void ClearAutocomplete()
        {
            _autocompleteContainer.RemoveFromClassList(BossyTheme.CliSuggestionsVisibleClass);
            _autocompleteContainer.Clear();
            _completionLabels.Clear();
            _completions = CompletionResult.Empty;
            _suggestionIndex = 0;
        }
    }
}
