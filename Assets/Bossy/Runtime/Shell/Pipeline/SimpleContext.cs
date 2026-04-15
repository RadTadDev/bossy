namespace Bossy.Shell
{
    /// <summary>
    /// A simple command context.
    /// </summary>
    public class SimpleContext
    {
        /// <summary>
        /// The output writer
        /// </summary>
        protected readonly IWriteable writer;
        
        /// <summary>
        /// Creates a new simple context.
        /// </summary>
        /// <param name="writer">The output writer.</param>
        public SimpleContext(IWriteable writer)
        {
            this.writer = writer;
        }
        
        /// <summary>
        /// Writes a value.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public virtual void Write(object value)
        {
            writer.Write(value);
        }
    }
}