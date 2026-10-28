using System;
using System.Collections.Generic;
using System.Linq;
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

            // R3
            if (before.Count == 0 && atCursor == null)
            {
                return CompletionResult.Empty;
            }

            var slot = atCursor.HasValue
                ? CompletionSlot.ForToken(atCursor.Value, TypedText(line, atCursor.Value, cursor))
                : CompletionSlot.At(cursor, cursor < line.Length && !char.IsWhiteSpace(line[cursor]));

            return CompleteCommandAtCursor(line, before, slot);
        }

        private CompletionResult CompleteCommandAtCursor(string line, List<Token> before, CompletionSlot slot)
        {
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

                return CheckBefore(line, before[window]) ?? CompletionResult.Ready("Ready to execute (in window)");
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

            var result = new CompletionWalker(_context, _operators, _userSuggestions, schema, segment.Skip(1).ToList(), slot).Walk();

            if (!corrected) return result;

            var correction = new Completion(commandToken.Start, commandToken.End, schema.Name, schema.Name,
                $"Replaces '{commandToken.Text}'", CompletionKind.Command);
            return result.WithFirst(correction);
        }

        private CompletionResult CompleteCommandName(CompletionSlot slot)
        {
            // R4, R5: command names and aliases, fuzzy ranked
            var candidates = _context.SchemaRegistry.GetValidSchemas()
                .Select(s => new Completion(slot.Start, slot.End, s.Name + slot.Suffix, s.Name, s.Description, CompletionKind.Command))
                .ToList();

            if (_aliases != null)
            {
                candidates.AddRange(_aliases.Select(a =>
                    new Completion(slot.Start, slot.End, a.Key + slot.Suffix, a.Key, a.Value, CompletionKind.Alias)));
            }

            var ranked = FuzzyMatcher.Rank(slot.Typed, candidates, c => c.DisplayText);

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
         * B4: aliases are expanded the way the parser expands them: every unquoted word that names an alias,
         * except in "alias remove". The expanded tokens keep the span of the alias they replace. Returns null if
         * an expansion contains an operator, since the command boundaries would then differ from the typed line.
         */
        private List<Token> ExpandAliases(List<Token> tokens)
        {
            if (_aliases == null || _aliases.Count == 0) return tokens;
            if (tokens.Count >= 2 && tokens[0].Text == "alias" && tokens[1].Text == "remove") return tokens;

            var result = new List<Token>();
            foreach (var token in tokens)
            {
                if (token.IsQuoted || !_aliases.TryGetValue(token.Text, out var expansion))
                {
                    result.Add(token);
                    continue;
                }

                foreach (var expanded in Tokenizer.Tokenize(expansion, _operators.ToEnumerable()))
                {
                    if (expanded.IsOperator) return null;
                    result.Add(new Token(expanded.Text, isQuoted: expanded.IsQuoted, start: token.Start, end: token.End));
                }
            }

            return result;
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
    }
}
