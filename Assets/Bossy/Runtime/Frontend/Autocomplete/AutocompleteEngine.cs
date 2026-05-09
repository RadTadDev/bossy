using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Command;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Schema.Registry;
using Bossy.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// The engine that drives autocomplete.
    /// </summary>
    public class AutocompleteEngine
    {
        private readonly List<string> _allCmdNames;
        
        private readonly SchemaRegistry _schemaRegistry;
        private readonly TypeAdapterRegistry _adapterRegistry;

        // TODO: These should be settings, not hardcoded
        private const int MaxSuggestions = 5;
        private const float Threshold = 0.1f;

        /// <summary>
        /// Creates a new autocomplete engine.
        /// </summary>
        /// <param name="schemaRegistry">The command registry.</param>
        /// <param name="adapterRegistry">The type adapter registry.</param>
        public AutocompleteEngine(SchemaRegistry schemaRegistry, TypeAdapterRegistry adapterRegistry)
        {
            _schemaRegistry = schemaRegistry;
            _adapterRegistry = adapterRegistry;
            _allCmdNames = _schemaRegistry.GetValidSchemas().Select(s => s.Name.ToLower()).ToList();
        }
        
        /// <summary>
        /// Gets suggestions based on the current raw input line.
        /// </summary>
        /// <param name="line">The full input line.</param>
        /// <param name="cursorPosition">The position of the cursor where each space is in index starting from 0 (space before first letter).</param>
        public IEnumerable<Suggestion> Suggest(string line, int cursorPosition)
        {
            if (cursorPosition <= line.Length && cursorPosition >= 0)
            {
                line = line[..cursorPosition];
            }

            var predictNext = line.Length > 0 && char.IsWhiteSpace(line[^1]);
            return ResolveSuggestions(line, predictNext);
        }

        private IEnumerable<Suggestion> ResolveSuggestions(string line, bool predictNext)
        {
            var predictedLine = string.Empty;
            var stream = new TokenStream(Tokenizer.Tokenize(line));

            // Returns a bool indicating whether to continue or not
            if (!HandleResolvingCommand(predictNext, stream, out var schema, out var enumerable))
            {
                return enumerable;
            }

            // We use predicted line to keep in mind the full suggestions
            predictedLine += schema.Name;

            // Note, there is no need to pool subcommands. If we match one, we update schema and there is a whole new set...
            var context = new SuggestionContext(schema, stream);
            
            // If the user typed only the command name then a space, try to suggest a single next thing
            if (!stream.TryPeek(out _) && predictNext)
            {
                return HandleCommandOnly(context, predictedLine);
            }
            
            var result = new List<Suggestion>();
            while (stream.TryConsume(out var current))
            {
                // --- Switch token ---
                if (current.StartsWith("-"))
                {
                    // Returns a bool indicating whether to continue or not
                    var switchStatus = HandleSwitch(context, current, predictNext, out var switchSuggestions);
                    
                    // This means we have more tokens and there was not an error. Append token and keep going
                    if (switchStatus is SwitchStatus.Continue)
                    {
                        predictedLine += $" {current}";
                    
                        // We need to add any consumed and validate values back to the predicted line since the adapter reg may consume tokens
                        foreach (var s in switchSuggestions)
                        {
                            predictedLine += $" {s}";
                        }
                        
                        continue;
                    }
                    
                    if (switchStatus is SwitchStatus.Hint)
                    {
                        result.Clear();
                        result.Add(new Suggestion("", switchSuggestions[0], isHint:true));
                        return result;
                    }
                    
                    if (switchStatus is SwitchStatus.Error)
                    {
                        result.Clear();
                        result.Add(new Suggestion("", switchSuggestions[0], isError:true));
                        return result;
                    }
                    
                    if (switchStatus is SwitchStatus.Switches) 
                    {
                        result.AddRange(switchSuggestions.Select(s => new Suggestion($"{predictedLine} --{s}", $"--{s}")));
                        return result;
                    }
                    
                    if (switchStatus is SwitchStatus.ShortSwitches) 
                    {
                        result.AddRange(switchSuggestions.Select(s => new Suggestion($"{predictedLine} -{s}", $"-{s}")));
                        return result;
                    }
                    
                    if (switchStatus is SwitchStatus.Values)
                    {
                        // Values are predicted in look-ahead fashion, so we need to append actual switch to the prediction first
                        predictedLine += $" {current}";
                        
                        result.AddRange(switchSuggestions.Select(s => new Suggestion($"{predictedLine} {s}", $"{s}")));
                        return result;
                    }
                }
            
                // Then prefer named things. Subcommands win with no input, but prefer the user's input and actual suggestions when available
                
                predictedLine += $" {current}";
            }
            
            // --- Stream exhausted, suggest what comes next ---
            if (context.TryGetNextOrderedSchema(out var arg))
            {
                GetUserSuggestions(context.Schema, arg, out var suggestions);
                result.AddRange(suggestions.Select(s => new Suggestion($"{predictedLine} {s}", $"{Format.Color(arg.Name, Format.Gray)}: {s}")));
            }
            else if (context.IsOnVariadic)
            {
                GetUserSuggestions(context.Schema, context.Variadic, out var suggestions);
                result.AddRange(suggestions.Select(s => new Suggestion($"{predictedLine} {s}", $"{Format.Color(context.Variadic.Name, Format.Gray)}: {s}")));
            }
            
            return result;
        }

        private bool HandleResolvingCommand(bool predictNext, TokenStream stream, out CommandSchema schema, out IEnumerable<Suggestion> enumerable)
        {
            enumerable = Array.Empty<Suggestion>();
            
            // Nothing typed at all
            if (!stream.TryConsume(out var cmdName))
            {
                schema = null;
                return false;
            }

            cmdName = cmdName.ToLower();
            
            // Get command name matches
            var cmdMatches = BestMatches(cmdName, _allCmdNames);
            if (cmdMatches.Count == 0)
            {
                // No close matches
                schema = null;
                return false;
            }
            
            // We have at least one token. If it is exactly one and the user is still working on it, return predictions
            if (!stream.TryPeek(out _) && !predictNext)
            {
                schema = null;
                enumerable = cmdMatches.Select(n => new Suggestion(n, n));
                return false;
            }
            
            // Resolve schema: try exact first, then best match
            if (_schemaRegistry.TryResolveSchema(cmdName, out schema) is not SchemaQueryStatus.Found)
            {
                if (_schemaRegistry.TryResolveSchema(cmdMatches.First(), out schema) is not SchemaQueryStatus.Found)
                {
                    // This should be impossible to get to and indicates a bug if it happens since we know we have matches
                    return false;
                }
            }

            return true;
        }

        private IEnumerable<Suggestion> HandleCommandOnly(SuggestionContext context, string predictedLine)
        {
            var subcommands = context.Schema.ChildSchemas.Select(s => s.Name.ToLower()).ToList();

            // Initialize result to be subcommands
            var result = subcommands.Select(subcommand => new Suggestion($"{predictedLine} {subcommand}", subcommand)).Take(MaxSuggestions).ToList();

            // If we filled the results with subcommands, just return
            if (result.Count == MaxSuggestions)
            {
                return result;
            }
                
            // Otherwise, suggest the first positional if it exists
            if (context.Positionals.Count > 0)
            {
                var positional = context.Positionals[0];

                // Add user suggested positional values
                if (GetUserSuggestions(context.Schema, positional, out var userSuggestions))
                {
                    result.AddRange(userSuggestions.Select(s => new Suggestion($"{predictedLine} {s}", $"{Format.Color(positional.Name, Format.Gray)}: {s}")));
                }

                return result.Take(MaxSuggestions).ToList();
            }
                
            // If there are no positionals, do the same for optionals
            if (context.Optionals.Count > 0)
            {
                var optional = context.Optionals[0];

                // Add user suggested positional values
                if (GetUserSuggestions(context.Schema, optional, out var userSuggestions))
                {
                    result.AddRange(userSuggestions.Select(s => new Suggestion($"{predictedLine} {s}", $"{Format.Color(optional.Name, Format.Gray)}: {s}")));
                }
            }

            // Return any suggestions we have
            return result;
        }

        private SwitchStatus HandleSwitch(SuggestionContext context, string current, bool predictNext, out List<string> switchSuggestions)
        {
            switchSuggestions = new List<string>();

            // Don't consume tokens as switches if they have been surrendered
            if (context.ConsumedSwitchSurrender) return SwitchStatus.Continue;
            
            if (current.StartsWith("--"))
            {
                // User has typed "--[something or nothing]"
                if (current == "--")
                {
                    // User has typed "--" and nothing else - suggest switch names
                    if (!context.HasMoreTokens && !predictNext)
                    {
                        switchSuggestions = context.Switches.Select(s => s.Name).Take(MaxSuggestions).ToList();
                        return SwitchStatus.Switches;
                    }

                    // User typed "-- [something or nothing]" so we defer prediction of next token to main loop since "-- " declares no more switches coming
                    context.ConsumedSwitchSurrender = true;
                    return SwitchStatus.Continue;
                }
                
                // User typed "--<something>", lets predict switch names
                if (!context.HasMoreTokens && !predictNext)
                {
                    var matches = BestMatches(current[2..], context.Switches.Select(s => s.Name).ToList());
                    
                    switchSuggestions = matches.Take(MaxSuggestions).ToList();
                    return SwitchStatus.Switches;
                }

                return HandleValueAfterSwitch(context, current, predictNext, ref switchSuggestions, false);
            }
            
            // Since it doesn't start with "--", it is a short switch
            
            // The user has typed "-" or "- ", the latter of which is an error
            if (current == "-")
            {
                /*
                 * TODO: Expose this as a setting so users can prefer short names instead.
                 * We are currently saying that when we see "-" just predict the long names,
                 * but some users may prefer suggesting short names
                 */
                // The user has typed "-" and nothing else - suggest long names since we still can
                if (!context.HasMoreTokens && !predictNext)
                {
                    switchSuggestions = context.Switches.Select(s => s.Name).Take(MaxSuggestions).ToList();
                    return SwitchStatus.Switches;
                }
                
                // The user has typed "- [something or nothing]" which is invalid
                switchSuggestions.Add("'-' is not a valid token");
                return SwitchStatus.Error;
            }
            
            // The user has typed "-<something>[something or nothing]" - Lets check for aggregate bools, then fallback on long names
            if (current.Length > 2)
            {
                var parts = current[1..].ToCharArray();
            
                var boolSwitches = context.Switches
                    .Where(s => s.Type == typeof(bool))
                    .Select(arg => ((SwitchAttribute)arg.ArgumentAttribute).ShortName);

                // This does not match any combination of aggregate bool switches
                if (!parts.All(p => boolSwitches.Contains(p)))
                {
                    // Let's try suggesting a prefix matched long name before failing if the user is still typing this
                    if (!predictNext)
                    {
                        var options = context.Switches.Select(s => s.Name);

                        var matches = new List<string>();
                        foreach (var option in options)
                        {
                            if (PrefixScore(current[1..], option) > 0.99f)
                            {
                                matches.Add(option);
                            }
                        }
                
                        // The user has begun typing a long switch without the second dash, suggest it
                        if (matches.Any())
                        {
                            switchSuggestions.Clear();
                            switchSuggestions.AddRange(matches.Take(MaxSuggestions));
                            return SwitchStatus.Switches;
                        }    
                    }
                    
                    // Otherwise fallout and fail on the bad aggregate
                    switchSuggestions.Clear();
                    switchSuggestions.Add($"Unrecognized boolean switch in aggregate flags: {current[1..]}");
                    return SwitchStatus.Error;
                }

                // All parts of the aggregate matched - remove these and continue since they are booleans
                if (context.HasMoreTokens || predictNext)
                {
                    context.Switches.RemoveAll(s => s.Type == typeof(bool) && parts
                            .Any(p => ((SwitchAttribute)s.ArgumentAttribute).ShortName == p));

                    return SwitchStatus.Continue;
                }

                switchSuggestions.Clear();
                switchSuggestions.Add("<bool> Aggregate flags");
                return SwitchStatus.Hint;
            }
            
            // User typed "-<x>", lets predict switch names
            if (!context.HasMoreTokens && !predictNext)
            {
                Log.Info("HERE");
                var options = context.Switches.Select(s => ((SwitchAttribute)s.ArgumentAttribute).ShortName).ToList();
                
                // If the current short switch is valid, make sure its the first suggestion
                if (options.Contains(current[1]))
                {
                    options.Remove(current[1]);
                    options.Insert(0, current[1]);
                }
                
                switchSuggestions = options.Select(c => c.ToString()).Take(MaxSuggestions).ToList();
                return SwitchStatus.ShortSwitches;
            }
            
            // User typed "-<x> [something or nothing]" - handle this as a value
            return HandleValueAfterSwitch(context, current, predictNext, ref switchSuggestions, true);
        }

        private SwitchStatus HandleValueAfterSwitch(SuggestionContext context, string current, bool predictNext, ref List<string> switchSuggestions, bool shortName)
        {
            var tokenName = shortName ? current[1..] : current[2..];
            
            // At this point the user has attempted to indicate a switch name and moved on. If its not valid, thats an error.
            // Search in schema switches since those are static and not consumed as we go
            var arg = shortName 
                ? context.Schema.GetSwitches().FirstOrDefault(a => ((SwitchAttribute)a.ArgumentAttribute).ShortName == tokenName[0]) 
                : context.Schema.GetSwitches().FirstOrDefault(s => s.Name == tokenName);
            
            if (arg == null)
            {
                // Return this so it can be suggested as an error
                switchSuggestions.Add($"Unrecognized switch arg: {tokenName}");
                return SwitchStatus.Error;
            }

            // Remove this from the pool now that its used
            context.Switches.Remove(arg);
                
            // User typed "--switch " or "-s ", lets predict values
            if (!context.HasMoreTokens && predictNext)
            {
                // Arg name is valid, however we don't suggest bool values because they are implicitly true and 
                // as soon as another token is typed it will recognize it as not part of this switch and the hint is confusing
                if (arg.Type == typeof(bool))
                {
                    return SwitchStatus.Continue;
                }
                    
                // The argument name is valid and not a bool, lets suggest values
                if (!GetUserSuggestions(context.Schema, arg, out switchSuggestions))
                {
                    switchSuggestions.Clear();
                    switchSuggestions.Add($"<{arg.Type.GetFriendlyName()}>");
                    return SwitchStatus.Hint;
                }
                    
                switchSuggestions = switchSuggestions.Take(MaxSuggestions).ToList();
                return SwitchStatus.Values;
            }
                
            // The '!context.HasMoreTokens && !predictNext' case is handled before this function is called, so now 
            // the user typed at least the switch and a value now 
            
            var beforeConsumption = context.Stream.Explode();
            var adapterResult = _adapterRegistry.TryConvert(arg.Type, context.Stream, out _);
            var consumed = beforeConsumption.Count - context.Stream.Remaining;
                
            // This holds the consumed tokens so that if we continue, we can note how many in the main loop
            switchSuggestions = beforeConsumption.Take(consumed).ToList();

            var consumedString = string.Join(" ", beforeConsumption.Take(consumed));
                
            // The current value is invalid
            if (!adapterResult.Success)
            {
                // The user moved on after entering the expected number of tokens, return error
                if ((context.Stream.TryPeek(out _) || predictNext) && adapterResult.TokensDesired == consumed)
                {
                    var errorName = shortName ? $"-{((SwitchAttribute)arg.ArgumentAttribute).ShortName}": $"--{arg.Name}";
                    switchSuggestions.Clear();
                    switchSuggestions.Add($"{consumedString} is invalid for arg {errorName}");
                    return SwitchStatus.Error;
                }
                    
                // There are no more tokens, return values or a hint
                if (!GetUserSuggestions(context.Schema, arg, out switchSuggestions, filter:consumedString))
                {
                    switchSuggestions.Clear();
                        
                    // If the value is wrong but the user has not moved on, tell them
                    if (adapterResult.TokensDesired == consumed)
                    {
                        switchSuggestions.Add($"<{arg.Type.GetFriendlyName()}> - current value '{consumedString}' is invalid");
                    }
                    // If it is wrong but, they have not entered enough tokens, just wait for more
                    else
                    {
                        switchSuggestions.Add($"<{arg.Type.GetFriendlyName()}>");
                    }

                    return SwitchStatus.Hint;
                }

                return SwitchStatus.Values;
            }
                
            // We have a valid switch and value, just return how many tokens we consumed and continue
            if (context.HasMoreTokens || predictNext)
            {
                return SwitchStatus.Continue;
            }

            // We are still on the current token, so return suggested values or a hint. 
            // It is okay that we overwrite consumed tokens here because out suggestions will be the same length
            // and the hint will not be applied to input string ever
            if (!GetUserSuggestions(context.Schema, arg, out switchSuggestions, filter:consumedString))
            {
                switchSuggestions.Clear();
                switchSuggestions.Add($"<{arg.Type.GetFriendlyName()}>");
                return SwitchStatus.Hint;
            }

            return SwitchStatus.Values;
        }

        private bool GetUserSuggestions(CommandSchema command, ArgumentSchema arg, out List<string> suggestions, string filter = null)
        {
            suggestions = new List<string>();
            
            var suggestor = arg.FieldInfo.GetCustomAttribute<SuggestAttribute>();

            if (suggestor == null)
            {
                return false;
            }
            
            var type = suggestor.Type ?? command.CommandType;
                    
            var method = type.GetMethod(suggestor.FunctionName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

            if (method == null)
            {
                return false;
            }

            if (!method.IsStatic)
            {
                return false;
            }
            
            if (!typeof(IEnumerable<string>).IsAssignableFrom(method.ReturnType))
            {
                return false;
            }

            suggestions = ((IEnumerable<string>)method.Invoke(null, null)).ToList();

            if (filter != null)
            {
                suggestions = BestMatches(filter, suggestions);
            }
            
            return true;
        }
        
        private List<string> BestMatches(string token, List<string> options)
        {
            List<(float score, string option)> best = new();

            foreach (var option in options)
            {
                var score = Score(token, option);

                if (score < Threshold) continue;
                
                best.Add((score, option));
                best = best.OrderByDescending(t => t.score).Take(MaxSuggestions).ToList();
            }
            
            return best.Select(x => x.option).ToList();
        }

        private float Score(string input, string candidate)
        {
            input = input.ToLower();
            candidate = candidate.ToLower();
            
            var pre = PrefixScore(input, candidate);
            var tri = TrigramScore(input, candidate);
            var lev = LevenshteinScore(input, candidate);
            
            return Mathf.Max(pre, Mathf.Max(tri, lev));
        }

        private float PrefixScore(string input, string candidate)
        {
            return candidate.StartsWith(input) ? 1 : 0;
        }
        
        /*
         * This is a normalized trigram search where each score is computed as
         * 2 * |intersection| / (|input| + |candidate|)
         * where |x| is the magnitude of the set x.
         *
         * We also artificially return 1 (max) when the candidate starts with the input.
         */
        private float TrigramScore(string input, string candidate)
        {
            var inputGrams = GetTrigrams(input);
            var candidateGrams = GetTrigrams(candidate);

            var intersection = inputGrams.Intersect(candidateGrams);
            
            return 2.0f * intersection.Count() / (inputGrams.Count + candidateGrams.Count);
        }
        
        /*
         * This is a normalized Levenshtein score which counts the number of single character
         * changes needed to match the strings and then divides by the 
         *
         * We also artificially return 1 (max) when the candidate starts with the input.
         */
        private float LevenshteinScore(string a, string b)
        {
            int[,] d = new int[a.Length + 1, b.Length + 1];
    
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            float score = d[a.Length, b.Length];
            return 1.0f - (score / Mathf.Max(a.Length, b.Length));
        }

        private HashSet<string> GetTrigrams(string word)
        {
            var lower = 0;
            var result = new HashSet<string>();
            
            while (lower < word.Length - 2)
            {
                var upper = Mathf.Min(lower + 3, word.Length);

                result.Add(word[lower..upper]);
                
                lower++;
            }

            return result;
        }

        private enum SwitchStatus
        {
            /// <summary>
            /// There are more tokens and everything looks good so far.
            /// </summary>
            Continue,
            
            /// <summary>
            /// We are returning a single error.
            /// </summary>
            Error,
            
            /// <summary>
            /// We are returning a single hint.
            /// </summary>
            Hint,
            
            /// <summary>
            /// There are no more tokens and we are suggesting a switch name.
            /// </summary>
            Switches,

            /// <summary>
            /// There are no more tokens and we are suggesting a short switch name.
            /// </summary>
            ShortSwitches,
            
            /// <summary>
            /// There are no more tokens and we are suggesting a value.
            /// </summary>
            Values
        }
        
        private class SuggestionContext
        {
            /// <summary>
            /// True if the schema has variadic args and that is the current token position.
            /// </summary>
            public bool IsOnVariadic => Variadic != null && Positionals.Count == 0 && Optionals.Count == 0;
            
            /// <summary>
            /// True if there are more tokens to parse.
            /// </summary>
            public bool HasMoreTokens => Stream.TryPeek(out _);

            /// <summary>
            /// True if the "--" switch surrender token has been consumed.
            /// </summary>
            public bool ConsumedSwitchSurrender;
            
            public readonly CommandSchema Schema;
            public readonly List<ArgumentSchema> Switches;
            public readonly List<ArgumentSchema> Positionals;
            public readonly List<ArgumentSchema> Optionals;
            public readonly ArgumentSchema Variadic;
            public readonly TokenStream Stream;

            public SuggestionContext(CommandSchema schema, TokenStream stream)
            {
                Schema = schema;
                Switches = schema.GetSwitches();
                Positionals = schema.GetOrderedPositionalArguments();
                Optionals = schema.GetOrderedOptionalArguments();
                schema.TryGetVariadic(out Variadic);
                
                Stream = stream;
            }

            /// <summary>
            /// Gets the next ordered argument from among positionals and optionals.
            /// </summary>
            /// <returns>The argument or null if there is none.</returns>
            public bool TryGetNextOrderedSchema(out ArgumentSchema argument)
            {
                argument = Positionals.Count > 0 ? Positionals.First() : Optionals.FirstOrDefault();
                return argument != null;
            }
        }
    }
}