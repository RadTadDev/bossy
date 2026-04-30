using System;
using System.Collections.Generic;

namespace Bossy.Frontend
{
    internal interface IHostController
    {
        public Action NoSessionRemains { get; set; }
        
        public void AddViewer(SessionViewer viewer);
        
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