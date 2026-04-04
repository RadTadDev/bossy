using System.Reflection;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// A data container holding information to build a custom attribute.
    /// </summary>
    public class AttributeData
    {
        /// <summary>
        /// The constructor info associated with this attribute.
        /// </summary>
        public readonly ConstructorInfo ConstructorInfo;
        
        /// <summary>
        /// The argument list for this attribute's constructor.
        /// </summary>
        public readonly object[] Arguments;
        
        /// <summary>
        /// Builds a new <see cref="AttributeData"/> container.
        /// </summary>
        /// <param name="constructorInfo">Constructor information.</param>
        /// <param name="arguments">Arguments for the constructor.</param>
        public AttributeData(ConstructorInfo constructorInfo, object[] arguments)
        {
            ConstructorInfo = constructorInfo;
            Arguments = arguments;
        }
    }
}