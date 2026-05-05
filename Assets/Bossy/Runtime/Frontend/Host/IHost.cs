using System;
using Bossy.Settings;

namespace Bossy.Frontend
{
    /// <summary>
    /// Hosts graphics in a particular space.
    /// </summary>
    internal interface IHost
    {
        /// <summary>
        /// The host's controller.
        /// </summary>
        public IHostController Controller { get; }

        /// <summary>
        /// The space this host is operating in.
        /// </summary>
        public SessionSpace Space { get; }
        
        /// <summary>
        /// Initializes the host.
        /// </summary>
        /// <param name="manager">The host manager.</param>
        /// <param name="settings">The input settings.</param>
        /// <param name="createNewSession">A hook to create new sessions.</param>
        /// <param name="space">The space the host is operating in.</param>
        public void Initialize(HostManager manager, BossyInputSettings settings, Action<FrontendType, SessionSpace> createNewSession, SessionSpace space);
        
        /// <summary>
        /// Reveals this host.
        /// </summary>
        public void Reveal();

        /// <summary>
        /// Hides this host.
        /// </summary>
        public void Hide();

        /// <summary>
        /// Closes this host.
        /// </summary>
        public void Close();
    }
}