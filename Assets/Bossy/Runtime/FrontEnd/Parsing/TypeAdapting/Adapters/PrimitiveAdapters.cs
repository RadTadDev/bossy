namespace Bossy.FrontEnd.Parsing
{
    public class BoolAdapter : BaseTypeAdapter<bool>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out bool output)
        {
            // Implicit 'true' on no token
            if (!cursor.TryPeek(out var token))
            {
                output = true;
                return TypeAdapterResult.Pass();
            }

            if (bool.TryParse(token, out output))
            {
                cursor.TryConsume(out _);
                return TypeAdapterResult.Pass();
            }

            output = true;
            return TypeAdapterResult.Pass();
        }
    }

    public class ByteAdapter : BaseTypeAdapter<byte>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out byte output)
        {
            if (cursor.TryConsume(out var token) && byte.TryParse(token, out output))
                return TypeAdapterResult.Pass();

            output = 0;
            return TypeAdapterResult.Fail($"Expected byte, got \"{token ?? "nothing"}\"");
        }
    }

    public class SByteAdapter : BaseTypeAdapter<sbyte>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out sbyte output)
        {
            if (cursor.TryConsume(out var token) && sbyte.TryParse(token, out output))
                return TypeAdapterResult.Pass();

            output = 0;
            return TypeAdapterResult.Fail($"Expected sbyte, got \"{token ?? "nothing"}\"");
        }
    }

    public class ShortAdapter : BaseTypeAdapter<short>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out short output)
        {
            if (cursor.TryConsume(out var token) && short.TryParse(token, out output))
                return TypeAdapterResult.Pass();

            output = 0;
            return TypeAdapterResult.Fail($"Expected short, got \"{token ?? "nothing"}\"");
        }
    }

    public class UShortAdapter : BaseTypeAdapter<ushort>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out ushort output)
        {
            if (cursor.TryConsume(out var token) && ushort.TryParse(token, out output))
                return TypeAdapterResult.Pass();

            output = 0;
            return TypeAdapterResult.Fail($"Expected ushort, got \"{token ?? "nothing"}\"");
        }
    }

    public class IntAdapter : BaseTypeAdapter<int>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out int output)
        {
            if (cursor.TryConsume(out var token) && int.TryParse(token, out output))
                return TypeAdapterResult.Pass();

            output = 0;
            return TypeAdapterResult.Fail($"Expected int, got \"{token ?? "nothing"}\"");
        }
    }

    public class UIntAdapter : BaseTypeAdapter<uint>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out uint output)
        {
            if (cursor.TryConsume(out var token) && uint.TryParse(token, out output))
                return TypeAdapterResult.Pass();

            output = 0;
            return TypeAdapterResult.Fail($"Expected uint, got \"{token ?? "nothing"}\"");
        }
    }

    public class LongAdapter : BaseTypeAdapter<long>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out long output)
        {
            if (cursor.TryConsume(out var token) && long.TryParse(token, out output))
                return TypeAdapterResult.Pass();

            output = 0;
            return TypeAdapterResult.Fail($"Expected long, got \"{token ?? "nothing"}\"");
        }
    }

    public class ULongAdapter : BaseTypeAdapter<ulong>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out ulong output)
        {
            if (cursor.TryConsume(out var token) && ulong.TryParse(token, out output))
                return TypeAdapterResult.Pass();

            output = 0;
            return TypeAdapterResult.Fail($"Expected ulong, got \"{token ?? "nothing"}\"");
        }
    }

    public class FloatAdapter : BaseTypeAdapter<float>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out float output)
        {
            if (cursor.TryConsume(out var token) && float.TryParse(token, out output))
                return TypeAdapterResult.Pass();

            output = 0;
            return TypeAdapterResult.Fail($"Expected float, got \"{token ?? "nothing"}\"");
        }
    }

    public class DoubleAdapter : BaseTypeAdapter<double>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out double output)
        {
            if (cursor.TryConsume(out var token) && double.TryParse(token, out output))
                return TypeAdapterResult.Pass();

            output = 0;
            return TypeAdapterResult.Fail($"Expected double, got \"{token ?? "nothing"}\"");
        }
    }

    public class CharAdapter : BaseTypeAdapter<char>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out char output)
        {
            if (cursor.TryConsume(out var token) && token.Length == 1)
            {
                output = token[0];
                return TypeAdapterResult.Pass();
            }

            output = '\0';
            return TypeAdapterResult.Fail($"Expected single character, got \"{token ?? "nothing"}\"");
        }
    }

    public class StringAdapter : BaseTypeAdapter<string>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out string output)
        {
            if (cursor.TryConsume(out output))
                return TypeAdapterResult.Pass();

            output = null;
            return TypeAdapterResult.Fail("Expected string, got nothing");
        }
    }
}