using System.Collections.Generic;

namespace Bossy.Frontend
{
    /// <summary>
    /// A front end which can keep and manage history.
    /// </summary>
    public interface IHistorical
    {
        /// <summary>
        /// Gets the history.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetHistory();

        /// <summary>
        /// Clears the history.
        /// </summary>
        public void ClearHistory();
    }
}