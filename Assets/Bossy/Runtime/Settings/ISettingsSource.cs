namespace Bossy.Settings
{
    /// <summary>
    /// Loads and saves json text.
    /// </summary>
    public interface ISettingsSource
    {
        /// <summary>
        /// Loads the json string from the source.
        /// </summary>
        /// <returns>The json string.</returns>
        public string LoadJson();
        
        /// <summary>
        /// Saves a json string to the source.
        /// </summary>
        /// <param name="json">The json to save.</param>
        public void SaveJson(string json);
    }
}