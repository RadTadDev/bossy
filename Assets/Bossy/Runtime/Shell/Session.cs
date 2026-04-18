using System.Threading;
using System.Threading.Tasks;
using Bossy.FrontEnd;

namespace Bossy.Shell
{
    /// <summary>
    /// A session container with state and a single front end.
    /// </summary>
    public class Session
    {
        // TODO: Add all session state here. This is a pure data object
        public ICancellationSource CancellationSource { get; }
        
        public UserInterface UserInterface { get; }
        
        public Session(UserInterface userInterface, ICancellationSource cancellationSource)
        {
            UserInterface = userInterface;
            CancellationSource = cancellationSource;
        }
        
        public async Task<CommandGraph> GetCommandGraphAsync(CancellationToken token)
        {
            return await UserInterface.GetCommandGraph(token);
        }
    }
}