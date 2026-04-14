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
        
        private readonly FrontEnd.FrontEnd _frontEnd;
        
        public Session(FrontEnd.FrontEnd frontEnd, ICancellationSource cancellationSource)
        {
            _frontEnd = frontEnd;
            CancellationSource = cancellationSource;
        }
        
        public async Task<CommandGraph> GetCommandGraphAsync(CancellationToken token)
        {
            return await _frontEnd.GetCommandGraph(token);
        }
    }
}