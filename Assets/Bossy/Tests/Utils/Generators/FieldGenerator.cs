using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Bossy.Command;
using UnityEngine;

namespace Bossy.Tests.Utils
{
    internal interface IDeclareTypeStep
    {
        public IDeclareArgumentStep WithType(Type type);
    }

    internal interface IDeclareArgumentStep
    {
        public void AsSwitch(TypeBuilder typeBuilder, char shortName);
        public void AsPositional(TypeBuilder typeBuilder, int index);
        public void AsOptional(TypeBuilder typeBuilder, int index);
        public void AsVariadic(TypeBuilder typeBuilder);
    }
    
    /// <summary>
    /// Generates fields dynamically.
    /// </summary>
    internal class FieldGenerator : IDeclareTypeStep, IDeclareArgumentStep
    {
        private readonly string _name;
        private Type _type;

        private FieldGenerator(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Create a field generator.
        /// </summary>
        /// <param name="name">The name of the field to generate.</param>
        /// <returns>The generator.</returns>
        /// <exception cref="ArgumentException">Throws on invalid C# field name.</exception>
        public static IDeclareTypeStep WithName(string name)
        {
            if (!GenerationRules.IsValidName(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));    
            }
            
            var generator = new FieldGenerator(name);
            return generator;
        }
        
        /// <summary>
        /// Declares the type of this field.
        /// </summary>
        /// <param name="type">The type to generate.</param>
        /// <returns>The generator.</returns>
        /// <exception cref="ArgumentException">Throws on null input type.</exception>
        public IDeclareArgumentStep WithType(Type type)
        {
            _type = type ?? throw new ArgumentException("Cannot declare a field with a null type!");
            return this;
        }

        /// <summary>
        /// Declares this field as a switch argument.
        /// </summary>
        /// <param name="typeBuilder">The builder used to define the type this field will be part of.</param>
        /// <param name="shortName">The short name for the switch.</param>
        /// <exception cref="InvalidOperationException">Throws when it cannot find a constructor for
        /// <see cref="SwitchAttribute"/></exception>
        public void AsSwitch(TypeBuilder typeBuilder, char shortName)
        {
            var constructorInfo = typeof(SwitchAttribute).GetConstructor(new[] { typeof(char),  typeof(string), typeof(string) });

            if (constructorInfo == null)
            {
                throw new InvalidOperationException("Not matching constructor parameter " +
                                                    $"list for attribute {typeof(SwitchAttribute).FullName}");
            }
            
            var args = new object[] { shortName, "Description.", _name };
         
            var fieldBuilder = typeBuilder.DefineField(_name, _type, FieldAttributes.Public);

            var builder = new CustomAttributeBuilder(constructorInfo, args);
            fieldBuilder.SetCustomAttribute(builder);
        }

        /// <summary>
        /// Declares this field as a positional argument.
        /// </summary>
        /// <param name="typeBuilder">The builder used to define the type this field will be part of.</param>
        /// <param name="index">The order in which this positional arg applies.</param>
        /// <exception cref="InvalidOperationException">Throws when it cannot find a constructor for
        /// <see cref="PositionalAttribute"/></exception>
        public void AsPositional(TypeBuilder typeBuilder, int index)
        {
            if (index < 0)
            {
                throw new ArgumentException("Indices for generated positional arguments must be >= 0");
            }
            
            var constructorInfo = typeof(PositionalAttribute).GetConstructor(new[] { typeof(int),  typeof(string), typeof(string) });
            
            if (constructorInfo == null)
            {
                throw new InvalidOperationException("Not matching constructor parameter " +
                                                    $"list for attribute {typeof(PositionalAttribute).FullName}");
            }
            
            var args = new object[] { index, "Description.", _name };
         
            var fieldBuilder = typeBuilder.DefineField(_name, _type, FieldAttributes.Public);

            var builder = new CustomAttributeBuilder(constructorInfo, args);
            fieldBuilder.SetCustomAttribute(builder);
        }
        
        /// <summary>
        /// Declares this field as an optional argument.
        /// </summary>
        /// <param name="typeBuilder">The builder used to define the type this field will be part of.</param>
        /// <param name="index">The order in which this optional arg applies.</param>
        /// <exception cref="InvalidOperationException">Throws when it cannot find a constructor for
        /// <see cref="OptionalAttribute"/></exception>
        public void AsOptional(TypeBuilder typeBuilder, int index)
        {
            var constructorInfo = typeof(OptionalAttribute).GetConstructor(new[] { typeof(int),  typeof(string), typeof(string) });
            
            if (constructorInfo == null)
            {
                throw new InvalidOperationException("Not matching constructor parameter " +
                                                    $"list for attribute {typeof(OptionalAttribute).FullName}");
            }
            
            var args = new object[] { index, "Description.", _name };
         
            var fieldBuilder = typeBuilder.DefineField(_name, _type, FieldAttributes.Public);

            var builder = new CustomAttributeBuilder(constructorInfo, args);
            fieldBuilder.SetCustomAttribute(builder);
        }

        /// <summary>
        /// Declares this field as an array of variadic arguments.
        /// </summary>
        /// <param name="typeBuilder">The builder used to define the type this field will be part of.</param>
        /// <exception cref="InvalidOperationException">Throws when it cannot find a constructor for
        /// <see cref="VariadicAttribute"/></exception>
        public void AsVariadic(TypeBuilder typeBuilder)
        {
            var constructorInfo = typeof(VariadicAttribute).GetConstructor(new[] { typeof(string), typeof(string) });
            
            if (constructorInfo == null)
            {
                throw new InvalidOperationException("Not matching constructor parameter " +
                                                    $"list for attribute {typeof(VariadicAttribute).FullName}");
            }
            
            var args = new object[] { "Description.", _name };
            var arrayType = _type.MakeArrayType();
            var fieldBuilder = typeBuilder.DefineField(_name, arrayType, FieldAttributes.Public);
            var builder = new CustomAttributeBuilder(constructorInfo, args);

            fieldBuilder.SetCustomAttribute(builder);
        }
    }
}