using System;
using System.Linq;
using Bossy.Command;

namespace Bossy.Frontend
{
    public class OptionsPromptDisplayAdapter : CliDisplayAdapter
    {
        public override string Display(object value)
        {
            var prompt = value as OptionsPrompt;
            var count = 1;

            return prompt!.GetOptions().Cast<object>().Aggregate(string.Empty, (current, option) => current + $"{count++}: {option}{Environment.NewLine}");
        }
    }
}