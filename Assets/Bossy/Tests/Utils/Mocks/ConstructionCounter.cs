namespace Bossy.Tests.Utils
{
    /// <summary>
    /// Counts how often generated commands are created, for use with
    /// <see cref="CommandGenerator.WithConstructorCall"/>. It is public because generated commands live in a dynamic
    /// assembly that can only call public methods.
    /// </summary>
    public static class ConstructionCounter
    {
        /// <summary>
        /// The number of constructions counted. Reset it before use.
        /// </summary>
        public static int Count;

        /// <summary>
        /// Counts one construction.
        /// </summary>
        public static void Increment()
        {
            Count++;
        }
    }
}
