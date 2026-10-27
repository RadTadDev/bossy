using System.Linq;
using Bossy.Execution;

namespace Bossy.Frontend
{
    /// <summary>
    /// Creates session views with their sessions and frontends.
    /// </summary>
    internal sealed class SessionFactory
    {
        private const string ConsoleTitle = "Bossy";
        
        private readonly BossyContext _context;
        private readonly BossyPermissions _permissions;

        /// <summary>
        /// Creates a new session factory.
        /// </summary>
        /// <param name="context">The Bossy context.</param>
        /// <param name="permissions">The permissions every session checks against.</param>
        public SessionFactory(BossyContext context, BossyPermissions permissions)
        {
            _context = context;
            _permissions = permissions;
        }

        /// <summary>
        /// Creates a view for a new interactive console session.
        /// </summary>
        /// <param name="openCommandWindow">Opens command windows for the session.</param>
        /// <returns>The view. Call <see cref="SessionView.Start()"/> to run it.</returns>
        public SessionView CreateConsole(SessionView.CommandWindowOpener openCommandWindow)
        {
            var frontend = new CliView(_context, CliView.InputVisibility.Always);
            
            return new SessionView(
                ConsoleTitle,
                frontend,
                host => new Session(_context, _permissions, frontend, host),
                openCommandWindow,
                _context.Settings.BossyInputSettings);
        }

        /// <summary>
        /// Creates a view for a session that runs a single graph in a command window.
        /// </summary>
        /// <param name="template">The session that opened the window.</param>
        /// <param name="graph">The graph the window will run, used for its title.</param>
        /// <param name="openCommandWindow">Opens command windows for the new session.</param>
        /// <returns>The view. Call <see cref="SessionView.Start"/> to run it.</returns>
        public SessionView CreateCommandWindow(Session template, CommandGraph graph, SessionView.CommandWindowOpener openCommandWindow)
        {
            var frontend = new CliView(_context, CliView.InputVisibility.WhenReading);
            var commandName = graph.ToArray().FirstOrDefault()?.Schema?.Name;
            
            return new SessionView(
                commandName == null ? ConsoleTitle : $"{ConsoleTitle}: {commandName}",
                frontend,
                host => template.Clone(frontend, host),
                openCommandWindow,
                _context.Settings.BossyInputSettings);
        }
    }
}
