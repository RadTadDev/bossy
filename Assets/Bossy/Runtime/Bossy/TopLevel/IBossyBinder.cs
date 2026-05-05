using System;

namespace Bossy
{
    /// <summary>
    /// Allows Bossy to get items that are registered.
    /// </summary>
    public interface IBossyBinder
    {
        /// <summary>
        /// Tries to get an object of type T.
        /// </summary>
        /// <param name="obj">The object to return.</param>
        /// <param name="requestedType">The type of the object to get.</param>
        /// <returns>True if the item was returned, otherwise false.</returns>
        public bool TryGet(Type requestedType, out object obj);
    }
}