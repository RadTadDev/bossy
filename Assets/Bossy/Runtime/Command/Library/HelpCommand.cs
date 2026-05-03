using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bossy.Command;
using Bossy.Schema;
using Bossy.Schema.Registry;
using Bossy.Session;
using Bossy.Utils;

[Command("help", "Displays information about a command and its arguments.")]
public class HelpCommand : SimpleCommand
{
    [Variadic("The command path to look up.")]
    private string[] _command;

    [Switch('v', "Show argument types and validation attributes.")]
    private bool _verbose;

    [Switch('r', "Also display all subcommands recursively.")]
    private bool _recursive;

    protected override CommandStatus Execute(SimpleContext ctx)
    {
        var sb = new StringBuilder();

        if (_command.Length == 0)
        {
            ctx.Bossy.SchemaRegistry.TryResolveSchema("help", out var help);
            
            ctx.Write("To get a list of all available commands, use 'reg'.");
            ctx.Write("To get help for any command, use the following:");
            ctx.Write("  " + BuildUsageLine(help));
            ctx.Write("Your current terminal operator configuration is the following:");

            ctx.NewLine();
            var operators = new List<(string, string)>
            {
                ($"{Format.Color("Then  ", Format.LightBlue)} {Format.Color(ctx.Bossy.Settings.BossyCliSettings.ThenOperator, Format.Green)}",   "Run a command after another."),
                ($"{Format.Color("And   ", Format.LightBlue)} {Format.Color(ctx.Bossy.Settings.BossyCliSettings.AndOperator, Format.Green)}",    "Run a command after another if it succeeds."),
                ($"{Format.Color("Or    ", Format.LightBlue)} {Format.Color(ctx.Bossy.Settings.BossyCliSettings.OrOperator, Format.Green)}",     "Run a command after another if it fails."),
                ($"{Format.Color("Pipe  ", Format.LightBlue)} {Format.Color(ctx.Bossy.Settings.BossyCliSettings.PipeOperator, Format.Green)}",   "Pipe the output of one command to the input of another."),
                ($"{Format.Color("Window", Format.LightBlue)} {Format.Color(ctx.Bossy.Settings.BossyCliSettings.WindowOperator, Format.Green)}", "Run this command in a window."),
            };

            Format.Align(operators, t => t.Item1, t => t.Item2, ctx);
            return CommandStatus.Ok;
        }

        var root = _command[0];
        var subcommands = _command.Skip(1);

        var status = ctx.Bossy.SchemaRegistry.TryResolveSchema(root, subcommands, out var schema);

        if (status is not SchemaQueryStatus.Found)
        {
            ctx.WriteError($"Unknown command: '{string.Join(" ", _command)}'");
            return CommandStatus.Error;
        }

        AppendSchema(sb, schema, depth: 0);
        ctx.Write(sb.ToString());
        return CommandStatus.Ok;
    }

    private void AppendSchema(StringBuilder sb, CommandSchema schema, int depth)
    {
        var indent = new string(' ', depth * 2);

        // Header
        sb.Append(indent).Append(Format.Bold(schema.Name));

        if (!string.IsNullOrEmpty(schema.Description))
        {
            sb.Append("  ").Append(Format.Italics(Format.Color(schema.Description, Format.Gray)));
        }

        sb.AppendLine();

        // Usage line
        var usageLine = BuildUsageLine(schema);
        if (usageLine != null)
        {
            sb.Append(indent).Append(Format.Color("  Usage: ", Format.Yellow)).AppendLine(usageLine);
        }

        // Arguments
        if (schema.Arguments.Count > 0)
        {
            sb.Append(indent).AppendLine(Format.Color("  Arguments:", Format.Yellow));

            if (_verbose)
            {
                foreach (var arg in schema.Arguments)
                {
                    AppendArgumentVerbose(sb, arg, indent + "    ");
                }
            }
            else
            {
                sb.Append(Format.Align(
                    schema.Arguments,
                    arg => indent + "    " + Format.Color(arg.Name, Format.LightBlue)
                               + Format.Color($" <{arg.Type.GetFriendlyName()}>", Format.Gray)
                               + Format.Color($" {GetKindLabel(arg.ArgumentAttribute)}", Format.Gray),
                    arg => arg.Description
                ));
            }
        }

        // Subcommands
        if (!_recursive && schema.ChildSchemas.Count > 0)
        {
            sb.Append(indent).AppendLine(Format.Color("  Subcommands:", Format.Yellow));
            sb.Append(Format.Align(
                schema.ChildSchemas,
                c => indent + "    " + c.Name,
                c => c.Description,
                Format.LightBlue
            ));
        }

        if (_recursive && schema.ChildSchemas.Count > 0)
        {
            sb.Append(indent).AppendLine(Format.Color("  Subcommands:", Format.Yellow));
            foreach (var child in schema.ChildSchemas)
            {
                AppendSchema(sb, child, depth + 1);
            }
        }
    }

    private static string BuildUsageLine(CommandSchema schema)
    {
        if (schema.Arguments.Count == 0)
        {
            return null;
        }

        var parts = new List<string> { Format.Bold(schema.Name) };

        var positionals = schema.GetOrderedPositionalArguments();
        var optionals   = schema.GetOrderedOptionalArguments();
        var switches    = schema.GetSwitches();
        schema.TryGetVariadic(out var variadic);

        parts.AddRange(positionals.Select(arg => Format.Color($"<{arg.Name}>", Format.LightBlue)));
        parts.AddRange(optionals.Select(arg => Format.Color($"[{arg.Name}]", Format.Gray)));
        parts.AddRange(switches.Select(arg => (SwitchAttribute)arg.ArgumentAttribute).Select(attr => Format.Color($"[-{attr.ShortName}]", Format.Gray)));

        if (variadic != null)
        {
            parts.Add(Format.Color($"[{variadic.Name}...]", Format.Gray));
        }

        return string.Join(" ", parts);
    }

    private void AppendArgumentVerbose(StringBuilder sb, ArgumentSchema arg, string indent)
    {
        sb.Append(indent).Append(Format.Color(arg.Name, Format.LightBlue));
        sb.Append(Format.Color($" <{arg.Type.GetFriendlyName()}>", Format.Gray));
        sb.Append(Format.Color($" {GetKindLabel(arg.ArgumentAttribute)}", Format.Gray));

        if (!string.IsNullOrEmpty(arg.Description))
        {
            sb.Append("  ").Append(arg.Description);
        }

        sb.AppendLine();

        sb.Append(indent).Append("  ")
          .AppendLine(Format.Color($"Field: {arg.FieldInfo.DeclaringType?.Name}.{arg.FieldInfo.Name}", Format.Gray));

        if (arg.ValidationAttributes.Count > 0)
        {
            sb.Append(indent).Append("  ").AppendLine(Format.Color("Validators:", Format.Gray));
            foreach (var validator in arg.ValidationAttributes)
            {
                sb.Append(indent).Append("    ")
                  .AppendLine(Format.Color($"[{validator.GetType().Name}]", Format.Gray));
            }
        }
    }

    private static string GetKindLabel(ArgumentAttribute attr)
    {
        return attr switch
        {
            PositionalAttribute => "[positional]",
            OptionalAttribute   => "[optional]",
            SwitchAttribute     => "[switch]",
            VariadicAttribute   => "[variadic]",
            _                   => "[argument]",
        };
    }
}