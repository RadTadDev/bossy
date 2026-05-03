using System;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend;
using Bossy.Utils;

namespace Bossy.Session
{
    /// <summary>
    /// A session container with state and a single front end.
    /// </summary>
    public class Session : IDisposable
    {
        /// <summary>
        /// The bridge to the front end.
        /// </summary>
        public readonly Bridge Bridge;
        
        /// <summary>
        /// The space this session is running in.
        /// </summary>
        public readonly SessionSpace Space;
        
        private readonly CommandExecutor _commandExecutor;

        private readonly BossyContext _context;
        private readonly Action<Session, CommandGraph> _createCommandSession;
        
        private CancellationTokenSource _commandSource;
        private readonly CancellationTokenSource _sessionSource = new();
        
        /// <summary>
        /// Creates a new session.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        /// <param name="bridge">The bridge to the frontend.</param>
        /// <param name="createCommandSession">An action to invoke when this session wants to create a new one.</param>
        /// <param name="space">The space this session is running in.</param>
        public Session(BossyContext context, Bridge bridge, Action<Session, CommandGraph> createCommandSession, SessionSpace space)
        {
            _context = context;
            Bridge = bridge;
            _commandExecutor = new CommandExecutor(this, context);
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

        /// <summary>
        /// Creates a command session which simply rungs the command graph and nothing else.
        /// </summary>
        /// <param name="graph">The graph to run.</param>
        public void CreateCommandSession(CommandGraph graph)
        {
            _createCommandSession?.Invoke(this, graph);
        }
        
        /// <summary>
        /// Cancels the current command.
        /// </summary>
        public void CancelCommand()
        {
            // No command is running
            if (_commandSource == null) return;
            
            if (_commandSource.Token.CanBeCanceled)
            {
                _commandSource.Cancel();
            }
        }

        /// <summary>
        /// Clones this session.
        /// </summary>
        /// <param name="bridge">The bridge to the new frontend.</param>
        /// <returns>The cloned session.</returns>
        public Session Clone(Bridge bridge)
        {
            var session = new Session(_context, bridge, _createCommandSession, Space);
            return session;
        }
        
        /// <summary>
        /// Safely disposes this session's managed resources.
        /// </summary>
        public void Dispose()
        {
            _commandSource?.Cancel();
            _sessionSource.Cancel();
            _commandSource?.Dispose();
            _sessionSource.Dispose();
        }
    }
}