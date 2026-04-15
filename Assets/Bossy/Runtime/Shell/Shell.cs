using System.Threading;
using System.Threading.Tasks;
using Bossy.FrontEnd.Parsing;

namespace Bossy.Shell
{
    public class Shell
    {
        public TypeAdapterRegistry TypeAdapterRegistry { get; }

        private CommandExecutor _executor = new();
        
        public Shell(TypeAdapterRegistry typeAdapterRegistry)
        {
            TypeAdapterRegistry = typeAdapterRegistry;
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
            
            // Executor catches all exceptions, so instead we rely on this check to close sessions
            while (!sessionToken.IsCancellationRequested)
            {
                var graph = await session.GetCommandGraphAsync(sessionToken);
                var commandToken = session.CancellationSource.GetCommandToken();
                var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(sessionToken, commandToken);
                
                await Execute(graph, session.FrontEnd, session.FrontEnd, combinedCts.Token);
            }
            
            // TODO: Clean up like merge in history, destroy gui, etc.
        }
    }
}