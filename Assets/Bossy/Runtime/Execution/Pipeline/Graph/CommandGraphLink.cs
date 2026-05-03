namespace Bossy.Session
{
    /// <summary>
    /// Enumerates possible command links in a graph.
    /// </summary>
    public enum CommandGraphLink
    {
        /// <summary>
        /// No following command.
        /// </summary>
        None,
        
        /// <summary>
        /// Run the next command after this one exits.
        /// </summary>
        Then,
        
        /// <summary>
        /// Run the next command if this one exits with success.
        /// </summary>
        And,
        
        /// <summary>
        /// Run the next command if this one exits with failure.
        /// </summary>
        Or,
        
        /// <summary>
        /// Run the next command immediately and pipe data from this one to it,
        /// </summary>
        Pipe
    }
}