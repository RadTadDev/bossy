using System;
using Bossy.Command;

namespace Bossy.Tests.Utils
{
    internal interface IDeclareTypeStep
    {
        public IDeclareArgumentStep WithType(Type type);
    }

    internal interface IDeclareArgumentStep
    {
        public ArgumentFieldRecord AsSwitch(char shortName);
        public ArgumentFieldRecord AsPositional(int index);
        public ArgumentFieldRecord AsOptional(int index);
        public ArgumentFieldRecord AsVariadic();
    }
    
    /// <summary>
    /// Generates fields dynamically.
    /// </summary>
    internal class ArgumentGenerator : IDeclareTypeStep, IDeclareArgumentStep
    {
        private readonly string _name;
        private Type _type;

        private ArgumentGenerator(string name)
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
            
            var generator = new ArgumentGenerator(name);
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
        /// <param name="shortName">The short name for the switch.</param>
        /// <returns>A record to construct this field.</returns>
        /// <exception cref="InvalidOperationException">Throws when it cannot find a constructor for
        /// <see cref="SwitchAttribute"/></exception>
        public ArgumentFieldRecord AsSwitch(char shortName)
        {
            var constructorInfo = typeof(SwitchAttribute).GetConstructor(new[] { typeof(char),  typeof(string), typeof(string) });

            if (constructorInfo == null)
            {
                throw new InvalidOperationException("Not matching constructor parameter " +
                                                    $"list for attribute {typeof(SwitchAttribute).FullName}");
            }
            
            var args = new object[] { shortName, "Description.", _name };
         
            return new ArgumentFieldRecord(_name, _type, constructorInfo, args);
        }

        /// <summary>
        /// Declares this field as a positional argument.
        /// </summary>
        /// <param name="index">The order in which this positional arg applies.</param>
        /// <returns>A record to construct this field.</returns>
        /// <exception cref="InvalidOperationException">Throws when it cannot find a constructor for
        /// <see cref="PositionalAttribute"/></exception>
        public ArgumentFieldRecord AsPositional(int index)
        {
            var constructorInfo = typeof(PositionalAttribute).GetConstructor(new[] { typeof(int),  typeof(string), typeof(string) });
            
            if (constructorInfo == null)
            {
                throw new InvalidOperationException("Not matching constructor parameter " +
                                                    $"list for attribute {typeof(PositionalAttribute).FullName}");
            }
            
            var args = new object[] { index, "Description.", _name };
         
            return new ArgumentFieldRecord(_name, _type, constructorInfo, args);
        }
        
        /// <summary>
        /// Declares this field as an optional argument.
        /// </summary>
        /// <param name="index">The order in which this optional arg applies.</param>
        /// <returns>A record to construct this field.</returns>
        /// <exception cref="InvalidOperationException">Throws when it cannot find a constructor for
        /// <see cref="OptionalAttribute"/></exception>
        public ArgumentFieldRecord AsOptional(int index)
        {
            var constructorInfo = typeof(OptionalAttribute).GetConstructor(new[] { typeof(int),  typeof(string), typeof(string) });
            
            if (constructorInfo == null)
            {
                throw new InvalidOperationException("Not matching constructor parameter " +
                                                    $"list for attribute {typeof(OptionalAttribute).FullName}");
            }
            
            var args = new object[] { index, "Description.", _name };
         
            return new ArgumentFieldRecord(_name, _type, constructorInfo, args);
        }

        /// <summary>
        /// Declares this field as an array of variadic arguments.
        /// </summary>
        /// <returns>A record to construct this field.</returns>
        /// <exception cref="InvalidOperationException">Throws when it cannot find a constructor for
        /// <see cref="VariadicAttribute"/></exception>
        public ArgumentFieldRecord AsVariadic()
        {
            var constructorInfo = typeof(VariadicAttribute).GetConstructor(new[] { typeof(string), typeof(string) });
            
            if (constructorInfo == null)
            {
                throw new InvalidOperationException("Not matching constructor parameter " +
                                                    $"list for attribute {typeof(VariadicAttribute).FullName}");
            }
            
            var args = new object[] { "Description.", _name };
            var arrayType = _type.MakeArrayType();
            return new ArgumentFieldRecord(_name, arrayType, constructorInfo, args);
        }
    }
}