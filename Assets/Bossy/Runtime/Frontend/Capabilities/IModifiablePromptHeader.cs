namespace Bossy.Frontend
{
    public interface IModifiablePromptHeader
    {
        /// <summary>
        /// Sets the prompt header to a new value.
        /// </summary>
        /// <param name="header">The value to set the header to.</param>
        public void SetPromptHeader(string header);

        /// <summary>
        /// Resets the header to its default value.
        /// </summary>
        public void ResetHeader();
    }
}