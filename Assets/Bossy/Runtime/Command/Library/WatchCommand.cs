using System;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Execution;
using Bossy.Frontend;

namespace Bossy.Runtime.Command.Library
{
    [Command("watch", "Repeats another command on an interval.")]
    public class WatchCommand : ICommand
    {
        [Range(0.001f, float.PositiveInfinity)]
        [Switch('i', "The interval in seconds.")]
        private float _interval = 1;
        
        [Switch('r', "Repeat count. -1 means indefinitely.")]
        private int _repeatCount = -1;

        [Switch('o', "Overwrite last line instead of appending.")]
        private bool _overwrite = true;
        
        [Variadic("The command to repeat.")] 
        private string[] _command;
        
        private CommandContext _ctx;

        private IModifiableOutputBuffer _buffer;
        
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            _ctx = ctx;
            
            var command = string.Join(" ", _command);

            if (_overwrite)
            {
                if (ctx.Capabilities is IModifiableOutputBuffer buffer)
                {
                    _buffer = buffer;
                }
                else
                {
                    ctx.WriteWarning("Current UI does not support overwriting last buffer output. Appending instead.");
                }
            }
            
            while (_repeatCount-- != 0)
            {
                var pipe = new ObservablePipe(Write);
                
                await ctx.ExecuteAsync(command, pipe);
                
                await ctx.Delay(TimeSpan.FromSeconds(_interval));
            }

            return CommandStatus.Ok;
        }

        private void Write(object value)
        {
            var line = $"[Watch] {value}";
            
            if (_buffer != null)
            {
                _buffer.Overwrite(line);
            }
            else
            {
                _ctx.Write(line);
            }
        }
    }
}