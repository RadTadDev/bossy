using System;
using UnityEngine;

namespace Bossy.Utils
{
    /// <summary>
    /// Internal logging class.
    /// </summary>
    internal static class Log
    {
        private const string Header = "[Bossy]";
        
        /// <summary>
        /// Log info.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Info(object message) => Debug.Log($"{Header} {message}");
        
        /// <summary>
        /// Log a warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Warning(object message) => Debug.LogWarning($"{Header} {message}");
        
        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Error(object message) => Debug.LogError($"{Header} {message}");
        
        /// <summary>
        /// Log an exception.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        public static void Exception(Exception exception) => Debug.LogException(exception);
    }
}