using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Command;
using Bossy.Schema;
using Bossy.Utils;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// Passed to a suggestion function. It gives the values the user has already typed for the command's other
    /// arguments, the text of the value being completed, and the Bossy context. The engine remembers which values
    /// the function read, and runs the function again only when one of those values changes.
    /// </summary>
    public class SuggestionContext
    {
        /// <summary>
        /// The text typed so far for the value being completed, without quotes. Empty if the user has not started
        /// typing it. Reading this makes the function run again on every keystroke, so only read it when the
        /// suggestions depend on it, such as for completing file paths.
        /// </summary>
        public string Current
        {
            get
            {
                ReadCurrent = true;
                return _current;
            }
        }

        /// <summary>
        /// True if the function read <see cref="Current"/>.
        /// </summary>
        internal bool ReadCurrent { get; private set; }

        /// <summary>
        /// The Bossy context.
        /// </summary>
        public readonly BossyContext Bossy;

        /// <summary>
        /// The schema of the command being completed. This is the subcommand's schema once a subcommand is typed.
        /// </summary>
        public readonly CommandSchema Schema;

        /// <summary>
        /// The argument whose values are being suggested.
        /// </summary>
        public readonly ArgumentSchema Argument;

        private readonly IReadOnlyDictionary<ArgumentSchema, BoundValue> _values;
        private readonly string _current;

        // The arguments read with TryGet, with the key of their value at the time, or null if they had none
        private readonly Dictionary<ArgumentSchema, string> _reads = new();

        internal SuggestionContext(BossyContext bossy, CommandSchema schema, ArgumentSchema argument,
            IReadOnlyDictionary<ArgumentSchema, BoundValue> values, string current)
        {
            Bossy = bossy;
            Schema = schema;
            Argument = argument;
            _values = values;
            _current = current;
        }

        /// <summary>
        /// Gets the value the user typed for another argument of the command, converted to its type. It is only
        /// set once the user has typed the whole value and moved on, so a value is never a stale default. For a
        /// variadic argument, the value is an array of the elements typed before the one being completed.
        /// </summary>
        /// <param name="fieldName">The argument's field name, such as nameof(_objectName), or its argument name.</param>
        /// <param name="value">The value, or the default if the argument has not been typed.</param>
        /// <typeparam name="T">The argument's type, such as string, or string[] for a variadic of strings.</typeparam>
        /// <returns>True if the user has typed a value for the argument.</returns>
        /// <exception cref="ArgumentException">Throws when the command has no argument with that name.</exception>
        /// <exception cref="InvalidCastException">Throws when the argument's type is not T.</exception>
        public bool TryGet<T>(string fieldName, out T value)
        {
            var argument = FindArgument(fieldName);

            if (!typeof(T).IsAssignableFrom(argument.Type))
            {
                throw new InvalidCastException($"Argument '{argument.Name}' is a {argument.Type.GetFriendlyName()}, not a {typeof(T).GetFriendlyName()}");
            }

            var bound = ValueOf(argument);
            _reads[argument] = bound?.Key;

            if (bound == null)
            {
                value = default;
                return false;
            }

            value = (T)AsArgumentValue(argument, bound);
            return true;
        }

        /// <summary>
        /// Gets whether the values the function read are the same in another context, so its earlier results
        /// still apply.
        /// </summary>
        /// <param name="other">The newer context.</param>
        /// <returns>True if every value read is unchanged.</returns>
        internal bool SameReadsAs(SuggestionContext other)
        {
            if (ReadCurrent && other._current != _current) return false;

            return _reads.All(r => other.ValueOf(r.Key)?.Key == r.Value);
        }

        private ArgumentSchema FindArgument(string fieldName)
        {
            var argument = Schema.Arguments.FirstOrDefault(a => a.FieldInfo.Name == fieldName)
                           ?? Schema.Arguments.FirstOrDefault(a => a.Name == fieldName);

            if (argument == null)
            {
                throw new ArgumentException($"Command '{Schema.Name}' has no argument named '{fieldName}'", nameof(fieldName));
            }

            return argument;
        }

        private BoundValue ValueOf(ArgumentSchema argument)
        {
            return _values.TryGetValue(argument, out var bound) ? bound : null;
        }

        private static object AsArgumentValue(ArgumentSchema argument, BoundValue bound)
        {
            if (argument.ArgumentAttribute is not VariadicAttribute)
            {
                return bound.Values[0];
            }

            var array = Array.CreateInstance(argument.Type.GetElementType(), bound.Values.Count);
            for (var i = 0; i < bound.Values.Count; i++)
            {
                array.SetValue(bound.Values[i], i);
            }

            return array;
        }
    }
}
