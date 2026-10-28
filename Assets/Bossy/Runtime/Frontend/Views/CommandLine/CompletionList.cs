using Bossy.Frontend.Autocomplete;

namespace Bossy.Frontend
{
    /// <summary>
    /// The completions shown for one input line, with the highlighted row and how accepting it changes the line.
    /// Completions replace spans of the line they were made for, so the line and cursor are kept with them.
    /// </summary>
    internal class CompletionList
    {
        /// <summary>
        /// The result being shown.
        /// </summary>
        public CompletionResult Result { get; }

        /// <summary>
        /// The index of the highlighted completion. Only meaningful when <see cref="HasRows"/> is true.
        /// </summary>
        public int Highlighted { get; private set; }

        /// <summary>
        /// True if there are completions to move through and accept.
        /// </summary>
        public bool HasRows => Result.Completions.Count > 0;

        /// <summary>
        /// A list with nothing in it.
        /// </summary>
        public static readonly CompletionList Empty = new(CompletionResult.Empty, string.Empty, 0);

        private readonly string _line;
        private readonly int _cursor;

        /// <summary>
        /// Makes a list for a result, highlighting the first completion.
        /// </summary>
        /// <param name="result">The result to show.</param>
        /// <param name="line">The line the result was made for.</param>
        /// <param name="cursor">The cursor position the result was made for.</param>
        public CompletionList(CompletionResult result, string line, int cursor)
        {
            Result = result;
            _line = line;
            _cursor = cursor;
        }

        /// <summary>
        /// Moves the highlight, wrapping around at either end.
        /// </summary>
        /// <param name="delta">How many rows to move. Positive moves down.</param>
        public void Move(int delta)
        {
            if (!HasRows) return;

            var count = Result.Completions.Count;
            Highlighted = ((Highlighted + delta) % count + count) % count;
        }

        /// <summary>
        /// Applies the highlighted completion to the line. When it completes the token at the cursor, the cursor
        /// moves past it and a space follows, so the next token can be completed straight away.
        /// </summary>
        /// <param name="line">The new line.</param>
        /// <param name="cursor">The new cursor position.</param>
        /// <returns>False if there is nothing to accept.</returns>
        public bool TryAccept(out string line, out int cursor)
        {
            line = _line;
            cursor = _cursor;
            if (!HasRows) return false;

            var completion = Result.Completions[Highlighted];
            line = completion.ApplyTo(_line);

            // A completion away from the cursor, such as a fix for a misspelled command, shifts the cursor instead
            if (_cursor < completion.Start || _cursor > completion.End)
            {
                var shift = completion.InsertText.Length - (completion.End - completion.Start);
                cursor = _cursor > completion.End ? _cursor + shift : _cursor;
                return true;
            }

            cursor = completion.CursorAfterApply;

            if (cursor < line.Length && char.IsWhiteSpace(line[cursor]))
            {
                cursor++;
            }
            else if (cursor == 0 || !char.IsWhiteSpace(line[cursor - 1]))
            {
                line = line.Insert(cursor, " ");
                cursor++;
            }

            return true;
        }
    }
}
