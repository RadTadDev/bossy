using System;

namespace RadTadDev.Bossy
{
    public class BossyNotAdaptableException : Exception
    {
        public BossyNotAdaptableException(string message) : base(message) { }
    }
}