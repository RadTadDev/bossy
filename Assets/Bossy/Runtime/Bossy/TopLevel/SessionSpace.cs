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
        /// When in a build or in playmode in the editor.
        /// </summary>
        Runtime,
        
        /// <summary>
        /// When running a command in a build or in playmode in the editor.
        /// </summary>
        RuntimeCommand
    }
}