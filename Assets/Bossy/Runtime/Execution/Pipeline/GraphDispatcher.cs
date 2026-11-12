using System.Threading;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Frontend;

namespace Bossy.Execution
{
    /// <summary>
    /// Sends a session's graphs where they should run. Windowed graphs open a command window through the
    /// session's host, and endpoints without a host run them inline instead. Other graphs run in the executor,
    /// and this keeps the endpoint's views and capabilities in step with each group.
    /// </summary>
    internal class GraphDispatcher : IExecutionListener
    {
        private readonly IEndpoint _endpoint;
        private readonly ISessionHost _host;
        private readonly CommandExecutor _executor;
        private readonly IWriteable _endpointWriter;

        /// <summary>
        /// Creates a new graph dispatcher.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        /// <param name="permissions">The current permissions.</param>
        /// <param name="endpoint">The endpoint the session reads from and writes to.</param>
        /// <param name="host">Where the session's UI lives, or null if it has none.</param>
        public GraphDispatcher(BossyContext context, BossyPermissions permissions, IEndpoint endpoint, ISessionHost host)
        {
            _endpoint = endpoint;
            _host = host;
            _executor = new CommandExecutor(context, permissions, this);
            _endpointWriter = new NonClosingWriter(endpoint);
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

            // Commands close their output when they finish, which would end the endpoint for the rest of the session.
            // Only an output the caller passed in is closed, because the caller may be waiting for it to end.
            var resolvedOutput = output ?? _endpointWriter;

            await _executor.ExecuteAsync(graph, session, token, input ?? _endpoint, resolvedOutput);
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

            // Only a group that writes to the endpoint owns its capabilities. A group writing to a pipe, such as a
            // command reading another command's output, must not reset what the endpoint's writer has set.
            if (group.Nodes[^1].Context.GetWriter() == _endpointWriter)
            {
                (_endpoint as IVisualEndpointCapabilities)?.ResetCapabilities();
            }
        }

    }
}
