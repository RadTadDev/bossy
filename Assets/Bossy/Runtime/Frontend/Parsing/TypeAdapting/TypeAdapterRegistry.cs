using System;
using System.Collections.Generic;
using Bossy.Utils;
using Unity.GraphToolkit.Editor;

namespace Bossy.Frontend.Parsing
{
    /// <summary>
    /// A registry of all type adapters.
    /// </summary>
    public class TypeAdapterRegistry
    {
        private readonly Dictionary<Type, ITypeAdapter> _adapters = new();

        /// <summary>
        /// Converts a string to type T.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="output">The converted output.</param>
        /// <param name="requireExplicitBool">Whether to force boolean values to have an explicit token.</param>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <returns>The result.</returns>
        public TypeAdapterResult TryConvert<T>(string input, out T output, bool requireExplicitBool = false)
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
        /// <param name="requireExplicitBool">Whether to force boolean values to have an explicit token.</param>
        /// <returns>The result.</returns>
        public TypeAdapterResult TryConvert(Type type, TokenStream stream, out object output, bool requireExplicitBool = false)
        {
            output = null;

            if (!_adapters.TryGetValue(type, out var adapter))
            {
                return TypeAdapterResult.Fail($"No registered adapter handles type \"{type.GetFriendlyName()}\"", 0);
            }

            if (type != typeof(bool))
            {
                return adapter.TryConvert(stream, this, out output);
            }
            
            // Booleans are a special case since they are sometimes explicit and other times not
            var result = ((BoolAdapter)adapter).HandleAdaptation(stream, this, out var boolean, requireExplicitBool);

            if (result.Success)
            {
                output = boolean;
            }
                
            return result;
        }

        /// <summary>
        /// Sets a type adapter in the registry for a certain type.
        /// </summary>
        /// <param name="type">The type that it converts for.</param>
        /// <param name="adapter">The adapter.</param>
        public void RegisterAdapter(Type type, ITypeAdapter adapter)
        {
            _adapters[type] = adapter;
        }

        /// <summary>
        /// Sets a type adapter in the registry for a certain type.
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <typeparam name="T">The type to handle.</typeparam>
        public void RegisterAdapter<T>(BaseTypeAdapter<T> adapter)
        {
            _adapters[typeof(T)] = adapter;
        }
    }
}