using System;
using System.Collections.Generic;
using Bossy.Settings;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend.Parsing;
using Bossy.Shell;
using Bossy.Utils;
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
        
        private TextField _input;
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

        public VisualElement CreateView()
        {
            var root = ContentViewUtility.GetRootFromUxml("BossyCli");
            
            _input = root.Q<TextField>("input-field");
            _input.selectAllOnFocus = false;
            _input.selectAllOnMouseUp = false;
            _input.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (_blockInput) return;
                
                if (_inputSettings.ToggleMainHost.IsAsserted(evt))
                {
                    _signaler.ReleaseFocus();
                    _input.focusController.IgnoreEvent(evt);
                    evt.StopPropagation();
                }
                else if (_inputSettings.SubmitCommand.IsAsserted(evt))
                {
                    Submit();
                }
                else if (_inputSettings.CancelCommand.IsAsserted(evt))
                {
                    _input.value = string.Empty;
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

        public async Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            _requestingCommand = requestedType == typeof(CommandGraph);
            
            _readSource = new TaskCompletionSource<object>();
            token.Register(() => _readSource.TrySetCanceled()); 

            var result = await _readSource.Task;
            
            return result;
        }

        public void Submit()
        {
            var line = _input.value;
            object result = line;
            
            _input.value = string.Empty;
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
            _input.focusable = true;
            _input.schedule.Execute(() =>
            {
                _input.Focus();
                _input.schedule.Execute(() =>
                {
                    _input.value = _cachedInput;
                    _input.schedule.Execute(() =>
                    {
                        _input.cursorIndex = _cachedInput.Length;
                        _input.selectIndex = _cachedInput.Length;
                        _blockInput = false;
                    });
                });
            });
        }

        public void Defocus()
        {
            _cachedInput = _input.value;
            _input.focusable = false;
        }

        private void FocusInput()
        {
            _input?.schedule.Execute(() => _input?.Focus());
        }
    }
}