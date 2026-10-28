using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bossy.Frontend.Parsing
{
    /// <summary>
    /// Handles tokenizing.
    /// </summary>
    public static class Tokenizer
    {
        /// <summary>
        /// Tokenizes a string input into a list of tokens. A quote with no closing quote runs to the end of its
        /// segment, so it becomes a single token.
        /// </summary>
        /// <param name="line">The string to tokenize.</param>
        /// <param name="operators">A list of operators to split on.</param>
        /// <returns>The list of tokens.</returns>
        public static List<Token> Tokenize(string line, IEnumerable<string> operators = null)
        {
            if (string.IsNullOrWhiteSpace(line)) return new List<Token>();

            var ops = operators?.OrderByDescending(o => o.Length).ToList() ?? new List<string>();
            var segments = SplitOnOperators(line, ops);

            var result = new List<Token>();
            foreach (var (segment, isOperator, start) in segments)
            {
                if (isOperator)
                {
                    result.Add(new Token(segment, isOperator: true, start: start, end: start + segment.Length));
                    continue;
                }

                var matches = Regex.Matches(segment, @"(?:\\.|""(?:\\.|[^""\\])*(?:""|\z)|'(?:\\.|[^'\\])*(?:'|\z)|\S)+");
                foreach (Match match in matches)
                {
                    var token = Regex.Replace(match.Value,
                        @"""([^""\\]*(?:\\.[^""\\]*)*)(?:""|\z)|'([^'\\]*(?:\\.[^'\\]*)*)(?:'|\z)|\\(.)|(.)",
                        m => {
                            if (m.Groups[1].Success) return Unescape(m.Groups[1].Value);
                            if (m.Groups[2].Success) return Unescape(m.Groups[2].Value);
                            if (m.Groups[3].Success) return m.Groups[3].Value == "\\" ? "\\" : m.Groups[3].Value;
                            return m.Groups[4].Value;
                        });
                    var isQuoted = match.Value.IndexOfAny(new[] { '"', '\'', '\\' }) >= 0;
                    var tokenStart = start + match.Index;
                    result.Add(new Token(token, isQuoted: isQuoted, start: tokenStart, end: tokenStart + match.Length));
                }
            }
            return result;
        }

        private static List<(string Text, bool IsOperator, int Start)> SplitOnOperators(string line, List<string> operators)
        {
            var segments = new List<(string Text, bool IsOperator, int Start)>();
            var current = new StringBuilder();
            var currentStart = 0;
            bool inDouble = false, inSingle = false;
            int i = 0;

            while (i < line.Length)
            {
                char c = line[i];

                if (c == '\\' && (inDouble || inSingle) && i + 1 < line.Length)
                {
                    current.Append(c);
                    current.Append(line[i + 1]);
                    i += 2;
                    continue;
                }

                if (c == '"' && !inSingle) { inDouble = !inDouble; current.Append(c); i++; continue; }
                if (c == '\'' && !inDouble) { inSingle = !inSingle; current.Append(c); i++; continue; }

                if (!inDouble && !inSingle)
                {
                    var matched = operators.FirstOrDefault(op =>
                        i + op.Length <= line.Length &&
                        line.Substring(i, op.Length) == op);

                    if (matched != null)
                    {
                        if (current.Length > 0) { segments.Add((current.ToString(), false, currentStart)); current.Clear(); }
                        segments.Add((matched, true, i));
                        i += matched.Length;
                        currentStart = i;
                        continue;
                    }
                }

                current.Append(c);
                i++;
            }

            if (current.Length > 0) segments.Add((current.ToString(), false, currentStart));
            return segments;
        }

        private static string Unescape(string s)
        {
            return Regex.Replace(s, @"\\(.)", m => m.Groups[1].Value == "\\" ? "\\" : m.Groups[1].Value);
        }
    }
}
