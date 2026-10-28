using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Command;
using Bossy.Schema;
using Bossy.Utils;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// Runs the suggestion functions named by <see cref="SuggestAttribute"/>. Each function is looked up once per
    /// argument. Its results are cached per argument, keyed on the text typed before the value, so the function
    /// runs once when the user starts typing a value and later keystrokes only re-rank the cached list.
    /// </summary>
    internal class UserSuggestions
    {
        private readonly Dictionary<ArgumentSchema, Func<SuggestionContext, IEnumerable<string>>> _functions = new();
        private readonly Dictionary<ArgumentSchema, CachedSuggestions> _cache = new();
        private readonly HashSet<ArgumentSchema> _reportedFailures = new();

        /// <summary>
        /// Gets the suggestions for an argument.
        /// </summary>
        /// <param name="argument">The argument being completed.</param>
        /// <param name="commandType">The type of the command the argument is on.</param>
        /// <param name="key">The text typed before the value. The cached list is reused while it is unchanged.</param>
        /// <param name="makeContext">Creates the context passed to the function.</param>
        /// <param name="error">The message of an exception thrown by the function, or null.</param>
        /// <returns>The suggestions. Empty if the argument has no suggestion function.</returns>
        public IReadOnlyList<string> Get(ArgumentSchema argument, Type commandType, string key, Func<SuggestionContext> makeContext, out string error)
        {
            if (_cache.TryGetValue(argument, out var cached) && cached.Key == key)
            {
                error = cached.Error;
                return cached.Values;
            }

            var function = GetFunction(argument, commandType);
            IReadOnlyList<string> values = Array.Empty<string>();
            error = null;

            if (function != null)
            {
                try
                {
                    values = function(makeContext())?.Where(v => v != null).ToList() ?? new List<string>();
                }
                catch (Exception e)
                {
                    error = e.Message;

                    // Log once per argument so a failing function does not flood the console on every keystroke
                    if (_reportedFailures.Add(argument))
                    {
                        Log.Exception(e);
                    }
                }
            }

            _cache[argument] = new CachedSuggestions(key, values, error);
            return values;
        }

        private Func<SuggestionContext, IEnumerable<string>> GetFunction(ArgumentSchema argument, Type commandType)
        {
            if (!_functions.TryGetValue(argument, out var function))
            {
                function = ResolveFunction(argument, commandType);
                _functions[argument] = function;
            }

            return function;
        }

        private static Func<SuggestionContext, IEnumerable<string>> ResolveFunction(ArgumentSchema argument, Type commandType)
        {
            var suggestor = argument.FieldInfo.GetCustomAttribute<SuggestAttribute>();

            if (suggestor == null)
            {
                return null;
            }

            var type = suggestor.Type ?? commandType;
            var method = type.GetMethod(suggestor.FunctionName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

            if (method == null)
            {
                Log.Warning($"No {Format.Bold("static")} suggestion function with name '{suggestor.FunctionName}' found on type {type}");
                return null;
            }

            if (!typeof(IEnumerable<string>).IsAssignableFrom(method.ReturnType))
            {
                Log.Warning("Suggestion function must return a type assignable to IEnumerable<string>");
                return null;
            }

            var parameters = method.GetParameters();

            if (parameters.Length == 0)
            {
                var noContext = (Func<IEnumerable<string>>)method.CreateDelegate(typeof(Func<IEnumerable<string>>));
                return _ => noContext();
            }

            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(SuggestionContext))
            {
                return (Func<SuggestionContext, IEnumerable<string>>)method.CreateDelegate(typeof(Func<SuggestionContext, IEnumerable<string>>));
            }

            Log.Warning($"Suggestion function may only have a parameter of type {typeof(SuggestionContext)}, or no parameters");
            return null;
        }

        private readonly struct CachedSuggestions
        {
            public readonly string Key;
            public readonly IReadOnlyList<string> Values;
            public readonly string Error;

            public CachedSuggestions(string key, IReadOnlyList<string> values, string error)
            {
                Key = key;
                Values = values;
                Error = error;
            }
        }
    }
}
