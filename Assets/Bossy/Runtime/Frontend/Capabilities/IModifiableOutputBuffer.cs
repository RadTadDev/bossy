namespace Bossy.Frontend
{
    /// <summary>
    /// This front end is capable of modifying the output buffer in non-linear ways.
    /// </summary>
    public interface IModifiableOutputBuffer
    {
        /// <summary>
        /// Overwrites the last line with a new value.
        /// </summary>
        /// <param name="value">The value to overwrite with.</param>
        public void Overwrite(object value);
    }
}