using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bossy.Settings;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Frontend.Parsing;
using Bossy.Execution;
using Bossy.Frontend.Autocomplete;
using Bossy.Schema.Registry;
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
        private readonly BossyCliSettings _cliSettings;
        private readonly BossyInputSettings _inputSettings;

        private TaskCompletionSource<object> _readSource;

        private readonly Dictionary<string, string> _aliases = new();
        private readonly Dictionary<Type, CliDisplayAdapter> _displayAdapters = new();
        
        private readonly List<string> _outputBuffer = new() { string.Empty };
        
        protected TextField Input;
        private ListView _view;

        private string _cachedInput = string.Empty;

        private bool _blockInput;

        private Label _promptHeaderElement;
        private string _promptHeader = string.Empty;
        
        private readonly Parser _parser;
        private bool _reading;
        private bool _requestingCommand;
        private Signaler _signaler;

        private static bool _historyLoaded;
        private string _historyFilePath = Path.Combine(Application.persistentDataPath, "bossy_cli_history.txt");

        private int _suggestionIndex;
        private bool _cyclingSuggestions;
        private AutocompleteEngine _autocomplete;
        private VisualElement _autocompleteContainer;
        
        private static List<string> _historyBuffer;
        private int _historyIndex;
        
        
        /// <summary>
        /// Creates a Cli interface.
        /// </summary>
        /// <param name="parser">The parser.</param>
        /// <param name="registry">The schema registry.</param>
        /// <param name="cliSettings">The Cli settings.</param>
        /// <param name="inputSettings">The input settings.</param>
        public CliUserInterfaceView(Parser parser, SchemaRegistry registry, BossyCliSettings cliSettings, BossyInputSettings inputSettings)
        {
            _parser = parser;
            _cliSettings = cliSettings;
            _inputSettings = inputSettings;

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

            _autocomplete = new AutocompleteEngine(registry);
            
            // Register adapters
            _displayAdapters[typeof(OptionsPrompt)] = new OptionsPromptDisplayAdapter();
        }

        public virtual VisualElement CreateView()
        {
            var root = ContentViewUtility.GetRootFromUxml("BossyCli");

            _promptHeaderElement = (Label)root.Q("prompt-label");
            
            Input = root.Q<TextField>("input-field");
            Input.parent.focusable = true;
            Input.style.fontSize = 13.5f;
            Input.selectAllOnFocus = false;
            Input.selectAllOnMouseUp = false;
            Input.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (_blockInput) return;
                
                if (_inputSettings.ToggleMainHost.IsAsserted(evt))
                {
                    _signaler.ReleaseFocus();
                    Input.focusController.IgnoreEvent(evt);
                    evt.StopPropagation();
                }
                else if (_inputSettings.SubmitCommand.IsAsserted(evt))
                {
                    Submit();
                }
                else if (_inputSettings.HistoryBack.IsAsserted(evt))
                {
                    HistoryBack();
                }
                else if (_inputSettings.HistoryForward.IsAsserted(evt))
                {
                    HistoryForward();
                }
                else if (_inputSettings.CycleSuggestions.IsAsserted(evt))
                {
                    CycleSuggestions();
                    evt.StopImmediatePropagation();
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
                return adapter.Display(value);
            }

            return value.ToString();
        }
        
        public virtual async Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            _reading = true;
            Input.focusable = true;
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
                    _cliSettings.ThenOperator,
                    _cliSettings.AndOperator,
                    _cliSettings.OrOperator,
                    _cliSettings.PipeOperator,
                    _cliSettings.WindowOperator
                );
                
                var parseResult = _parser.Parse(line, operatorList, _aliases);
                if (!parseResult.TryGetGraph(out var graph))
                {
                    Write(Format.Error(parseResult.Message));
                    return;
                }

                result = graph;
            }
            
            Input.parent.Focus();
            Input.focusable = false;
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
            
            foreach (var s in suggestions)
            {
                var label = new Label(s.DisplayTest)
                {
                    userData = s.FullText,
                    style =
                    {
                        fontSize = 13.5f
                    }
                };
                
                _autocompleteContainer?.Add(label);
            }
        }

        private void CycleSuggestions()
        {
            if (_autocompleteContainer.childCount == 0) return;
            
            var prev = (Label)_autocompleteContainer[(_suggestionIndex - 1 + _autocompleteContainer.childCount) % _autocompleteContainer.childCount];
            prev.style.backgroundColor = StyleKeyword.Null;

            var line = (Label)_autocompleteContainer[_suggestionIndex];
            line.style.backgroundColor = Color.cyan;
            
            SetInput((string)line.userData, true);

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