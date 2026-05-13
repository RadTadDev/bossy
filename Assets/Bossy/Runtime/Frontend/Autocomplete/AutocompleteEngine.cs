using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bossy.Command;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Schema.Registry;
using Bossy.Utils;
using UnityEngine;

/* =============================================================
 * Warning, adventurer! Here be DRAGONS!
 *
 * It works, modify at your own risk :D. PRs are welcome!
 * =============================================================
 */

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// The engine that drives autocomplete.
    /// </summary>
    public class AutocompleteEngine
    {
        private readonly List<string> _allCmdNames;

        private readonly BossyContext _context;

        // TODO: These should be settings, not hardcoded
        private const int MaxSuggestions = 5;
        private const float Threshold = 0.1f;
        
        /// <summary>
        /// Creates a new autocomplete engine.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        public AutocompleteEngine(BossyContext context)
        {
            _context = context;
            _allCmdNames = _context.SchemaRegistry.GetValidSchemas().Select(s => s.Name.ToLower()).ToList();
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

            var result = new List<Suggestion>();
            var context = new CompletionContext(schema, stream);
            
            ArgumentSchema lastArg = null;
            while (context.Stream.TryConsume(out var current))
            {
                var finishedQuote = current.StartsWith("\"") && current.EndsWith("\"");
                var unfinishedQuote = current.StartsWith("\"") && !current.EndsWith("\"");
                var cursorHere = !context.HasMoreTokens && !predictNext;
                
                // Don't update prediction if this is a quote unless we have the ending quote too.
                if (unfinishedQuote || (cursorHere && finishedQuote))
                {
                    if (result.Count == 0)
                    {
                        if (lastArg == null && !context.TryGetNextOrderedArg(out lastArg, out _))
                        {
                            if (context.IsOnVariadic)
                            {
                                lastArg = context.Variadic;
                                result.Add(new Suggestion("", $"{lastArg.Name}: <{lastArg.Type.GetFriendlyName()}>", isHint:true));
                            }
                            else
                            {
                                result.Add(new Suggestion("", $"Invalid token: {current}", isError:true));
                            }
                        }
                        else
                        {
                            result.Add(new Suggestion("", $"{lastArg.Name}: <{lastArg.Type.GetFriendlyName()}>", isHint:true));
                        }
                    }
                    return result;    
                }
                
                // --- Switch token ---
                if (current.StartsWith("-"))
                {
                    // Returns a bool indicating whether to continue or not
                    var switchStatus = HandleSwitch(context, current, predictedLine, predictNext, out var switchSuggestions, out var arg);

                    if (arg != null)
                    {
                        lastArg = arg;
                    }
                    
                    // This means we have more tokens and there was not an error. Append token and keep going
                    if (switchStatus is SuggestStatus.Continue)
                    {
                        predictedLine += $" {current}";
                    
                        // We need to add any consumed and validate values back to the predicted line since the adapter reg may consume tokens
                        foreach (var s in switchSuggestions)
                        {
                            predictedLine += $" {s}";
                        }
                        
                        continue;
                    }
                    
                    if (switchStatus is SuggestStatus.Hint)
                    {
                        result.Clear();
                        result.Add(new Suggestion("", switchSuggestions[0], isHint:true));
                        return result;
                    }
                    
                    if (switchStatus is SuggestStatus.Error)
                    {
                        result.Clear();
                        result.Add(new Suggestion("", switchSuggestions[0], isError:true));
                        return result;
                    }
                    
                    if (switchStatus is SuggestStatus.Switches) 
                    {
                        result.AddRange(switchSuggestions.Select(s => new Suggestion($"{predictedLine} --{s}", $"--{s}")));
                        return result;
                    }
                    
                    if (switchStatus is SuggestStatus.ShortSwitches) 
                    {
                        result.AddRange(switchSuggestions.Select(s => new Suggestion($"{predictedLine} -{s}", $"-{s}")));
                        return result;
                    }
                    
                    if (switchStatus is SuggestStatus.Values)
                    {
                        // Values are predicted in look-ahead fashion, so we need to append actual switch to the prediction first
                        predictedLine += $" {current}";
                        
                        result.AddRange(switchSuggestions.Select(s => new Suggestion($"{predictedLine} {s}", $"{s}")));
                        return result;
                    }
                }
                
                // --- It's not a switch so attempt to match a position based token ---
                var status = HandleOrderedToken(context, current, predictNext, predictedLine, out var orderedSuggestions);
                
                if (status is SuggestStatus.Hint)
                {
                    result.Clear();
                    result.Add(orderedSuggestions[0]);
                    return result;
                }
                
                if (status is SuggestStatus.Error)
                {
                    result.Clear();
                    result.Add(orderedSuggestions[0]);
                    return result;
                }

                if (status is SuggestStatus.Values)
                {
                    result = orderedSuggestions;
                    return result;
                }
                
                // Otherwise, continue and add the consumed tokens to the predictedLine
                predictedLine += $" {string.Join(" ", orderedSuggestions.Select(s => s.FullText))}";
            }

            if (predictNext)
            {
                return SuggestNext(context, predictedLine);
            }

            result.Clear();
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
            if (_context.SchemaRegistry.TryResolveSchema(cmdName, out schema) is not SchemaQueryStatus.Found)
            {
                if (_context.SchemaRegistry.TryResolveSchema(cmdMatches.First(), out schema) is not SchemaQueryStatus.Found)
                {
                    // This should be impossible to get to and indicates a bug if it happens since we know we have matches
                    return false;
                }
            }

            return true;
        }

        private SuggestStatus HandleSwitch(CompletionContext context, string current, string predictedLine, bool predictNext, out List<string> switchSuggestions, out ArgumentSchema lastArg)
        {
            switchSuggestions = new List<string>();
            lastArg = null;
            
            // Don't consume tokens as switches if they have been surrendered
            if (context.ConsumedSwitchSurrender) return SuggestStatus.Continue;
            
            if (current.StartsWith("--"))
            {
                // User has typed "--[something or nothing]"
                if (current == "--")
                {
                    // User has typed "--" and nothing else - suggest switch names
                    if (!context.HasMoreTokens && !predictNext)
                    {
                        switchSuggestions = context.Switches.Select(s => s.Name).Take(MaxSuggestions).ToList();
                        return SuggestStatus.Switches;
                    }

                    // User typed "-- [something or nothing]" so we defer prediction of next token to main loop since "-- " declares no more switches coming
                    context.ConsumedSwitchSurrender = true;
                    return SuggestStatus.Continue;
                }
                
                // User typed "--<something>", lets predict switch names
                if (!context.HasMoreTokens && !predictNext)
                {
                    var matches = BestMatches(current[2..], context.Switches.Select(s => s.Name).ToList());
                    
                    switchSuggestions = matches.Take(MaxSuggestions).ToList();
                    return SuggestStatus.Switches;
                }

                return HandleValueAfterSwitch(context, current, predictedLine, predictNext, ref switchSuggestions, false, out lastArg);
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
                    return SuggestStatus.Switches;
                }
                
                // The user has typed "- [something or nothing]" which is invalid
                switchSuggestions.Add("'-' is not a valid token");
                return SuggestStatus.Error;
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
                            return SuggestStatus.Switches;
                        }    
                    }
                    
                    // Otherwise fallout and fail on the bad aggregate
                    switchSuggestions.Clear();
                    switchSuggestions.Add($"Unrecognized boolean switch in aggregate flags: {current[1..]}");
                    return SuggestStatus.Error;
                }

                lastArg = context.Switches.First(s => s.Type == typeof(bool) && ((SwitchAttribute)s.ArgumentAttribute).ShortName == current[^1]);

                // All parts of the aggregate matched - remove these and continue since they are booleans
                if (context.HasMoreTokens || predictNext)
                {
                    context.Switches.RemoveAll(s => s.Type == typeof(bool) && parts
                            .Any(p => ((SwitchAttribute)s.ArgumentAttribute).ShortName == p));
                    context.MarkAnyArgumentTaken();    

                    return SuggestStatus.Continue;
                }

                switchSuggestions.Clear();
                switchSuggestions.Add("<bool> Aggregate flags");
                return SuggestStatus.Hint;
            }
            
            // User typed "-<x>", lets predict switch names
            if (!context.HasMoreTokens && !predictNext)
            {
                var options = context.Switches.Select(s => ((SwitchAttribute)s.ArgumentAttribute).ShortName).ToList();
                
                // If the current short switch is valid, make sure its the first suggestion
                if (options.Contains(current[1]))
                {
                    options.Remove(current[1]);
                    options.Insert(0, current[1]);
                }
                
                switchSuggestions = options.Select(c => c.ToString()).Take(MaxSuggestions).ToList();
                return SuggestStatus.ShortSwitches;
            }
            
            // User typed "-<x> [something or nothing]" - handle this as a value
            return HandleValueAfterSwitch(context, current, predictedLine, predictNext, ref switchSuggestions, true, out lastArg);
        }

        private SuggestStatus HandleValueAfterSwitch(CompletionContext context, string current, string predictedLine, bool predictNext, ref List<string> switchSuggestions, bool shortName, out ArgumentSchema arg)
        {
            var tokenName = shortName ? current[1..] : current[2..];
            
            // At this point the user has attempted to indicate a switch name and moved on. If its not valid, thats an error.
            // Search in schema switches since those are static and not consumed as we go
            arg = shortName 
                ? context.Schema.GetSwitches().FirstOrDefault(a => ((SwitchAttribute)a.ArgumentAttribute).ShortName == tokenName[0]) 
                : context.Schema.GetSwitches().FirstOrDefault(s => s.Name == tokenName);
            
            if (arg == null)
            {
                // Return this so it can be suggested as an error
                switchSuggestions.Add($"Unrecognized switch arg: {tokenName}");
                return SuggestStatus.Error;
            }

            // Remove this from the pool now that its used
            context.Switches.Remove(arg);
            context.MarkAnyArgumentTaken();    
            
            // User typed "--switch " or "-s ", lets predict values
            if (!context.HasMoreTokens && predictNext)
            {
                // Arg name is valid, however we don't suggest bool values because they are implicitly true and 
                // as soon as another token is typed it will recognize it as not part of this switch and the hint is confusing
                if (arg.Type == typeof(bool))
                {
                    return SuggestStatus.Continue;
                }
                    
                // The argument name is valid and not a bool, lets suggest values
                if (!GetUserSuggestions(context, predictedLine, arg, out switchSuggestions))
                {
                    switchSuggestions.Clear();
                    switchSuggestions.Add(TypeHintString(arg));
                    return SuggestStatus.Hint;
                }
                    
                switchSuggestions = switchSuggestions.Take(MaxSuggestions).ToList();
                return SuggestStatus.Values;
            }
                
            // The '!context.HasMoreTokens && !predictNext' case is handled before this function is called, so now 
            // the user typed at least the switch and a value now 
            
            var beforeConsumption = context.Stream.Explode();
            var adapterResult = _context.TypeAdapterRegistry.TryConvert(arg.Type, context.Stream, out _);
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
                    return SuggestStatus.Error;
                }
                    
                // There are no more tokens, return values or a hint
                if (!GetUserSuggestions(context, predictedLine, arg, out switchSuggestions, filter:consumedString))
                {
                    switchSuggestions.Clear();
                        
                    // If the value is wrong but the user has not moved on, tell them
                    if (adapterResult.TokensDesired == consumed)
                    {
                        switchSuggestions.Add($"{TypeHintString(arg)} - current value '{consumedString}' is invalid");
                    }
                    // If it is wrong but, they have not entered enough tokens, just wait for more
                    else
                    {
                        switchSuggestions.Add(TypeHintString(arg));
                    }

                    return SuggestStatus.Hint;
                }

                return SuggestStatus.Values;
            }
                
            // We have a valid switch and value, just return how many tokens we consumed and continue
            if (context.HasMoreTokens || predictNext)
            {
                return SuggestStatus.Continue;
            }

            // We are still on the current token, so return suggested values or a hint. 
            // It is okay that we overwrite consumed tokens here because out suggestions will be the same length
            // and the hint will not be applied to input string ever
            if (!GetUserSuggestions(context, predictedLine, arg, out switchSuggestions, filter:consumedString))
            {
                switchSuggestions.Clear();
                switchSuggestions.Add(TypeHintString(arg));
                return SuggestStatus.Hint;
            }

            return SuggestStatus.Values;
        }

        private SuggestStatus HandleOrderedToken(CompletionContext context, string current, bool predictNext, string predictedLine, out List<Suggestion> suggestions)
        {
            // Being in this function means the user has typed somthing that is not a switch. The 
            // current input looks like this: "<cmd> [something or nothing] <something>[something or nothing]"
            suggestions = new List<Suggestion>();

            // This will hold all possible options in the case that nothing matches exactly
            var options = new List<Suggestion>();

            // If we have subcommands, prefer matching those
            if (context.TryGetValidSubcommands(out var subcommands))
            {
                var names = subcommands.Select(s => s.Name).ToList();
                
                if (names.Contains(current))
                {
                    context.UpdateSchema(subcommands.First(s => s.Name == current));
                    suggestions.Add(new Suggestion(current, $"{Format.Color("subcommand", Format.Yellow)}: {current}"));
                    return SuggestStatus.Continue;
                }
                
                // No exact match, record options for later
                options.AddRange(names.Select(n => new Suggestion(n, $"{Format.Color("subcommand", Format.Yellow)}: {n}")));
            }

            // If we have an ordered argument remaining, see if this value is valid for it.
            if (context.TryGetNextOrderedArg(out var arg, out var positional))
            {
                // We need to rebuild the stream with the current token since this token is a value, not a name like for switches
                var tokens = context.Stream.Explode();
                tokens.Insert(0, current);

                var stream = new TokenStream(tokens);
                var adapterResult = _context.TypeAdapterRegistry.TryConvert(arg.Type, stream, out _, true);
                
                // Add one to account for the already popped 'current' string
                var consumed = 1 + context.Stream.Remaining - stream.Remaining;
                var consumedString = string.Join(" ", tokens.Take(consumed));

                context.Stream = stream;

                // The current value matches this argument
                if (adapterResult.Success)
                {
                    // The user moved on, lock in this value
                    if (context.HasMoreTokens || predictNext)
                    {
                        context.RemoveArg(arg);
                        context.MarkAnyArgumentTaken();
                        
                        suggestions.Clear();
                        suggestions.Add(new Suggestion(consumedString, ""));

                        return SuggestStatus.Continue;
                    }
                    
                    // The user has not moved on, suggest values including subcommands if this is a string and we have some
                    return SuggestCommandsAndValues(context, current, predictedLine, ref suggestions, arg, options);
                }

                // The adapt result failed
                if (adapterResult.TokensDesired == consumed)
                {
                    // Check if variadic is an option and prefer it since the current value does not work for optionals
                    if (!positional && context.Variadic != null)
                    {
                        stream = new TokenStream(tokens);
                        adapterResult = _context.TypeAdapterRegistry.TryConvert(context.Variadic.Type.GetElementType(), stream, out _, true);
                            
                        if (adapterResult.Success)
                        {
                            context.ForceVariadic();
                            context.MarkAnyArgumentTaken();
                            suggestions.Clear();

                            if (context.HasMoreTokens || predictNext)
                            {
                                suggestions.Add(new Suggestion(consumedString, ""));
                                return SuggestStatus.Continue;
                            }
                            
                            suggestions.Add(new Suggestion("", TypeHintString(context.Variadic), isHint:true));
                            return SuggestStatus.Hint;
                        }
                    }
                    
                    // The result failed and the user moved on
                    if (context.HasMoreTokens || predictNext)
                    {
                        // Since it did not match the optional or variadic, this is an error
                        suggestions.Clear();
                        suggestions.Add(new Suggestion("", $"Invalid input for arg {arg.Name} <{arg.Type.GetFriendlyName()}>: '{consumedString}'", isError:true));
                        return SuggestStatus.Error;
                    }
                    
                    // The result failed - fall through to suggestions
                }
                
                // The result failed and we either don't have enough tokens or haven't moved on yet.
                // At this point, variadic is not an option.
                if (!GetUserSuggestions(context, predictedLine, arg, out var newUserSuggestions, filter: consumedString))
                {
                    suggestions.Clear();

                    string hint;
                    
                    // If the value is wrong but the user has not moved on, tell them
                    if (adapterResult.TokensDesired == consumed)
                    {
                        hint = $"{TypeHintString(arg)} - current value '{consumedString}' is invalid";
                    }
                    // If it is wrong but, they have not entered enough tokens, just wait for more.
                    // Note, this cannot be a subcommand since those are single tokens
                    else
                    {
                        hint = TypeHintString(arg);
                    }
                    
                    suggestions.Add(new Suggestion("", hint, isHint:true));
                    return SuggestStatus.Hint;
                }

                // Adaptation failed but the user is still working on it, check invalid before suggesting
                if (!context.HasMoreTokens && !predictNext && adapterResult.TokensDesired == consumed)
                {
                    suggestions.Clear();
                    var hint = $"{TypeHintString(arg)} - current value '{consumedString}' is invalid";
                    suggestions.Add(new Suggestion("", hint, isHint:true));
                    return SuggestStatus.Hint;
                }
                
                return SuggestCommandsAndValues(context, current, predictedLine, ref suggestions, arg, options, newUserSuggestions);
            }
            
            // This means we have no ordered args. Lets check variadic
            if (context.IsOnVariadic)
            {
                // We need to rebuild the stream with the current token since this token is a value, not a name like for switches
                var tokens = context.Stream.Explode();
                tokens.Insert(0, current);

                var stream = new TokenStream(tokens);
                var adapterResult = _context.TypeAdapterRegistry.TryConvert(context.Variadic.Type.GetElementType(), stream, out _, true);
                
                // Add one to account for the already popped 'current' string
                var consumed = 1 + context.Stream.Remaining - stream.Remaining;
                var consumedString = string.Join(" ", tokens.Take(consumed));

                context.Stream = stream;

                // The current value matches this argument
                if (adapterResult.Success)
                {
                    // The user moved on, lock in this value
                    if (context.HasMoreTokens || predictNext)
                    {
                        context.MarkAnyArgumentTaken();
                        
                        suggestions.Clear();
                        suggestions.Add(new Suggestion(consumedString, ""));

                        return SuggestStatus.Continue;
                    }
                    
                    // The user has not moved on, suggest values including subcommands if this is a string and we have some
                    return SuggestCommandsAndValues(context, current, predictedLine, ref suggestions, context.Variadic, options);
                }
                
                // The type adaptation failed and we have enough tokens
                if (adapterResult.TokensDesired == consumed && (context.HasMoreTokens || predictNext))
                {
                    // The user moved on, this is an error
                    suggestions.Clear();
                    suggestions.Add(new Suggestion("", $"Invalid input for arg {context.Variadic.Name} <{context.Variadic.Type.GetElementType().GetFriendlyName()}>: '{consumedString}'", isError:true));
                    return SuggestStatus.Error;
                }

                // The adaptation failed but we either have not input enough tokens yet or have not moved on - suggest values
                if (!GetUserSuggestions(context, predictedLine, context.Variadic, out var newUserSuggestions, filter: consumedString))
                {
                    suggestions.Clear();
                    string hint;
                    
                    // If the value is wrong but the user has not moved on, tell them
                    if (adapterResult.TokensDesired == consumed)
                    {
                        hint = $"{TypeHintString(context.Variadic)} - current value '{consumedString}' is invalid";
                    }
                    // If it is wrong but, they have not entered enough tokens, just wait for more.
                    // Note, this cannot be a subcommand since those are single tokens
                    else
                    {
                        hint = TypeHintString(context.Variadic);
                    }
                    
                    suggestions.Add(new Suggestion("", hint, isHint:true));
                    return SuggestStatus.Hint;
                }

                // Adaptation failed but the user is still working on it, check invalid before suggesting
                if (!context.HasMoreTokens && !predictNext && adapterResult.TokensDesired == consumed)
                {
                    suggestions.Clear();
                    var hint = $"{TypeHintString(context.Variadic)} - current value '{consumedString}' is invalid";
                    suggestions.Add(new Suggestion("", hint, isHint:true));
                    return SuggestStatus.Hint;
                }
                
                // We have suggestions - return them
                return SuggestCommandsAndValues(context, current, predictedLine, ref suggestions, context.Variadic, options, newUserSuggestions);
            }
            
            // Being here means there are no args to match - suggest subcommands if possible
            var matches = BestMatches(current, options.Select(o => o.FullText).ToList());

            if (matches.Count == 0)
            {
                suggestions.Clear();
                suggestions.Add(new Suggestion(current, ""));
                return SuggestStatus.Continue;
            }

            // Get the suggestions back in order from the best options
            suggestions = matches
                .Select(m => options.FirstOrDefault(o => o.FullText == m))
                .Where(option => option != null)
                .Select(s => new Suggestion($"{predictedLine} {s.FullText}", s.DisplayText))
                .ToList();
            
            return SuggestStatus.Values;
        }

        private static string TypeHintString(ArgumentSchema arg)
        {
            return $"{arg.Name}: <{arg.Type.GetFriendlyName()}>";
        }

        private SuggestStatus SuggestCommandsAndValues(CompletionContext context, string current, string predictedLine,
            ref List<Suggestion> suggestions, ArgumentSchema arg, List<Suggestion> options, List<string> userSuggestions = null)
        {
            var currentOptions = arg.Type != typeof(string) ? new List<string>() : options.Select(s => s.FullText).ToList();

            if (userSuggestions == null)
            {
                if (GetUserSuggestions(context, predictedLine, arg, out userSuggestions, filter: current))
                {
                    currentOptions.AddRange(userSuggestions);
                }
            }
                    
            // Predict best values
            var ordered = BestMatches(current, currentOptions);
            if (ordered.Count > 0)
            {
                suggestions = ordered.Select(o =>
                {
                    var option = options.FirstOrDefault(s => s.FullText == o);
                    return new Suggestion($"{predictedLine} {o}", option != null ? option.DisplayText : $"{Format.Color(arg.Name, Format.Yellow)}: {o}");
                }).ToList();

                return SuggestStatus.Values;
            }
            // No suggestions, just inform that we know what the user is doing
            suggestions.Clear();
            suggestions.Add(new Suggestion("", TypeHintString(arg), isHint:true));
            return SuggestStatus.Hint;
        }

        private List<Suggestion> SuggestNext(CompletionContext context, string predictedLine)
        {
            var result = new List<Suggestion>();
            
            // --- Stream exhausted, suggest what comes next ---
            if (context.TryGetValidSubcommands(out var subcommands))
            {
                result.AddRange(subcommands.Take(MaxSuggestions).Select(s => new Suggestion($"{predictedLine} {s.Name}", $"{Format.Color("subcommand", Format.Yellow)}: {s.Name}")));
            }
            if (context.TryGetNextOrderedArg(out var arg, out _))
            {
                // If we have no suggestions and there are not allowed commands, return a hint
                if (!GetUserSuggestions(context, predictedLine, arg, out var suggestions) && result.Count == 0)
                {
                    result.Add(new Suggestion("", TypeHintString(arg), isHint:true));
                    return result;
                }
                result.AddRange(suggestions.Select(s => new Suggestion($"{predictedLine} {s}", $"{Format.Color(arg.Name, Format.Yellow)}: {s}")));
                result = result.Take(MaxSuggestions).ToList();
            }
            else if (context.IsOnVariadic)
            {
                // If we have no suggestions and there are not allowed commands, return a hint
                if (!GetUserSuggestions(context, predictedLine, context.Variadic, out var suggestions) && result.Count == 0)
                {
                    result.Add(new Suggestion("", $"{context.Variadic.Name}: <{context.Variadic.Type.GetFriendlyName()}>", isHint:true));
                    return result;
                }
                result.AddRange(suggestions.Take(MaxSuggestions).Select(s => new Suggestion($"{predictedLine} {s}", $"{Format.Color(context.Variadic.Name, Format.Yellow)}: {s}")));
            }

            if (result.Count == 0)
            {
                result.Add(new Suggestion("", "Ready to execute", isHint:true));
            }

            return result;
        }
        
        private bool GetUserSuggestions(CompletionContext context, string predictedLine, ArgumentSchema arg, out List<string> suggestions, string filter = null)
        {
            suggestions = new List<string>();
            
            var suggestor = arg.FieldInfo.GetCustomAttribute<SuggestAttribute>();

            if (suggestor == null)
            {
                return false;
            }
            
            var type = suggestor.Type ?? context.Schema.CommandType;
                    
            var method = type.GetMethod(suggestor.FunctionName, BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);

            if (method == null)
            {
                Debug.LogWarning($"No {Format.Bold("static")} suggestion function with name '{suggestor.FunctionName}' found on type {type}");
                return false;
            }

            if (!method.IsStatic)
            {
                Debug.LogWarning($"Suggestion function must be static");
                return false;
            }
            
            if (!typeof(IEnumerable<string>).IsAssignableFrom(method.ReturnType))
            {
                Debug.LogWarning($"Suggestion function must return a type assignable to IEnumerable<string>");
                return false;
            }

            if (method.GetParameters().Length > 1)
            {
                Debug.LogWarning($"Suggestion function may only have a parameter of type {typeof(SuggestionContext)}, or no parameters");
                return false;
            }

            object[] args = null;
            if (method.GetParameters().Length == 1)
            {
                if (method.GetParameters()[0].ParameterType != typeof(SuggestionContext))
                {
                    Debug.LogWarning($"Suggestion function has a parameter but it was not of type {typeof(SuggestionContext)}");
                    return false;
                }

                var completion = new ReadOnlyCompletionContext(context, Tokenizer.Tokenize(predictedLine));
                args = new object[] { new SuggestionContext(_context, context.Schema, completion) };
            }
            
            suggestions = ((IEnumerable<string>)method.Invoke(null, args)).ToList();

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

        private enum SuggestStatus
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
        
        internal class CompletionContext
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
            
            public CommandSchema Schema;
            public List<ArgumentSchema> Switches;
            public List<ArgumentSchema> Positionals;
            public List<ArgumentSchema> Optionals;
            public ArgumentSchema Variadic;
            public TokenStream Stream;

            private bool _anyArgConsumed;
            
            public CompletionContext(CommandSchema schema, TokenStream stream)
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
            public bool TryGetNextOrderedArg(out ArgumentSchema argument, out bool positional)
            {
                positional = Positionals.Count > 0;
                argument = positional ? Positionals.First() : Optionals.FirstOrDefault();
                return argument != null;
            }

            /// <summary>
            /// Gets a list of subcommands if there are any.
            /// </summary>
            /// <param name="subcommands">The subcommands.</param>
            /// <returns>True if there are any subcommands.</returns>
            public bool TryGetValidSubcommands(out List<CommandSchema> subcommands)
            {
                subcommands = null;

                if (Schema.ChildSchemas.Count == 0 || _anyArgConsumed)
                {
                    return false;
                }

                subcommands = Schema.ChildSchemas.ToList();
                return true;
            }

            /// <summary>
            /// Removes an ordered argument.
            /// </summary>
            /// <param name="arg">The arg to remove.</param>
            public void RemoveArg(ArgumentSchema arg)
            {
                MarkAnyArgumentTaken();
                if (!Positionals.Remove(arg))
                {
                    Optionals.Remove(arg);
                }
            }

            /// <summary>
            /// Clears any remaining options args.
            /// </summary>
            public void ForceVariadic()
            {
                Optionals.Clear();
            }

            /// <summary>
            /// Used to declare 1 or more arguments have been used and thus subcommands are no longer valid.
            /// </summary>
            /// <returns></returns>
            public void MarkAnyArgumentTaken()
            {
                _anyArgConsumed = true;
            }

            public void UpdateSchema(CommandSchema newSchema)
            {
                if (_anyArgConsumed) throw new InvalidOperationException("Cannot update schema after an argument has been consumed!");
                
                Schema = newSchema;
                Switches = Schema.GetSwitches();
                Positionals = Schema.GetOrderedPositionalArguments();
                Optionals = Schema.GetOrderedOptionalArguments();
                Schema.TryGetVariadic(out Variadic);
            }
        }
    }
}