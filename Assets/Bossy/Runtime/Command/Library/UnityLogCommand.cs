using System;
using System.Threading.Tasks;
using Bossy.Command;
using Bossy.Frontend.Autocomplete;
using UnityEngine;

namespace Bossy.Runtime.Command.Library
{
    [Command("unity-log", "Pipes the unity log to the console.")]
    public class UnityLogCommand : ICommand
    {
        [Suggest(nameof(Suggest))]
        [Optional(0, "The minimum log level to log")]
        private LogType _level = LogType.Log;
        
        [Switch('s', "Suppress the stack trace output on error")]
        private bool _suppress;
        
        public async Task<CommandStatus> ExecuteAsync(CommandContext ctx)
        {
            ctx.Write("Consider using this command in its own window by typing 'unity-log start!'");

            Application.LogCallback action = (m, s, t) => OnLog(m, s, t, ctx);
            Application.logMessageReceived += action;

            try
            {
                while (true)
                {   
                    await Task.Delay(100, ctx.CancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Catch the exception to do cleanup, then rethrow
                Application.logMessageReceived -= action;
                throw;
            }
        }
        
        private void OnLog(string condition, string stackTrace, LogType type, CommandContext ctx)
        {
            switch (type)
            {
                case LogType.Log:
                    if (_level is not LogType.Log) return; 
                    ctx.Write($"[Unity] {condition}");
                    break;
                case LogType.Warning:
                    if (_level is not (LogType.Log or LogType.Warning)) return;
                    ctx.WriteWarning($"[Unity] {condition}");
                    break;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    var message = $"[Unity] {condition}{(_suppress ? "" : $"{Environment.NewLine}[STACK] {stackTrace}")}";
                    ctx.WriteError(message);
                    break;   
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static string[] Suggest()
        {
            return Enum.GetNames(typeof(LogType));
        }
    }
}