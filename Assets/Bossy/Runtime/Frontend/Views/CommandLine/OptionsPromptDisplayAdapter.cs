using System;
using System.Linq;
using Bossy.Command;

namespace Bossy.Frontend
{
    public class OptionsPromptDisplayAdapter : ICliDisplayAdapter
    {
        private OptionsPrompt _current;
        
        public string Display(object value)
        {
            var prompt = value as OptionsPrompt;

            _current = prompt;
            
            var count = 1;
            
            return prompt!.GetOptions().Cast<IOptionGetter>()
                .Aggregate(string.Empty, (current, option) => current + $"{count++}: {option.Read()}{Environment.NewLine}")
                .TrimEnd();
        }

        public bool OwnsRead() => true;

        public object Read(string input)
        {
            if (!int.TryParse(input, out var index))
            {
                return input;
            }

            // The user inputs in 1-indexed
            index--;
            
            if (index >= _current.Count)
            {
                return input;
            }
            
            var option = _current.GetOptions().Cast<object>().ElementAt(index);

            return ((IOptionGetter)option).Read();
        }
    }
}