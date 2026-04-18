using System;
using System.Threading;
using System.Threading.Tasks;
using Bossy.FrontEnd.Parsing;
using Bossy.Shell;

namespace Bossy.FrontEnd
{
    /// <summary>
    /// Base class for front end objects.
    /// </summary>
    public abstract class UserInterface : IReadable, IWriteable, IUserInterface, ICancellationSource
    {
        // TODO: Inject readonly session state
        
        /*
         * TODO: front ends only read and write, same as commands. When a prompt
         * is received it will come as a write then a read (of the front end). The
         * context object will hold the abstractions for Prompt(), etc.
         *
         * TODO: For the GUI, you need to have a scrolling terminal just like normal. However, this
         * can display complex types (by testing the type of `object value`and looking up if it
         * has a type displayer and otherwise just falling back on tostring. It will look almost
         * like a jupyter notebook, while another side panel allows command creation.
         */

        public async Task<object> ReadAsync(Type requestedType, CancellationToken token)
        {
            return await Task.FromResult(default(object));
        }
        
        public void Write(object value)
        {
            
        }

        public void Close()
        {
            Write(null);
        }
        
        /// <summary>
        /// Gets a cancellation token that controls the lifetime of the session.
        /// </summary>
        /// <returns>The cancellation token.</returns>
        public abstract CancellationToken GetSessionToken();

        public abstract CancellationToken GetCommandToken();

        Task<object> IReadable.ReadAsync(Type requestedType, CancellationToken token)
        {
            return ReadAsync(requestedType, token);
        }

        public abstract Task<CommandGraph> GetCommandGraph(CancellationToken token);
    }
}