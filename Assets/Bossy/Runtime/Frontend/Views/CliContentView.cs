using System;
using System.Collections.Generic;
using Bossy.Settings;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend.Parsing;
using Bossy.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    internal class CliContentView : IContentView
    {
        private readonly BossyCliSettings _cliSettings;
        private readonly BossyInputSettings _inputSettings;

        private TaskCompletionSource<object> _readSource;

        private readonly List<string> _outputBuffer = new() { string.Empty };
        
        protected TextField Input;
        private ListView _view;

        private string _cachedInput = string.Empty;

        private bool _blockInput;
        
        private readonly Parser _parser;
        private bool _requestingCommand;
        private Signaler _signaler;
        
        public CliContentView(Parser parser, BossyCliSettings cliSettings, BossyInputSettings inputSettings)
        {
            _parser = parser;
            _cliSettings = cliSettings;
            _inputSettings = inputSettings;
        }

        public virtual VisualElement CreateView()
        {
            var root = ContentViewUtility.GetRootFromUxml("BossyCli");
            
            Input = root.Q<TextField>("input-field");
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
                else if (_inputSettings.CancelCommand.IsAsserted(evt))
                {
                    Input.value = string.Empty;
                    _cachedInput = string.Empty;
                    _signaler.CancelCommand();
                }
                else
                {
                    FocusInput();
                }
            },TrickleDown.TrickleDown);
            
            FocusInput();
            
            _view = root.Q<ListView>("output-list");
            _view.itemsSource = _outputBuffer;
            _view.makeItem = () =>
            {
                var label = new Label
                {
                    style =
                    {
                        color = Color.white
                    }
                };
                return label;
            };
            _view.bindItem = (ele, i) => ((Label)ele).text = _outputBuffer[i];
            
            return root;
        }

        public void Write(object value)
        {
            var line = $"> {value}";
            
            _outputBuffer.Add(line);
            _view.RefreshItems();
            _view.ScrollToItem(_outputBuffer.Count - 1);
        }

        public virtual async Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            _requestingCommand = requestedType == typeof(CommandGraph);
            
            _readSource = new TaskCompletionSource<object>();
            token.Register(() => _readSource.TrySetCanceled()); 

            var result = await _readSource.Task;
            
            return result;
        }

        public void Submit()
        {
            var line = Input.value;
            object result = line;
            
            Input.value = string.Empty;
            FocusInput();
        
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
                
                var parseResult = _parser.Parse(line, operatorList);
                if (!parseResult.TryGetGraph(out var graph))
                {
                    Write(parseResult.Message);
                    return;
                }

                result = graph;
            }
            
            _readSource.TrySetResult(result);
        }

        public void SetSignaler(Signaler signaler)
        {
            _signaler = signaler;
        }

        public void Focus()
        {
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

        public void Defocus()
        {
            _cachedInput = Input.value;
            Input.focusable = false;
        }

        private void FocusInput()
        {
            Input?.schedule.Execute(() => Input?.Focus());
        }
    }
}