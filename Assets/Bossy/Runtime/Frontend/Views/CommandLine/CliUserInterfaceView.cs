using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Frontend.Parsing;
using Bossy.Execution;
using Bossy.Frontend.Autocomplete;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bossy.Frontend
{
    /// <summary>
    /// A default command line interface to Bossy.
    /// </summary>
    internal class CliUserInterfaceView : IUserInterfaceView,
        IHistorical,
        IClearable,
        IModifiableOutputBuffer,
        IAliasCapability,
        IModifiablePromptHeader
    {
        private Signaler _signaler;
        private readonly BossyContext _context;

        private readonly Dictionary<string, string> _aliases = new();
        private readonly Dictionary<Type, ICliDisplayAdapter> _displayAdapters = new();

        // Output
        private ListView _view;
        private TaskCompletionSource<object> _readSource;
        private readonly List<string> _outputBuffer = new() { string.Empty };

        // Input
        protected TextField Input;
        private string _cachedInput = string.Empty;
        private bool _blockInput;
        private bool _reading;
        private bool _requestingCommand;
        private ICliDisplayAdapter _currentReadOwner;
        
        // History
        private static List<string> _historyBuffer;
        private int _historyIndex;
        private static bool _historyLoaded;
        private string _historyFilePath = Path.Combine(Application.persistentDataPath, "bossy_cli_history.txt");

        // Autocomplete
        private int _suggestionIndex;
        private bool _cyclingSuggestions;
        private AutocompleteEngine _autocomplete;
        private VisualElement _autocompleteContainer;

        // Prompt header
        private Label _promptHeaderElement;
        private string _promptHeader = string.Empty;
        
        /// <summary>
        /// Creates a Cli interface.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        public CliUserInterfaceView(BossyContext context)
        {
            _context = context;

            if (!_historyLoaded)
            {
                _historyLoaded = true;

                if (!File.Exists(_historyFilePath))
                {
                    File.Create(_historyFilePath).Dispose();
                }
                
                _historyBuffer = File.ReadAllLines(_historyFilePath).ToList();
            }
            
            _historyIndex = _historyBuffer.Count;
            
            Application.quitting += OnBeforeReload;
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
            EditorApplication.quitting += OnBeforeReload;
#endif

            _autocomplete = new AutocompleteEngine(_context, context.Settings.BossyCliSettings.ToOperatorList());
            
            // Register adapters
            _displayAdapters[typeof(OptionsPrompt)] = new OptionsPromptDisplayAdapter();
        }

        public virtual VisualElement CreateView()
        {
            var root = ContentViewUtility.GetRootFromUxml("BossyCli");

            _promptHeaderElement = (Label)root.Q("prompt-label");
            
            Input = root.Q<TextField>("input-field");
            
            // This is necessary for removing padding
            Input.Q<VisualElement>("unity-text-input").style.paddingTop = 0;
            Input.Q<VisualElement>("unity-text-input").style.paddingBottom = 0;
            Input.Q<VisualElement>("unity-text-input").style.paddingLeft = 0;
            Input.Q<VisualElement>("unity-text-input").style.paddingRight = 0;
            
            Input.parent.focusable = true;
            Input.style.fontSize = 14f;
            Input.selectAllOnFocus = false;
            Input.selectAllOnMouseUp = false;
            Input.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (_blockInput) return;
                
                if (_context.Settings.BossyInputSettings.ToggleMainHost.IsAsserted(evt))
                {
                    _signaler.ReleaseFocus();
                    Input.focusController?.IgnoreEvent(evt);
                    evt.StopPropagation();
                }
                else if (_context.Settings.BossyInputSettings.SubmitCommand.IsAsserted(evt))
                {
                    Submit();
                }
                else if (_context.Settings.BossyInputSettings.HistoryBack.IsAsserted(evt))
                {
                    HistoryBack();
                }
                else if (_context.Settings.BossyInputSettings.HistoryForward.IsAsserted(evt))
                {
                    HistoryForward();
                }
                else if (_context.Settings.BossyInputSettings.CycleSuggestions.IsAsserted(evt))
                {
                    CycleSuggestions();
                    evt.StopImmediatePropagation();
                }
                else
                {
                    // Reset the history index on any normal character press
                    _historyIndex = _historyBuffer.Count;
                }
            },TrickleDown.TrickleDown);

            Input.RegisterValueChangedCallback(OnValueUpdated);
            
            FocusInput();
            
            _view = root.Q<ListView>("output-list");
            _view.itemsSource = _outputBuffer;
            _view.makeItem = () =>
            {
                var label = new Label
                {
                    style =
                    {
                        color = Color.white,
                        whiteSpace = WhiteSpace.Normal,
                        fontSize = 15
                    },
                    pickingMode = PickingMode.Ignore,
                };
                return label;
            };
            _view.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _view.bindItem = (ele, i) => ((Label)ele).text = _outputBuffer[i];

            _autocompleteContainer = root.Q("auto-complete");
            
            return root;
        }
        
        public void Write(object value)
        {
            var line = DisplayObject(value);
            
            line = Format.Render(line);
            
            _outputBuffer.Add(line);
            _view.RefreshItems();
            _view.ScrollToItem(_outputBuffer.Count - 1);
        }

        private string DisplayObject(object value)
        {
            // The Gui can do this too, but even better with control widgets and such
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
        
        public virtual async Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            _reading = true;
            
            Input.focusable = true;
            Input.value = string.Empty;
            Input.enabledSelf = true;
            
            FocusInput();
            
            _requestingCommand = requestedType == typeof(CommandGraph);
            
            _readSource = new TaskCompletionSource<object>();
            token.Register(() => _readSource.TrySetCanceled()); 

            var result = await _readSource.Task;
            
            return result;
        }
        
        private void Submit()
        {
            var line = Input.value;

            Write($"{_promptHeader}> {line}");
            
            object result = line;

            if (_currentReadOwner != null)
            {
                result = _currentReadOwner.Read(line);
                _currentReadOwner = null;
            }
            
            AppendHistory(line);
            _historyIndex = _historyBuffer.Count;
            
            Input.value = string.Empty;
            FocusInput();
            ClearAutocomplete();
        
            if (_requestingCommand)
            {
                // Remake this each time to re-apply settings that could change
                var operatorList = new OperatorList
                (
                    _context.Settings.BossyCliSettings.ThenOperator,
                    _context.Settings.BossyCliSettings.AndOperator,
                    _context.Settings.BossyCliSettings.OrOperator,
                    _context.Settings.BossyCliSettings.PipeOperator,
                    _context.Settings.BossyCliSettings.WindowOperator
                );
                
                var parseResult = _context.Parser.Parse(line, operatorList, _aliases);
                if (parseResult.IsEmpty)
                {
                    Write("");
                    return;
                }
                if (!parseResult.TryGetGraph(out var graph))
                {
                    Write(Format.Error(parseResult.Message));
                    return;
                }

                result = graph;
            }
            
            Input.parent.Focus();
            Input.focusable = false;
            Input.enabledSelf = false;
            Input.value = "Executing...";
            
            _reading = false;
            _readSource.TrySetResult(result);
        }

        public void SetSignaler(Signaler signaler)
        {
            _signaler = signaler;
        }

        public void OnFocus()
        {
            if (!_reading)
            {
                return; 
            }
            
            _blockInput = true;
            Input.focusable = true;
            Input.schedule.Execute(() =>
            {
                Input.Focus();
                Input.schedule.Execute(() =>
                {
                    Input.value = _cachedInput;
                    Input.schedule.Execute(() =>
                    {
                        Input.cursorIndex = _cachedInput.Length;
                        Input.selectIndex = _cachedInput.Length;
                        _blockInput = false;
                    });
                });
            });
        }

        private void SetInput(string value, bool cyclingSuggestions = false)
        {
            if (cyclingSuggestions)
            {
                _cyclingSuggestions = true;
            }
            
            _cachedInput = value;
            Input.value = value;
            
            Input.schedule.Execute(() =>
            {
                Input.cursorIndex = _cachedInput.Length;
                Input.selectIndex = _cachedInput.Length;
                _cyclingSuggestions = false;
            });
        }
        
        public void OnDefocus()
        {
            _cachedInput = Input.value;
            Input.Blur();
        }

        public void OnCommandCanceled()
        {
            _cachedInput = string.Empty;
            Input.value = string.Empty;
        }

        private void FocusInput()
        {
            Input?.schedule.Execute(() =>
            {
                Input.Focus();
                Input.cursorIndex = _cachedInput.Length;
                Input.selectIndex = _cachedInput.Length;
            });
        }

        private void OnBeforeReload()
        {
            File.Delete(_historyFilePath);

            if (_historyBuffer.Count > 0)
            {
                File.WriteAllLines(_historyFilePath, _historyBuffer);
            }
        }

        private void HistoryBack()
        {
            if (_historyIndex == 0 || _historyBuffer.Count == 0) return;

            ClearAutocomplete();
            
            _historyIndex--;
            
            SetInput(_historyBuffer[_historyIndex]);
        }

        private void HistoryForward()
        {
            if (_historyIndex >= _historyBuffer.Count - 1 || _historyBuffer.Count == 0) return;
            
            ClearAutocomplete();
            
            _historyIndex++;
            
            SetInput(_historyBuffer[_historyIndex]);
        }

        private void AppendHistory(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            if (_historyBuffer.LastOrDefault() == line)
            {
                return;
            }
            
            _historyBuffer.Add(line);
        }

        public IEnumerable<string> GetHistory()
        {
            return _historyBuffer;
        }

        public void ClearHistory()
        {
            _historyBuffer.Clear();
        }

        public void Clear()
        {
            ClearAutocomplete();
            _outputBuffer.Clear();
            _outputBuffer.Add(string.Empty);
            _view.RefreshItems();
        }

        public void Overwrite(object value)
        {
            if (_outputBuffer.Count == 0)
            {
                Write(value);
            }
            else
            {
                var line = value.ToString();
            
                line = Format.Render(line);
            
                _outputBuffer[^1] = line;
                _view.RefreshItems();
                _view.ScrollToItem(_outputBuffer.Count - 1);
            }
        }

        public Dictionary<string, string> GetAliases() => _aliases;

        public bool AssignAlias(string alias, string value)
        {
            alias = alias.Trim();

            if (alias.Any(c => !char.IsLetter(c))) return false;
            _aliases[alias] = value;
            return true;
        }

        public bool DeleteAlias(string alias)
        {
            return _aliases.Remove(alias);
        }

        private void OnValueUpdated(ChangeEvent<string> evt)
        {
            if (!_requestingCommand || !_reading || _cyclingSuggestions || _blockInput) return;

            if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                ClearAutocomplete();
                return;
            }
            
            var suggestions = _autocomplete.Suggest(evt.newValue, Input.cursorIndex);

            ShowSuggestions(suggestions);
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
            
            // If the first suggestion is a hint or error, thats all we care about
            if (first.IsHint)
            {
                var label = new Label(first.DisplayText)
                {
                    userData = first,
                    style =
                    {
                        fontSize = 13.5f,
                        color = Format.LightBlue,
                    }
                };
                _autocompleteContainer?.Add(label);
                return;
            }
            if (first.IsError)
            {
                var label = new Label(first.DisplayText)
                {
                    userData = first,
                    style =
                    {
                        fontSize = 13.5f,
                        color = Format.Red,
                    }
                };
                _autocompleteContainer?.Add(label);
                return;
            }
            
            foreach (var s in all)
            {
                var label = new Label(s.DisplayText)
                {
                    userData = s,
                    style =
                    {
                        fontSize = 13.5f,
                        color = Format.White
                    }
                };
                
                _autocompleteContainer?.Add(label);
            }
        }

        private void CycleSuggestions()
        {
            // If autocomplete is not open, try opening it
            if (_autocompleteContainer.childCount == 0)
            {
                var suggestions = _autocomplete.Suggest(Input.value, Input.cursorIndex);
                ShowSuggestions(suggestions);
                return;
            }

            // Hints and errors are not applyable
            var suggestion = (Suggestion)_autocompleteContainer[0].userData;
            if (suggestion.IsError || suggestion.IsHint)
            {
                return;
            }
            
            var prev = (Label)_autocompleteContainer[(_suggestionIndex - 1 + _autocompleteContainer.childCount) % _autocompleteContainer.childCount];
            prev.style.backgroundColor = StyleKeyword.Null;
            prev.style.color = Color.white;

            var line = (Label)_autocompleteContainer[_suggestionIndex];
            line.style.backgroundColor = Format.DarkBlue;
            
            SetInput(((Suggestion)line.userData).FullText, true);

            _suggestionIndex = (_suggestionIndex + 1) % _autocompleteContainer.childCount;
        }

        private void ClearAutocomplete()
        {
            _autocompleteContainer?.Clear();
            _suggestionIndex = 0;
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
        
        public void ResetCapabilities()
        {
            ResetHeader();
        }
    }
}