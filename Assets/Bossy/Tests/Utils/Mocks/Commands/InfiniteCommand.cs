using System;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Shell;

namespace Bossy.Tests.Utils.Commands
{
    public enum InfiniteOperation
    {
        Delay,
        Write,
        Read
    }
    
    public class InfiniteCommand : ICommand
    {
        private readonly InfiniteOperation _operation;
        private readonly Action _onStarted;
        
        public InfiniteCommand(Action onStarted, InfiniteOperation infiniteOperation)
        {
            _operation = infiniteOperation;
            _onStarted = onStarted;
        }
        
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            _onStarted?.Invoke();

            while (true)
            {
                switch (_operation)
                {
                    case InfiniteOperation.Delay:
                        await ctx.Delay(TimeSpan.FromMilliseconds(10));
                        break;
                    case InfiniteOperation.Write:
                        ctx.Write("Hello, world!");
                        break;
                    case InfiniteOperation.Read:
                        await ctx.ReadAsync<object>();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // ReSharper disable once FunctionNeverReturns
        }
    }
}