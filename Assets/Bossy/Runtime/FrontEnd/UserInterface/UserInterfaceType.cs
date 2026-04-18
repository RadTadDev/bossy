namespace Bossy.FrontEnd
{
    /// <summary>
    /// Enumerated possible user interface types.
    /// </summary>
    public enum UserInterfaceType
    {
        #if UNITY_EDITOR
        /// <summary>
        /// An editor version of the command line.
        /// </summary>
        EDITOR_CommandLine,
        
        /// <summary>
        /// An editor version of the GUI.
        /// </summary>
        EDITOR_Graphical,
        #endif
        
        /// <summary>
        /// A typical CLI.
        /// </summary>
        CommandLine,
        
        /// <summary>
        /// A GUI.
        /// </summary>
        Graphical
    }
}