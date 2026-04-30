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

        private TypeAdapterRegistry _adapterRegistry;
        private Action<Session, CommandGraph> _createCommandSession;
        private CancellationTokenSource _commandSource;
        private readonly CancellationTokenSource _sessionSource = new();
        public readonly SessionSpace Space;
        
        public Session(Bridge bridge, TypeAdapterRegistry adapterRegistry, Action<Session, CommandGraph> createCommandSession, SessionSpace space)
        {
            Bridge = bridge;
            _adapterRegistry = adapterRegistry;
            _commandExecutor = new CommandExecutor(this, adapterRegistry);
            _createCommandSession = createCommandSession;
            Space = space;
        }

        /// <summary>
        /// Runs a single graph.
        /// </summary>
        /// <param name="graph">The graph to run.</param>
        public async Task RunGraphAsync(CommandGraph graph)
        {
            _commandSource = new CancellationTokenSource();
            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_commandSource.Token, _sessionSource.Token);

            await _commandExecutor.ExecuteAsync(graph, this, linkedSource.Token);
        }
        
        /// <summary>
        /// Starts the session execution REPL.
        /// </summary>
        /// <exception cref="BossyNotAdaptableException">Throws when a front end fails to give a command graph after one is requested.</exception>
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

        public void CreateCommandSession(CommandGraph graph)
        {
            _createCommandSession?.Invoke(this, graph);
        }
        
        public void CancelCommand()
        {
            if (_commandSource.Token.CanBeCanceled)
            {
                _commandSource.Cancel();
            }
        }

        public Session Clone(Bridge bridge)
        {
            var session = new Session(bridge, _adapterRegistry, _createCommandSession, Space);
            
            // TODO: Set other properties
            
            return session;
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