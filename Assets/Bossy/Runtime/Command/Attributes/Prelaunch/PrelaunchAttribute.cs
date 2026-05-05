using System;

namespace Bossy.Command
{
    /// <summary>
    /// Runs a callback before a command launches.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class PrelaunchHookAttribute : Attribute
    {
        /// <summary>
        /// Runs before a command launches.
        /// </summary>
        /// <param name="command">The command about to execute.</param>
        /// <returns>True if the command should execute, otherwise false.</returns>
        public abstract PrelaunchResult OnPrelaunch(ICommand command);
    }
}