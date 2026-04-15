using System.Threading;
using System.Threading.Tasks;

namespace Bossy.Shell
{
    /// <summary>
    /// A session container with state and a single front end.
    /// </summary>
    public class Session
    {
        // TODO: Add all session state here. This is a pure data object
        public ICancellationSource CancellationSource { get; }
        
        public FrontEnd.FrontEnd FrontEnd { get; }
        
        public Session(FrontEnd.FrontEnd frontEnd, ICancellationSource cancellationSource)
        {
            FrontEnd = frontEnd;
            CancellationSource = cancellationSource;
        }
        
        public async Task<CommandGraph> GetCommandGraphAsync(CancellationToken token)
        {
            return await FrontEnd.GetCommandGraph(token);
        }
    }
}