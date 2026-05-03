using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Command;
using Bossy.Schema;
using Bossy.Execution;

namespace Bossy.Runtime.Command.Library
{
    [Command("bossy", "Inspects the Bossy system.")]
    public class BossyCommand : SimpleCommand
    {
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            ctx.Write("Bossy console allows you to interact with Unity and its subsystems from a command line! " +
                      "To get a list of available commands, type 'reg'. To get help type 'help'.");
            return CommandStatus.Ok;
        }
    }
    
    [Command("invalid", "Gets a list of invalid commands that are not available.", typeof(BossyCommand))]
    public class InvalidBossyCommand : SimpleCommand
    {
        [Switch('w', "Whether to show warnings.")]
        private bool _warnings = true;
        
        protected override CommandStatus Execute(SimpleContext ctx)
        {
            var invalid = ctx.Bossy.SchemaRegistry.GetInvalidSchemas(_warnings).ToList();

            if (invalid.Count == 0)
            {
                ctx.Write("No invalid commands found. Well done :)");
                return CommandStatus.Ok;
            }

            var map = new Dictionary<CommandSchema, ValidationResult>();
            
            foreach (var schema in invalid)
            {
                var result = ctx.Bossy.SchemaRegistry.GetValidationResult(schema);
                
                map.Add(schema, result);
                
                ctx.Write($"Command '{schema.Name}' has the following problems:");
                
                foreach (var error in result.Errors)
                {
                    ctx.WriteError($"{error.Message}", 4);
                }

                foreach (var warning in result.Warnings)
                {
                    ctx.WriteWarning($"{warning.Message}", 4);
                }
            }
            
            ctx.Write(Environment.NewLine);
            ctx.Write(Format.Color("==== Summary ====", Format.Green));
            foreach (var kvp in map)
            {
                ctx.Write($"  -{kvp.Key.Name}: {kvp.Value.Errors.Count} error(s) and {kvp.Value.Warnings.Count} warning(s)");
            }
            
            ctx.Write("Commands with errors are not available for execution.");
            
            return CommandStatus.Ok;
        }
    }
}