using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bossy.Session
{
    /// <summary>
    /// Able to read from a standard stream.
    /// </summary>
    public interface IReadable
    {
        /// <summary>
        /// Reads an object from the stream.
        /// </summary>
        /// <param name="requestedType">The type of the read being requested.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>The read object.</returns>
        /// <remarks>This method has no obligation to return the requested type.</remarks>
        public Task<object> ReadAsync(Type requestedType, CancellationToken token);
    }
}