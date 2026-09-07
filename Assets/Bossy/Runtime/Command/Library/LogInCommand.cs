using System.Reflection;
using Bossy.Command;
using UnityEngine;

namespace Bossy.Runtime.Command.Library
{
    [Command("login", "Logs in to the bossy runtime.")]
    public class LogInCommand : SimpleCommand
    {
        [Positional(0, "The password to attempt.")]
        private string _password;
        
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            // Purposefully do not expose the permissions object to everyone. We will 
            // cheat here to get it, but also log clearly if something breaks

            var field = typeof(BossyContext).GetField("_permissions", BindingFlags.NonPublic | BindingFlags.Instance);

            if (field is null)
            {
                Debug.LogError("Failed to grab permissions from the context object! Ensure name is consistent.");
                return CommandStatus.Error;
            }

            if (field.GetValue(ctx.Bossy) is not BossyPermissions permissions)
            {
                Debug.LogError("Failed to grab permissions from the context object! Ensure name is consistent.");
                return CommandStatus.Error;
            }

            if (permissions.AttemptLogIn(_password))
            {
                ctx.Write("Login successful!");
                return CommandStatus.Ok;
            }
            
            ctx.WriteError("Login failed!");
            return CommandStatus.Error;
        }
    }
}