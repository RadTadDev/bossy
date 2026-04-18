using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Bossy.Command;
using Bossy.Schema;
using Bossy.Tests.Utils;
using NUnit.Framework;

namespace Bossy.Tests.Schema
{
    /// <summary>
    /// Tests the <see cref="SchemaValidator"/> class.
    /// </summary>
    internal class SchemaValidatorTest
    {
        private SchemaValidator _schemaValidator;

        [SetUp]
        public void SetUp()
        {
            _schemaValidator = new SchemaValidator();
        }

        // -------------------------
        // Command-level tests
        // -------------------------

        [Test]
        public void Test_Validate_Nominal_NoArguments()
        {
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        public void Test_Validate_MissingCommandName()
        {
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var schema = new CommandSchema("",  "description", type, null);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<MissingNameError>().Any(), Is.True);
        }

        [Test]
        public void Test_Validate_CommandNameStartsWithNonLetter()
        {
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var schema = new CommandSchema("1invalid", "description", type, null);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<InvalidNameError>().Any(), Is.True);
        }

        [Test]
        public void Test_Validate_MissingDescription_ProducesWarning()
        {
            var type = CommandGenerator.WithName("test").Generate().GetType();
            var schema = new CommandSchema("test", "", type, null);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Warnings.OfType<MissingDescriptionWarning>().Any(), Is.True);
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Test_Validate_NotACommandType_ProducesError()
        {
            var schema = BuildSchemaFromType(typeof(string)); // not an ICommand

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<NotACommandError>().Any(), Is.True);
        }

        // -------------------------
        // Switch argument tests
        // -------------------------

        [Test]
        public void Test_Validate_Nominal_WithSwitch()
        {
            var type = CommandGenerator.WithName("test")
                .WithSwitch("myswitch", typeof(bool))
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Test_Validate_DuplicateSwitchShortName()
        {
            var type = CommandGenerator.WithName("test")
                .WithSwitch("switch1", typeof(bool), 's')
                .WithSwitch("switch2", typeof(bool), 's')
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<DuplicateSwitchNameError>().Any(), Is.True);
        }

        [Test]
        public void Test_Validate_SwitchShortNameNotLetter()
        {
            var type = CommandGenerator.WithName("test")
                .WithSwitch("myswitch", typeof(bool), '1')
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<InvalidShortSwitchNameError>().Any(), Is.True);
        }

        // -------------------------
        // Positional argument tests
        // -------------------------

        [Test]
        public void Test_Validate_Nominal_WithPositionals()
        {
            var type = CommandGenerator.WithName("test")
                .WithPositional("pos0", typeof(string))
                .WithPositional("pos1", typeof(string))
                .WithPositional("pos2", typeof(string))
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Test_Validate_DuplicatePositionalIndex()
        {
            var type = CommandGenerator.WithName("test")
                .WithPositional("pos0a", typeof(string), 0)
                .WithPositional("pos0b", typeof(string), 0)
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<DuplicateIndexError>().Any(e => e.IsPositional), Is.True);
        }

        [Test]
        public void Test_Validate_NegativePositionalIndex()
        {
            var type = CommandGenerator.WithName("test")
                .WithPositional("pos", typeof(string), -2)
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<NegativeIndexError>().Any(e => e.IsPositional), Is.True);
        }

        [Test]
        public void Test_Validate_NonContiguousPositionalIndices()
        {
            var type = CommandGenerator.WithName("test")
                .WithPositional("pos0", typeof(string), 0)
                .WithPositional("pos2", typeof(string), 2)
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<BadIndexOrderError>().Any(e => e.IsPositional), Is.True);
        }

        // -------------------------
        // Optional argument tests
        // -------------------------

        [Test]
        public void Test_Validate_Nominal_WithOptionals()
        {
            var type = CommandGenerator.WithName("test")
                .WithOptional("opt0", typeof(string))
                .WithOptional("opt1", typeof(string))
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Test_Validate_DuplicateOptionalIndex()
        {
            var type = CommandGenerator.WithName("test")
                .WithOptional("opt0a", typeof(string), 0)
                .WithOptional("opt0b", typeof(string), 0)
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<DuplicateIndexError>().Any(e => !e.IsPositional), Is.True);
        }

        [Test]
        public void Test_Validate_NegativeOptionalIndex()
        {
            var type = CommandGenerator.WithName("test")
                .WithOptional("opt", typeof(string), -2)
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<NegativeIndexError>().Any(e => !e.IsPositional), Is.True);
        }

        [Test]
        public void Test_Validate_NonContiguousOptionalIndices()
        {
            var type = CommandGenerator.WithName("test")
                .WithOptional("opt0", typeof(string), 0)
                .WithOptional("opt2", typeof(string), 2)
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<BadIndexOrderError>().Any(e => !e.IsPositional), Is.True);
        }

        // -------------------------
        // Variadic argument tests
        // -------------------------

        [Test]
        public void Test_Validate_Nominal_WithVariadic()
        {
            var type = CommandGenerator.WithName("test")
                .WithVariadic("args", typeof(string))
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Test_Validate_DuplicateVariadic()
        {
            var type = CommandGenerator.WithName("test")
                .WithVariadic("args1", typeof(string))
                .WithVariadic("args2", typeof(string))
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<DuplicateVariadicError>().Any(), Is.True);
        }

        [Test]
        public void Test_Validate_VariadicFieldNotArray()
        {
            
        }

        // -------------------------
        // Cross-argument tests
        // -------------------------

        [Test]
        public void Test_Validate_DuplicateArgumentName()
        {
            var type = CommandGenerator.WithName("test")
                .WithSwitch("arg1", typeof(bool))
                .WithSwitch("arg2", typeof(bool))
                .Generate().GetType();

            var arg1Field = type.GetField("arg1");
            var arg2Field = type.GetField("arg2");
            
            var arg1 = new ArgumentSchema("arg", "desc", arg1Field, arg1Field.GetCustomAttribute<ArgumentAttribute>(), null);
            var arg2 = new ArgumentSchema("arg", "desc", arg2Field, arg2Field.GetCustomAttribute<ArgumentAttribute>(), null);
            
            var args = new HashSet<ArgumentSchema> { arg1, arg2 };
            
            var schema = new CommandSchema("test", "description", type, args);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<ArgumentDuplicateNameError>().Any(), Is.True);
        }

        [Test]
        public void Test_Validate_DuplicateArgumentDescription_ProducesWarning()
        {
            var type = CommandGenerator.WithName("test")
                .WithSwitch("arg1", typeof(bool))
                .WithSwitch("arg2", typeof(bool), 'b')
                .Generate().GetType();
            var schema = BuildSchema(type);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Warnings.OfType<ArgumentDuplicateDescriptionWarning>().Any(), Is.True);
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.HasWarnings, Is.True);
        }

        [Test]
        public void Test_Validate_MissingArgumentName_ProducesError()
        {
            var type = CommandGenerator.WithName("test")
                .WithSwitch("sw", typeof(bool), 'a')
                .Generate().GetType();

            var field = type.GetField("sw");
            var arg = new ArgumentSchema("  ", "des", field, field.GetCustomAttribute<ArgumentAttribute>(), null);
            
            var args = new HashSet<ArgumentSchema> { arg };

            var schema = new CommandSchema("test", "desc", type, args);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<ArgumentMissingNameError>().Any(), Is.True);
        }
        
        [Test]
        public void Test_Validate_InvalidArgumentName_ProducesError()
        {
            var type = CommandGenerator.WithName("test")
                .WithSwitch("sw", typeof(bool), 'a')
                .Generate().GetType();

            var field = type.GetField("sw");
            var arg = new ArgumentSchema("1arg", "des", field, field.GetCustomAttribute<ArgumentAttribute>(), null);
            
            var args = new HashSet<ArgumentSchema> { arg };

            var schema = new CommandSchema("test", "desc", type, args);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<ArgumentInvalidNameError>().Any(), Is.True);
        }
        
        [Test]
        public void Test_Validate_MissingArgumentDescription_ProducesWarning()
        {
            var type = CommandGenerator.WithName("test")
                .WithSwitch("sw", typeof(bool), 'a')
                .Generate().GetType();

            var field = type.GetField("sw");
            var arg = new ArgumentSchema("1arg", "", field, field.GetCustomAttribute<ArgumentAttribute>(), null);
            
            var args = new HashSet<ArgumentSchema> { arg };

            var schema = new CommandSchema("test", "desc", type, args);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Warnings.OfType<ArgumentMissingDescriptionWarning>().Any(), Is.True);
        }
        
        [Test]
        public void Test_Validate_NullArgumentAttribute_ProducesError()
        {
            var type = CommandGenerator.WithName("test")
                .WithSwitch("sw", typeof(bool), 'a')
                .Generate().GetType();

            var field = type.GetField("sw");
            var arg = new ArgumentSchema("1arg", "", field, null, null);
            
            var args = new HashSet<ArgumentSchema> { arg };

            var schema = new CommandSchema("test", "desc", type, args);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<ArgumentMissingAttributeError>().Any(), Is.True);
        }
        
        [Test]
        public void Test_Validate_VariadicNotArray_ProducesWarning()
        {
            var type = CommandGenerator.WithName("test")
                .WithVariadic("arg", typeof(bool))
                .WithSwitch("sw", typeof(bool))
                .Generate().GetType();
            
            var field = type.GetField("arg");
            var swField = type.GetField("sw");
            
            var arg = new ArgumentSchema("1arg", "", swField, field.GetCustomAttribute<ArgumentAttribute>(), null);
            
            var args = new HashSet<ArgumentSchema> { arg };
            
            var schema = new CommandSchema("test", "desc", type, args);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<VariadicTypeNotArrayError>().Any(), Is.True);
        }
        
        [Test]
        public void Test_Validate_DuplicateSubcommand_ProducesError()
        {
            var parent = CommandGenerator
                .WithName("parent")
                .Generate().GetType();
            
            var child1 = CommandGenerator
                .WithName("test")
                .AsSubcommand(parent)
                .Generate().GetType();

            var child2 = CommandGenerator
                .WithName("test")
                .AsSubcommand(parent)
                .Generate().GetType();

            var schema = new CommandSchema("parent", "desc", parent, null);

            var child1Schema = new CommandSchema("test", "desc", child1, null);
            var child2Schema = new CommandSchema("test", "desc", child2, null);
            
            ICommandSchemaWriter writer = schema;
            writer.AddChild(child1Schema);
            writer.AddChild(child2Schema);
            
            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<ArgumentDuplicateNameError>().Any(), Is.True);
        }
        
        [Test]
        public void Test_Validate_UnknownAttribute_ProducesError()
        {
            var cmdType = CommandGenerator.WithName("test")
                .WithSwitch("dup1", typeof(bool))
                .Generate().GetType();

            var type = DynamicAssemblyCache.CreateType(BuildConstructor, typeName:"customarg", parentType:typeof(ArgumentAttribute));
            
            var field = cmdType.GetField("dup1");
            
            var arg =  new ArgumentSchema("dup", "desc", field, (ArgumentAttribute)Activator.CreateInstance(type), null);
            var args = new HashSet<ArgumentSchema> { arg };
            
            var schema = new CommandSchema("test", "description", cmdType, args);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<UnknownArgumentType>().Any(), Is.True);
        }
        
        [Test]
        public void Test_Validate_MultipleErrors_AllReported()
        {
            var type = CommandGenerator.WithName("test")
                .WithSwitch("dup1", typeof(bool))
                .WithSwitch("dup2", typeof(bool))
                .Generate().GetType();

            var field1 = type.GetField("dup1");
            var field2 = type.GetField("dup2");
            
            var arg1 =  new ArgumentSchema("dup", "desc", field1, field1.GetCustomAttribute<ArgumentAttribute>(), null);
            var arg2 =  new ArgumentSchema("dup", "desc", field2, field2.GetCustomAttribute<ArgumentAttribute>(), null);
            
            var args = new HashSet<ArgumentSchema> { arg1, arg2 };
            
            var schema = new CommandSchema("test", "description", type, args);

            var result = _schemaValidator.Validate(schema);

            Assert.That(result.Errors.OfType<ArgumentDuplicateNameError>().Any(), Is.True);
            Assert.That(result.Errors.OfType<DuplicateSwitchNameError>().Any(), Is.True);
            Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(2));
        }
        
        [Test]
        public void Test_Print_All_Errors_And_Warnings()
        {
            // This test exists to make it easier to see what coverage is missing.
            // By hitting these classes we cover them

            ErrorContext ctx;

            ctx = new MissingNameError();
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new InvalidNameError("");
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new ArgumentInvalidNameError("");
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new NotACommandError(typeof(int));
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new ArgumentMissingNameError();
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new ArgumentMissingAttributeError("");
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new ArgumentDuplicateNameError("");
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new DuplicateSwitchNameError('a');
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new InvalidShortSwitchNameError('a', "");
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new NegativeIndexError("", 0, false);
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new DuplicateIndexError("", 0, false);
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new DuplicateVariadicError("");
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new VariadicTypeNotArrayError("");
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new BadIndexOrderError(true);
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);
            
            ctx = new UnknownArgumentType(typeof(int));
            Assert.That(string.IsNullOrWhiteSpace(ctx.Message), Is.False);

            WarningContext wctx;
            wctx = new MissingDescriptionWarning();
            Assert.That(string.IsNullOrWhiteSpace(wctx.Message), Is.False);
            
            wctx = new ArgumentDuplicateDescriptionWarning("", "");
            Assert.That(string.IsNullOrWhiteSpace(wctx.Message), Is.False);
            
            wctx = new ArgumentMissingDescriptionWarning("");
            Assert.That(string.IsNullOrWhiteSpace(wctx.Message), Is.False);
        }

        // -------------------------
        // Helpers
        // -------------------------

        private CommandSchema BuildSchema(Type type)
        {
            var graph = CommandDependencyGraphBuilder.BuildGraph(new List<Type> { type });
            return SchemaFactory.BuildCommandSchemas(graph).First();
        }

        private CommandSchema BuildSchemaFromType(Type type)
        {
            // Construct a schema directly without going through discovery
            return new CommandSchema("invalid", "Description.", type, new HashSet<ArgumentSchema>());
        }

        private void BuildConstructor(TypeBuilder typeBuilder)
        {
            var constructor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                Type.EmptyTypes);

            var il = constructor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
            il.Emit(OpCodes.Ret);
        }
    }
}