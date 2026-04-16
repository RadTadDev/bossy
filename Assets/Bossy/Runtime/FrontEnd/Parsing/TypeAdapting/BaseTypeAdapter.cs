namespace Bossy.FrontEnd.Parsing
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
        /// <param name="output">The output type.</param>
        /// <returns>The result.</returns>
        public TypeAdapterResult TryConvert(TokenStream stream, out object output)
        {
            var result = TryConvertToType(stream, out var converted);

            output = result.Success ? converted : null;

            return result;
        }

        /// <summary>
        /// Consumes tokens and converts from string to a type.
        /// </summary>
        /// <param name="stream">The token stream.</param>
        /// <param name="output">The output type.</param>
        /// <returns>The result.</returns>
        protected abstract TypeAdapterResult TryConvertToType(TokenStream stream, out T output);
        
        public override bool Equals(object obj) => obj?.GetType() == GetType();
        public override int GetHashCode() => GetType().GetHashCode();
    }
}