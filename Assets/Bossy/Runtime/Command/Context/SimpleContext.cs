using System;
using Bossy.Frontend;
using Bossy.Execution;
using UnityEngine;

namespace Bossy.Command
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
        protected IWriteable Writer;
        
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
        /// Skips a line.
        /// </summary>
        public virtual void NewLine()
        {
            // Write a space because it will be invisible and naturally add a new line.
            Writer.Write(" ");
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
            Format.Warning(value, this, indentCount);
        }

        /// <summary>
        /// Writes an error.
        /// </summary>
        /// <param name="value">The error.</param>
        /// <param name="indentCount">The number of spaces to indent.</param>
        public virtual void WriteError(object value, int indentCount = 0)
        {
            Format.Error(value, this, indentCount);
        }

        /// <summary>
        /// Tells if this command is running in the editor.
        /// </summary>
        /// <returns>True if this is the editor.</returns>
        public bool IsEditor()
        {
#if UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Tells if this command is running in a build.
        /// </summary>
        /// <returns>True if this is in a build.</returns>
        public bool IsBuild()
        {
            return !IsEditor();
        }

        /// <summary>
        /// Tells if this command is running in a runtime. Note that playing in the editor is a runtime environment.
        /// </summary>
        /// <returns>True if this is runtime.</returns>
        public bool IsRuntime()
        {
            return Application.isPlaying;
        }
        
        /// <summary>
        /// Tells if this command is running in edit mode or not. Note that when in the editor, all commands
        /// are considered to NOT be running in edit mode if the application is playing.
        /// </summary>
        /// <returns>True if this is edit mode.</returns>
        public bool IsEditMode()
        {
            return !IsRuntime();
        }
    }
}