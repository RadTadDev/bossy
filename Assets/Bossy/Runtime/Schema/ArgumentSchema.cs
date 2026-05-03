using System;
using System.Collections.Generic;
using System.Reflection;
using Bossy.Command;

namespace Bossy.Schema
{
    /// <summary>
    /// 
    /// </summary>
    public class ArgumentSchema
    {
        /// <summary>
        /// The name of this argument.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The description of this argument.
        /// </summary>
        public readonly string Description;
        
        /// <summary>
        /// The type of this argument.
        /// </summary>
        public Type Type => FieldInfo.FieldType;
        
        /// <summary>
        /// The reflective field information for this argument.
        /// </summary>
        public readonly FieldInfo FieldInfo;
        
        /// <summary>
        /// The argument attribute associated with this argument.
        /// </summary>
        public readonly ArgumentAttribute ArgumentAttribute;
        
        /// <summary>
        /// All validator attributes on this argument.
        /// </summary>
        public readonly IReadOnlyList<ArgumentValidationAttribute> Validators;
        
        /// <summary>
        /// Builds a new Argument schema.
        /// </summary>
        /// <param name="name">The name of this argument.</param>
        /// <param name="description">The description of this argument.</param>
        /// <param name="fieldInfo">The reflective field information for this argument.</param>
        /// <param name="argumentAttribute">The argument attribute associated with this argument.</param>
        /// <param name="validators">All validator attributes on this argument.</param>
        public ArgumentSchema(string name, string description, FieldInfo fieldInfo, ArgumentAttribute argumentAttribute, IReadOnlyList<ArgumentValidationAttribute> validators)
        {
            Name = name;
            Description = description;
            FieldInfo = fieldInfo;
            ArgumentAttribute = argumentAttribute;
            Validators = validators ?? new List<ArgumentValidationAttribute>();
        }

        /// <summary>
        /// Sets the value of this argument on an object.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue(object target, object value)
        {
            FieldInfo.SetValue(target, value);
        }
    }
}