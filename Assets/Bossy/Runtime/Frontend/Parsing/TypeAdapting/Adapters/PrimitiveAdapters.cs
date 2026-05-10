using System;
using Bossy.Utils;

namespace Bossy.Frontend.Parsing
{
    public class BoolAdapter : BaseTypeAdapter<bool>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out bool output)
        {
            // Assume that if the general method is called, we can use implicit booleans
            return HandleAdaptation(stream, registry, out output, false);
        }
        
        public TypeAdapterResult HandleAdaptation(TokenStream stream, TypeAdapterRegistry registry, out bool output, bool requireExplicitBool)
        {
            // Implicit 'true' on no token
            if (!stream.TryPeek(out var token))
            {
                // Only return if we are allowed
                if (!requireExplicitBool)
                {
                    output = true;
                    return TypeAdapterResult.Pass(0);
                }
            }

            // If the token is a boolean, always consume it
            if (bool.TryParse(token, out output))
            {
                stream.TryConsume(out _);
                return TypeAdapterResult.Pass(1);
            }
            
            // If we required an explicit bool but did not get one, fail
            if (requireExplicitBool)
            {
                // Consume here for downstream error checking consistency
                stream.TryConsume(out _);
                return TypeAdapterResult.Fail("An explicit boolean value is required in this case", 1);
            }

            // Implicit booleans are allowed, return success
            output = true;
            return TypeAdapterResult.Pass(0);
        }
    }

    public class ByteAdapter : BaseTypeAdapter<byte>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out byte output)
        {
            if (stream.TryConsume(out var token) && byte.TryParse(token, out output))
                return TypeAdapterResult.Pass(1);

            output = 0;
            return TypeAdapterResult.Fail($"Expected byte, got \"{token ?? "nothing"}\"", 1);
        }
    }

    public class SByteAdapter : BaseTypeAdapter<sbyte>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out sbyte output)
        {
            if (stream.TryConsume(out var token) && sbyte.TryParse(token, out output))
                return TypeAdapterResult.Pass(1);

            output = 0;
            return TypeAdapterResult.Fail($"Expected sbyte, got \"{token ?? "nothing"}\"", 1);
        }
    }

    public class ShortAdapter : BaseTypeAdapter<short>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out short output)
        {
            if (stream.TryConsume(out var token) && short.TryParse(token, out output))
                return TypeAdapterResult.Pass(1);

            output = 0;
            return TypeAdapterResult.Fail($"Expected short, got \"{token ?? "nothing"}\"", 1);
        }
    }

    public class UShortAdapter : BaseTypeAdapter<ushort>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out ushort output)
        {
            if (stream.TryConsume(out var token) && ushort.TryParse(token, out output))
                return TypeAdapterResult.Pass(1);

            output = 0;
            return TypeAdapterResult.Fail($"Expected ushort, got \"{token ?? "nothing"}\"", 1);
        }
    }

    public class IntAdapter : BaseTypeAdapter<int>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out int output)
        {
            if (stream.TryConsume(out var token) && int.TryParse(token, out output))
                return TypeAdapterResult.Pass(1);

            output = 0;
            return TypeAdapterResult.Fail($"Expected int, got \"{token ?? "nothing"}\"", 1);
        }
    }

    public class UIntAdapter : BaseTypeAdapter<uint>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out uint output)
        {
            if (stream.TryConsume(out var token) && uint.TryParse(token, out output))
                return TypeAdapterResult.Pass(1);

            output = 0;
            return TypeAdapterResult.Fail($"Expected uint, got \"{token ?? "nothing"}\"", 1);
        }
    }

    public class LongAdapter : BaseTypeAdapter<long>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out long output)
        {
            if (stream.TryConsume(out var token) && long.TryParse(token, out output))
                return TypeAdapterResult.Pass(1);

            output = 0;
            return TypeAdapterResult.Fail($"Expected long, got \"{token ?? "nothing"}\"", 1);
        }
    }

    public class ULongAdapter : BaseTypeAdapter<ulong>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out ulong output)
        {
            if (stream.TryConsume(out var token) && ulong.TryParse(token, out output))
                return TypeAdapterResult.Pass(1);

            output = 0;
            return TypeAdapterResult.Fail($"Expected ulong, got \"{token ?? "nothing"}\"", 1);
        }
    }

    public class FloatAdapter : BaseTypeAdapter<float>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out float output)
        {
            if (stream.TryConsume(out var token) && float.TryParse(token, out output))
                return TypeAdapterResult.Pass(1);

            output = 0;
            return TypeAdapterResult.Fail($"Expected float, got \"{token ?? "nothing"}\"", 1);
        }
    }

    public class DoubleAdapter : BaseTypeAdapter<double>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out double output)
        {
            if (stream.TryConsume(out var token) && double.TryParse(token, out output))
                return TypeAdapterResult.Pass(1);

            output = 0;
            return TypeAdapterResult.Fail($"Expected double, got \"{token ?? "nothing"}\"", 1);
        }
    }

    public class CharAdapter : BaseTypeAdapter<char>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out char output)
        {
            if (stream.TryConsume(out var token) && token.Length == 1)
            {
                output = token[0];
                return TypeAdapterResult.Pass(1);
            }

            output = '\0';
            return TypeAdapterResult.Fail($"Expected single character, got \"{token ?? "nothing"}\"", 1);
        }
    }

    public class StringAdapter : BaseTypeAdapter<string>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out string output)
        {
            if (stream.TryConsume(out output))
                return TypeAdapterResult.Pass(1);

            output = null;
            return TypeAdapterResult.Fail("Expected string, got nothing", 1);
        }
    }
    
    public class EnumAdapter<T> : BaseTypeAdapter<T> where T : struct, Enum
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream stream, TypeAdapterRegistry registry, out T output)
        {
            var valid = string.Join(", ", Enum.GetNames(typeof(T)));

            if (stream.TryConsume(out var token) && Enum.TryParse(token, ignoreCase: true, out output))
            {
                if (Enum.IsDefined(typeof(T), output))
                {
                    return TypeAdapterResult.Pass(1);
                }

                return TypeAdapterResult.Fail($"\"{token}\" is not a valid {typeof(T).GetFriendlyName()}. Valid values: {valid}", 1);
            }

            output = default;
            return TypeAdapterResult.Fail($"Expected {typeof(T).GetFriendlyName()}, got \"{token ?? "nothing"}\". Valid values: {valid}", 1);
        }
    }
}