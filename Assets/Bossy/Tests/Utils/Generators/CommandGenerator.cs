using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Frontend.Autocomplete;
using Bossy.Schema.Registry;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// Used to generate dummy commands at runtime for testing purposes.
    /// </summary>
    internal class CommandGenerator
    {
        private readonly string _name;
        private readonly HashSet<string> _fieldNames = new();
        private Type _parentCommandType;
        private int _positionalIndex;
        private int _optionalIndex;

        private List<ArgumentFieldRecord> _arguments = new();
        private readonly Dictionary<string, (string FunctionName, Type ProviderType)> _suggestions = new();
        private string _output;
        private MethodInfo _constructorCall;

        private CommandGenerator(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Starts building a command by declaring its name.
        /// </summary>
        /// <param name="name">The name of the command. Must be unique with in the
        /// <see cref="SchemaRegistry"/> scope.</param>
        /// <returns>The generator.</returns>
        /// <exception cref="ArgumentException">Throws on invalid name - must match C# syntax.</exception>
        public static CommandGenerator WithName(string name)
        {
            if (!GenerationRules.IsValidName(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));    
            }
            
            var generator = new CommandGenerator(name);
            return generator;
        }
        
        /// <summary>
        /// Makes it a subcommand.
        /// </summary>
        /// <param name="parentType">The parent type.</param>
        /// <returns>The Generator.</returns>
        public CommandGenerator AsSubcommand(Type parentType)
        {
            _parentCommandType = parentType;
            return this;
        }

        /// <summary>
        /// Adds a switch argument to this command.
        /// </summary>
        /// <param name="fullname">The long --name of the switch.</param>
        /// <param name="type">The underlying argument type.</param>
        /// <param name="overrideShortName">Optional override shortname, if not provided the
        /// first letter character of the fullname is used.</param>
        /// <returns>The generator.</returns>
        /// <exception cref="ArgumentException">Throws on invalid or duplicate name or invalid shortname.</exception>
        /// <exception cref="ArgumentNullException">Throws on null type.</exception>
        public CommandGenerator WithSwitch(string fullname, Type type, char overrideShortName = '\0')
        {
            if (!GenerationRules.IsValidName(fullname))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(fullname));
            }

            if (_fieldNames.Contains(fullname))
            {
                throw new ArgumentException("Name must not collide with other types.", nameof(fullname));
            }
            
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            
            _fieldNames.Add(fullname);

            char shortName;
            if (overrideShortName == '\0')
            {
                shortName = fullname.FirstOrDefault(char.IsLetter);
                
                if (shortName is '\0')
                {
                    throw new ArgumentException($"Cannot produce automatic short name from variable with not letters: {fullname}", fullname);
                }
            }
            else
            {
                shortName = overrideShortName;
            }
            
            var arg = ArgumentGenerator.WithName(fullname).WithType(type).AsSwitch(shortName);
            _arguments.Add(arg);
            
            return this;
        }

        /// <summary>
        /// Adds a positional argument to this command.
        /// </summary>
        /// <param name="name">The name of the argument.</param>
        /// <param name="type">The underlying argument type.</param>
        /// <param name="index">The index of the argument. If unspecified, it will auto increment.</param>
        /// <returns>The generator.</returns>
        /// <exception cref="ArgumentException">Throws on invalid or duplicate name.</exception>
        /// <exception cref="ArgumentNullException">Throws on null type.</exception>
        public CommandGenerator WithPositional(string name, Type type, int index = -1)
        {
            if (!GenerationRules.IsValidName(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            if (_fieldNames.Contains(name))
            {
                throw new ArgumentException("Name must not collide with other types.", nameof(name));
            }
            
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _fieldNames.Add(name);
            
            if (index == -1)
            {
                index = _positionalIndex++;    
            }
            
            var arg = ArgumentGenerator.WithName(name).WithType(type).AsPositional(index);
            _arguments.Add(arg);
            
            return this;
        }
        
        /// <summary>
        /// Adds an optional argument to this command.
        /// </summary>
        /// <param name="name">The name of the argument.</param>
        /// <param name="type">The underlying argument type.</param>
        /// <param name="index">The index of the argument. If unspecified, it will auto increment.</param>
        /// <returns>The generator.</returns>
        /// <exception cref="ArgumentException">Throws on invalid or duplicate name.</exception>
        /// <exception cref="ArgumentNullException">Throws on null type.</exception>
        public CommandGenerator WithOptional(string name, Type type, int index = -1)
        {
            if (!GenerationRules.IsValidName(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }
            
            if (_fieldNames.Contains(name))
            {
                throw new ArgumentException("Name must not collide with other types.", nameof(name));
            }
            
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _fieldNames.Add(name);
            
            if (index == -1)
            {
                index = _optionalIndex++;    
            }
            
            var arg = ArgumentGenerator.WithName(name).WithType(type).AsOptional(index);
            _arguments.Add(arg);
            
            return this;
        }
        
        /// <summary>
        /// Adds a variadic argument array to this command.
        /// </summary>
        /// <param name="name">The name of the argument.</param>
        /// <param name="type">The underlying generic argument type. This will be converted
        /// to an array type automatically.</param>
        /// <returns>The generator.</returns>
        /// <exception cref="ArgumentException">Throws on invalid or duplicate name.</exception>
        /// <exception cref="ArgumentNullException">Throws on null type.</exception>
        public CommandGenerator WithVariadic(string name, Type type)
        {
            if (!GenerationRules.IsValidName(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            if (_fieldNames.Contains(name))
            {
                throw new ArgumentException("Name must not collide with other types.", nameof(name));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _fieldNames.Add(name);
            
            var arg = ArgumentGenerator.WithName(name).WithType(type).AsVariadic();
            _arguments.Add(arg);
            
            return this;
        }
        
        /// <summary>
        /// Adds a <see cref="SuggestAttribute"/> to the most recently added argument.
        /// </summary>
        /// <param name="functionName">The name of the static suggestion function.</param>
        /// <param name="providerType">The type the suggestion function resides on.</param>
        /// <returns>The generator.</returns>
        /// <exception cref="InvalidOperationException">Throws when no argument has been added yet.</exception>
        public CommandGenerator WithSuggestions(string functionName, Type providerType)
        {
            if (_arguments.Count == 0)
            {
                throw new InvalidOperationException("Add an argument before adding suggestions to it.");
            }

            _suggestions[_arguments[^1].Name] = (functionName, providerType);
            return this;
        }

        /// <summary>
        /// Makes the command write text to its output when it runs.
        /// </summary>
        /// <param name="text">The text to write.</param>
        /// <returns>The generator.</returns>
        public CommandGenerator WithOutput(string text)
        {
            _output = text;
            return this;
        }

        /// <summary>
        /// Makes the command's constructor call a public static method, so tests can see when the command is
        /// created. Note that <see cref="Generate"/> creates one instance itself.
        /// </summary>
        /// <param name="method">A public static method with no parameters and no return value.</param>
        /// <returns>The generator.</returns>
        /// <exception cref="ArgumentException">Throws when the method does not have that shape.</exception>
        public CommandGenerator WithConstructorCall(MethodInfo method)
        {
            if (method == null || !method.IsStatic || !method.IsPublic || method.GetParameters().Length > 0 || method.ReturnType != typeof(void))
            {
                throw new ArgumentException("The method must be public and static, take no parameters, and return nothing.", nameof(method));
            }

            _constructorCall = method;
            return this;
        }

        /// <summary>
        /// Generates a command object.
        /// </summary>
        /// <returns>The generated command.</returns>
        public ICommand Generate()
        {
            var type = DynamicAssemblyCache.CreateType(BuildType, interfaces: new []{ typeof(ICommand) });
            return (ICommand)Activator.CreateInstance(type);
        }
        
        private void BuildType(TypeBuilder typeBuilder)
        {
            GenerateArguments(typeBuilder);
            GenerateCommandAttribute(typeBuilder);
            GenerateConstructorStub(typeBuilder);
            GenerateExecutionStub(typeBuilder);
        }

        private void GenerateArguments(TypeBuilder typeBuilder)
        {
            foreach (var argRecord in _arguments)
            {
                var fieldBuilder = typeBuilder.DefineField(argRecord.Name, argRecord.Type, FieldAttributes.Public);
                
                var builder = new CustomAttributeBuilder(argRecord.ConstructorInfo, argRecord.ConstructorArgs);
                fieldBuilder.SetCustomAttribute(builder);

                if (_suggestions.TryGetValue(argRecord.Name, out var suggestion))
                {
                    fieldBuilder.SetCustomAttribute(BuildSuggestAttribute(suggestion.FunctionName, suggestion.ProviderType));
                }
            }
        }

        private static CustomAttributeBuilder BuildSuggestAttribute(string functionName, Type providerType)
        {
            var constructorInfo = typeof(SuggestAttribute).GetConstructor(new[] { typeof(string), typeof(Type) });

            if (constructorInfo == null)
            {
                throw new InvalidOperationException($"Cannot find constructor for {nameof(SuggestAttribute)}");
            }

            return new CustomAttributeBuilder(constructorInfo, new object[] { functionName, providerType });
        }

        private void GenerateCommandAttribute(TypeBuilder typeBuilder)
        {
            var constructorInfo = typeof(CommandAttribute).GetConstructor(new [] { typeof(string), typeof(string), typeof(Type) });

            if (constructorInfo == null)
            {
                throw new InvalidOperationException($"Cannot find constructor for {nameof(CommandAttribute)}");
            }
            
            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructorInfo, new object[] {_name, "Description.", _parentCommandType}));
        }
        
        private void GenerateConstructorStub(TypeBuilder typeBuilder)
        {
            var constructor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                Type.EmptyTypes);

            var il = constructor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);

            if (_constructorCall != null)
            {
                il.Emit(OpCodes.Call, _constructorCall);
            }

            il.Emit(OpCodes.Ret);
        }
        
        private void GenerateExecutionStub(TypeBuilder typeBuilder)
        {
            var interfaceMethod = typeof(ICommand).GetMethod(nameof(ICommand.ExecuteAsync))!;
    
            var methodBuilder = typeBuilder.DefineMethod(
                interfaceMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig,
                interfaceMethod.ReturnType,
                interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray()
            );

            var fromResultMethod = typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(typeof(CommandStatus));

            var il = methodBuilder.GetILGenerator();

            if (_output != null)
            {
                // ctx.Write(_output)
                var writeMethod = typeof(CommandContext).GetMethod(nameof(CommandContext.Write), new[] { typeof(object) });
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldstr, _output);
                il.Emit(OpCodes.Callvirt, writeMethod);
            }

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Call, fromResultMethod);
            il.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, interfaceMethod);
        }
    }
}