using Bossy.Command;
using Bossy.Execution;
using Bossy.Frontend;

namespace Bossy.Runtime.Command.Library
{
    [Command("alias", "Adds an alias.")]
    public class AliasCommand : SimpleCommand
    {
        [Positional(0, "The alias name.")] 
        private string _alias;
        
        [Variadic("The expansion")]
        private string[] _expansion;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            if (ctx.Capabilities is not IAliasCapability aliases)
            {
                ctx.WriteError("This frontend does not support aliasing.");
                return CommandStatus.Error;
            }

            var value = string.Join(" ", _expansion);

            if (string.IsNullOrWhiteSpace(value))
            {
                ctx.WriteError("Value was null or whitespace.");
                return CommandStatus.Error;
            }
            
            if (!aliases.AssignAlias(_alias, value))
            {
                ctx.WriteError("Alias invalid, not assigned.");
                return CommandStatus.Error;
            }
            
            ctx.Write($"Added alias {_alias} = {value}.");
            return CommandStatus.Ok;
        }
    }
    
    [Command("remove", "Removes an alias.", typeof(AliasCommand))]
    public class AliasRemoveCommand : SimpleCommand
    {
        [Positional(0, "The alias name.")] 
        private string _alias;

        protected override CommandStatus Execute(SimpleContext ctx)
        {
            if (ctx.Capabilities is not IAliasCapability aliases)
            {
                ctx.WriteError("This frontend does not support aliasing.");
                return CommandStatus.Error;
            }

            if (aliases.DeleteAlias(_alias))
            {
                ctx.Write($"Removed alias {_alias}.");
            }
            
            return CommandStatus.Ok;
        }
    }
}