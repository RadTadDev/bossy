using System;
using System.Threading.Tasks;

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
        /// <param name="ctx">The context of the command about to run.</param>
        /// <returns>True if the command should execute, otherwise false.</returns>
        public abstract Task<PrelaunchResult> OnPrelaunch(ICommand command, CommandContext ctx);
    }
}