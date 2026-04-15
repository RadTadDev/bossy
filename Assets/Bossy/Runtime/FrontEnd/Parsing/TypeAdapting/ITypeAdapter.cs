namespace Bossy.FrontEnd.Parsing
{
    /// <summary>
    /// A converter from one type to another.
    /// </summary>
    public interface ITypeAdapter
    {
        /// <summary>
        /// Attempts to convert one type to another.
        /// </summary>
        /// <param name="cursor">The current token cursor.</param>
        /// <param name="output">The output.</param>
        /// <returns>The result.</returns>
        public TypeAdapterResult TryConvert(TokenStream cursor, out object output);
    }
}