using Bossy.FrontEnd.Parsing;

namespace Bossy
{
    /// <summary>
    /// The command console system.
    /// </summary>
    public class Bossy
    {
        // TODO: This will be the single instantiation point for the system. 
        //  This object will likely just contain a small API surface for registering things 
        //  and create the shell which will be a session factory. Also needs to likely be a singleton 
        //  so that code which creates it does not need to keep a reference to it... but think more on that later
        
        public Shell.Shell Shell { get; }
        public TypeAdapterRegistry TypeAdapterRegistry { get; }
    }
}