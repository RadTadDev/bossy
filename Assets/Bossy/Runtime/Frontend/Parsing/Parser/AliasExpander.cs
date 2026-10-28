using System.Collections.Generic;

namespace Bossy.Frontend.Parsing
{
    /// <summary>
    /// Expands aliases in a tokenized line. The parser and autocomplete both use it, so they agree on what a line means.
    /// </summary>
    internal static class AliasExpander
    {
        /// <summary>
        /// Replaces every unquoted token that names an alias with the tokens of its expansion. The new tokens keep
        /// the span of the alias. Tokens from an alias typed as an argument remember the expansion, so
        /// <see cref="Collapse"/> can turn them back into one value. Nothing is expanded in "alias remove".
        /// </summary>
        /// <param name="tokens">The tokens of the line.</param>
        /// <param name="aliases">The aliases. May be null.</param>
        /// <param name="operators">The operators, used to tokenize expansions.</param>
        /// <returns>The expanded tokens.</returns>
        public static List<Token> Expand(List<Token> tokens, Dictionary<string, string> aliases, OperatorList operators)
        {
            if (aliases == null || aliases.Count == 0) return tokens;
            if (tokens.Count >= 2 && tokens[0].Text == "alias" && tokens[1].Text == "remove") return tokens;

            var result = new List<Token>();
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.IsOperator || token.IsQuoted || !aliases.TryGetValue(token.Text, out var expansion))
                {
                    result.Add(token);
                    continue;
                }

                // A command name starts the line or follows an operator
                var isCommandName = i == 0 || tokens[i - 1].IsOperator;
                var remembered = isCommandName ? null : expansion;

                foreach (var expanded in Tokenizer.Tokenize(expansion, operators.ToEnumerable()))
                {
                    result.Add(new Token(expanded.Text, expanded.IsOperator, expanded.IsQuoted, token.Start, token.End, remembered));
                }
            }

            return result;
        }

        /// <summary>
        /// If the token at an index came from an alias typed as an argument, replaces the tokens of that alias with
        /// one quoted token holding the whole expansion.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <param name="index">The index of the token to check.</param>
        public static void Collapse(List<Token> tokens, int index)
        {
            var first = tokens[index];
            if (first.AliasExpansion == null) return;

            var count = 1;
            while (index + count < tokens.Count && IsSameAlias(first, tokens[index + count]))
            {
                count++;
            }

            tokens.RemoveRange(index, count);
            tokens.Insert(index, new Token(first.AliasExpansion, isQuoted: true, start: first.Start, end: first.End));
        }

        private static bool IsSameAlias(Token first, Token other)
        {
            return other.AliasExpansion == first.AliasExpansion && other.Start == first.Start;
        }
    }
}
