using System;
using System.Collections.Generic;
using System.Linq;

namespace Bossy.FrontEnd.Parsing
{
    /// <summary>
    /// A registry of all type adapters.
    /// </summary>
    public class TypeAdapterRegistry
    {
        private readonly Dictionary<Type, HashSet<ITypeAdapter>> _adapters = new();

        /// <summary>
        /// Converts a string to type T.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="output">The converted output.</param>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <returns>The result.</returns>
        public TypeAdapterResult TryConvert<T>(string input, out T output)
        {
            var stream = new TokenStream(input);
            
            output = default;
            
            var result = TryConvert(typeof(T), stream, out var obj);

            if (result.Success)
            {
                output = (T)obj;
            }
            
            return result;
        }
        
        /// <summary>
        /// Converts a string to a type.
        /// </summary>
        /// <param name="type">The type to convert to.</param>
        /// <param name="stream">The token stream.</param>
        /// <param name="output">The converted output.</param>
        /// <returns>The result.</returns>
        public TypeAdapterResult TryConvert(Type type, TokenStream stream, out object output)
        {
            output = null;

            if (!_adapters.TryGetValue(type, out var set))
            {
                return TypeAdapterResult.Fail($"No registered adapter handles type \"{type}\"");
            }

            // Multiple adapters are allowed to specify distinct input schemes for the same type.
            var errors = new List<string>();
            foreach (var adapter in set)
            {
                var result = adapter.TryConvert(stream, out output);

                if (!result.Success)
                {
                    errors.Add(result.ErrorMessage);
                    continue;
                }
                
                return result;   
            }

            var message = errors.Count == 1 ? errors.First() : "Could not match any converter:\n" + string.Join("\n- ", errors);
            return TypeAdapterResult.Fail(message);
        }

        /// <summary>
        /// Adds a type adapter to the registry.
        /// </summary>
        /// <param name="type">The type that it converts for.</param>
        /// <param name="adapter">The adapter.</param>
        public void RegisterAdapter(Type type, ITypeAdapter adapter)
        {
            if (_adapters.TryGetValue(type, out var set))
            {
                set.Add(adapter);
            }
            else
            {
                _adapters[type] = new HashSet<ITypeAdapter> { adapter };
            }
        }
    }
}