using System.Collections;
using System.Collections.Generic;

namespace Bossy.Command
{
    /// <summary>
    /// A prompt type that holds a set of allowed options.
    /// </summary>
    /// <typeparam name="T">The type of the options.</typeparam>
    public class OptionsPrompt
    {
        private readonly ICollection _options;

        private OptionsPrompt(ICollection options)
        {
            _options = options;
        }

        /// <summary>
        /// Creates a new options prompt.
        /// </summary>
        /// <param name="options">The options to allow.</param>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>The new prompt.</returns>
        public static OptionsPrompt Create<T>(IReadOnlyCollection<T> options)
        {
            return new OptionsPrompt((ICollection)options);
        }

        /// <summary>
        /// The number of options.
        /// </summary>
        public int Count => _options.Count;
    
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <returns>The options.</returns>
        public IEnumerable GetOptions() => _options;
    }
}