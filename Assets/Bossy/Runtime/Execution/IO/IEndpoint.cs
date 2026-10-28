namespace Bossy.Execution
{
    /// <summary>
    /// Where a session reads commands and input from and writes output to. Every session has one.
    /// </summary>
    public interface IEndpoint : IReadable, IWriteable { }
}
