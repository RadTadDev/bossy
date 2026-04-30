namespace Bossy
{
    /// <summary>
    /// Enumerates possible session spaces.
    /// </summary>
    public enum SessionSpace
    {
        /// <summary>
        /// When in the editor and not in playmode.
        /// </summary>
        Edit,
        
        /// <summary>
        /// When in the editor and in playmode or in a build.
        /// </summary>
        Runtime
    }
}