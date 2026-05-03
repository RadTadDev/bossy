using System;
using Bossy.Command;
using Bossy.Frontend;

namespace Bossy.Session
{
    /// <summary>
    /// A simple command context.
    /// </summary>
    public class SimpleContext
    {
        /// <summary>
        /// Holds Bossy utilities and managers.
        /// </summary>
        public readonly BossyContext Bossy;
        
        /// <summary>
        /// The user interface that spawned this command.
        /// Use this to test for specific front end capabilities.
        /// </summary>
        public IFrontEndCapabilities Capabilities => _capabilitiesSourcer?.Invoke();
        
        /// <summary>
        /// The output writer
        /// </summary>
        protected readonly IWriteable Writer;
        
        private Func<IFrontEndCapabilities> _capabilitiesSourcer;

        /// <summary>
        /// Creates a new simple context.
        /// </summary>
        /// <param name="writer">The output writer.</param>
        /// <param name="bossyContext">The Bossy context.</param>
        public SimpleContext(IWriteable writer, BossyContext bossyContext)
        {
            Writer = writer;
            Bossy = bossyContext;
        }
        
        /// <summary>
        /// Writes a value.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public virtual void Write(object value)
        {
            Writer.Write(value);
        }
        
        /// <summary>
        /// Sets the capabilities sourcer.
        /// </summary>
        /// <param name="sourcer">The sourcer.</param>
        public void SetCapabilitySourcer(Func<IFrontEndCapabilities> sourcer)
        {
            _capabilitiesSourcer = sourcer;
        }

        /// <summary>
        /// Writes a warning.
        /// </summary>
        /// <param name="value">The warning.</param>
        /// <param name="indentCount">The number of spaces to indent.</param>
        public virtual void WriteWarning(object value, int indentCount = 0)
        {
            Formatter.Warning(value, this, indentCount);
        }

        /// <summary>
        /// Writes an error.
        /// </summary>
        /// <param name="value">The error.</param>
        /// <param name="indentCount">The number of spaces to indent.</param>
        public virtual void WriteError(object value, int indentCount = 0)
        {
            Formatter.Error(value, this, indentCount);
        }
    }
}