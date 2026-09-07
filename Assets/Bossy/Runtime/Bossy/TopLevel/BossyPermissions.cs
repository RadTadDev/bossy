using System.Collections.Generic;
using Bossy.Schema;

namespace Bossy
{
    /// <summary>
    /// A very simple permissions manager to gate playtests. This is not intended to be foolproof,
    /// just enough to keep people from running any command they want with no resistance.
    /// </summary>
    public class BossyPermissions
    {
        public static bool IsLoggedIn { get; private set; } = false;

        private HashSet<CommandSchema> _whiteList;
        
        private readonly string _password;
        
        public BossyPermissions(string password, HashSet<CommandSchema> whiteList, bool startLoggedIn)
        {
            _password = password;
            _whiteList =  whiteList;
            
            if (startLoggedIn)
            {
                IsLoggedIn = true;
            }
        }
        
        /// <summary>
        /// Attempts to log in.
        /// </summary>
        /// <param name="password">The attempted password.</param>
        /// <returns>True if the login was successful.</returns>
        public bool AttemptLogIn(string password)
        {
            if (password != _password) return false;
            
            IsLoggedIn = true;
            return true;
        }

        /// <summary>
        /// Tells if a command can be executed.
        /// </summary>
        /// <param name="schema">The schema to check for.</param>
        /// <returns>True if the command is allowed to be executed.</returns>
        public bool CanExecute(CommandSchema schema) => IsLoggedIn || _whiteList.Contains(schema);
    }
}