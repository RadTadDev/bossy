using System;
using System.Collections.Generic;

namespace Bossy.Frontend
{
    /// <summary>
    /// Defines the logic a host needs to support.
    /// </summary>
    internal interface IHostController
    {
        /// <summary>
        /// Invoked when no session remains for a host.
        /// </summary>
        public Action NoSessionRemains { get; set; }
        
        /// <summary>
        /// Adds a session and its viewer to the host.
        /// </summary>
        /// <param name="viewer">The session viewer.</param>
        public void AddViewer(SessionViewer viewer);
        
        /// <summary>
        /// Gets a list of all hosted bridges.
        /// </summary>
        /// <returns>The list of bridges.</returns>
        public IEnumerable<Bridge> GetHostedBridges();

        /// <summary>
        /// Called when the host is being shown.
        /// </summary>
        public void Show();

        /// <summary>
        /// Called when the host is being hidden.
        /// </summary>
        public void Hide();
    }
}