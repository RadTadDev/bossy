using System.Collections.Generic;
using System.Linq;

namespace Bossy.FrontEnd.Parsing
{
    /// <summary>
    /// A managed stream of tokens.
    /// </summary>
    public class TokenStream
    {
        private int _cursor;
        private readonly List<string> _tokens;
        
        /// <summary>
        /// The cursor position of the stream. Is equivalent to number of tokens already consumed.
        /// </summary>
        public int Cursor => _cursor;
        
        /// <summary>
        /// Creates a new token stream.
        /// </summary>
        /// <param name="line">The input to tokenize.</param>
        public TokenStream(string line)
        {
            _tokens = Tokenizer.Tokenize(line);
        }

        /// <summary>
        /// Creates a new token stream.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        public TokenStream(IEnumerable<string> tokens)
        {
            _tokens = tokens.ToList();
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
            if (_cursor + count > _tokens.Count) return false;
            
            tokens.AddRange(_tokens.GetRange(_cursor, count));
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
            if (_cursor >= _tokens.Count) return false;

            token = _tokens[_cursor];
            return true;
        }

        /// <summary>
        /// Gets the remaining tokens as a list.
        /// </summary>
        /// <returns>The list of remaining tokens.</returns>
        public List<string> Explode()
        {
            return _tokens.GetRange(_cursor, _tokens.Count - _cursor);
        }
    }
}