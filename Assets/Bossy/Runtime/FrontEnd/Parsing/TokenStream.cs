using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Bossy.FrontEnd.Parsing
{
    /// <summary>
    /// A managed stream of tokens.
    /// </summary>
    public class TokenStream
    {
        private int _cursor;
        private readonly List<string> _parts;
        
        /// <summary>
        /// Creates a new token stream.
        /// </summary>
        /// <param name="line">The input to tokenize.</param>
        public TokenStream(string line)
        {
            _parts = Tokenize(line);
        }

        /// <summary>
        /// Tries to get multiple tokens from the stream.
        /// </summary>
        /// <param name="count">The number of tokens to get.</param>
        /// <param name="tokens">The returned tokens.</param>
        /// <returns>Whether there were enough remaining tokens or not.</returns>
        public bool TryConsume(int count, out List<string> tokens)
        {
            tokens = new List<string>();
            if (_cursor + count > _parts.Count) return false;
            
            tokens.AddRange(_parts.GetRange(_cursor, count));
            _cursor += count;

            return true;
        }

        /// <summary>
        /// Tries to get a single token from the stream.
        /// </summary>
        /// <param name="token">The returned token.</param>
        /// <returns>Whether there was a token to consume or not.</returns>
        public bool TryConsume(out string token)
        {
            token = null;
            
            var success = TryConsume(1, out var tokens);

            if (success)
            {
                token = tokens[0];
            }

            return success;
        }

        /// <summary>
        /// Tries to read the next token without consuming it.
        /// </summary>
        /// <param name="token">The token being peeked.</param>
        /// <returns>True if there is another token.</returns>
        public bool TryPeek(out string token)
        {
            token = null;
            if (_cursor >= _parts.Count) return false;

            token = _parts[_cursor];
            return true;
        }
        
        private List<string> Tokenize(string line)
        {
            var matches = Regex.Matches(line, @"(?:\\.|""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'|\S)+");

            var result = new List<string>();
            foreach (Match match in matches)
            {
                var token = Regex.Replace(match.Value,
                    @"""([^""\\]*(?:\\.[^""\\]*)*)""|'([^'\\]*(?:\\.[^'\\]*)*)'|\\(.)|(.)",
                    m => {
                        if (m.Groups[1].Success) return Unescape(m.Groups[1].Value);
                        if (m.Groups[2].Success) return Unescape(m.Groups[2].Value);
                        if (m.Groups[3].Success) return m.Groups[3].Value == "\\" ? "\\" : m.Groups[3].Value;
                        return m.Groups[4].Value;
                    });
                result.Add(token);
            }
            return result;
        }

        private static string Unescape(string s) =>
            Regex.Replace(s, @"\\(.)", m => m.Groups[1].Value == "\\" ? "\\" : m.Groups[1].Value);
    }
}