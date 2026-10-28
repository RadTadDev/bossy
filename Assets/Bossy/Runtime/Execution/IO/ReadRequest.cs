using System;
using System.Collections.Generic;

namespace Bossy.Execution
{
    /// <summary>
    /// Describes a read that a reader is asked to fulfill.
    /// </summary>
    public sealed class ReadRequest
    {
        /// <summary>
        /// The type being requested. A reader may still respond with another type; the caller converts it.
        /// </summary>
        public Type RequestedType { get; }

        /// <summary>
        /// The choices to offer, or null when the read is free-form.
        /// </summary>
        public IReadOnlyList<ReadChoice> Choices { get; }

        /// <summary>
        /// Creates a read request.
        /// </summary>
        /// <param name="requestedType">The type being requested.</param>
        /// <param name="choices">The choices to offer, or null when the read is free-form.</param>
        public ReadRequest(Type requestedType, IReadOnlyList<ReadChoice> choices = null)
        {
            RequestedType = requestedType;
            Choices = choices;
        }
    }
}
