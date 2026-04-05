using Bossy.Registry;
using System.Collections.Generic;
using System.Linq;

namespace Bossy.Command.Schema
{
    /// <summary>
    /// Validates a schema to ensure that no errors have been compiled into a command.
    /// </summary>
    public class Validator
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
            // Require a valid name
            if (string.IsNullOrWhiteSpace(schema.Name))
            {
                AddError(new MissingNameError());
            }

            // Require a valid description
            if (string.IsNullOrWhiteSpace(schema.Description))
            {
                AddWarning(new MissingDescriptionWarning());
            }

            // Require a valid command type
            if (!ReflectiveCommandDiscoverer.IsCommandType(schema.CommandType))
            {
                AddError(new NotACommandError(schema.CommandType));
            }

            foreach (var arg in schema.Arguments)
            {
                // Require a valid argument name
                if (string.IsNullOrWhiteSpace(arg.Name))
                {
                    AddError(new ArgumentMissingNameError());
                }

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

                switch (arg.ArgumentAttribute)
                {
                    // Require unique switch short names
                    case SwitchAttribute sw:
                        if (!_switches.Add(sw.ShortName))
                        {
                            AddError(new DuplicateSwitchNameError(sw.ShortName));
                        }

                        break;
                    // Require unique positional indexing
                    case PositionalAttribute positional:
                        if (positional.Index < 0)
                        {
                            AddError(new NegativeIndexError(arg.Name, positional.Index, true));
                        }
                        if (!_positionalIndices.Add(positional.Index))
                        {
                            AddError(new DuplicateIndexError(arg.Name, positional.Index, true));
                        }
                        break;
                    // Require unique optional indexing
                    case OptionalAttribute optional:
                        if (optional.Index < 0)
                        {
                            AddError(new NegativeIndexError(arg.Name, optional.Index, false));
                        }
                        if (!_optionalIndices.Add(optional.Index))
                        {
                            AddError(new DuplicateIndexError(arg.Name, optional.Index, false));
                        }
                        break;
                    // Require unique variadic argument
                    case VariadicAttribute:
                        if (_usesVariadic)
                        {
                            AddError(new DuplicateVariadicError(arg.Name));
                        }
                        _usesVariadic = true;
                        break;
                }

                if (arg.ArgumentAttribute!.GetType() == typeof(VariadicAttribute) && !arg.FieldInfo.FieldType.IsArray)
                {
                    AddError(new VariadicTypeNotArrayError(arg.Name));
                }
            }

            // Require ordered indices 
            if (!Enumerable.Range(0, _positionalIndices.Count).All(_positionalIndices.Contains))
            {
                AddError(new BadIndexOrderError(true));
            }
            if (!Enumerable.Range(0, _optionalIndices.Count).All(_optionalIndices.Contains))
            {
                AddError(new BadIndexOrderError(false));
            }

            return new ValidationResult(_warnings, _errors);
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