using UnityEngine;

namespace Bossy.Shell
{
    public static class CloseWriterSentinel
    {
        public static readonly object Object = new();
    }
    
    /// <summary>
    /// Is able to write to a standard stream.
    /// </summary>
    public interface IWriteable
    {
        /// <summary>
        /// Writes a value.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public void Write(object value);

        /// <summary>
        /// Closes this output stream.
        /// </summary>
        public void CloseWriter()
        {
            Write(CloseWriterSentinel.Object);
        }
    }
}