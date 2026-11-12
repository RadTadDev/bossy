namespace Bossy.Execution
{
    /// <summary>
    /// Writes through to a writer but ignores requests to close it, so a nested run cannot end its caller's stream.
    /// </summary>
    internal sealed class NonClosingWriter : IWriteable
    {
        private readonly IWriteable _target;

        /// <summary>
        /// Creates a writer that forwards to the target without closing it.
        /// </summary>
        /// <param name="target">The writer to forward to.</param>
        public NonClosingWriter(IWriteable target)
        {
            _target = target;
        }

        public void Write(object value)
        {
            _target.Write(value);
        }

        public void CloseWriter() { }
    }
}
