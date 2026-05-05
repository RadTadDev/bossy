namespace Bossy.Command
{
    /// <summary>
    /// Determines if a prelaunch hook will allow a command to run or not.
    /// </summary>
    public class PrelaunchResult
    {
        /// <summary>
        /// Whether to allow execution or not.
        /// </summary>
        public readonly bool Execute;
        
        /// <summary>
        /// The message if execution is denied.
        /// </summary>
        public readonly string Message;

        private PrelaunchResult(bool execute, string message)
        {
            Execute = execute;
            Message = message;
        }

        /// <summary>
        /// Allows the command to run.
        /// </summary>
        /// <returns>The result.</returns>
        public static PrelaunchResult Allow()
        {
            return new PrelaunchResult(true, string.Empty);
        }

        /// <summary>
        /// Prevents the command from running.
        /// </summary>
        /// <param name="reason">The reason for denying it.</param>
        /// <returns>The result.</returns>
        public static PrelaunchResult Deny(string reason)
        {
            return new PrelaunchResult(false, reason);
        }
    }
}