using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend;
using Bossy.Frontend.Parsing;
using Bossy.Utils;

namespace Bossy.Shell
{
    /// <summary>
    /// A context object providing utility functionality to commands.
    /// </summary>
    public sealed class CommandContext : SimpleContext
    {
        /// <summary>
        /// The user interface that spawned this command.
        /// Use this to test for specific front end capabilities.
        /// </summary>
        public IFrontEndCapabilities Capabilities => _capabilitiesSourcer?.Invoke();

        /// <summary>
        /// The session running this command.
        /// </summary>
        public ISafeSession Session => _session;
        
        /// <summary>
        /// The context's reader.
        /// </summary>
        public IReadable Reader { get; }
        
        /// <summary>
        /// The context's writer.
        /// </summary>
        public IWriteable OutputStream => Writer;

        private Session _session;
        private TypeAdapterRegistry _adapterRegistry;
        private Func<IFrontEndCapabilities> _capabilitiesSourcer;
        
        private readonly bool _allowRetry;
        private readonly CancellationToken _token;

        /// <summary>
        /// Builds a new command context.
        /// </summary>
        /// <param name="session">The session running this command.</param>
        /// <param name="adapterRegistry">An adapter registry to convert string to type.</param>
        /// <param name="reader">A readable source.</param>
        /// <param name="writer">A writeable sink.</param>
        /// <param name="allowRetry">Whether to allow reads to be retried on bad type input.</param>
        /// <param name="token">The cancellation token associated with this execution.</param>
        public CommandContext
        (
            Session session,
            TypeAdapterRegistry adapterRegistry,
            IReadable reader,
            IWriteable writer,
            bool allowRetry,
            CancellationToken token
        ) : base(writer)
        {
            _session = session;
            _adapterRegistry = adapterRegistry;
            Reader = reader;
            _allowRetry = allowRetry;
            _token = token;
        }

        public void SetCapabilitySourcer(Func<IFrontEndCapabilities> sourcer)
        {
            _capabilitiesSourcer = sourcer;
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
            object response;
            var triedAdapting = false;
            TypeAdapterResult adapterResult = default;

            // TODO: IF THIS SURVIVES, REFACTOR IT
            
            do
            {
                _token.ThrowIfCancellationRequested();

                response = await Reader.ReadAsync(typeof(T), _token);
            
                if (response == CloseWriterSentinel.Object)
                {
                    throw new BossyStreamClosedException();
                }
                
                if (response is T original) return original;
                
                if (response is string textual)
                {
                    triedAdapting = true;
                    adapterResult = _adapterRegistry.TryConvert(textual, out T typed);
            
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

        public void CloseOutStream()
        {
            Writer.CloseWriter();
        }
    }
}