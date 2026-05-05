namespace Bossy.Frontend.Parsing
{
    /// <summary>
    /// A generic type adapter.
    /// </summary>
    /// <typeparam name="T">The type to handle converting to.</typeparam>
    public abstract class BaseTypeAdapter<T> : ITypeAdapter
    {
        /// <summary>
        /// Consumes tokens and converts from string to a type.
        /// </summary>
        /// <param name="stream">The token stream.</param>
        /// <param name="registry"></param>
        /// <param name="output">The output type.</param>
        /// <returns>The result.</returns>
        public TypeAdapterResult TryConvert(TokenStream stream, TypeAdapterRegistry registry, out object output)
        {
            var result = TryConvertToType(stream, registry, out var converted);

            output = result.Success ? converted : null;

            return result;
        }

        /// <summary>
        /// Consumes tokens and converts from string to a type.
        /// </summary>
        /// <param name="stream">The token stream.</param>
        /// <param name="registry">The type adapter registry.</param>
        /// <param name="output">The output type.</param>
        /// <returns>The result.</returns>
        protected abstract TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out T output);
    }
}