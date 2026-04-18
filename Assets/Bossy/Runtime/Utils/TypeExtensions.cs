using System;
using System.Collections.Generic;

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
    }
}