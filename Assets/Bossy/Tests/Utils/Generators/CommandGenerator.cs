using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Bossy.Command;

namespace Bossy.Tests.Utils
{
    /// <summary>
    /// Used to generate dummy commands at runtime for testing purposes.
    /// </summary>
    public class CommandGenerator
    {
        private string _name;
        private Type _parentType;
        private List<FieldInfo> _fields = new();
        private readonly Type _baseType = typeof(ICommand);

        /// <summary>
        /// Declare the name of this command.
        /// </summary>
        /// <param name="name">The command's name.</param>
        /// <returns>The generator.</returns>
        public CommandGenerator WithName(string name)
        {
            _name = name;
            return this;
        }
        
        /// <summary>
        /// Makes it a subcommand.
        /// </summary>
        /// <param name="parentType">The parent type.</param>
        /// <returns>The builder.</returns>
        public CommandGenerator AsSubcommand(Type parentType)
        {
            _parentType = parentType;
            return this;
        }
        
        private Type BuildType()
        {
            var typeBuilder = DynamicAssemblyCache.ModuleBuilder.DefineType($"DynamicTestClass_{DynamicAssemblyCache.NextId}", TypeAttributes.Public, _baseType);

            foreach (var field in _fields)
            {
                var fieldBuilder = typeBuilder.DefineField(field.Name, field.FieldType, FieldAttributes.Public);

                // TODO: Apply attributes from field to fieldBuilder
            }

            // TODO: Apply pre-execution validators to command type

            var name = _name ?? $"test_{DynamicAssemblyCache.NextId}";
            var cmdAttribute = _parentType == null ? new CommandAttribute(name, "") : new CommandAttribute(name, "", _parentType);
            var constructorInfo = typeof(CommandAttribute).GetConstructors().First();
            
            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(constructorInfo, new object[] {cmdAttribute.Name, cmdAttribute.Description, cmdAttribute.ParentType}));

            // Generate a default execution method in IL so the class compiles
            GenerateExecutionStub(typeBuilder);
            
            return typeBuilder.CreateType();
        }

        private void GenerateExecutionStub(TypeBuilder typeBuilder)
        {
            const string functionName = nameof(ICommand.ExecuteAsync);

            var methodToOverride = _baseType.GetMethod(functionName);
            
            if (methodToOverride == null)
            {
                throw new InvalidOperationException($"Cannot build command with execution method name {functionName}");
            }
            
            // Create a method override for execution method
            var methodBuilder = typeBuilder.DefineMethod
            (
                methodToOverride.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
                methodToOverride.ReturnType,
                new [] { typeof(CommandContext) }
            );

            // Generate IL for the method (return Task.FromResult(default(CommandStatus)))
            var il = methodBuilder.GetILGenerator();

            // Prepare to call Task.FromResult(CommandStatus)
            var commandOutputType = typeof(CommandStatus);
            var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(commandOutputType);

            // Load default(CommandStatus) onto stack
            var local = il.DeclareLocal(commandOutputType);
            il.Emit(OpCodes.Ldloca_S, local);
            
            // initialize default value
            il.Emit(OpCodes.Initobj, commandOutputType);  

            // Load the local (default CommandStatus)
            il.Emit(OpCodes.Ldloc_0);

            // Call Task.FromResult(default(CommandStatus))
            il.Emit(OpCodes.Call, fromResultMethod);

            // Return the Task<CommandOutput>
            il.Emit(OpCodes.Ret);

            // Mark this method as override of the abstract base method
            typeBuilder.DefineMethodOverride(methodBuilder, methodToOverride);
        }

        /// <summary>
        /// Defaults to unique name, simple command base, not a subcommand, no fields or prevalidators.
        /// </summary>
        /// <returns>The command.</returns>
        public ICommand Generate()
        {
            return (ICommand)Activator.CreateInstance(BuildType());
        }
    }
}