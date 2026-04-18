using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bossy.FrontEnd;
using Bossy.FrontEnd.Parsing;
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
        public IUserInterface UserInterface { get; }
        
        /// <summary>
        /// The context's reader.
        /// </summary>
        public IReadable InputStream { get; }
        
        /// <summary>
        /// The context's writer.
        /// </summary>
        public IWriteable OutputStream => writer;
        
        private readonly SessionManager _sessionManager;
        private readonly bool _allowRetry;
        private readonly CancellationToken _token;

        /// <summary>
        /// Builds a new command context.
        /// </summary>
        /// <param name="sessionManager">The shell.</param>
        /// <param name="userInterface">The user interface that spawned this command.</param>
        /// <param name="inputStream">A readable source.</param>
        /// <param name="writer">A writeable sink.</param>
        /// <param name="allowRetry">Whether to allow reads to be retried on bad type input.</param>
        /// <param name="token">The cancellation token associated with this execution.</param>
        public CommandContext
        (
            SessionManager sessionManager,
            IUserInterface userInterface,
            IReadable inputStream,
            IWriteable writer,
            bool allowRetry,
            CancellationToken token
        ) : base(writer)
        {
            UserInterface = userInterface;
            InputStream = inputStream;
            _allowRetry = allowRetry;
            _sessionManager = sessionManager;
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
            object response;
            var triedAdapting = false;
            TypeAdapterResult adapterResult = default;

            do
            {
                _token.ThrowIfCancellationRequested();

                // Prevent hot loops on suppliers
                await Task.Yield();
                response = await InputStream.ReadAsync(typeof(T), _token);
            
                if (response is null)
                {
                    throw new BossyStreamClosedException();
                }
                
                if (response is T original) return original;
                
                if (response is string textual)
                {
                    triedAdapting = true;
                    adapterResult = _sessionManager.TypeAdapterRegistry.TryConvert(textual, out T typed);
            
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
                    writer.Write($"Type \"{response.GetType()}\" could not be converted to type \"{typeof(T)}.");
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
    }
}