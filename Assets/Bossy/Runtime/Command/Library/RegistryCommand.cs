using Bossy.Command;
using Bossy.Session;

namespace Bossy.Runtime.Command.Library
{
    [Command("reg", "Displays available commands.")]
    public class RegistryCommand : SimpleCommand
    {
        [Variadic("A parent command to search in")]
        private string[] _parent;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            var schemas = ctx.Bossy.SchemaRegistry.GetValidSchemas(_parent);
            
            Formatter.Align(schemas, s => s.Name, s => s.Description, ctx, Formatter.LightBlue);
            
            return CommandStatus.Ok;
        }
    }
}