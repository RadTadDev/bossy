using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Schema.Registry;
using UnityEngine;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// The engine that drives autocomplete.
    /// </summary>
    public class AutocompleteEngine
    {
        private readonly List<string> _allCmdNames;
        private readonly SchemaRegistry _registry;

        private Task _suggestTask;

        private const int MaxSuggestions = 5;
        private const float Threshold = 0.1f;
        
        /// <summary>
        /// Creates a new autocomplete engine.
        /// </summary>
        /// <param name="registry">The command registry.</param>
        public AutocompleteEngine(SchemaRegistry registry)
        {
            _registry = registry;
            _allCmdNames = _registry.GetValidSchemas().Select(s => s.Name.ToLower()).ToList();
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
                line = line.ToLower()[..cursorPosition];
            }

            var predictNext = line.Length > 0 && char.IsWhiteSpace(line[^1]);
            return Suggest(line, predictNext);
        }

        private IEnumerable<Suggestion> Suggest(string line, bool predictNext)
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
            var positionals = schema.GetOrderedPositionalArguments();
            var optionals = schema.GetOrderedOptionalArguments();
            var switches = schema.GetSwitches();
            var onVariadic = schema.TryGetVariadic(out var variadic) && positionals.Count == 0 && optionals.Count == 0;
            
            // If the user typed only the command name then a space, try to suggest a single next thing
            if (!stream.TryPeek(out _) && predictNext)
            {
                return HandleCommandOnly(schema, predictedLine, positionals, optionals);
            }
            
            while (stream.TryConsume(out var current))
            {
                var isLastToken = !stream.TryPeek(out _);
            
                // --- Switch token ---
                if (current.StartsWith("-"))
                {
                    if (!HandleSwitch(current, stream, switches, isLastToken))
                    {
                        return Array.Empty<Suggestion>();
                    }
                }
            
                // Then prefer named things. Subcommands win with no input, but prefer the user's input and actual suggestions when available
            }
            
            // --- Stream exhausted, suggest what comes next ---


            return Array.Empty<Suggestion>();
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
            if (_registry.TryResolveSchema(cmdName, out schema) is not SchemaQueryStatus.Found)
            {
                if (_registry.TryResolveSchema(cmdMatches.First(), out schema) is not SchemaQueryStatus.Found)
                {
                    // This should be impossible to get to and indicates a bug if it happens since we know we have matches
                    return false;
                }
            }

            return true;
        }

        private IEnumerable<Suggestion> HandleCommandOnly(CommandSchema schema, string predictedLine, List<ArgumentSchema> positionals, List<ArgumentSchema> optionals)
        {
            var subcommands = schema.ChildSchemas.Select(s => s.Name.ToLower()).ToList();

            // Initialize result to be subcommands
            var result = subcommands.Select(subcommand => new Suggestion($"{predictedLine} {subcommand}", subcommand)).Take(MaxSuggestions).ToList();

            // If we filled the results with subcommands, just return
            if (result.Count == MaxSuggestions)
            {
                return result;
            }
                
            // Otherwise, suggest the first positional if it exists
            if (positionals.Count > 0)
            {
                var positional = positionals[0];

                // Add user suggested positional values
                if (GetUserSuggestions(schema, positional, out var userSuggestions))
                {
                    result.AddRange(userSuggestions.Select(s => new Suggestion($"{predictedLine} {s}", $"{Format.Color(positional.Name, Format.Gray)}: {s}")));
                }

                return result.Take(MaxSuggestions).ToList();
            }
                
            // If there are no positionals, do the same for optionals
            if (optionals.Count > 0)
            {
                var optional = optionals[0];

                // Add user suggested positional values
                if (GetUserSuggestions(schema, optional, out var userSuggestions))
                {
                    result.AddRange(userSuggestions.Select(s => new Suggestion($"{predictedLine} {s}", $"{Format.Color(optional.Name, Format.Gray)}: {s}")));
                }
            }

            // Return any suggestions we have
            return result;
        }

        private bool HandleSwitch(string current, TokenStream stream, List<ArgumentSchema> switches, bool isLastToken) // Decide what else we need
        {
            // Note: This function returns true to continue the upper loop, and false to break and return.
            // You can give suggestions to the front end via 'public Action<List<Suggestion>> SuggestionsReady'
            // You can give a single error indication via 'public Action<string> ErrorTextReady' which should return 
            // The users input as is but highlight in red the first offending token (excluding commands since we can reasonably
            // guess these even with typos) using 'string Format.Color(<token>, Format.Red)'
            
            // This indicates no further switches
            if (current == "--")
            {
                // if this is the last token and predictiveSuggestion is false, just end
                // if this is the last and predictiveSuggestion is true, we want to suggest the next positional, optional, variadic, etc
                // if this is not the last token, continue suggestions by returning true and not signaling the front end
            }
            
            // Only suggest a short name if the user ALREADY has a valid short name and there are more and they 
            // are booleans because these can be aggregated like -abc. You can tell this because swithces[i].FieldInfo.FieldType will be bool
            
            // We prefer long names in suggestions.
            // If this is the last token, try suggesting close names via BestMatches
            
            // If it is not the last token and it matches a long name or short name, remove that switch from the list,
                // If this is a boolean, simply return
                // If it is not, we need to validate the value coming in which will require the type adapter registry for handling the mapping
            
            
            return true;
        }

        private bool GetUserSuggestions(CommandSchema command, ArgumentSchema arg, out List<string> suggestions)
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
    }
}