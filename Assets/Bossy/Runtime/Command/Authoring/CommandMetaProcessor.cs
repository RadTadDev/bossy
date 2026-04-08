using System.Linq;
using System.Reflection;

namespace Bossy.Command
{
    /// <summary>
    /// Preprocesses command metadata so that the registry has a normalized format.
    /// </summary>
    internal static class CommandMetaProcessor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string CommandName(string name)
        {
            // The schema validator will catch null/empty case in order to list all errors together
            return string.IsNullOrWhiteSpace(name) ? name : name.Trim().ToLower();
        }
        
        /// <summary>
        /// Normalizes an argument's name.
        /// </summary>
        /// <param name="attribute">The argument attribute.</param>
        /// <param name="field">The argument field.</param>
        /// <returns>The normalized argument name.</returns>
        public static string ArgumentName(ArgumentAttribute attribute, FieldInfo field)
        {
            // Prefer the style of explicitly overriden names
            if (!string.IsNullOrWhiteSpace(attribute.OverrideName))
            {
                return attribute.OverrideName.Trim();
            }

            var name = field.Name.Trim();
            
            return name[name.TakeWhile(c => !char.IsLetter(c)).Count()..];
        }

        /// <summary>
        /// Normalizes descriptions.
        /// </summary>
        /// <param name="description">The unnormalized description.</param>
        /// <returns>The normalized description.</returns>
        public static string Description(string description)
        {
            // The schema validator will catch this case in order to list all errors together
            if (string.IsNullOrWhiteSpace(description)) return description;

            description = description.Trim();
            
            var first = description[0];

            if (char.IsLetter(first) && !char.IsUpper(first))
            {
                description = char.ToUpper(description[0]) +  description[1..];
            }
            
            if (!description.EndsWith('.'))
            {
                description += '.';
            }
            
            return description;
        }
    }
}