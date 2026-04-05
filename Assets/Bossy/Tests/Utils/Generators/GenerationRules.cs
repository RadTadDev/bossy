using System.Text.RegularExpressions;

namespace Bossy.Tests.Utils
{
    public static class GenerationRules
    {
        private static readonly Regex Filter = new("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        
        public static bool IsValidName(string name) => Filter.IsMatch(name);
    }
}