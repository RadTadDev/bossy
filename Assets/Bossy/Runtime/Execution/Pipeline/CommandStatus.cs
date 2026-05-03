namespace Bossy.Execution
{
    /// <summary>
    /// Enumerates the possible statuses that a command can return with.
    /// </summary>
    public enum CommandStatus
    {
        /// <summary>
        /// The command was successful.
        /// </summary>
        Ok,
        
        /// <summary>
        /// The command did not complete successfully.
        /// </summary>
        Error,
        
        /// <summary>
        /// The command was canceled during execution.
        /// </summary>
        Cancelled
    }
}