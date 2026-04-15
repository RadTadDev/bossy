using System.Collections.Generic;
using System.Linq;
using Bossy.Command;

namespace Bossy.Shell
{
    public interface IBeginGraphBuilderStep
    {
        public IGeneralGraphBuilderStep Execute(ICommand cmd);
    }

    public interface IGeneralGraphBuilderStep
    {
        public IGeneralGraphBuilderStep Then(ICommand cmd);
        public IGeneralGraphBuilderStep And(ICommand cmd);
        public IGeneralGraphBuilderStep Or(ICommand cmd);
        public IGeneralGraphBuilderStep AndPipeTo(ICommand cmd);
        public CommandGraph Build();
    }
    
    /// <summary>
    /// The most basic executable unit for commands.
    /// </summary>
    public class CommandGraph : IBeginGraphBuilderStep, IGeneralGraphBuilderStep
    {
        /// <summary>
        /// The main reader.
        /// </summary>
        public IReadable DefaultReader { get; }
        
        /// <summary>
        /// The main writer.
        /// </summary>
        public IWriteable DefaultWriter { get; }

        /// <summary>
        /// Tells if the graph should execute in a window.
        /// </summary>
        public bool InWindow;

        /// <summary>
        /// Tells if the graph is empty.
        /// </summary>
        public bool IsEmpty => _nodes.Count == 0;
        
        private List<CommandGraphNode> _nodes = new();

        private CommandGraph(IReadable defaultReader, IWriteable defaultWriter, bool windowed)
        {
            DefaultReader = defaultReader;
            DefaultWriter = defaultWriter;
            InWindow = windowed;
        }

        /// <summary>
        /// Creates a new Command graph.
        /// </summary>
        /// <param name="defaultReader">The default reader.</param>
        /// <param name="defaultWriter">The default writer.</param>
        /// <param name="windowed">Whether this graph will run in a window.</param>
        /// <returns></returns>
        public static IBeginGraphBuilderStep Create(IReadable defaultReader, IWriteable defaultWriter, bool windowed)
        {
            return new CommandGraph(defaultReader, defaultWriter, windowed);
        }

        /// <summary>
        /// Adds a command to run.
        /// </summary>
        /// <param name="cmd">The command to run.</param>
        /// <returns>The builder.</returns>
        public IGeneralGraphBuilderStep Execute(ICommand cmd)
        {
            _nodes.Add(new CommandGraphNode(cmd));
            return this;
        }

        /// <summary>
        /// Adds a node to run next.
        /// </summary>
        /// <param name="cmd">The command to run.</param>
        /// <returns>The builder.</returns>
        public IGeneralGraphBuilderStep Then(ICommand cmd)
        {
            return AddNode(cmd, CommandGraphLink.Then);
        }

        /// <summary>
        /// Adds a new node to run if the previous one succeeded.
        /// </summary>
        /// <param name="cmd">The command to run.</param>
        /// <returns>The builder.</returns>
        public IGeneralGraphBuilderStep And(ICommand cmd)
        {
            return AddNode(cmd, CommandGraphLink.And);
        }

        /// <summary>
        /// Adds a new node to run if the previous one failed.
        /// </summary>
        /// <param name="cmd">The command to run.</param>
        /// <returns>The builder.</returns>
        public IGeneralGraphBuilderStep Or(ICommand cmd)
        {
            return AddNode(cmd, CommandGraphLink.Or);
        }

        /// <summary>
        /// Adds a new node to be piped to.
        /// </summary>
        /// <param name="cmd">The command to pipe to.</param>
        /// <returns>The builder.</returns>
        public IGeneralGraphBuilderStep AndPipeTo(ICommand cmd)
        {
            return AddNode(cmd, CommandGraphLink.Pipe);
        }

        /// <summary>
        /// Completes the graph.
        /// </summary>
        /// <returns>The command graph.</returns>
        public CommandGraph Build()
        {
            return this;
        }

        /// <summary>
        /// Converts the graph to an array.
        /// </summary>
        /// <returns>The array of nodes.</returns>
        public CommandGraphNode[] ToArray() => _nodes.ToArray();
        
        private IGeneralGraphBuilderStep AddNode(ICommand command, CommandGraphLink link)
        {
            ICommandGraphNodeWriter writer = _nodes.Last();
            writer.AddLink(link);
            _nodes.Add(new CommandGraphNode(command));
            return this;
        }
    }
}