namespace Bossy.Shell
{
    /// <summary>
    /// A simple command context.
    /// </summary>
    public class SimpleContext
    {
        private IWriteable _outStream;
        
        /// <summary>
        /// Creates a new simple context.
        /// </summary>
        /// <param name="outStream">The output target.</param>
        public SimpleContext(IWriteable outStream)
        {
            _outStream = outStream;
        }
        
        /// <summary>
        /// Writes a value.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public virtual void Write(object value)
        {
            _outStream.Write(value);
        }
    }
}