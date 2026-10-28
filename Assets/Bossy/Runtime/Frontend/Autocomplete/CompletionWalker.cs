using System;
using System.Collections.Generic;
using System.Linq;
using Bossy.Command;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Utils;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// Walks the tokens of one command, the way the parser binds them, and completes the slot at the cursor.
    /// Each token before the cursor is either read and the walk goes on, or it ends the walk with a result,
    /// such as an error. Rule numbers (R1 to R31, B1 to B9) and proposal numbers refer to the rule catalogue in
    /// Documentation/AutocompleteRules.md, each pinned by a test in AutocompleteEngineTest.
    /// </summary>
    internal class CompletionWalker
    {
        private readonly BossyContext _context;
        private readonly OperatorList _operators;
        private readonly UserSuggestions _userSuggestions;
        private readonly IReadOnlyList<Token> _tokens;
        private readonly CompletionSlot _slot;
        private readonly ArgumentCursor _cursor;

        // The index of the next token to read
        private int _index;

        // The first validator that threw during the walk, reported with the result
        private SuggestionFailure _validatorFailure;

        /// <summary>
        /// Creates a walker for one command.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        /// <param name="operators">The operators, used to decide when inserted values need quotes.</param>
        /// <param name="userSuggestions">The suggestion functions and their cache.</param>
        /// <param name="root">The root command schema.</param>
        /// <param name="tokens">The tokens after the command name and before the slot, with aliases expanded.</param>
        /// <param name="slot">The slot at the cursor.</param>
        public CompletionWalker(BossyContext context, OperatorList operators, UserSuggestions userSuggestions,
            CommandSchema root, IReadOnlyList<Token> tokens, CompletionSlot slot)
        {
            _context = context;
            _operators = operators;
            _userSuggestions = userSuggestions;
            _tokens = tokens;
            _slot = slot;
            _cursor = new ArgumentCursor(root);
        }

        /// <summary>
        /// Walks the tokens and completes the slot.
        /// </summary>
        /// <returns>The result.</returns>
        public CompletionResult Walk()
        {
            var result = ReadTokens() ?? CompleteSlot();
            return _validatorFailure != null ? result.WithFailure(_validatorFailure) : result;
        }

        // --- Reading tokens before the cursor ---

        private CompletionResult ReadTokens()
        {
            while (_index < _tokens.Count)
            {
                var token = _tokens[_index];

                var result = IsSwitch(token) ? ReadSwitch(token.Text) : ReadOrdered(token);

                if (result != null) return result;
            }

            return null;
        }

        private CompletionResult ReadSwitch(string text)
        {
            _index++;

            if (text == "--")
            {
                _cursor.Surrendered = true;
                return null;
            }

            // R12
            if (text == "-")
            {
                return CompletionResult.Error("'-' is not a valid token");
            }

            ArgumentSchema argument;

            // R15: an unknown switch is an error once the user moves on
            if (text.StartsWith("--"))
            {
                if (!_cursor.Schema.TryFindSwitch(text[2..], out argument))
                {
                    return CompletionResult.Error(new InvalidSwitchError(text).Message);
                }
            }
            else
            {
                // R13: "-abc" is a group of bool switches, each one used
                argument = null;
                foreach (var shortName in text[1..])
                {
                    if (!_cursor.Schema.TryFindSwitch(shortName, out argument))
                    {
                        return CompletionResult.Error(new InvalidSwitchError($"-{shortName}").Message);
                    }

                    if (text.Length > 2 && argument.Type != typeof(bool))
                    {
                        return CompletionResult.Error(new NonBoolAggregateSwitchError(text, shortName).Message);
                    }

                    _cursor.TakeSwitch(argument);
                    _cursor.Bind(argument, true, string.Empty);
                }
            }

            _cursor.TakeSwitch(argument);

            // R16 to R18: the switch reads its value greedily from the tokens after it, as in the parser
            var read = ReadValue(argument.Type, false, false);

            if (read.State == ValueState.Read)
            {
                _index += read.TokensRead;
                return Bind(argument, read);
            }

            return ValueNotRead(argument, argument.Type, read);
        }

        private CompletionResult ReadOrdered(Token token)
        {
            // R19: an exact subcommand name before any argument switches the schema
            if (_cursor.CanTakeSubcommand)
            {
                var subcommand = _cursor.Schema.ChildSchemas.FirstOrDefault(c => c.Name == token.Text);
                if (subcommand != null)
                {
                    _cursor.EnterSubcommand(subcommand);
                    _index++;
                    return null;
                }
            }

            // R20 to R23
            if (_cursor.TryGetNextOrdered(out var argument, out var positional))
            {
                var read = ReadValue(argument.Type, true, true);

                if (read.State == ValueState.Read)
                {
                    _cursor.TakeOrdered(argument);
                    _index += read.TokensRead;
                    return Bind(argument, read);
                }

                // R22: a value that does not fit an optional goes to the variadic instead, as in the parser
                if (!positional && _cursor.Variadic != null && read.IsCompleteAndInvalid)
                {
                    _cursor.SkipOptionals();
                    return null;
                }

                return ValueNotRead(argument, argument.Type, read);
            }

            // R24
            if (_cursor.Variadic != null)
            {
                var elementType = _cursor.Variadic.Type.GetElementType();
                var read = ReadValue(elementType, true, true);

                if (read.State == ValueState.Read)
                {
                    _cursor.TakeVariadicValue();
                    _index += Math.Max(1, read.TokensRead);
                    return Bind(_cursor.Variadic, read);
                }

                return ValueNotRead(_cursor.Variadic, elementType, read);
            }

            // Every argument is filled, so the parser rejects anything more
            return CompletionResult.Error(new UnexpectedTokensError(_tokens.Skip(_index).Select(t => t.Text).ToList()).Message);
        }

        // A value before the cursor that fails a validator is an error once the user moves on, as in the parser
        private CompletionResult Bind(ArgumentSchema argument, ValueRead read)
        {
            if (FailedValidator(argument, read.Value, out var message) != null)
            {
                return CompletionResult.Error(new ArgumentValidationError(message, argument.Name).Message);
            }

            _cursor.Bind(argument, read.Value, read.Text);
            return null;
        }

        private CompletionResult ValueNotRead(ArgumentSchema argument, Type type, ValueRead read)
        {
            return read.State switch
            {
                ValueState.Invalid => CompletionResult.Error(new TypeAdaptError(type, _tokens[_index].Text, read.ErrorMessage).Message),
                ValueState.Split   => CompletionResult.Error(new SplitValueError(read.Text, argument.Name).Message),
                _                  => CompleteValue(argument, read)
            };
        }

        private bool IsSwitch(Token token)
        {
            // R10: after "--", nothing is a switch
            return !_cursor.Surrendered && token.IsSwitch;
        }

        // --- Completing the slot ---

        private CompletionResult CompleteSlot()
        {
            // R2: with no token at the cursor, predict the next one
            if (_slot.IsEmpty)
            {
                return CompleteNext();
            }

            if (!_cursor.Surrendered && _slot.IsSwitch)
            {
                return CompleteSwitchName();
            }

            if (_cursor.TryGetNextOrdered(out var argument, out var positional))
            {
                var read = ReadValue(argument.Type, true, true);

                // R22 at the cursor
                if (!positional && _cursor.Variadic != null && read.IsCompleteAndInvalid)
                {
                    _cursor.SkipOptionals();
                    return CompleteSlot();
                }

                return CompleteValue(argument, read);
            }

            if (_cursor.Variadic != null)
            {
                return CompleteValue(_cursor.Variadic, ReadValue(_cursor.Variadic.Type.GetElementType(), true, true));
            }

            // R25: with no arguments left, only a subcommand can follow
            var subcommands = Rank(SubcommandCompletions());
            return subcommands.Count > 0
                ? CompletionResult.List(subcommands)
                : CompletionResult.Error(new UnexpectedTokensError(new List<string> { _slot.Typed }).Message);
        }

        private CompletionResult CompleteNext()
        {
            // R26: suggest subcommands and the next value together
            if (_cursor.Positionals.Count > 0)
            {
                var positional = _cursor.Positionals[0];
                return CompleteValue(positional, ReadValue(positional.Type, true, true));
            }

            // Every positional is filled, so the line can run. The optional parts stay discoverable.
            var completions = SubcommandCompletions();
            SuggestionFailure failure = null;
            var next = _cursor.TryGetNextOrdered(out var optional, out _) ? optional : _cursor.Variadic;
            if (next != null)
            {
                completions.AddRange(ValueCompletions(next, out failure));
            }

            if (!_cursor.Surrendered)
            {
                completions.AddRange(_cursor.Switches.Select(s => SwitchCompletion(s, false)));
            }

            return CompletionResult.Ready(ReadyMessage(), Distinct(completions), failure);
        }

        private CompletionResult CompleteValue(ArgumentSchema argument, ValueRead read)
        {
            var completions = SubcommandCompletions();
            SuggestionFailure failure = null;

            // Suggested values are whole values, so they only apply to the first token of a value
            if (read.TokenNumber == 1)
            {
                completions.AddRange(ValueCompletions(argument, out failure));
            }

            // R17, R23: the value has all its tokens but does not parse
            var invalidValue = read.IsCompleteAndInvalid ? read.Text : null;

            // A value at the cursor that fails a validator is still being typed, so it is not an error yet
            ArgumentValidationAttribute failedValidator = null;
            string validationError = null;
            if (read.State == ValueState.AtSlot && read.IsValid)
            {
                failedValidator = FailedValidator(argument, read.Value, out validationError);
            }

            var hint = new ArgumentHint(argument, read.TokenNumber, read.TokensDesired, invalidValue, failedValidator, validationError);

            return CompletionResult.Hint(HintText(hint), Rank(completions), hint, failure);
        }

        private CompletionResult CompleteSwitchName()
        {
            var typed = _slot.Typed;

            // R10, R12: "-" and "--" list every switch left
            // TODO: Make listing short or long names on "-" a Bossy setting once settings are implemented.
            if (typed == "-" || typed == "--")
            {
                return CompletionResult.List(_cursor.Switches.Select(s => SwitchCompletion(s, false)).ToList());
            }

            // R11
            if (typed.StartsWith("--"))
            {
                var matches = FuzzyMatcher.Rank(typed[2..], _cursor.Switches, s => s.Name);
                return matches.Count > 0
                    ? CompletionResult.List(matches.Select(s => SwitchCompletion(s, false)).ToList())
                    : CompletionResult.Hint($"No switch matches '{typed}'");
            }

            // R14: a short name lists the short switches, with an exact match first
            if (typed.Length == 2)
            {
                var ordered = _cursor.Switches.OrderBy(s => ShortName(s) == typed[1] ? 0 : 1);
                return CompletionResult.List(ordered.Select(s => SwitchCompletion(s, true)).ToList());
            }

            // R13: a group of bool switches
            var letters = typed[1..];
            if (letters.All(c => _cursor.Schema.TryFindSwitch(c, out var s) && s.Type == typeof(bool)))
            {
                return CompletionResult.Hint($"Aggregate flags: {string.Join(", ", letters.Select(c => $"-{c}"))}");
            }

            // R13: a long name typed with one dash
            var longNames = _cursor.Switches.Where(s => s.Name.StartsWith(letters, StringComparison.OrdinalIgnoreCase)).ToList();
            if (longNames.Count > 0)
            {
                return CompletionResult.List(longNames.Select(s => SwitchCompletion(s, false)).ToList());
            }

            foreach (var letter in letters)
            {
                if (!_cursor.Schema.TryFindSwitch(letter, out var argument))
                {
                    return CompletionResult.Error(new InvalidSwitchError($"-{letter}").Message);
                }

                if (argument.Type != typeof(bool))
                {
                    return CompletionResult.Error(new NonBoolAggregateSwitchError(typed, letter).Message);
                }
            }

            return CompletionResult.Empty;
        }

        // --- Values ---

        /*
         * Runs the type adapter on the tokens from the current index, plus the text typed in the slot, and works
         * out where the value ends: inside the tokens already typed, or at the slot because it reaches the cursor.
         * An ordered value's tokens stop at the next switch, since the parser rejects a value split by a switch.
         * A switch's own value reads greedily, switches included, as in the parser.
         */
        private ValueRead ReadValue(Type type, bool requireExplicitBool, bool isOrdered)
        {
            var available = 0;
            while (_index + available < _tokens.Count && !(isOrdered && IsSwitch(_tokens[_index + available])))
            {
                available++;
            }

            var splitBy = _index + available < _tokens.Count ? _tokens[_index + available].Text : null;
            var texts = _tokens.Skip(_index).Take(available).Select(t => t.Text).ToList();

            if (!_slot.IsEmpty && splitBy == null)
            {
                texts.Add(_slot.Typed);
            }

            var stream = new TokenStream(texts);
            var result = _context.TypeAdapterRegistry.TryConvert(type, stream, out var value, requireExplicitBool);

            if (result.Success)
            {
                var read = stream.Cursor;
                return read <= available
                    ? ValueRead.InTokens(read, value, string.Join(" ", texts.Take(read)))
                    : ValueRead.AtSlot(true, true, read, read, string.Join(" ", texts), value);
            }

            var desired = Math.Max(1, result.TokensDesired);
            var valueText = string.Join(" ", texts.Take(desired));

            if (desired <= available)
            {
                return ValueRead.Invalid(result.ErrorMessage);
            }

            if (splitBy != null)
            {
                return ValueRead.Split(splitBy);
            }

            var complete = desired <= texts.Count;
            return ValueRead.AtSlot(false, complete, available + 1, desired, valueText);
        }

        /*
         * Runs an argument's validators on one value. A variadic's validators apply to each of its elements, and
         * a variadic is read one element at a time, so the value is always one element. Validators are author code
         * running on every keystroke, so one that throws is reported like a failing suggestion function, and the
         * value is treated as passing it.
         */
        private ArgumentValidationAttribute FailedValidator(ArgumentSchema argument, object value, out string message)
        {
            message = null;

            foreach (var validator in argument.Validators)
            {
                ArgumentValidationResult result;
                try
                {
                    result = validator.Validate(value);
                }
                catch (Exception e)
                {
                    _validatorFailure ??= new SuggestionFailure(argument, e.Message, true);
                    continue;
                }

                if (!result.Success)
                {
                    message = result.Message;
                    return validator;
                }
            }

            return null;
        }

        private List<Completion> ValueCompletions(ArgumentSchema argument, out SuggestionFailure failure)
        {
            // Proposal 14: a failing suggestion function is reported through the failure, never thrown
            var context = new SuggestionContext(_context, _cursor.Schema, argument, _cursor.Values, _slot.Typed);
            var values = _userSuggestions.Get(context, out var error);
            failure = error != null ? new SuggestionFailure(argument, error) : null;

            return values.Select(v => MakeCompletion(Quote(v.Value, _slot.IsQuoted), v.Value, v.Description, CompletionKind.Value)).ToList();
        }

        /*
         * Proposal 12: one quoting rule for every inserted value. A value is quoted when it would otherwise split
         * into several tokens, read as an operator or a switch, or lose its quotes and escapes.
         */
        private string Quote(string value, bool force)
        {
            var needsQuotes = force
                              || value.Length == 0
                              || value.Any(c => char.IsWhiteSpace(c) || c == '"' || c == '\'' || c == '\\')
                              || _operators.ToEnumerable().Any(value.Contains)
                              || new Token(value).IsSwitch;

            return needsQuotes ? $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"" : value;
        }

        // --- Completions ---

        private List<Completion> SubcommandCompletions()
        {
            if (!_cursor.CanTakeSubcommand) return new List<Completion>();

            return _cursor.Schema.ChildSchemas
                .Select(c => MakeCompletion(c.Name, c.Name, c.Description, CompletionKind.Subcommand))
                .ToList();
        }

        /*
         * Proposal 11: each switch row shows both names, its type, and its description. Accepting it inserts the
         * long name, unless the user typed a short name.
         */
        private Completion SwitchCompletion(ArgumentSchema argument, bool insertShortName)
        {
            var shortName = ShortName(argument);
            var names = char.IsLetter(shortName) ? $"-{shortName}, --{argument.Name}" : $"--{argument.Name}";
            var display = argument.Type == typeof(bool) ? names : $"{names} <{argument.Type.GetFriendlyName()}>";
            var insert = insertShortName && char.IsLetter(shortName) ? $"-{shortName}" : $"--{argument.Name}";

            return MakeCompletion(insert, display, argument.Description, CompletionKind.Switch);
        }

        private Completion MakeCompletion(string insertText, string displayText, string description, CompletionKind kind)
        {
            return new Completion(_slot.Start, _slot.End, insertText + _slot.Suffix, displayText, description, kind);
        }

        private List<Completion> Rank(List<Completion> completions)
        {
            return Distinct(FuzzyMatcher.Rank(_slot.Typed, completions, c => c.DisplayText));
        }

        private static List<Completion> Distinct(IEnumerable<Completion> completions)
        {
            var seen = new HashSet<string>();
            return completions.Where(c => seen.Add(c.InsertText)).ToList();
        }

        // --- Messages ---

        /*
         * Proposal 4: a short plain status line for the argument being typed, such as "pos <Vector3> 2/3  invalid".
         * The description and the argument's kind are left out to keep it short; views that want them read the
         * ArgumentHint instead. A failing suggestion function is reported apart, in CompletionResult.SuggestionFailure.
         */
        private static string HintText(ArgumentHint hint)
        {
            var text = $"{hint.Argument.Name} <{hint.Argument.Type.GetFriendlyName()}>";

            if (hint.HasProgress)
            {
                text += $" {hint.TokenNumber}/{hint.TokensDesired}";
            }

            if (hint.InvalidValue != null)
            {
                text += "  invalid";
            }

            if (hint.ValidationError != null)
            {
                text += hint.FailedConstraint >= 0 ? $"  fails: {hint.Constraints[hint.FailedConstraint]}" : "  fails validation";
            }

            return text;
        }

        private string ReadyMessage()
        {
            var parts = new List<string>();

            var optionalCount = _cursor.Optionals.Count + (_cursor.Variadic != null ? 1 : 0);
            if (optionalCount > 0)
            {
                parts.Add($"{optionalCount} optional");
            }

            var switchCount = _cursor.Surrendered ? 0 : _cursor.Switches.Count;
            if (switchCount > 0)
            {
                parts.Add(switchCount == 1 ? "1 switch" : $"{switchCount} switches");
            }

            return parts.Count == 0 ? "Ready" : $"Ready  {string.Join(", ", parts)}";
        }

        private static char ShortName(ArgumentSchema argument)
        {
            return ((SwitchAttribute)argument.ArgumentAttribute).ShortName;
        }

        private enum ValueState
        {
            /// <summary>
            /// The value fits in the tokens before the cursor and parses.
            /// </summary>
            Read,

            /// <summary>
            /// The value has all its tokens before the cursor but does not parse.
            /// </summary>
            Invalid,

            /// <summary>
            /// The value reaches the cursor, so the slot is (part of) this value.
            /// </summary>
            AtSlot,

            /// <summary>
            /// A switch comes before the value has all its tokens, which the parser rejects.
            /// </summary>
            Split
        }

        private readonly struct ValueRead
        {
            public readonly ValueState State;
            public readonly int TokensRead;
            public readonly string ErrorMessage;
            public readonly bool IsValid;
            public readonly bool IsComplete;
            public readonly int TokenNumber;
            public readonly int TokensDesired;
            public readonly string Text;
            public readonly object Value;

            public bool IsCompleteAndInvalid => State == ValueState.Invalid || (State == ValueState.AtSlot && IsComplete && !IsValid);

            private ValueRead(ValueState state, int tokensRead, string errorMessage, bool isValid, bool isComplete, int tokenNumber, int tokensDesired,
                string text, object value = null)
            {
                State = state;
                TokensRead = tokensRead;
                ErrorMessage = errorMessage;
                IsValid = isValid;
                IsComplete = isComplete;
                TokenNumber = tokenNumber;
                TokensDesired = tokensDesired;
                Text = text;
                Value = value;
            }

            public static ValueRead InTokens(int tokensRead, object value, string text) => new(ValueState.Read, tokensRead, null, true, true, 0, tokensRead, text, value);

            public static ValueRead Invalid(string errorMessage) => new(ValueState.Invalid, 0, errorMessage, false, true, 0, 0, string.Empty);

            public static ValueRead Split(string splitBy) => new(ValueState.Split, 0, null, false, true, 0, 0, splitBy);

            public static ValueRead AtSlot(bool isValid, bool isComplete, int tokenNumber, int tokensDesired, string text, object value = null) =>
                new(ValueState.AtSlot, 0, null, isValid, isComplete, tokenNumber, tokensDesired, text, value);
        }
    }
}
