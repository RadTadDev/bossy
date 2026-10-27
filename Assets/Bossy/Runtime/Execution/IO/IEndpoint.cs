namespace Bossy.Execution
{
    /// <summary>
    /// Where a session reads commands and input from and writes output to. Every session has one.
    /// </summary>
    public interface IEndpoint : IReadable, IWriteable
    {
        /// <summary>
        /// An endpoint stays open for the whole session, so closing it as a command's output does nothing.
        /// </summary>
        void IWriteable.CloseWriter() { }
    }
}
