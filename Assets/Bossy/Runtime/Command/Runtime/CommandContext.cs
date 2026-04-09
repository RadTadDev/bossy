using System.Threading;
using System.Threading.Tasks;

namespace Bossy.Command
{
    public sealed class CommandContext : SimpleCommandContext
    {
        private readonly CancellationToken _token;
        
        public CommandContext(CancellationToken token)
        {
            // Internally, we use this token to throw when canceled so the user never needs to.
            _token = token;
        }

        /// <summary>
        /// Get the cancellation token for this command.
        /// </summary>
        public CancellationToken CancellationToken => _token;
        
        public async Task<object> Read()
        {
            _token.ThrowIfCancellationRequested();
            
            await Task.CompletedTask;
            return null;
        }
        
        public async Task Write(object value)
        {
            _token.ThrowIfCancellationRequested();
            
            await Task.CompletedTask;
        }
        
        public async Task Delay(object value)
        {
            _token.ThrowIfCancellationRequested();
            
            await Task.CompletedTask;
        }
    }
}