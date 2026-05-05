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
        private object _options;
        
        private OptionsPrompt(object options)
        {
            _options = options;
        }

        /// <summary>
        /// Creates a new options prompt.
        /// </summary>
        /// <param name="options">The list of options.</param>
        /// <typeparam name="T">The type of the options.</typeparam>
        /// <returns>The prompt object.</returns>
        public static OptionsPrompt Create<T>(IReadOnlyCollection<T> options)
        {
            return new OptionsPrompt(options);
        }
        
        /// <summary>
        /// Gets the options.
        /// </summary>
        public IEnumerable GetOptions()
        {
            return (IEnumerable)_options;
        }
    }
}