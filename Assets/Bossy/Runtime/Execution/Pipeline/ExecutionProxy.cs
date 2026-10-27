using System.Threading;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Frontend;

namespace Bossy.Execution
{
    /// <summary>
    /// Sits between a session and the command executor. Opens command windows and shows command views
    /// through the session's host. Endpoints without a host run windowed commands inline instead.
    /// </summary>
    internal class ExecutionProxy : IExecutionListener
    {
        private readonly IEndpoint _endpoint;
        private readonly ISessionHost _host;
        private readonly CommandExecutor _executor;

        /// <summary>
        /// Creates a new execution proxy.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        /// <param name="permissions">The current permissions.</param>
        /// <param name="endpoint">The endpoint the session reads from and writes to.</param>
        /// <param name="host">Where the session's UI lives, or null if it has none.</param>
        public ExecutionProxy(BossyContext context, BossyPermissions permissions, IEndpoint endpoint, ISessionHost host)
        {
            _endpoint = endpoint;
            _host = host;
            _executor = new CommandExecutor(context, permissions, this);
        }

        /// <summary>
        /// Runs a graph, opening a command window for it when it is windowed and the endpoint has a host.
        /// </summary>
        /// <param name="graph">The graph to run.</param>
        /// <param name="session">The session running this.</param>
        /// <param name="token">The token controlling cancellation.</param>
        /// <param name="input">An input override, or null to read from the endpoint.</param>
        /// <param name="output">An output override, or null to write to the endpoint.</param>
        public async Task ExecuteAsync(CommandGraph graph, Session session, CancellationToken token, IReadable input = null, IWriteable output = null)
        {
            if (graph.IsEmpty) return;

            if (graph.Windowed)
            {
                if (_host != null)
                {
                    _host.OpenCommandWindow(session, graph, input, output);
                    return;
                }

                (output ?? _endpoint).Write(Format.Warning("Command windows are not available for this endpoint, so the command runs here."));
                graph.Windowed = false;
            }

            await _executor.ExecuteAsync(graph, session, token, input ?? _endpoint, output ?? _endpoint);
        }

        public void OnGroupStarted(CommandGroup group)
        {
            if (group.View != null)
            {
                _host?.PushView(group.View);
            }
        }

        public void OnGroupFinished(CommandGroup group)
        {
            if (group.View != null)
            {
                _host?.PopView();
            }

            (_endpoint as IVisualEndpointCapabilities)?.ResetCapabilities();
        }
    }
}
