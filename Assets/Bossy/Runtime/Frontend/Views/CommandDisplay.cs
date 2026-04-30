using System;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend.Parsing;
using Bossy.Settings;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    internal class CommandDisplay : CliContentView
    {
        public CommandDisplay(Parser parser, BossyCliSettings cliSettings, BossyInputSettings inputSettings) : base(parser, cliSettings, inputSettings) { }

        public override VisualElement CreateView()
        {
            var view = base.CreateView();
            
            Input.style.display = DisplayStyle.None;
            
            return view;
        }

        public override async Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            Input.style.display = DisplayStyle.Flex;
            
            var obj = await base.ReadAsync(requestedType, token);
            
            Input.style.display = DisplayStyle.None;
            
            return obj;
        }
    }
}