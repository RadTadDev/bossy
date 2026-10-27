using System;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend;
using Bossy.Utils;

namespace Bossy.Execution
{
    /// <summary>
    /// A session container with state and a single front end.
    /// </summary>
    public class Session : IDisposable
    {
        /// <summary>
        /// The frontend this session reads from and writes to.
        /// </summary>
        public readonly ISessionEndpoint Endpoint;

        /// <summary>
        /// Where this session's UI lives, or null if it has none.
        /// </summary>
        internal readonly ISessionHost Host;

        private readonly BossyContext _context;
        private readonly BossyPermissions _permissions;
        private readonly CommandExecutor _commandExecutor;

        private CancellationTokenSource _commandCancellationSource;
        private readonly CancellationTokenSource _sessionCancellationSource = new();

        /// <summary>
        /// Creates a new session.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        /// <param name="permissions">The permissions manager.</param>
        /// <param name="endpoint">The frontend to read from and write to.</param>
        /// <param name="host">Where this session's UI lives, or null if it has none.</param>
        internal Session(BossyContext context, BossyPermissions permissions, ISessionEndpoint endpoint, ISessionHost host)
        {
            _context = context;
            _permissions = permissions;
            Endpoint = endpoint;
            Host = host;
            _commandExecutor = new CommandExecutor(context, permissions);
        }

        /// <summary>
        /// Runs a single graph.
        /// </summary>
        /// <param name="graph">The graph to run.</param>
        /// <param name="reader">An override reader to retarget from the default window.</param>
        /// <param name="writer">An override writer to retarget from the default window.</param>
        public async Task RunGraphAsync(CommandGraph graph, IReadable reader = null, IWriteable writer = null)
        {
            _commandCancellationSource = new CancellationTokenSource();
            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_commandCancellationSource.Token, _sessionCancellationSource.Token);

            await _commandExecutor.ExecuteAsync(graph, this, linkedSource.Token, reader, writer);
        }

        /// <summary>
        /// Starts the session execution REPL.
        /// </summary>
        /// <exception cref="BossyNotAdaptableException">Throws when a front end fails to give a command graph after one is requested.</exception>
        public async Task RunAsync()
        {
            while (!_sessionCancellationSource.IsCancellationRequested)
            {
                var response = await Endpoint.ReadAsync(typeof(CommandGraph), _sessionCancellationSource.Token);

                // Require direct graphs from the front ends, no adapting here because the parser should be used
                if (response is not CommandGraph graph)
                {
                    throw new BossyNotAdaptableException("All front ends must return command graphs where queried for one.");
                }

                _commandCancellationSource = new CancellationTokenSource();
                var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_commandCancellationSource.Token, _sessionCancellationSource.Token);

                await _commandExecutor.ExecuteAsync(graph, this, linkedSource.Token);
            }
        }

        /// <summary>
        /// Cancels the current command.
        /// </summary>
        public void CancelCommand()
        {
            // No command is running
            if (_commandCancellationSource == null) return;

            if (_commandCancellationSource.Token.CanBeCanceled)
            {
                _commandCancellationSource.Cancel();
            }
        }

        /// <summary>
        /// Creates a new session that shares this session's context and permissions.
        /// </summary>
        /// <param name="endpoint">The frontend of the new session.</param>
        /// <param name="host">Where the new session's UI lives.</param>
        /// <returns>The cloned session.</returns>
        internal Session Clone(ISessionEndpoint endpoint, ISessionHost host)
        {
            return new Session(_context, _permissions, endpoint, host);
        }

        /// <summary>
        /// Executes a command string.
        /// </summary>
        /// <param name="graph">The graph to execute.</param>
        /// <param name="token">The cancellation token.</param>
        /// <param name="input">An input source.</param>
        /// <param name="output">An output source.</param>
        public async Task ExecuteAsync(CommandGraph graph, CancellationToken token, IReadable input = null, IWriteable output = null)
        {
            await _commandExecutor.ExecuteAsync(graph, this, token, input, output);
        }

        /// <summary>
        /// Safely disposes this session's managed resources.
        /// </summary>
        public void Dispose()
        {
            _commandCancellationSource?.Cancel();
            _sessionCancellationSource.Cancel();
            _commandCancellationSource?.Dispose();
            _sessionCancellationSource.Dispose();
        }
    }
}
