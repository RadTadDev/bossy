using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bossy.Execution
{
    /// <summary>
    /// The command lines a session has entered, oldest first. When created with a file path, the history is
    /// loaded from that file on first use and saved when Unity quits or reloads scripts.
    /// </summary>
    public class CommandHistory
    {
        private readonly string _filePath;
        private List<string> _lines;

        /// <summary>
        /// Creates a new history.
        /// </summary>
        /// <param name="filePath">The file to persist to, or null to keep the history in memory only.</param>
        public CommandHistory(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// The history, oldest first.
        /// </summary>
        public IReadOnlyList<string> Lines => Load();

        /// <summary>
        /// Adds a line unless it is blank or repeats the last line.
        /// </summary>
        /// <param name="line">The line to add.</param>
        public void Add(string line)
        {
            var lines = Load();

            if (string.IsNullOrWhiteSpace(line) || lines.LastOrDefault() == line)
            {
                return;
            }

            lines.Add(line);
        }

        /// <summary>
        /// Clears the history.
        /// </summary>
        public void Clear() => Load().Clear();

        private List<string> Load()
        {
            if (_lines != null)
            {
                return _lines;
            }

            _lines = _filePath != null && File.Exists(_filePath) ? File.ReadAllLines(_filePath).ToList() : new List<string>();

            if (_filePath != null)
            {
                Application.quitting += Save;
#if UNITY_EDITOR
                AssemblyReloadEvents.beforeAssemblyReload += Save;
                EditorApplication.quitting += Save;
#endif
            }

            return _lines;
        }

        private void Save()
        {
            File.WriteAllLines(_filePath, _lines);
        }
    }
}
