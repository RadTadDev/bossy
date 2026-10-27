using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Bossy.Frontend
{
    /// <summary>
    /// Command line history shared by every CLI. Loaded from disk on first use and saved when Unity quits or reloads scripts.
    /// </summary>
    internal static class CliHistory
    {
        private const string FileName = "bossy_cli_history.txt";

        private static List<string> _lines;

        /// <summary>
        /// The history, oldest first.
        /// </summary>
        public static IReadOnlyList<string> Lines => Load();

        /// <summary>
        /// Adds a line unless it is blank or repeats the last line.
        /// </summary>
        /// <param name="line">The line to add.</param>
        public static void Add(string line)
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
        public static void Clear() => Load().Clear();

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        private static List<string> Load()
        {
            if (_lines != null)
            {
                return _lines;
            }

            _lines = File.Exists(FilePath) ? File.ReadAllLines(FilePath).ToList() : new List<string>();

            Application.quitting += Save;
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += Save;
            EditorApplication.quitting += Save;
#endif
            return _lines;
        }

        private static void Save()
        {
            File.WriteAllLines(FilePath, _lines);
        }
    }
}
