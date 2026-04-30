namespace Bossy
{
    /// <summary>
    /// Enumerates the possible platforms.
    /// </summary>
    public enum Platform
    {
        /// <summary>
        /// In the editor.
        /// </summary>
        Editor,
        
        /// <summary>
        /// In a build.
        /// </summary>
        Build,
        
        /// <summary>
        /// In the editor and not in play mode.
        /// </summary>
        EditMode,
        
        /// <summary>
        /// In a build or the editor during play mode.
        /// </summary>
        Runtime
    }
}