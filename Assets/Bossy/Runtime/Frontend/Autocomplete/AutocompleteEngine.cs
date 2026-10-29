using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Bossy.Command;
using Bossy.Frontend.Parsing;
using Bossy.Schema;
using Bossy.Schema.Registry;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// The engine that drives autocomplete. It finds the command the cursor is in, checks the commands before it
    /// with the parser, and walks the command's tokens to complete the token at the cursor.
    /// </summary>
    public class AutocompleteEngine
    {
        private readonly BossyContext _context;
        private readonly OperatorList _operators;
        private readonly Dictionary<string, string> _aliases;
        private readonly Parser _parser;
        private readonly UserSuggestions _userSuggestions = new();

        /// <summary>
        /// Creates a new autocomplete engine.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        /// <param name="operators">The operators this terminal is using.</param>
        /// <param name="aliases">The session's aliases, read on every call so new aliases show up at once. May be null.</param>
        public AutocompleteEngine(BossyContext context, OperatorList operators, Dictionary<string, string> aliases = null)
        {
            _context = context;
            _operators = operators;
            _aliases = aliases;
            _parser = new Parser(context.SchemaRegistry, context.TypeAdapterRegistry);
        }

        /// <summary>
        /// Gets completions for an input line. Completions replace the whole token at the cursor and keep the text
        /// after it, but only the text before the cursor is used to decide what to suggest.
        /// </summary>
        /// <param name="line">The full input line.</param>
        /// <param name="cursorPosition">The cursor position, from 0 (before the first character) to the line length.</param>
        /// <returns>The completions and status message.</returns>
        public CompletionResult Complete(string line, int cursorPosition)
        {
            _userSuggestions.BeginCompletion();

            try
            {
                return CompleteLine(line, cursorPosition, null);
            }
            finally
            {
                _userSuggestions.EndCompletion();
            }
        }

        /// <summary>
        /// Lists the command names and aliases that can fill a slot.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <returns>The completions, unranked.</returns>
        internal List<Completion> CommandNameCompletions(CompletionSlot slot)
        {
            var candidates = _context.SchemaRegistry.GetValidSchemas()
                .Select(s => new Completion(slot.Start, slot.End, s.Name + slot.Suffix, s.Name, s.Description, CompletionKind.Command))
                .ToList();

            if (_aliases != null)
            {
                candidates.AddRange(_aliases.Select(a =>
                    new Completion(slot.Start, slot.End, a.Key + slot.Suffix, a.Key, a.Value, CompletionKind.Alias)));
            }

            return candidates;
        }

        /// <summary>
        /// Checks whether a command string would parse, the way the parser checks it.
        /// </summary>
        /// <param name="command">The command string.</param>
        /// <returns>The parse error, or null if it would parse.</returns>
        internal ParseResult CheckCommandString(CommandString command)
        {
            return _parser.TryCheck(command.Text, _operators, _aliases, out var error) ? null : error;
        }

        /*
         * Completes the command inside a slot that opens a quote, as in: watch "get -n |. The text inside the quote
         * is completed as a line of its own, and the completions are moved back into this line. Values inside use
         * the other kind of quote, and inserted text is escaped for the quote around it. Returns null if the slot
         * does not open a quote.
         */
        internal CompletionResult CompleteQuotedCommand(string line, CompletionSlot slot)
        {
            if (!TryUnquote(line, slot, out var quote, out var inner, out var positions))
            {
                return null;
            }

            var innerCursor = positions.Take(inner.Length).Count(p => p < slot.Cursor);
            var result = CompleteLine(inner, innerCursor, quote);

            var moved = result.Completions
                .Select(c => c.MovedTo(positions[c.Start], positions[c.End], Escape(c.InsertText, quote)))
                .ToList();

            return result.WithCompletions(moved);
        }

        private CompletionResult CompleteLine(string line, int cursorPosition, char? enclosingQuote)
        {
            var cursor = Math.Clamp(cursorPosition, 0, line.Length);
            var tokens = Tokenizer.Tokenize(line, _operators.ToEnumerable());

            // R1: tokens after the cursor are ignored
            var before = new List<Token>();
            Token? atCursor = null;
            foreach (var token in tokens)
            {
                if (!token.IsOperator && token.Start < cursor && cursor <= token.End)
                {
                    atCursor = token;
                    break;
                }

                if (token.End > cursor) break;
                before.Add(token);
            }

            var slot = atCursor.HasValue
                ? CompletionSlot.ForToken(atCursor.Value, TypedText(line, atCursor.Value, cursor), cursor)
                : CompletionSlot.At(cursor, cursor < line.Length && !char.IsWhiteSpace(line[cursor]));

            // R3, except inside an open quote for a command string, which is waiting for a command
            if (before.Count == 0 && atCursor == null)
            {
                return enclosingQuote.HasValue ? CompleteCommandName(slot) : CompletionResult.Empty;
            }

            return CompleteCommandAtCursor(line, before, slot, enclosingQuote);
        }

        private CompletionResult CompleteCommandAtCursor(string line, List<Token> before, CompletionSlot slot, char? enclosingQuote)
        {
            var result = CompleteSlot(line, before, slot, enclosingQuote, out var correction);
            result = result.WithMatches(slot);

            // The correction replaces the command name, not the slot, so it has no matches to show
            return correction != null ? result.WithFirst(correction) : result;
        }

        private CompletionResult CompleteSlot(string line, List<Token> before, CompletionSlot slot, char? enclosingQuote, out Completion correction)
        {
            correction = null;

            // Proposal 2: a leading window operator only marks the job as windowed, so completion carries on after it
            if (before.Count > 0 && IsWindowOperator(before[0]))
            {
                before.RemoveAt(0);
            }

            // Proposal 2: anywhere else, the window operator must end the line
            var window = before.FindIndex(IsWindowOperator);
            if (window >= 0)
            {
                if (window != before.Count - 1 || !slot.IsEmpty)
                {
                    return CompletionResult.Error(new BadWindowOperatorError().Message);
                }

                return CheckBefore(line, before[window]) ?? CompletionResult.Ready("Ready (in window)");
            }

            // R8, proposal 1: the commands before the last operator must parse before the next one is completed
            var lastOperator = before.FindLastIndex(t => t.IsOperator);
            if (lastOperator >= 0)
            {
                var error = CheckBefore(line, before[lastOperator]);
                if (error != null) return error;

                before = before.GetRange(lastOperator + 1, before.Count - lastOperator - 1);
            }

            var segment = ExpandAliases(before);
            if (segment == null)
            {
                return CompletionResult.Empty;
            }

            if (segment.Count == 0)
            {
                return CompleteCommandName(slot);
            }

            // R6, proposals 7 and 8: a misspelled command resolves to its closest match, which is offered as a fix
            var commandToken = segment[0];
            if (!TryResolveCommand(commandToken.Text, out var schema, out var corrected))
            {
                return CompletionResult.Error(new NoMatchingCommandError(commandToken.Text).Message);
            }

            if (corrected)
            {
                correction = new Completion(commandToken.Start, commandToken.End, schema.Name, schema.Name,
                    $"Replaces '{commandToken.Text}'", CompletionKind.Command);
            }

            // Values inside a quoted command string use the other kind of quote
            var quote = enclosingQuote == '"' ? '\'' : '"';
            var walker = new CompletionWalker(_context, _operators, _userSuggestions, this, schema, segment.Skip(1).ToList(), line, slot, quote);
            return walker.Walk();
        }

        private CompletionResult CompleteCommandName(CompletionSlot slot)
        {
            // R4, R5: command names and aliases, fuzzy ranked
            var ranked = FuzzyMatcher.Rank(slot.Typed, CommandNameCompletions(slot), c => c.DisplayText);

            // Proposal 8
            return ranked.Count > 0
                ? CompletionResult.List(ranked)
                : CompletionResult.Hint($"No command matches '{slot.Typed}'");
        }

        private bool TryResolveCommand(string name, out CommandSchema schema, out bool corrected)
        {
            corrected = false;

            if (_context.SchemaRegistry.TryResolveSchema(name, out schema) is SchemaQueryStatus.Found)
            {
                return true;
            }

            schema = FuzzyMatcher.Rank(name, _context.SchemaRegistry.GetValidSchemas(), s => s.Name).FirstOrDefault();
            corrected = schema != null;
            return corrected;
        }

        private CompletionResult CheckBefore(string line, Token op)
        {
            var text = line[..op.Start];

            if (string.IsNullOrWhiteSpace(text))
            {
                return CompletionResult.Error(new BadOperatorPositionError(op.Text).Message);
            }

            return _parser.TryCheck(text, _operators, _aliases, out var error) ? null : CompletionResult.Error(error.Message);
        }

        /*
         * B4: aliases are expanded the way the parser expands them. Returns null if an expansion contains an
         * operator, since the command boundaries would then differ from the typed line.
         */
        private List<Token> ExpandAliases(List<Token> tokens)
        {
            var expanded = AliasExpander.Expand(tokens, _aliases, _operators);
            return expanded.Any(t => t.IsOperator) ? null : expanded;
        }

        private bool IsWindowOperator(Token token)
        {
            return token.IsOperator && token.Text == _operators.WindowOperator;
        }

        private static string TypedText(string line, Token token, int cursor)
        {
            // Retokenizing the part before the cursor removes its quotes and escapes, even if a quote is still open
            return string.Concat(Tokenizer.Tokenize(line[token.Start..cursor]).Select(t => t.Text));
        }

        /*
         * Reads a slot made of one quoted string, closed or not, the way the tokenizer reads it. Positions maps each
         * character of the inner text to its index in the line, plus one last entry for the end of the inner text.
         * An escaped character maps to its backslash, so replacing it replaces the escape too.
         */
        private static bool TryUnquote(string line, CompletionSlot slot, out char quote, out string inner, out int[] positions)
        {
            quote = slot.IsEmpty ? '\0' : line[slot.Start];
            inner = null;
            positions = null;
            if (quote != '"' && quote != '\'') return false;

            var text = new StringBuilder();
            var map = new List<int>();
            var i = slot.Start + 1;
            while (i < slot.End && line[i] != quote)
            {
                map.Add(i);
                if (line[i] == '\\' && i + 1 < slot.End)
                {
                    i++;
                }

                text.Append(line[i]);
                i++;
            }

            // Text after the closing quote makes the token more than one quoted string
            if (i < slot.End - 1) return false;

            map.Add(i);
            inner = text.ToString();
            positions = map.ToArray();
            return true;
        }

        private static string Escape(string text, char quote)
        {
            return text.Replace("\\", "\\\\").Replace(quote.ToString(), $"\\{quote}");
        }
    }
}
