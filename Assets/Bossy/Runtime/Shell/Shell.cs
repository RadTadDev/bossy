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

        public Shell(TypeAdapterRegistry typeAdapterRegistry)
        {
            TypeAdapterRegistry = typeAdapterRegistry;
        }
        
        public void CreateSession(FrontEnd.FrontEnd frontEnd)
        {
            var session = new Session(frontEnd, frontEnd);

            _ = SessionRunner(session);
        }

        private async Task SessionRunner(Session session)
        {
            var sessionToken = session.CancellationSource.GetSessionToken();
            
            while (!sessionToken.IsCancellationRequested)
            {
                try
                {
                    var graph = await session.GetCommandGraphAsync(sessionToken);

                    // TODO: Combine session with individual token
                    var commandToken = session.CancellationSource.GetCommandToken();

                    await Execute(graph, sessionToken);
                }
                catch (OperationCanceledException)
                {
                    // This is expected when a user closes the session
                }
                catch (BossyNotAdaptableException e)
                {
                    Log.Exception(e);
                }
                catch (Exception exception)
                {
                    Log.Exception(exception);
                }
            }
            
            // TODO: Clean up like merge in history, destroy gui, etc.
        }
        
        private async Task Execute(CommandGraph graph, CancellationToken token)
        {
            await Task.CompletedTask;
        }
    }
}