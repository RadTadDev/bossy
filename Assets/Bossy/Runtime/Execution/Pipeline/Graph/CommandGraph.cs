using System.Collections.Generic;
using System.Linq;
using Bossy.Command;
using Bossy.Schema;

namespace Bossy.Execution
{
    /// <summary>
    /// Begins building a graph.
    /// </summary>
    public interface IBeginGraphBuilderStep
    {
        /// <summary>
        /// Adds a command to run.
        /// </summary>
        /// <param name="cmd">The command to run.</param>
        /// <param name="schema">The schema for this command.</param>
        /// <returns>The builder.</returns>
        public IGeneralGraphBuilderStep Execute(ICommand cmd, CommandSchema schema);
    }

    /// <summary>
    /// Builds a graph.
    /// </summary>
    public interface IGeneralGraphBuilderStep
    {
        /// <summary>
        /// Adds a node to run next.
        /// </summary>
        /// <param name="cmd">The command to run.</param>
        /// <param name="schema">The schema for this command.</param>
        /// <returns>The builder.</returns>
        public IGeneralGraphBuilderStep Then(ICommand cmd, CommandSchema schema);
        
        /// <summary>
        /// Adds a new node to run if the previous one succeeded.
        /// </summary>
        /// <param name="cmd">The command to run.</param>
        /// <param name="schema">The schema for this command.</param>
        /// <returns>The builder.</returns>
        public IGeneralGraphBuilderStep And(ICommand cmd, CommandSchema schema);
        
        /// <summary>
        /// Adds a new node to run if the previous one failed.
        /// </summary>
        /// <param name="cmd">The command to run.</param>
        /// <param name="schema">The schema for this command.</param>
        /// <returns>The builder.</returns>
        public IGeneralGraphBuilderStep Or(ICommand cmd, CommandSchema schema);

        /// <summary>
        /// Adds a new node to be piped to.
        /// </summary>
        /// <param name="cmd">The command to pipe to.</param>
        /// <param name="schema">The schema for this command.</param>
        /// <returns>The builder.</returns>
        public IGeneralGraphBuilderStep AndPipeTo(ICommand cmd, CommandSchema schema);
        
        /// <summary>
        /// Completes the graph.
        /// </summary>
        /// <returns>The command graph.</returns>
        public CommandGraph Build();
    }
    
    /// <summary>
    /// The most basic executable unit for commands.
    /// </summary>
    public class CommandGraph : IBeginGraphBuilderStep, IGeneralGraphBuilderStep
    {
        /// <summary>
        /// Tells if the graph should execute in a window.
        /// </summary>
        public bool Windowed;

        /// <summary>
        /// Tells if the graph is empty.
        /// </summary>
        public bool IsEmpty => _nodes.Count == 0;
        
        private readonly List<CommandGraphNode> _nodes = new();

        private CommandGraph(bool windowed = false)
        {
            Windowed = windowed;
        }

        /// <summary>
        /// Creates a new Command graph.
        /// </summary>
        /// <param name="windowed">Whether this graph will run in a window.</param>
        /// <returns></returns>
        public static IBeginGraphBuilderStep Create(bool windowed)
        {
            return new CommandGraph(windowed);
        }
        
        public IGeneralGraphBuilderStep Execute(ICommand cmd, CommandSchema schema)
        {
            _nodes.Add(new CommandGraphNode(cmd, schema));
            return this;
        }
        
        public IGeneralGraphBuilderStep Then(ICommand cmd, CommandSchema schema)
        {
            return AddNode(cmd, schema, CommandGraphLink.Then);
        }
        
        public IGeneralGraphBuilderStep And(ICommand cmd, CommandSchema schema)
        {
            return AddNode(cmd, schema, CommandGraphLink.And);
        }
        
        public IGeneralGraphBuilderStep Or(ICommand cmd, CommandSchema schema)
        {
            return AddNode(cmd, schema, CommandGraphLink.Or);
        }
        
        public IGeneralGraphBuilderStep AndPipeTo(ICommand cmd, CommandSchema schema)
        {
            return AddNode(cmd, schema, CommandGraphLink.Pipe);
        }
        
        public CommandGraph Build() => this;
        
        /// <summary>
        /// Converts the graph to an array.
        /// </summary>
        /// <returns>The array of nodes.</returns>
        public CommandGraphNode[] ToArray() => _nodes.ToArray();
        
        private IGeneralGraphBuilderStep AddNode(ICommand command, CommandSchema schema, CommandGraphLink link)
        {
            ICommandGraphNodeWriter writer = _nodes.Last();
            writer.AddLink(link);
            _nodes.Add(new CommandGraphNode(command, schema));
            return this;
        }
    }
}