using Bossy.FrontEnd.Parsing;

namespace Bossy
{
    /// <summary>
    /// Constructs a Bossy console object.
    /// </summary>
    public interface IBossyBuilder
    {
        /// <summary>
        /// Registers a custom type adapter.
        /// </summary>
        /// <param name="typeAdapter">The type adapter.</param>
        /// <typeparam name="T">The type this adapter handles.</typeparam>
        /// <returns>The Bossy builder.</returns>
        public IBossyBuilder WithTypeAdapter<T>(BaseTypeAdapter<T> typeAdapter);
        
        /// <summary>
        /// Completes and returns the configured Bossy console.
        /// </summary>
        /// <returns>The console.</returns>
        public BossyConsole Build();
    }
}