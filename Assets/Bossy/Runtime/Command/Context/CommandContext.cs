using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bossy.Frontend.Parsing;
using Bossy.Execution;
using Bossy.Utils;

namespace Bossy.Command
{
    /// <summary>
    /// A context object providing utility functionality to commands.
    /// </summary>
    public sealed class CommandContext : SimpleContext
    {
        private IReadable _reader;
        private Session _session;

        private List<Task> _tasks = new List<Task>();
        
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
        internal CommandContext
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
            return await ReadInternalAsync<T>(_reader);
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
        /// Delays the execution of this command.
        /// </summary>
        /// <param name="seconds">The seconds to delay for.</param>
        public async Task Delay(float seconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), _token);
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
        /// <param name="input">An optional input to read from. If unspecified, the standard input is used.</param>
        /// <typeparam name="T">The type of data to read.</typeparam>
        /// <returns>Each enumerated value until the stream closes.</returns>
        public async IAsyncEnumerable<T> ReadAllAsync<T>(IReadable input = null)
        {
            input ??= _reader;
            
            while (true)
            {
                T value;
                try
                {
                    value = await ReadInternalAsync<T>(input);
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
            _token.ThrowIfCancellationRequested();
            
            Writer.CloseWriter();
        }

        /// <summary>
        /// Execute another command.
        /// </summary>
        /// <param name="command">The command to run.</param>
        /// <returns></returns>
        public async Task ExecuteAsync(string command)
        {
            _token.ThrowIfCancellationRequested();
            
            var result = Bossy.Parser.Parse(command, Bossy.Settings.BossyCliSettings.ToOperatorList());

            if (!result.TryGetGraph(out var graph))
            {
                return;
            }
            
            await _session.ExecuteAsync(graph, _token);
        }

        /// <summary>
        /// Execute a new command and link its lifetime to this one.
        /// </summary>
        /// <param name="command">The command to run.</param>
        /// <param name="input">The stream to take input from.</param>
        /// <param name="output">The stream to send output to.</param>
        public ParseResult ExecuteAndLink(string command, AsyncPipe input, AsyncPipe output)
        {
            _token.ThrowIfCancellationRequested();
            
            var result = Bossy.Parser.Parse(command, Bossy.Settings.BossyCliSettings.ToOperatorList());

            if (!result.TryGetGraph(out var graph))
            {
                return result;
            }
            
            var task = _session.ExecuteAsync(graph, _token, input, output);
            
            // We just add this so the task isn't GCed, it's already canceled by the token.
            _tasks.Add(task);

            return result;
        }

        /// <summary>
        /// Execute a command and read from it.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <typeparam name="T">The type to read.</typeparam>
        /// <returns>Each item from the output stream.</returns>
        public IAsyncEnumerable<T> ExecuteAndRead<T>(string command)
        {
            var pipe = new AsyncPipe();
            var dummyInput = new AsyncPipe();
            dummyInput.CloseWriter();
            
            var result = ExecuteAndLink(command, dummyInput, pipe);
            
            if (result.IsEmpty || !result.TryGetGraph(out _))
            {
                Write(result.Message);
                pipe.CloseWriter();
                return Empty<T>();
            }
            
            return ReadAllAsync<T>(pipe);
        }
        
        /// <summary>
        /// Displays the prompt string and waits for input.
        /// </summary>
        /// <param name="prompt">The prompt string.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The reading task.</returns>
        public Task<T> Prompt<T>(string prompt)
        {
            _token.ThrowIfCancellationRequested();
            
            Write(prompt);
            return ReadAsync<T>();
        }

        /// <summary>
        /// Prompts with options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="prompt">A custom prompt message.</param>
        /// <typeparam name="T">The type of the options.</typeparam>
        /// <returns>The selected choice.</returns>
        public async Task<T> PromptWithOptions<T>(IEnumerable<T> options, string prompt = null)
        {
            var list = options.ToList();
            
            while (true)
            {
                Write(prompt ?? "Chose one of the following options:");
                
                Write(OptionsPrompt.Create(list));
                
                var choice = await ReadAsync<T>();
                
                if (list.Contains(choice))
                {
                    return choice;
                }
            }
        }

        private async Task<T> ReadInternalAsync<T>(IReadable input)
        {
            object response;
            var triedAdapting = false;
            TypeAdapterResult adapterResult = default;

            do
            {
                _token.ThrowIfCancellationRequested();

                response = await input.ReadAsync(typeof(T), _token);
            
                if (response == CloseWriterSentinel.Object)
                {
                    throw new BossyStreamClosedException();
                }

                if (response is T original)
                {
                    return original;
                }
                
                if (response is string textual)
                {
                    triedAdapting = true;
                    adapterResult = Bossy.TypeAdapterRegistry.TryConvert(textual, out T typed, true);
            
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
                    Writer.Write($"\"{response}\" of type \"{response.GetType().GetFriendlyName()}\" could not be converted to type \"{typeof(T).GetFriendlyName()}. Please enter a valid response:");
                }
                
            } while (_allowRetry);

            if (triedAdapting)
            {
                throw new BossyNotAdaptableException($"Could not parse response to type \"{typeof(T).GetFriendlyName()}\":\n{adapterResult.ErrorMessage}");
            }
            
            throw new BossyNotAdaptableException($"Type \"{response.GetType()}\" could not be converted to type {typeof(T).GetFriendlyName()}");
        }
        
        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}