using System;
using System.Threading;
using System.Threading.Tasks;
using Bossy.FrontEnd;
using Bossy.FrontEnd.Parsing;
using Bossy.Utils;

namespace Bossy.Shell
{
    public partial class SessionManager
    {
        public TypeAdapterRegistry TypeAdapterRegistry { get; }

        private readonly CommandExecutor _executor;
        
        public SessionManager(TypeAdapterRegistry typeAdapterRegistry)
        {
            TypeAdapterRegistry = typeAdapterRegistry;
            _executor = new CommandExecutor(this);
        }
        
        public void CreateSession(UserInterfaceType type)
        {
            UserInterface userInterface = UserInterfaceFactory.Get(type);
            
            var session = new Session(userInterface, userInterface);

            _ = SessionRunner(session);
        }
        
        public async Task Execute(CommandGraph graph, IUserInterface userInterface, IReadable defaultInput, IWriteable defaultOutput, CancellationToken token)
        {
            await _executor.ExecuteAsync(graph, userInterface, defaultInput, defaultOutput, token);
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

                    await Execute(graph, session.UserInterface, session.UserInterface, session.UserInterface, combinedCts.Token);
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
            
            Log.Info("Doing clean up on closed session.");
            // TODO: Clean up like merge in history, destroy gui, etc.
        }
    }
}