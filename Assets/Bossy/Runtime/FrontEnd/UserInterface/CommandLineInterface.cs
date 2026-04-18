using System;
using System.Threading;
using System.Threading.Tasks;
using Bossy.FrontEnd.UI;
using Bossy.Shell;

namespace Bossy.FrontEnd
{
    /// <summary>
    /// Bossy command line object.
    /// </summary>
    public class CommandLineInterface : UserInterface
    {
        // This class is stateful and delegates all rendering calls to the _gui component
        private readonly ICommandLineGui _gui;
        
        public CommandLineInterface(ICommandLineGui gui)
        {
            _gui = gui;    
        }
        
        public override CancellationToken GetSessionToken() => _gui.GetSessionToken();

        public override CancellationToken GetCommandToken() => _gui.GetCommandToken();

        public override Task<CommandGraph> GetCommandGraph(CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}