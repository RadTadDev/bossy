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
    internal class CliView : IVisualEndpointEndpoint,
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

        private const string LineClass = "bossy-cli__line";
        private const string InputRowFocusedClass = "bossy-cli__input-row--focused";
        private const string SuggestionsVisibleClass = "bossy-cli__suggestions--visible";
        private const string SuggestionClass = "bossy-cli__suggestion";
        private const string SelectedSuggestionClass = "bossy-cli__suggestion--selected";
        private const string HintSuggestionClass = "bossy-cli__suggestion--hint";
        private const string ErrorSuggestionClass = "bossy-cli__suggestion--error";

        private readonly BossyContext _context;
        private readonly InputVisibility _inputVisibility;

        private readonly Dictionary<Type, ICliDisplayAdapter> _displayAdapters = new();

        // Output
        private readonly ListView _output;
        private readonly List<string> _outputBuffer = new() { string.Empty };

        // Input
        private readonly VisualElement _inputRow;
        private readonly TextField _input;
        private TaskCompletionSource<object> _readSource;
        private bool _reading;
        private bool _requestingCommand;
        private ICliDisplayAdapter _currentReadOwner;

        // History
        private readonly CommandHistory _history;
        private int _historyIndex;

        // Autocomplete
        private readonly AutocompleteEngine _autocomplete;
        private readonly VisualElement _autocompleteContainer;
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
        public CliView(BossyContext context, InputVisibility inputVisibility, CommandHistory history)
        {
            _context = context;
            _inputVisibility = inputVisibility;
            _history = history;
            _historyIndex = _history.Lines.Count;
            _autocomplete = new AutocompleteEngine(_context, context.Settings.BossyCliSettings.ToOperatorList());
            _displayAdapters[typeof(OptionsPrompt)] = new OptionsPromptDisplayAdapter();

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
            _outputBuffer.Add(Format.Render(DisplayObject(value)));
            _output.RefreshItems();
            _output.ScrollToItem(_outputBuffer.Count - 1);
        }

        public async Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            _requestingCommand = requestedType == typeof(CommandGraph);
            _readSource = new TaskCompletionSource<object>();

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
            _outputBuffer[^1] = Format.Render(value.ToString());
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

            _input.RegisterCallback<FocusInEvent>(_ => _inputRow.AddToClassList(InputRowFocusedClass));
            _input.RegisterCallback<FocusOutEvent>(_ => _inputRow.RemoveFromClassList(InputRowFocusedClass));

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
                line.AddToClassList(LineClass);
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

        private string DisplayObject(object value)
        {
            if (_displayAdapters.TryGetValue(value.GetType(), out var adapter))
            {
                if (adapter.OwnsRead())
                {
                    _currentReadOwner = adapter;
                }
                return adapter.Display(value);
            }

            return value.ToString();
        }

        private void Submit()
        {
            var line = _input.value;

            Write($"{Format.Color($"{_promptHeader}>", Format.Accent)} {line}");

            object result = line;

            if (_currentReadOwner != null)
            {
                result = _currentReadOwner.Read(line);
                _currentReadOwner = null;
            }

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
            _readSource.TrySetResult(result);
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

            ShowSuggestions(_autocomplete.Suggest(evt.newValue, _input.cursorIndex));
        }

        private void ShowSuggestions(IEnumerable<Suggestion> suggestions)
        {
            ClearAutocomplete();

            var all = suggestions as Suggestion[] ?? suggestions.ToArray();
            var first = all.FirstOrDefault();

            if (first == null)
            {
                return;
            }

            _autocompleteContainer.AddToClassList(SuggestionsVisibleClass);

            // If the first suggestion is a hint or error, that's all we show
            if (first.IsHint || first.IsError)
            {
                _autocompleteContainer.Add(MakeSuggestionLabel(first, first.IsHint ? HintSuggestionClass : ErrorSuggestionClass));
                return;
            }

            foreach (var suggestion in all)
            {
                _autocompleteContainer.Add(MakeSuggestionLabel(suggestion, null));
            }
        }

        private static Label MakeSuggestionLabel(Suggestion suggestion, string modifierClass)
        {
            var label = new Label(suggestion.DisplayText) { userData = suggestion };
            label.AddToClassList(SuggestionClass);

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
                ShowSuggestions(_autocomplete.Suggest(_input.value, _input.cursorIndex));
                return;
            }

            // Hints and errors cannot be applied
            var suggestion = (Suggestion)_autocompleteContainer[0].userData;
            if (suggestion.IsError || suggestion.IsHint)
            {
                return;
            }

            var count = _autocompleteContainer.childCount;
            var previous = _autocompleteContainer[(_suggestionIndex - 1 + count) % count];
            previous.RemoveFromClassList(SelectedSuggestionClass);

            var current = _autocompleteContainer[_suggestionIndex];
            current.AddToClassList(SelectedSuggestionClass);

            // Changing the input would otherwise recompute the suggestions being cycled. The change event
            // can arrive after this key event finishes, so the flag is cleared on the next tick.
            _cyclingSuggestions = true;
            SetInput(((Suggestion)current.userData).FullText);
            _input.schedule.Execute(() => _cyclingSuggestions = false);

            _suggestionIndex = (_suggestionIndex + 1) % count;
        }

        private void ClearAutocomplete()
        {
            _autocompleteContainer.RemoveFromClassList(SuggestionsVisibleClass);
            _autocompleteContainer.Clear();
            _suggestionIndex = 0;
        }
    }
}
