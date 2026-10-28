using Bossy.Command;

namespace Bossy.Frontend.Parsing
{
    /// <summary>
    /// Reads a <see cref="CommandString"/> from one token. Whether the command would parse is checked by the parser,
    /// since that needs the commands, operators, and aliases.
    /// </summary>
    public class CommandStringAdapter : BaseTypeAdapter<CommandString>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out CommandString output)
        {
            if (stream.TryConsume(out var token) && !string.IsNullOrWhiteSpace(token))
            {
                output = new CommandString(token);
                return TypeAdapterResult.Pass(1);
            }

            output = default;
            return TypeAdapterResult.Fail("Expected a command, got nothing", 1);
        }
    }
}
