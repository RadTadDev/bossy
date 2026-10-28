using System;
using System.Collections.Generic;
using Bossy.Execution;
using Bossy.Utils;

namespace Bossy.Frontend.Parsing
{
    /// <summary>
    /// The result of parsing text into a command graph.
    /// </summary>
    public abstract class ParseResult
    {
        /// <summary>
        /// A message about the result.
        /// </summary>
        public abstract string Message { get; }
        
        /// <summary>
        /// Attempts to get the graph. Succeeds if the parse was successful.
        /// </summary>
        /// <param name="graph">The returned graph.</param>
        /// <returns>True if the graph was fetched, otherwise false.</returns>
        public virtual bool TryGetGraph(out CommandGraph graph)
        {
            graph = null;
            return false;
        }

        /// <summary>
        /// Tells if this parse result is empty.
        /// </summary>
        public bool IsEmpty => GetType() == typeof(EmptyInputError);
    }

    /// <summary>
    /// Successful parse.
    /// </summary>
    internal class ParseSucceeded : ParseResult
    {
        private readonly CommandGraph _graph;
        
        public ParseSucceeded(CommandGraph graph)
        {
            _graph = graph;
        }

        public override string Message => string.Empty;

        public override bool TryGetGraph(out CommandGraph graph)
        {
            graph = _graph;
            return true;
        }
    }
    
    /// <summary>
    /// Empty input error.
    /// </summary>
    internal class EmptyInputError : ParseResult
    {
        public override string Message => string.Empty;
    }
    
    /// <summary>
    /// Contiguous operators error.
    /// </summary>
    internal class ContiguousOperatorsError : ParseResult
    {
        private readonly string _op1;
        private readonly string _op2;
        
        public override string Message =>
            $"Operators \"{_op1}\" and \"{_op2}\" appeared back to back without a command between them.";

        public ContiguousOperatorsError(string op1, string op2)
        {
            _op1 = op1;
            _op2 = op2;
        }
    }
    
    /// <summary>
    /// Bad window operator error.
    /// </summary>
    internal class BadWindowOperatorError : ParseResult
    {
        public override string Message => "Window operator may only appear first or last.";
    }
    
    /// <summary>
    /// Bad operator position error.
    /// </summary>
    internal class BadOperatorPositionError : ParseResult
    {
        private readonly string _op;
        
        public override string Message => $"Operator \"{_op}\" must not appear at the beginning or end of a command.";

        public BadOperatorPositionError(string op)
        {
            _op = op;
        }
    }
    
    /// <summary>
    /// No matching command error.
    /// </summary>
    internal class NoMatchingCommandError : ParseResult
    {
        private readonly string _root;
        
        public override string Message => $"No command with name \"{_root}\" was found.";

        public NoMatchingCommandError(string root)
        {
            _root = root;
        }
    }
    
    /// <summary>
    /// Invalid switch error.
    /// </summary>
    internal class InvalidSwitchError : ParseResult
    {
        private readonly string _token;
        
        public override string Message => $"Unknown switch \"{_token}\".";

        public InvalidSwitchError(string token)
        {
            _token = token;
        }
    }
    
    /// <summary>
    /// A switch that is not a bool was combined with others, as in "-ab".
    /// </summary>
    internal class NonBoolAggregateSwitchError : ParseResult
    {
        private readonly string _token;
        private readonly char _shortName;

        public override string Message => $"Switch \"-{_shortName}\" in \"{_token}\" takes a value, so it cannot be " +
                                          "combined with other switches. Only bool switches can be combined.";

        public NonBoolAggregateSwitchError(string token, char shortName)
        {
            _token = token;
            _shortName = shortName;
        }
    }

    /// <summary>
    /// Type adapt error.
    /// </summary>
    internal class TypeAdaptError : ParseResult
    {
        private readonly Type _targetType;
        private readonly string _token;
        private readonly string _message;

        public override string Message => $"Could not parse \"{_token}\" as type \"{_targetType.GetFriendlyName()}\": {_message}.";

        public TypeAdaptError(Type targetType, string token, string message)
        {
            _targetType = targetType;
            _token = token;
            _message = message;
        }
    }
    
    /// <summary>
    /// Missing positional error.
    /// </summary>
    internal class MissingPositionalError : ParseResult
    {
        private readonly Type _type;
        private readonly string _argName;

        public override string Message => $"Expected argument \"{_argName}\" of type " +
                                          $"\"{_type.GetFriendlyName()}\" was missing.";

        public MissingPositionalError(Type type, string argName)
        {
            _type = type;
            _argName = argName;
        }
    }
    
    /// <summary>
    /// Unexpected tokens error.
    /// </summary>
    internal class UnexpectedTokensError : ParseResult
    {
        private readonly List<string> _tokens;

        public override string Message => $"Received unexpected token(s): \"{string.Join(" ", _tokens)}\".";

        public UnexpectedTokensError(List<string> tokens)
        {
            _tokens = tokens;
        }
    }
    
    /// <summary>
    /// A switch or other token splits the tokens of a multi-token value.
    /// </summary>
    internal class SplitValueError : ParseResult
    {
        private readonly string _token;
        private readonly string _argName;

        public override string Message => $"\"{_token}\" splits the value of argument \"{_argName}\". Keep a value's tokens together.";

        public SplitValueError(string token, string argName)
        {
            _token = token;
            _argName = argName;
        }
    }
    
    /// <summary>
    /// Invalid schema error.
    /// </summary>
    internal class InvalidSchemaError : ParseResult
    {
        private readonly List<string> _tokens;

        public override string Message => $"Command \"{string.Join("->", _tokens)}\" is invalid. Please fix it before running it.";

        public InvalidSchemaError(List<string> tokens)
        {
            _tokens = tokens;
        }
    }

    /// <summary>
    /// Argument validation error.
    /// </summary>
    internal class ArgumentValidationError : ParseResult
    {
        public override string Message { get; }

        public ArgumentValidationError(string message, string argName)
        {
            Message = $"(Argument '{argName}') {message}";
        }
    }

    /// <summary>
    /// A command string argument holds a command that would not parse.
    /// </summary>
    internal class InvalidCommandStringError : ParseResult
    {
        public override string Message => $"(Argument '{_argName}') Invalid command: {Reason}";

        /// <summary>
        /// Why the inner command would not parse.
        /// </summary>
        public string Reason { get; }

        private readonly string _argName;

        public InvalidCommandStringError(ParseResult inner, string argName)
        {
            // An unquoted command is one token, so it cannot carry the arguments its command needs
            var advice = inner is MissingPositionalError ? " Quote the command to give it arguments." : string.Empty;
            Reason = inner.Message + advice;
            _argName = argName;
        }
    }

    /// <summary>
    /// Command strings are nested deeper than allowed, as with an alias that expands to itself.
    /// </summary>
    internal class CommandStringTooDeepError : ParseResult
    {
        private readonly int _maxDepth;

        public override string Message => $"Commands are nested more than {_maxDepth} levels deep.";

        public CommandStringTooDeepError(int maxDepth)
        {
            _maxDepth = maxDepth;
        }
    }
}