using Bossy.Shell;

namespace Bossy.FrontEnd.Parsing
{
    /// <summary>
    /// The result of parsing text into a command graph.
    /// </summary>
    internal abstract class ParseResult
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
            $"Operators {_op1} and {_op2} appeared back to back without a command between them.";

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
        
        public override string Message => $"Operator {_op} must not appear at the beginning or end of a command.";

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
}