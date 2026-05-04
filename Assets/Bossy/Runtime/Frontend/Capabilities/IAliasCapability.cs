using System.Collections.Generic;

namespace Bossy.Frontend
{
    /// <summary>
    /// This front end is capable of using aliases.
    /// </summary>
    public interface IAliasCapability
    {
        /// <summary>
        /// Gets all aliases.
        /// </summary>
        public Dictionary<string, string> GetAliases();
        
        /// <summary>
        /// Creates a new alias. Aliases must contain all alpha characters and no whitespace.
        /// </summary>
        /// <param name="alias">The alias name.</param>
        /// <param name="value">The value to replace the alias with.</param>
        /// <returns>True if the alias was added successfully.</returns>
        public bool AssignAlias(string alias, string value);
        
        /// <summary>
        /// Deletes an alias.
        /// </summary>
        /// <param name="alias">The alias to delete.</param>
        /// <returns>True if the alias existed.</returns>
        public bool DeleteAlias(string alias);
    }
}