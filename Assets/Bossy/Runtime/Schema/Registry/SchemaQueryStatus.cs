namespace Bossy.Schema.Registry
{
    /// <summary>
    /// Enumerated possible schema query status results.
    /// </summary>
    public enum SchemaQueryStatus
    {
        /// <summary>
        /// Schema was found.
        /// </summary>
        Found,
        
        /// <summary>
        /// Schema was not found.
        /// </summary>
        NotFound,
        
        /// <summary>
        /// Schema or its ancestor was invalid.
        /// </summary>
        Invalid
    }
}