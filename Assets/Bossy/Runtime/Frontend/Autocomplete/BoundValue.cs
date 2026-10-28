using System.Collections.Generic;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// A value the autocomplete engine read for an argument before the cursor, converted by the argument's type
    /// adapter. A variadic argument holds one value per element.
    /// </summary>
    internal class BoundValue
    {
        /// <summary>
        /// The converted values. A variadic argument has one per element; any other argument has one.
        /// </summary>
        public readonly List<object> Values = new();

        /// <summary>
        /// The typed text of each value, used to tell when a value changed.
        /// </summary>
        public readonly List<string> Texts = new();

        /// <summary>
        /// All the typed text, joined, so two bound values can be compared.
        /// </summary>
        public string Key => string.Join("\u001f", Texts);
    }
}
