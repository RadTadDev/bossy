namespace Bossy.Command
{
    public interface IOptionGetter
    {
        /// <summary>
        /// Read a value from an option.
        /// </summary>
        /// <returns>The option value.</returns>
        public object Read();
    }
    
    public class Option<T> : IOptionGetter
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

        public object Read()
        {
            return Value;
        }
    }
}