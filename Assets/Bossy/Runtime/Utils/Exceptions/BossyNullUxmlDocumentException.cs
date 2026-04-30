using System;

namespace Bossy.Utils
{
    /// <summary>
    /// Occurs when a path for a Uxml assert returns null.
    /// </summary>
    public class BossyNullUxmlDocumentException : Exception
    {
        /// <summary>
        /// Throws a new exception.
        /// </summary>
        /// <param name="path">The path that was queried.</param>
        public BossyNullUxmlDocumentException(string path) : 
            base($"No Uxml document was found at path {path}. This must be in a Resources folder.") { }
    }
}