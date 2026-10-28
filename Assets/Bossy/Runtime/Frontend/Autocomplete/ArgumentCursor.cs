using System.Collections.Generic;
using System.Linq;
using Bossy.Schema;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// Tracks which arguments of a command are still open while the autocomplete engine walks its tokens.
    /// </summary>
    internal class ArgumentCursor
    {
        /// <summary>
        /// The schema of the command being walked. Changes when a subcommand is entered.
        /// </summary>
        public CommandSchema Schema { get; private set; }

        /// <summary>
        /// The switches not used yet.
        /// </summary>
        public List<ArgumentSchema> Switches { get; private set; }

        /// <summary>
        /// The positionals not filled yet, in order.
        /// </summary>
        public List<ArgumentSchema> Positionals { get; private set; }

        /// <summary>
        /// The optionals not filled yet, in order.
        /// </summary>
        public List<ArgumentSchema> Optionals { get; private set; }

        /// <summary>
        /// The variadic argument, or null if the command has none.
        /// </summary>
        public ArgumentSchema Variadic { get; private set; }

        /// <summary>
        /// True once the "--" token is read. Later tokens are never switches.
        /// </summary>
        public bool Surrendered { get; set; }

        /// <summary>
        /// True if the next ordered value goes to the variadic argument.
        /// </summary>
        public bool IsOnVariadic => Variadic != null && Positionals.Count == 0 && Optionals.Count == 0;

        /// <summary>
        /// True if the next token may still name a subcommand. Subcommands must come before any argument.
        /// </summary>
        public bool CanTakeSubcommand => !_anyArgumentTaken && Schema.ChildSchemas.Count > 0;

        private bool _anyArgumentTaken;

        /// <summary>
        /// Creates a cursor at the start of a command's arguments.
        /// </summary>
        /// <param name="schema">The command schema.</param>
        public ArgumentCursor(CommandSchema schema)
        {
            SetSchema(schema);
        }

        /// <summary>
        /// Moves into a subcommand, whose arguments replace the current ones.
        /// </summary>
        /// <param name="subcommand">The subcommand schema.</param>
        public void EnterSubcommand(CommandSchema subcommand)
        {
            SetSchema(subcommand);
        }

        /// <summary>
        /// Gets the next ordered argument: the first open positional, otherwise the first open optional.
        /// </summary>
        /// <param name="argument">The argument.</param>
        /// <param name="positional">True if the argument is a positional.</param>
        /// <returns>True if an ordered argument is open.</returns>
        public bool TryGetNextOrdered(out ArgumentSchema argument, out bool positional)
        {
            positional = Positionals.Count > 0;
            argument = positional ? Positionals[0] : Optionals.FirstOrDefault();
            return argument != null;
        }

        /// <summary>
        /// Marks a switch as used.
        /// </summary>
        /// <param name="argument">The switch.</param>
        public void TakeSwitch(ArgumentSchema argument)
        {
            Switches.Remove(argument);
            _anyArgumentTaken = true;
        }

        /// <summary>
        /// Marks an ordered argument as filled.
        /// </summary>
        /// <param name="argument">The positional or optional.</param>
        public void TakeOrdered(ArgumentSchema argument)
        {
            if (!Positionals.Remove(argument))
            {
                Optionals.Remove(argument);
            }

            _anyArgumentTaken = true;
        }

        /// <summary>
        /// Marks one variadic value as read.
        /// </summary>
        public void TakeVariadicValue()
        {
            _anyArgumentTaken = true;
        }

        /// <summary>
        /// Skips the open optionals so later values go to the variadic argument, as the parser does when a value
        /// does not fit an optional.
        /// </summary>
        public void SkipOptionals()
        {
            Optionals.Clear();
        }

        private void SetSchema(CommandSchema schema)
        {
            Schema = schema;
            Switches = schema.GetSwitches();
            Positionals = schema.GetOrderedPositionalArguments();
            Optionals = schema.GetOrderedOptionalArguments();
            schema.TryGetVariadic(out var variadic);
            Variadic = variadic;
        }
    }
}
