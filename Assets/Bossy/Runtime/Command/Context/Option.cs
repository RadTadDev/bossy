namespace Bossy.Command
{
    public class Option<T>
    {
        /// <summary>
        /// The value of this option.
        /// </summary>
        public readonly T Value;
        
        /// <summary>
        /// The display text for this option.
        /// </summary>
        public readonly string Display;
        
        public Option(T value, string display)
        {
            Value = value;
            Display = display;
        }
    }
}