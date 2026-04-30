using System;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend;
using Bossy.Frontend.Parsing;
using Bossy.Utils;

namespace Bossy.Shell
{
    /// <summary>
    /// A session container with state and a single front end.
    /// </summary>
    public class Session : ISafeSession, IDisposable
    {
        public readonly Bridge Bridge;
        
        private readonly CommandExecutor _commandExecutor;

        private CancellationTokenSource _commandSource;
        private readonly CancellationTokenSource _sessionSource = new();
        
        public Session(Bridge bridge, TypeAdapterRegistry adapterRegistry)
        {
            Bridge = bridge;
            _commandExecutor = new CommandExecutor(adapterRegistry);
        }
        
        public async Task RunAsync()
        {
            while (!_sessionSource.IsCancellationRequested)
            {
                var response = await Bridge.ReadAsync(typeof(CommandGraph), _sessionSource.Token);
                
                // Require direct graphs from the front ends, no adapting here because the parser should be used
                if (response is not CommandGraph graph)
                {
                    throw new BossyNotAdaptableException("All front ends must return command graphs where queried for one.");
                }
                
                _commandSource = new CancellationTokenSource();
                var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_commandSource.Token, _sessionSource.Token);

                await _commandExecutor.ExecuteAsync(graph, this, linkedSource.Token);
            }
        }

        public void CancelCommand()
        {
            if (_commandSource.Token.CanBeCanceled)
            {
                _commandSource.Cancel();
            }
        }

        public void Dispose()
        {
            _commandSource?.Cancel();
            _sessionSource.Cancel();
            _commandSource?.Dispose();
            _sessionSource.Dispose();
        }
    }
}