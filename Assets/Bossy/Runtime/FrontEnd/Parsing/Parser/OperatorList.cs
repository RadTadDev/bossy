using System.Collections.Generic;

namespace Bossy.FrontEnd.Parsing
{
    /// <summary>
    /// A list of all operators the parser recognizes.
    /// </summary>
    public class OperatorList
    {
        /// <summary>
        /// The operator for running a command after another.
        /// </summary>
        public readonly string ThenOperator;
        
        /// <summary>
        /// The operator for running a command only if the previous one succeeded.
        /// </summary>
        public readonly string AndOperator;
        
        /// <summary>
        /// The operator for running a command only if the previous one failed.
        /// </summary>
        public readonly string OrOperator;

        /// <summary>
        /// The operator for piping one command's output to the next command's input.
        /// </summary>
        public readonly string PipeOperator;

        /// <summary>
        /// The operator for running a job in a window.
        /// </summary>
        public readonly string WindowOperator;

        /// <summary>
        /// Creates a new operator list.
        /// </summary>
        /// <param name="then">The then operator.</param>
        /// <param name="and">The and operator.</param>
        /// <param name="or">The or operator.</param>
        /// <param name="pipe">The pipe operator.</param>
        /// <param name="window">The window operator.</param>
        public OperatorList(string then, string and, string or, string pipe, string window)
        {
            ThenOperator = then;
            AndOperator = and;
            OrOperator = or;
            PipeOperator = pipe;
            WindowOperator = window;
        }
        
        /// <summary>
        /// Gets a collection of all operators.
        /// </summary>
        public IEnumerable<string> ToEnumerable() => new [] { ThenOperator, AndOperator, OrOperator, PipeOperator, WindowOperator };
    }
}