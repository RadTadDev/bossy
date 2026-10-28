using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Frontend;
using Bossy.Utils;

namespace Bossy.Execution
{
    /// <summary>
    /// A session container with state and a single endpoint.
    /// </summary>
    public class Session : IDisposable
    {
        /// <summary>
        /// The endpoint this session reads from and writes to.
        /// </summary>
        public readonly IEndpoint Endpoint;

        /// <summary>
        /// Tells if this session is running in a window.
        /// </summary>
        public readonly bool IsWindowed;
        
        /// <summary>
        /// The command lines this session has entered. Command windows opened by this session share it.
        /// </summary>
        public CommandHistory History { get; }

        /// <summary>
        /// The aliases expanded before a command line is parsed. Command windows opened by this session share them.
        /// </summary>
        public Dictionary<string, string> Aliases { get; }

        private readonly BossyContext _context;
        private readonly BossyPermissions _permissions;
        private readonly GraphDispatcher _dispatcher;

        private CancellationTokenSource _commandCancellationSource;
        private readonly CancellationTokenSource _sessionCancellationSource = new();

        /// <summary>
        /// Creates a new session.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        /// <param name="permissions">The permissions manager.</param>
        /// <param name="endpoint">The endpoint to read from and write to.</param>
        /// <param name="host">Where this session's UI lives, or null if it has none.</param>
        /// <param name="history">The command history to record into.</param>
        /// <param name="aliases">The aliases to expand before parsing.</param>
        /// <param name="isWindowed">True if this session runs in a command window.</param>
        internal Session(BossyContext context, BossyPermissions permissions, IEndpoint endpoint, ISessionHost host, CommandHistory history, Dictionary<string, string> aliases, bool isWindowed)
        {
            _context = context;
            _permissions = permissions;
            Endpoint = endpoint;
            History = history;
            Aliases = aliases;
            IsWindowed = isWindowed;
            
            _dispatcher = new GraphDispatcher(context, permissions, endpoint, host);
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

            await _dispatcher.ExecuteAsync(graph, this, linkedSource.Token, reader, writer);
        }

        /// <summary>
        /// Starts the session execution REPL.
        /// </summary>
        /// <exception cref="BossyNotAdaptableException">Throws when an endpoint gives something other than a command line or graph after one is requested.</exception>
        public async Task RunAsync()
        {
            while (!_sessionCancellationSource.IsCancellationRequested)
            {
                var response = await Endpoint.ReadAsync(new ReadRequest(typeof(CommandGraph)), _sessionCancellationSource.Token);

                // Command lines are parsed here so aliases apply to every endpoint. Graphs are accepted as is.
                CommandGraph graph;
                switch (response)
                {
                    case CommandGraph direct:
                        graph = direct;
                        break;

                    case string line:
                        var parseResult = _context.Parser.Parse(line, _context.Settings.BossyCliSettings.ToOperatorList(), Aliases);

                        if (parseResult.IsEmpty) continue;

                        // Lines that fail to parse are kept too, so a typo can be fixed from history
                        History.Add(line);

                        if (!parseResult.TryGetGraph(out graph))
                        {
                            Endpoint.Write(Format.Error(parseResult.Message));
                            continue;
                        }

                        break;

                    default:
                        throw new BossyNotAdaptableException("All endpoints must return a command line or command graph when queried for one.");
                }

                _commandCancellationSource = new CancellationTokenSource();
                var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_commandCancellationSource.Token, _sessionCancellationSource.Token);

                await _dispatcher.ExecuteAsync(graph, this, linkedSource.Token);
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
        /// Creates a new session that shares this session's context, permissions, history and aliases.
        /// </summary>
        /// <param name="endpoint">The endpoint of the new session.</param>
        /// <param name="host">Where the new session's UI lives.</param>
        /// <returns>The cloned session.</returns>
        internal Session CloneToWindow(IEndpoint endpoint, ISessionHost host)
        {
            return new Session(_context, _permissions, endpoint, host, History, Aliases, true);
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
            await _dispatcher.ExecuteAsync(graph, this, token, input, output);
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
