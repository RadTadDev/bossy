using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bossy.FrontEnd.Parsing
{
    /// <summary>
    /// Handles tokenizing.
    /// </summary>
    public static class Tokenizer
    {
        /// <summary>
        /// Tokenizes a string input into a list of tokens.
        /// </summary>
        /// <param name="line">The string to tokenize.</param>
        /// <param name="operators">A list of operators to split on.</param>
        /// <returns>The list of tokens.</returns>
        public static List<string> Tokenize(string line, IEnumerable<string> operators = null)
        {
            if (string.IsNullOrWhiteSpace(line)) return new List<string>();

            var ops = operators?.OrderByDescending(o => o.Length).ToList() ?? new List<string>();
            var segments = SplitOnOperators(line, ops);

            var result = new List<string>();
            foreach (var segment in segments)
            {
                if (ops.Contains(segment))
                {
                    result.Add(segment);
                    continue;
                }

                var matches = Regex.Matches(segment, @"(?:\\.|""(?:\\.|[^""\\])*""|'(?:\\.|[^'\\])*'|\S)+");
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
            }
            return result;
        }

        private static List<string> SplitOnOperators(string line, List<string> operators)
        {
            var segments = new List<string>();
            var current = new StringBuilder();
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
                        if (current.Length > 0) { segments.Add(current.ToString()); current.Clear(); }
                        segments.Add(matched);
                        i += matched.Length;
                        continue;
                    }
                }

                current.Append(c);
                i++;
            }

            if (current.Length > 0) segments.Add(current.ToString());
            return segments;
        }

        private static string Unescape(string s)
        {
            return Regex.Replace(s, @"\\(.)", m => m.Groups[1].Value == "\\" ? "\\" : m.Groups[1].Value);
        }
    }
}