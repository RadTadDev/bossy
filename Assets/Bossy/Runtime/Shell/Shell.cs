using System;
using System.Threading;
using System.Threading.Tasks;
using Bossy.FrontEnd.Parsing;
using Bossy.Utils;

namespace Bossy.Shell
{
    public class Shell
    {
        public TypeAdapterRegistry TypeAdapterRegistry { get; }

        private readonly CommandExecutor _executor;
        
        public Shell(TypeAdapterRegistry typeAdapterRegistry)
        {
            TypeAdapterRegistry = typeAdapterRegistry;
            _executor = new CommandExecutor(this);
        }
        
        public void CreateSession(FrontEnd.FrontEnd frontEnd)
        {
            var session = new Session(frontEnd, frontEnd);

            _ = SessionRunner(session);
        }
        
        public async Task Execute(CommandGraph graph, CancellationToken token)
        {
            await _executor.ExecuteAsync(graph, token);
        }
        
        private async Task SessionRunner(Session session)
        {
            var sessionToken = session.CancellationSource.GetSessionToken();

            try
            {
                while (!sessionToken.IsCancellationRequested)
                {
                    var graph = await session.GetCommandGraphAsync(sessionToken);

                    var commandToken = session.CancellationSource.GetCommandToken();

                    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(sessionToken, commandToken);

                    await Execute(graph, combinedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Happens when the session is canceled while waiting for a command.
            }
            catch (Exception e)
            {
                // This is an unexpected error, the session will die
                Log.Exception(e);
            }
            
            // TODO: Clean up like merge in history, destroy gui, etc.
        }
    }
}