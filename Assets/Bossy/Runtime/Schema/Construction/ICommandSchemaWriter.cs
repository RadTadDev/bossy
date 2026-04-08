namespace Bossy.Schema
{
    /// <summary>
    /// A writer interface to modify otherwise readonly state. 
    /// </summary>
    internal interface ICommandSchemaWriter
    {
        /// <summary>
        /// Sets the parent of this schema.
        /// </summary>
        /// <param name="parent">The parent schema.</param>
        public void SetParent(CommandSchema parent);

        /// <summary>
        /// Adds a child of this schema.
        /// </summary>
        /// <param name="child">The child to add.</param>
        public void AddChild(CommandSchema child);
    }
}