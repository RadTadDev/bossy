namespace Bossy.Shell
{
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
        /// Closes the output stream.
        /// </summary>
        public void Close();
    }
}