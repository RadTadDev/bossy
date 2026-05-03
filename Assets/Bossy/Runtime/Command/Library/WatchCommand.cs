using System;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Execution;

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
        
        [Variadic("The command to repeat.")] 
        private string[] _command;
        
        private CommandContext _ctx;
        
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            _ctx = ctx;
            
            var command = string.Join(" ", _command);

            while (_repeatCount-- != 0)
            {
                var pipe = new ObservablePipe(Write);
                
                await ctx.ExecuteAsync(command, pipe);
                
                await ctx.Delay(TimeSpan.FromSeconds(_interval));
            }

            return CommandStatus.Ok;
        }

        public void Write(object value)
        {
            _ctx.Write($"[Watch] {value}");
        }
    }
}