using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Bossy.Frontend
{
    /// <summary>
    /// Command display window view. This is a CLI that only shows the input bar when input is requested.
    /// </summary>
    internal class CommandDisplay : CliUserInterfaceView
    {
        /// <summary>
        /// Creates a new command display view.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        public CommandDisplay(BossyContext context) : base(context) { }

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