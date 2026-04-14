using System;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Shell;
using Bossy.Utils;

namespace Bossy.Shell
{
    /// <summary>
    /// A context object providing utility functionality to commands.
    /// </summary>
    public sealed class CommandContext : SimpleContext
    {
        private readonly Shell _shell;
        private readonly IReadable _reader;
        private readonly IWriteable _writer;
        private readonly bool _allowRetry;
        private readonly CancellationToken _token;

        /// <summary>
        /// Builds a new command context.
        /// </summary>
        /// <param name="shell">The shell.</param>
        /// <param name="reader">A readable source.</param>
        /// <param name="writer">A writeable sink.</param>
        /// <param name="allowRetry">Whether to allow reads to be retried on bad type input.</param>
        /// <param name="token">The cancellation token associated with this execution.</param>
        public CommandContext(Shell shell, IReadable reader, IWriteable writer, bool allowRetry, CancellationToken token) : base(writer)
        {
            _reader = reader;
            _writer = writer;
            _allowRetry = allowRetry;
            _shell = shell;
            _token = token;
        }

        /// <summary>
        /// Get the cancellation token for this command.
        /// </summary>
        public CancellationToken CancellationToken => _token;
        
        /// <summary>
        /// Writes to the standard output stream.
        /// </summary>
        /// <param name="value">The value to write.</param>
        public override void Write(object value)
        {
            _token.ThrowIfCancellationRequested();
            
            base.Write(value);
        }

        /// <summary>
        /// Reads a value of the given type.
        /// </summary>
        /// <typeparam name="T">The type to read.</typeparam>
        /// <returns>The typed value.</returns>
        /// <remarks>This function throws if a read response cannot be converted to the requested type. This is
        /// not necessary to handle, but catching it gives you explicit control when a command would otherwise die.</remarks>
        public async Task<T> ReadAsync<T>()
        {
            _token.ThrowIfCancellationRequested();
            
            object response;

            do
            {
                response = await _reader.ReadAsync(typeof(T), _token);

                if (response is T original) return original;

                if (response is string textual)
                {
                    var convertResult = _shell.TypeAdapterRegistry.TryConvert(textual, out T typed);

                    if (convertResult.Success)
                    {
                        return typed;
                    }
                }

                // Catch and allow all numeric conversions 
                try
                {
                    var casted = (T)Convert.ChangeType(response, typeof(T));
                    return casted;
                }
                catch
                {
                    // cast failed, ignore
                }

                _writer.Write($"Type \"{response.GetType()}\" could not be converted to type \"{typeof(T)}\". " +
                              "Please enter a valid response.");
            } while (_allowRetry);

            throw new BossyNotAdaptableException($"Type \"{response.GetType()}\" could not be converted to type \"{typeof(T)}\"");
        }
        
        /// <summary>
        /// Delays the execution of this command.
        /// </summary>
        /// <param name="seconds">The seconds to delay for.</param>
        public async Task Delay(float seconds)
        {
            _token.ThrowIfCancellationRequested();
            
            await Task.Delay(TimeSpan.FromSeconds(seconds), _token);
        }
    }
}