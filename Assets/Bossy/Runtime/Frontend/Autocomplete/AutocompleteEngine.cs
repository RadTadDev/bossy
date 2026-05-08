using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Schema.Registry;
using Bossy.Utils;
using UnityEngine;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// The engine that drives autocomplete
    /// </summary>
    public class AutocompleteEngine : IDisposable
    {
        /// <summary>
        /// Invoked when a list of suggestions for likely completions is ready for display.
        /// </summary>
        public Action<List<string>> SuggestionsReady;

        /// <summary>
        /// Invokes when an unrecoverable problem is detected and we suggest deleting some input.
        /// Replace current input with this line.
        /// </summary>
        public Action<string> ErrorTextReady;
        
        private readonly List<string> _names;
        private readonly SchemaRegistry _registry;

        private Task _suggestTask;

        private const int MaxSuggestions = 5;
        private const float Threshold = 0.1f;
        
        private CancellationTokenSource _cts;

        /// <summary>
        /// Creates a new autocomplete engine.
        /// </summary>
        /// <param name="registry">The command registry.</param>
        public AutocompleteEngine(SchemaRegistry registry)
        {
            _registry = registry;
            _names = _registry.GetValidSchemas().Select(s => s.Name.ToLower()).ToList();
        }
        
        /// <summary>
        /// Resets the engine and stops suggesting.
        /// </summary>
        public void Cancel()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="line">The full input line.</param>
        /// <param name="cursorPosition">The position of the cursor where each space is in index starting from 0 (space before first letter).</param>
        public void Update(string line, int cursorPosition)
        {
            Cancel();
            
            line = line.ToLower()[..cursorPosition];
            _ = Suggest(line);
        }

        private Task Suggest(string line)
        {
            var stream = new TokenStream(Tokenizer.Tokenize(line));
            
            // Nothing typed at all
            if (!stream.TryConsume(out var cmdName))
            {
                return Task.CompletedTask;
            }

            // We now have at least part of a single token. Get matches...
            var cmdMatches = BestMatches(cmdName, _names);

            // Bail if we have no good matches
            if (cmdMatches.Count == 0) return Task.CompletedTask;
            
            // We have suggestions on command. Submit if this is all the user has typed so far
            if (!stream.TryPeek(out var next))
            {
                SuggestionsReady?.Invoke(cmdMatches);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;

            // We have another token, search for best schema matches. First try what the user actually typed
            if (_registry.TryResolveSchema(cmdName, out var schema) is not SchemaQueryStatus.Found)
            {
                // Then fallback on the best suggestion we have
                if (_registry.TryResolveSchema(cmdMatches.First(), out schema) is not SchemaQueryStatus.Found)
                {
                    // If our best match doesn't fit at all, nothing will so return;
                    return Task.CompletedTask;
                }
            }

            // We now have a schema, lets pool the possible options and remove from them as we find
            bool onVariadic = false;
            var positionals = schema.GetOrderedPositionalArguments();
            var optionals = schema.GetOrderedOptionalArguments();
            var switches = schema.GetSwitches();
            
            // Note, there is no need to pool subcommands. If we match one, we update schema and there is a whole new set...
            
            while (!stream.TryConsume(out var current))
            {
                // Switches are special because we can easily identify them
                if (next.StartsWith("-") && !onVariadic)
                {
                    // var errorText = HandleSwitch(next, switches);
                    // if (!string.IsNullOrWhiteSpace(errorText))
                    // {
                    //     ErrorTextReady?.Invoke(errorText);
                    //     return Task.CompletedTask;
                    // }
                }
                else
                {
                    // Everything else 
                    // HandleNamedItem(next, options);
                }
            }
            
            return Task.CompletedTask;
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

        private string HandleSwitch(string input, List<ArgumentSchema> args, out List<string> suggestions)
        {
            // if (input.StartsWith("--") || args.)
            suggestions = new List<string>();
            return "";
        }
        
        public void Dispose()
        {
            _cts?.Dispose();
            _suggestTask?.Dispose();
        }
    }
}