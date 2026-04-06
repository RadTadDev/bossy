using Bossy.Registry;
using System.Collections.Generic;
using System.Linq;
using Bossy.Command;

namespace Bossy.Schema
{
    /// <summary>
    /// Validates a schema to ensure that no errors have been compiled into a command.
    /// </summary>
    internal class Validator
    {
        private readonly List<WarningContext> _warnings = new();
        private readonly List<ErrorContext> _errors = new();
        
        private readonly HashSet<string> _names = new();
        private readonly HashSet<string> _descriptions = new();
        private readonly HashSet<char> _switches = new();
        private readonly HashSet<int> _positionalIndices = new();
        private readonly HashSet<int> _optionalIndices = new();
        private bool _usesVariadic;

        /// <summary>
        /// Validates a command schema.
        /// </summary>
        /// <param name="schema">The schema to validate.</param>
        /// <returns>The validation result.</returns>
        public ValidationResult Validate(CommandSchema schema)
        {
            ValidateName(schema.Name, false);

            // Require a valid description
            if (string.IsNullOrWhiteSpace(schema.Description))
            {
                AddWarning(new MissingDescriptionWarning());
            }

            // Require a valid command type
            if (!ICommandDiscoverer.IsCommandType(schema.CommandType))
            {
                AddError(new NotACommandError(schema.CommandType));
            }

            foreach (var arg in schema.Arguments)
            {
                ValidateArgument(arg);
            }

            // Require non colliding positional, optional, and subcommand names
            
            /*
             * TODO: Note this is not currently necessary since positionals are never invoked by name. This is
             * here incase in the future you add option=value syntax for positionals and optionals to disambiguate
             * the case that a subcommand matches the literal value a user wants to input for a positional or optional
             */
            var posAndOpts = schema.Arguments
                .Select(a => a.ArgumentAttribute.GetType())
                .Where(t => t == typeof(PositionalAttribute) || t == typeof(OptionalAttribute));
            
            var set = new HashSet<string>(posAndOpts.Select(a => a.Name));
            foreach (var subcommand in schema.ChildSchemas.Select(s => s.Name))
            {
                if (!set.Add(subcommand))
                {
                    AddError(new ArgumentDuplicateNameError(subcommand));
                }
            }
            
            // Require ordered positional indices 
            if (!Enumerable.Range(0, _positionalIndices.Count).All(_positionalIndices.Contains))
            {
                AddError(new BadIndexOrderError(true));
            }

            // Require ordered optional indices 
            if (!Enumerable.Range(0, _optionalIndices.Count).All(_optionalIndices.Contains))
            {
                AddError(new BadIndexOrderError(false));
            }

            return new ValidationResult(_warnings, _errors);
        }

        private void ValidateName(string name, bool forArgument)
        {
            // The formatter has already applied style, only enforce hard rules here

            if (string.IsNullOrWhiteSpace(name))
            {
                if (forArgument)
                {
                    AddError(new ArgumentMissingNameError());
                }
                else
                {
                    AddError(new MissingNameError());
                }
            }
            
            // This is an else because these two errors are mutually exclusive
            else if (!char.IsLetter(name[0]))
            {
                if (forArgument)
                {
                    AddError(new ArgumentInvalidNameError(name));
                }
                else
                {
                    AddError(new InvalidNameError(name));
                }
            }
        }

        private void ValidateArgument(ArgumentSchema arg)
        {
            // Require a valid argument name
            ValidateName(arg.Name, true);

            // Require a valid argument description
            if (string.IsNullOrWhiteSpace(arg.Description))
            {
                AddWarning(new ArgumentMissingDescriptionWarning(arg.Name));
            }

            // Require argument attribute
            if (arg.ArgumentAttribute == null)
            {
                AddError(new ArgumentMissingAttributeError(arg.Name));
            }

            // Require unique names
            if (!_names.Add(arg.Name))
            {
                AddError(new ArgumentDuplicateNameError(arg.Name));
            }

            // Warn on duplicate descriptions
            if (!_descriptions.Add(arg.Description))
            {
                AddWarning(new ArgumentDuplicateDescriptionWarning(arg.Name, arg.Description));
            }

            ValidateArgumentAttributeProperties(arg);
        }

        private void ValidateArgumentAttributeProperties(ArgumentSchema arg)
        {
            switch (arg.ArgumentAttribute)
            {
                case SwitchAttribute sw:
                    // Require unique switch short names
                    if (!_switches.Add(sw.ShortName))
                    {
                        AddError(new DuplicateSwitchNameError(sw.ShortName));
                    }
                    
                    // Require short names to be letters
                    if (!char.IsLetter(sw.ShortName))
                    {
                        AddError(new InvalidShortSwitchNameError(sw.ShortName, sw.OverrideName));
                    }

                    break;
                case PositionalAttribute positional:
                    // Require positive unique positional indexing
                    if (positional.Index < 0)
                    {
                        AddError(new NegativeIndexError(arg.Name, positional.Index, true));
                    }
                    if (!_positionalIndices.Add(positional.Index))
                    {
                        AddError(new DuplicateIndexError(arg.Name, positional.Index, true));
                    }
                    break;
                case OptionalAttribute optional:
                    // Require positive unique optional indexing
                    if (optional.Index < 0)
                    {
                        AddError(new NegativeIndexError(arg.Name, optional.Index, false));
                    }
                    if (!_optionalIndices.Add(optional.Index))
                    {
                        AddError(new DuplicateIndexError(arg.Name, optional.Index, false));
                    }
                    break;
                case VariadicAttribute:
                    // Require unique variadic argument
                    if (_usesVariadic)
                    {
                        AddError(new DuplicateVariadicError(arg.Name));
                    }
                    _usesVariadic = true;

                    // Require underlying array type
                    if (!arg.FieldInfo.FieldType.IsArray)
                    {
                        AddError(new VariadicTypeNotArrayError(arg.Name));
                    }
                    break;
                default:
                    AddError(new UnknownArgumentType(arg.ArgumentAttribute.GetType()));
                    break;
            }
        }
        
        private void AddWarning(WarningContext warning)
        {
            _warnings.Add(warning);
        }
        
        private void AddError(ErrorContext error)
        {
            _errors.Add(error);
        }
    }
}