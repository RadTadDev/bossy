using System;
using System.Reflection;
using Bossy.Command;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// Information needed to generate an argument field.
    /// </summary>
    internal class ArgumentFieldRecord
    {
        /// <summary>
        /// Field name.
        /// </summary>
        public readonly string Name;
        
        /// <summary>
        /// Field Type.
        /// </summary>
        public readonly Type Type;
        
        /// <summary>
        /// Constructor info for <see cref="ArgumentAttribute"/>.
        /// </summary>
        public readonly ConstructorInfo ConstructorInfo;
        
        /// <summary>
        /// Constructor args for <see cref="ArgumentAttribute"/>.
        /// </summary>
        public readonly object[] ConstructorArgs;
        
        /// <summary>
        /// Builds an argument field record.
        /// </summary>
        /// <param name="name">Field name.</param>
        /// <param name="type">Field type.</param>
        /// <param name="attributeConstructorInfo">Constructor info for <see cref="ArgumentAttribute"/></param>
        /// <param name="attributeConstructorConstructorArgs">Constructor args for <see cref="ArgumentAttribute"/></param>
        public ArgumentFieldRecord
        (
            string name,
            Type type,
            ConstructorInfo attributeConstructorInfo,
            object[] attributeConstructorConstructorArgs
        )
        {
            Name = name;
            Type = type;
            ConstructorInfo = attributeConstructorInfo;
            ConstructorArgs = attributeConstructorConstructorArgs;
        }
    }
}