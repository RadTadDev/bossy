using System;

namespace RadTadDev.Bossy
{
    /// <summary>
    /// Indicates something went wrong during Bossy's initialization.
    /// </summary>
    public class BossyInitializationException : Exception
    {
        public BossyInitializationException(string message) : base(message) { }
    }
}