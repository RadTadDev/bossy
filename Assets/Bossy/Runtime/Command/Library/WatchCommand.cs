using System;
using System.Threading.Tasks;
using Bossy.Command;
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
        
        [Positional(0, "The command to repeat. Quote it to give it arguments.")]
        private CommandString _command;
        
        
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            IModifiableOutputBuffer buffer = null;
            if (_overwrite)
            {
                if (ctx.Endpoint is IModifiableOutputBuffer b)
                {
                    buffer = b;
                }
                else
                {
                    ctx.WriteWarning("Current UI does not support overwriting last buffer output. Appending instead.");
                }
            }
            
            while (_repeatCount-- != 0)
            {
                await foreach (var line in ctx.ExecuteAndRead<object>(_command))
                {
                    if (buffer != null)
                    {
                        buffer.Overwrite($"[watch]: {line}");                        
                    }
                    else
                    {
                        ctx.Write($"[watch]: {line}");                    
                    }
                }
               
                await ctx.Delay(TimeSpan.FromSeconds(_interval));
            }

            return CommandStatus.Ok;
        }
    }
}