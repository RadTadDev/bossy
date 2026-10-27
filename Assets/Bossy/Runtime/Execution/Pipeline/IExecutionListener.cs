namespace Bossy.Execution
{
    /// <summary>
    /// Receives the start and finish of each command group as a graph runs.
    /// </summary>
    internal interface IExecutionListener
    {
        /// <summary>
        /// Called once a group's commands are about to run.
        /// </summary>
        /// <param name="group">The group starting.</param>
        public void OnGroupStarted(CommandGroup group);

        /// <summary>
        /// Called when a group finishes, including when it was canceled or skipped.
        /// </summary>
        /// <param name="group">The group finishing.</param>
        public void OnGroupFinished(CommandGroup group);
    }
}
