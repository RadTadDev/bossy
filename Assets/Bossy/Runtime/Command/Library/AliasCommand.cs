using System.Linq;
using Bossy.Command;

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
            var alias = _alias.Trim();
            var value = string.Join(" ", _expansion);

            if (string.IsNullOrWhiteSpace(value))
            {
                ctx.WriteError("Value was null or whitespace.");
                return CommandStatus.Error;
            }

            // Aliases must contain only letters so they never match part of a longer word
            if (alias.Any(c => !char.IsLetter(c)))
            {
                ctx.WriteError("Alias invalid, not assigned.");
                return CommandStatus.Error;
            }
            
            ctx.Session.Aliases[alias] = value;
            ctx.Write($"Added alias {alias} = {value}.");
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
            if (ctx.Session.Aliases.Remove(_alias))
            {
                ctx.Write($"Removed alias {_alias}.");
            }
            
            return CommandStatus.Ok;
        }
    }
}
