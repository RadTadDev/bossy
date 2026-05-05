using System;
using System.Threading.Tasks;

namespace Bossy.Command
{
    /// <summary>
    /// General purpose confirmation enum.
    /// </summary>
    public enum Confirmation
    {
        Confirm,
        Deny
    }
    
    /// <summary>
    /// Requires confirmation from the user before continuing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfirmAttribute : PrelaunchHookAttribute
    {
        public override async Task<PrelaunchResult> OnPrelaunch(ICommand command, CommandContext ctx)
        {
            ctx.Write("Are you sure you want to run this command?");
            
            var response = await ctx.ReadAsync<Confirmation>();

            return response is Confirmation.Confirm ? PrelaunchResult.Allow() : PrelaunchResult.Deny("Confirmation not given.");
        }
    }
}