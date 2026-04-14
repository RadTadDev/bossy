using System;
using System.Collections.Generic;
using System.Linq;

namespace Bossy.FrontEnd.Parsing
{
    public class TypeAdapterRegistry
    {
        private Dictionary<Type, HashSet<ITypeAdapter>> _adapters = new();

        public TypeAdapterResult TryConvert<T>(string input, out T output)
        {
            return TryConvert(new TokenStream(input), out output);
        }
        
        public TypeAdapterResult TryConvert<T>(TokenStream cursor, out T output)
        {
            output = default;

            if (!_adapters.TryGetValue(typeof(T), out var set))
            {
                return TypeAdapterResult.Fail($"No registered adapter handles type \"{typeof(T)}\"");
            }

            // Multiple adapters are allowed to specify distinct input schemes for the same type.
            var errors = new List<string>();
            foreach (var adapter in set)
            {
                var result = adapter.TryConvert(cursor, out var converted);

                if (!result.Success)
                {
                    errors.Add(result.ErrorMessage);
                    continue;
                }
                
                output = (T)converted;
                return result;   
            }

            var message = errors.Count == 1 ? errors.First() : "Could not match any converter:\n" + string.Join("\n- ", errors);
            return TypeAdapterResult.Fail(message);
        }

        public void RegisterAdapter(Type type, ITypeAdapter adapter)
        {
            if (_adapters.TryGetValue(type, out var set))
            {
                set.Add(adapter);
            }
            else
            {
                _adapters[type] = new HashSet<ITypeAdapter> { adapter };
            }
        }
    }
}