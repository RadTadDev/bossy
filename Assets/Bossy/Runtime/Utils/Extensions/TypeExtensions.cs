using System;
using System.Collections.Generic;
using System.Linq;

namespace Bossy.Utils
{
    public static class TypeExtensions
    {
        private static readonly Dictionary<Type, string> FriendlyNames = new()
        {
            { typeof(bool),    "bool"    },
            { typeof(byte),    "byte"    },
            { typeof(sbyte),   "sbyte"   },
            { typeof(char),    "char"    },
            { typeof(short),   "short"   },
            { typeof(ushort),  "ushort"  },
            { typeof(int),     "int"     },
            { typeof(uint),    "uint"    },
            { typeof(long),    "long"    },
            { typeof(ulong),   "ulong"   },
            { typeof(float),   "float"   },
            { typeof(double),  "double"  },
            { typeof(decimal), "decimal" },
            { typeof(string),  "string"  },
            { typeof(object),  "object"  },
            { typeof(void),    "void"    },
        };

        
        private static Dictionary<string, Type> _typeIndex;

        private static Dictionary<string, Type> TypeIndex
        {
            get
            {
                if (_typeIndex != null) return _typeIndex;
        
                _typeIndex = new Dictionary<string, Type>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in assembly.GetTypes())
                    _typeIndex.TryAdd(type.Name, type);        // simple name, first-one-wins
        
                return _typeIndex;
            }
        }
        
        
        /// <summary>
        /// Prints a friendly name for common built-in types.
        /// </summary>
        public static string GetFriendlyName(this Type type)
        {
            // Nullable<T> -> "T?"
            var nullableInner = Nullable.GetUnderlyingType(type);
            if (nullableInner != null)
                return $"{nullableInner.GetFriendlyName()}?";

            if (FriendlyNames.TryGetValue(type, out var name)) return name;

            // Generic -> "Name<T1, T2>"
            if (!type.IsGenericType) return type.Name;
            
            var baseName = type.Name.Substring(0, type.Name.IndexOf('`'));
            var args = string.Join(", ", Array.ConvertAll(type.GetGenericArguments(), a => a.GetFriendlyName()));
            return $"{baseName}<{args}>";
        }
        

        public static Type GetTypeFromName(string name)
        {
            // Friendly name (int, string, bool etc.)
            var friendlyMatch = FriendlyNames.FirstOrDefault(kvp => kvp.Value == name);
            if (friendlyMatch.Key != null)
                return friendlyMatch.Key;

            // Assembly-qualified or full name
            var direct = Type.GetType(name);
            if (direct != null)
                return direct;

            // Qualified — scan assemblies directly (fast, O(1) per assembly)
            if (name.Contains('.'))
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var t = assembly.GetType(name);
                    if (t != null) return t;
                }
                return null;
            }

            // Simple name — index lookup
            TypeIndex.TryGetValue(name, out var result);
            return result;
        }
    }
}