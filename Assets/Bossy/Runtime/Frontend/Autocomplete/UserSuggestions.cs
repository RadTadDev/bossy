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
    /// Gets the suggested values for an argument: from the function named by its <see cref="SuggestAttribute"/>,
    /// or, without one, the names of an enum or true and false for a bool. Each function is looked up once per
    /// argument.
    ///
    /// The last results are cached. The cache holds while the same argument is completed and every value the
    /// function read through its <see cref="SuggestionContext"/> is unchanged. A function that reads nothing runs
    /// once when the user starts on a value, and later keystrokes only re-rank its results. The cache is dropped
    /// when a completion asks for no suggestions, so moving to another part of the line and back runs the function
    /// again.
    /// </summary>
    internal class UserSuggestions
    {
        private static readonly IReadOnlyList<Suggestion> BoolValues = new Suggestion[] { "true", "false" };

        private readonly Dictionary<ArgumentSchema, Func<SuggestionContext, IEnumerable<Suggestion>>> _functions = new();
        private CachedSuggestions _cache;
        private bool _askedThisCompletion;

        /// <summary>
        /// Marks the start of a completion.
        /// </summary>
        public void BeginCompletion()
        {
            _askedThisCompletion = false;
        }

        /// <summary>
        /// Marks the end of a completion. The cache is dropped if the completion asked for no suggestions.
        /// </summary>
        public void EndCompletion()
        {
            if (!_askedThisCompletion)
            {
                _cache = null;
            }
        }

        /// <summary>
        /// Gets the suggestions for an argument.
        /// </summary>
        /// <param name="context">The context for the argument being completed.</param>
        /// <param name="error">The message of an exception thrown by the function, or null.</param>
        /// <returns>The suggestions. Empty if the argument has nothing to suggest.</returns>
        public IReadOnlyList<Suggestion> Get(SuggestionContext context, out string error)
        {
            _askedThisCompletion = true;

            if (_cache != null && _cache.Context.Argument == context.Argument && _cache.Context.SameReadsAs(context))
            {
                error = _cache.Error;
                return _cache.Values;
            }

            var function = GetFunction(context.Argument, context.Schema.CommandType);
            IReadOnlyList<Suggestion> values = Array.Empty<Suggestion>();
            error = null;

            if (function != null)
            {
                try
                {
                    values = function(context)?.Where(v => v.Value != null).ToList() ?? new List<Suggestion>();
                }
                catch (Exception e)
                {
                    // Reported in the status line only, so a failing function never writes to the Unity console
                    error = e.Message;
                }
            }

            _cache = new CachedSuggestions(context, values, error);
            return values;
        }

        private Func<SuggestionContext, IEnumerable<Suggestion>> GetFunction(ArgumentSchema argument, Type commandType)
        {
            if (!_functions.TryGetValue(argument, out var function))
            {
                function = ResolveFunction(argument, commandType);
                _functions[argument] = function;
            }

            return function;
        }

        private static Func<SuggestionContext, IEnumerable<Suggestion>> ResolveFunction(ArgumentSchema argument, Type commandType)
        {
            var suggestor = argument.FieldInfo.GetCustomAttribute<SuggestAttribute>();

            return suggestor != null
                ? ResolveSuggestAttribute(suggestor, commandType)
                : ResolveAutomatic(argument);
        }

        private static Func<SuggestionContext, IEnumerable<Suggestion>> ResolveAutomatic(ArgumentSchema argument)
        {
            var type = argument.ArgumentAttribute is VariadicAttribute ? argument.Type.GetElementType() : argument.Type;

            if (type == typeof(bool))
            {
                return _ => BoolValues;
            }

            if (type != null && type.IsEnum)
            {
                var names = Enum.GetNames(type).Select(n => (Suggestion)n).ToList();
                return _ => names;
            }

            return null;
        }

        private static Func<SuggestionContext, IEnumerable<Suggestion>> ResolveSuggestAttribute(SuggestAttribute suggestor, Type commandType)
        {
            var type = suggestor.Type ?? commandType;
            var method = type.GetMethod(suggestor.FunctionName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

            if (method == null)
            {
                Log.Warning($"No {Format.Bold("static")} suggestion function with name '{suggestor.FunctionName}' found on type {type}");
                return null;
            }

            var parameters = method.GetParameters();
            var takesContext = parameters.Length == 1 && parameters[0].ParameterType == typeof(SuggestionContext);

            if (parameters.Length > 0 && !takesContext)
            {
                Log.Warning($"Suggestion function '{suggestor.FunctionName}' may only have a parameter of type {typeof(SuggestionContext)}, or no parameters");
                return null;
            }

            if (typeof(IEnumerable<Suggestion>).IsAssignableFrom(method.ReturnType))
            {
                return takesContext
                    ? (Func<SuggestionContext, IEnumerable<Suggestion>>)method.CreateDelegate(typeof(Func<SuggestionContext, IEnumerable<Suggestion>>))
                    : WithoutContext((Func<IEnumerable<Suggestion>>)method.CreateDelegate(typeof(Func<IEnumerable<Suggestion>>)));
            }

            if (typeof(IEnumerable<string>).IsAssignableFrom(method.ReturnType))
            {
                var strings = takesContext
                    ? (Func<SuggestionContext, IEnumerable<string>>)method.CreateDelegate(typeof(Func<SuggestionContext, IEnumerable<string>>))
                    : WithoutContext((Func<IEnumerable<string>>)method.CreateDelegate(typeof(Func<IEnumerable<string>>)));

                return context => strings(context)?.Select(s => (Suggestion)s);
            }

            Log.Warning($"Suggestion function '{suggestor.FunctionName}' must return a type assignable to IEnumerable<string> or IEnumerable<Suggestion>");
            return null;
        }

        private static Func<SuggestionContext, T> WithoutContext<T>(Func<T> function)
        {
            return _ => function();
        }

        private class CachedSuggestions
        {
            public readonly SuggestionContext Context;
            public readonly IReadOnlyList<Suggestion> Values;
            public readonly string Error;

            public CachedSuggestions(SuggestionContext context, IReadOnlyList<Suggestion> values, string error)
            {
                Context = context;
                Values = values;
                Error = error;
            }
        }
    }
}
