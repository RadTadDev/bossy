using System.IO;

namespace Bossy.Settings
{
    /// <summary>
    /// A file settings source.
    /// </summary>
    public class FileSource : ISettingsSource
    {
        private string _filePath;

        /// <summary>
        /// Creates a new file settings source.
        /// </summary>
        /// <param name="filePath">The filepath to read and write.</param>
        public FileSource(string filePath)
        {
            _filePath = filePath;
        }
        
        public string LoadJson()
        {
            // If the file does not exist, return empty json object.
            return !File.Exists(_filePath) ? string.Empty : File.ReadAllText(_filePath);
        }

        public void SaveJson(string json)
        {
            File.WriteAllText(_filePath, json);
        }
    }
}