using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend.Parsing;
using Bossy.Utils;

namespace Bossy.Session
{
    /// <summary>
    /// A context object providing utility functionality to commands.
    /// </summary>
    public sealed class CommandContext : SimpleContext
    {
        private IReadable _reader;
        private Session _session;
        
        private readonly bool _allowRetry;
        private readonly CancellationToken _token;

        /// <summary>
        /// Builds a new command context.
        /// </summary>
        /// <param name="session">The session running this command.</param>
        /// <param name="context">The Bossy context.</param>
        /// <param name="reader">A readable source.</param>
        /// <param name="writer">A writeable sink.</param>
        /// <param name="allowRetry">Whether to allow reads to be retried on bad type input.</param>
        /// <param name="token">The cancellation token associated with this execution.</param>
        public CommandContext
        (
            Session session,
            BossyContext context,
            IReadable reader,
            IWriteable writer,
            bool allowRetry,
            CancellationToken token
        ) : base(writer, context)
        {
            _session = session;
            _reader = reader;
            _allowRetry = allowRetry;
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

        public override void WriteWarning(object value, int indentCount = 0)
        {
            _token.ThrowIfCancellationRequested();
            
            base.WriteWarning(value, indentCount);
        }

        public override void WriteError(object value, int indentCount = 0)
        {
            _token.ThrowIfCancellationRequested();
            
            base.WriteError(value, indentCount);
        }

        public override void NewLine()
        {
            _token.ThrowIfCancellationRequested();
            
            base.NewLine();
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
            object response;
            var triedAdapting = false;
            TypeAdapterResult adapterResult = default;

            // TODO: IF THIS SURVIVES, REFACTOR IT
            
            do
            {
                _token.ThrowIfCancellationRequested();

                response = await _reader.ReadAsync(typeof(T), _token);
            
                if (response == CloseWriterSentinel.Object)
                {
                    throw new BossyStreamClosedException();
                }
                
                if (response is T original) return original;
                
                if (response is string textual)
                {
                    triedAdapting = true;
                    adapterResult = Bossy.TypeAdapterRegistry.TryConvert(textual, out T typed);
            
                    if (adapterResult.Success)
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
                    // Cast failed, ignore
                }

                if (_allowRetry)
                {
                    Writer.Write($"\"{response}\" could not be converted to type \"{typeof(T)}.");
                }
                
            } while (_allowRetry);

            if (triedAdapting)
            {
                throw new BossyNotAdaptableException($"Could not parse response to type " +
                                                 $"\"{typeof(T)}\":\n{adapterResult.ErrorMessage}");
            }
            
            throw new BossyNotAdaptableException($"Type \"{response.GetType()}\" could not be converted to type {typeof(T)}");
        }
        
        /// <summary>
        /// Delays the execution of this command.
        /// </summary>
        /// <param name="timeSpan">The time to delay for.</param>
        public async Task Delay(TimeSpan timeSpan)
        {
            await Task.Delay(timeSpan, _token);
        }

        /// <summary>
        /// Yields the execution of this command.
        /// </summary>
        public async Task Yield()
        {
            await Task.Yield();
        }

        /// <summary>
        /// Iterates the input stream until it closes.
        /// </summary>
        /// <typeparam name="T">The type of data to read.</typeparam>
        /// <returns>Each enumerated value until the stream closes.</returns>
        public async IAsyncEnumerable<T> ReadAllAsync<T>()
        {
            while (true)
            {
                T value;
                try
                {
                    value = await ReadAsync<T>();
                }
                catch (BossyStreamClosedException)
                {
                    yield break;
                }

                yield return value;
            }
        }

        /// <summary>
        /// Gets the context's reader.
        /// </summary>
        /// <returns>The reader.</returns>
        public IReadable GetReader() => _reader;
        
        /// <summary>
        /// Gets the context's writer.
        /// </summary>
        /// <returns>The writer.</returns>
        public IWriteable GetWriter() => Writer;
        
        /// <summary>
        /// Closes the output stream to indicate no more data is coming.
        /// </summary>
        public void CloseOutStream()
        {
            Writer.CloseWriter();
        }
    }
}