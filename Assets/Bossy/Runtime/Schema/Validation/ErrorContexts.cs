using System;
using Bossy.Command;

namespace Bossy.Schema
{
    /// <summary>
    /// A context for a command schema error.
    /// </summary>
    public abstract class ErrorContext
    {
        /// <summary>
        /// The human-readable error message.
        /// </summary>
        public abstract string Message { get; }
    }
    
    /// <summary>
    /// A context for a command schema warning.
    /// </summary>
    public abstract class WarningContext
    {
        /// <summary>
        /// The human-readable warning message.
        /// </summary>
        public abstract string Message { get; }
    }

    /// <summary>
    /// Missing name error.
    /// </summary>
    public class MissingNameError : ErrorContext
    {
        public override string Message => "Name is null or empty";
    }
    
    /// <summary>
    /// Invalid name error.
    /// </summary>
    public class InvalidNameError : ErrorContext
    {
        private readonly string _name;
        
        public override string Message => $"Command name \"{_name}\" must start with a letter";

        public InvalidNameError(string name)
        {
            _name = name;
        }
    }
    
    /// <summary>
    /// Argument invalid name error.
    /// </summary>
    public class ArgumentInvalidNameError : ErrorContext
    {
        private readonly string _name;
        
        public override string Message => $"Argument name \"{_name}\" must start with a letter";

        public ArgumentInvalidNameError(string name)
        {
            _name = name;
        }
    }
    
    /// <summary>
    /// Missing description warning.
    /// </summary>
    public class MissingDescriptionWarning : WarningContext
    {
        public override string Message => "Description is null or empty";
    }
    
    /// <summary>
    /// Not a command error.
    /// </summary>
    public class NotACommandError : ErrorContext
    {
        private readonly Type _commandType;
        
        public override string Message => $"\"{_commandType}\" is not a valid command type";

        public NotACommandError(Type commandType)
        {
            _commandType = commandType;
        }
    }

    /// <summary>
    /// Argument missing name error.
    /// </summary>
    public class ArgumentMissingNameError : ErrorContext
    {
        public override string Message => $"Argument name is null or empty";
    }
    
    /// <summary>
    /// Argument missing description warning.
    /// </summary>
    public class ArgumentMissingDescriptionWarning : WarningContext
    {
        private readonly string _argName;
        
        public override string Message => $"Argument \"{_argName}\" is missing a description";

        public ArgumentMissingDescriptionWarning(string argName)
        {
            _argName = argName;
        }
    }
    
    /// <summary>
    /// Argument missing attribute error.
    /// </summary>
    public class ArgumentMissingAttributeError : ErrorContext
    {
        private readonly string _argName;
        
        public override string Message => $"Argument \"{_argName}\" is missing an {nameof(ArgumentAttribute)} attribute";

        public ArgumentMissingAttributeError(string argName)
        {
            _argName = argName;
        }
    }
    
    /// <summary>
    /// Argument duplicate name error.
    /// </summary>
    public class ArgumentDuplicateNameError : ErrorContext
    {
        private readonly string _argName;
        
        public override string Message => $"Argument \"{_argName}\" is a duplicate name or is the same as the command name or a subcommand name";

        public ArgumentDuplicateNameError(string argName)
        {
            _argName = argName;
        }
    }
    
    /// <summary>
    /// Argument duplicate description warning.
    /// </summary>
    public class ArgumentDuplicateDescriptionWarning : WarningContext
    {
        private readonly string _argName;
        private readonly string _argDescription;
        
        public override string Message => $"Argument \"{_argName}\" contains duplicate description: \"{_argDescription}\"";

        public ArgumentDuplicateDescriptionWarning(string argName, string argDescription)
        {
            _argName = argName;
            _argDescription = argDescription;
        }
    }
    
    /// <summary>
    /// Duplicate switch name error.
    /// </summary>
    public class DuplicateSwitchNameError : ErrorContext
    {
        private readonly char _shortName;
        
        public override string Message => $"Switch \"-{_shortName}\" is a duplicate switch name";

        public DuplicateSwitchNameError(char shortName)
        {
            _shortName = shortName;
        }
    }
    
    /// <summary>
    /// Invalid short name error.
    /// </summary>
    public class InvalidShortSwitchNameError : ErrorContext
    {
        private readonly char _shortName;
        private readonly string _fullName;
        
        public override string Message => $"{_shortName} for switch {_fullName} is invalid and must be a letter. ";

        public InvalidShortSwitchNameError(char shortName, string fullname)
        {
            _shortName = shortName;
            _fullName = fullname;
        }
    }
    
    /// <summary>
    /// Negative index error.
    /// </summary>
    public class NegativeIndexError : ErrorContext
    {
        private readonly string _argName;
        private readonly int _index;
        
        private readonly string _positionalText;
        
        public bool IsPositional { get; }
        
        public override string Message => $"{_positionalText} arg \"{_argName}\" contains negative index {_index}";

        public NegativeIndexError(string argName, int index, bool positional)
        {
            _argName = argName;
            _index = index;
            IsPositional = positional;
            _positionalText = positional ? "Positional" : "Optional";
        }
    }
    
    /// <summary>
    /// Duplicate index error.
    /// </summary>
    public class DuplicateIndexError : ErrorContext
    {
        private readonly string _argName;
        private readonly int _index;
        private readonly string _positionalText;
        
        public bool IsPositional { get; }

        public override string Message => $"{_positionalText} arg \"{_argName}\" contains duplicate index {_index}";

        public DuplicateIndexError(string argName, int index, bool positional)
        {
            _argName = argName;
            _index = index;
            IsPositional = positional;
            _positionalText = positional ? "Positional" : "Optional";
        }
    }
    
    /// <summary>
    /// Duplicate variadic error.
    /// </summary>
    public class DuplicateVariadicError : ErrorContext
    {
        private readonly string _argName;
        
        public override string Message => $"Argument \"{_argName}\" contains more than one variadic argument list";

        public DuplicateVariadicError(string argName)
        {
            _argName = argName;
        }
    }
    
    /// <summary>
    /// Variadic type not array error.
    /// </summary>
    public class VariadicTypeNotArrayError : ErrorContext
    {
        private readonly string _argName;
        
        public override string Message => $"Variadic field argument \"{_argName}\" is not an array type";

        public VariadicTypeNotArrayError(string argName)
        {
            _argName = argName;
        }
    }
    
    /// <summary>
    /// Bad index order error.
    /// </summary>
    public class BadIndexOrderError : ErrorContext
    {
        private readonly string _positionalText;

        public bool IsPositional { get; }
        
        public override string Message => $"{_positionalText} indices were badly ordered. " +
                                          "Indices should start from 0 and count up";

        public BadIndexOrderError(bool positional)
        {
            IsPositional = positional;
            _positionalText = positional ? "Positional" : "Optional";
        }
    }
    /// <summary>
    /// Unknown argument type error.
    /// </summary>
    public class UnknownArgumentType : ErrorContext
    {
        private readonly Type _type;

        public override string Message => $"Unknown argument type {_type}";

        public UnknownArgumentType(Type type)
        {
            _type = type;
        }
    }

    public class NullSchemaError : ErrorContext
    {
        public override string Message => "Schema is null.";
    }
}