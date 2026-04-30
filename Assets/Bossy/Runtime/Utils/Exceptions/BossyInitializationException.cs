using System;

namespace Bossy.Utils
{
    /// <summary>
    /// Indicates something went wrong during Bossy's initialization.
    /// </summary>
    public class BossyInitializationException : Exception
    {
        public BossyInitializationException(string message) : base(message) { }
    }
}