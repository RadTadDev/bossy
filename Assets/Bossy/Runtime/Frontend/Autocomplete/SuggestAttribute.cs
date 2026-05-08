using System;

namespace Bossy.Frontend.Autocomplete
{
    /// <summary>
    /// Name a function returning an IEnumerable of strings used to suggest options for this argument.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SuggestAttribute : Attribute
    {
        /// <summary>
        /// The function name.
        /// </summary>
        public readonly string FunctionName;

        /// <summary>
        /// The type that the method resides on.
        /// </summary>
        public readonly Type Type;
        
        /// <summary>
        /// Suggest options for this argument.
        /// </summary>
        /// <param name="functionName">The function name to use for gathering suggestions.</param>
        /// <param name="type">The type that the method resides on.</param>
        public SuggestAttribute(string functionName, Type type = null)
        {
            FunctionName = functionName;
            Type = type;
        }
    }
}