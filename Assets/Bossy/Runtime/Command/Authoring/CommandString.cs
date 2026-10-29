using System;

namespace Bossy.Command
{
    /// <summary>
    /// A command line held as a value, such as the command a <c>watch</c> command repeats. As an argument, it is a
    /// single token: quote it to give the inner command arguments, switches, or operators, as in
    /// <c>watch "get -n hp Player"</c>. The parser checks that the inner command would parse before the outer one runs.
    /// </summary>
    public readonly struct CommandString : IEquatable<CommandString>
    {
        private readonly string _text;

        /// <summary>
        /// The command line. Empty if none was given.
        /// </summary>
        public string Text => _text ?? string.Empty;

        /// <summary>
        /// True if there is no command line.
        /// </summary>
        public bool IsEmpty => string.IsNullOrWhiteSpace(_text);

        /// <summary>
        /// Makes a command string.
        /// </summary>
        /// <param name="text">The command line.</param>
        public CommandString(string text)
        {
            _text = text;
        }

        /// <summary>
        /// Makes a command string from a command line.
        /// </summary>
        /// <param name="text">The command line.</param>
        public static implicit operator CommandString(string text) => new(text);

        /// <summary>
        /// Gets the command line of a command string.
        /// </summary>
        /// <param name="command">The command string.</param>
        public static implicit operator string(CommandString command) => command.Text;

        public bool Equals(CommandString other) => Text == other.Text;

        public override bool Equals(object obj) => obj is CommandString other && Equals(other);

        public override int GetHashCode() => Text.GetHashCode();

        public override string ToString() => Text;

        /// <summary>
        /// True if a type is <see cref="CommandString"/> or an array of it.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type holds command strings.</returns>
        internal static bool IsCommandStringType(Type type)
        {
            return type == typeof(CommandString) || type == typeof(CommandString[]);
        }
    }
}
