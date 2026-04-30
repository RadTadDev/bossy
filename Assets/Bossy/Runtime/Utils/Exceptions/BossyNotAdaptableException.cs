using System;

namespace Bossy.Utils
{
    public class BossyNotAdaptableException : Exception
    {
        public BossyNotAdaptableException(string message) : base(message) { }
    }
}