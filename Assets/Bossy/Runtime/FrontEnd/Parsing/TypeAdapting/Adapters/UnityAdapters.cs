using System.Collections.Generic;
using UnityEngine;

namespace Bossy.FrontEnd.Parsing
{
    public class Vector2Adapter : BaseTypeAdapter<Vector2>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out Vector2 output)
        {
            if (cursor.TryConsume(2, out var tokens)
                && float.TryParse(tokens[0], out var x)
                && float.TryParse(tokens[1], out var y))
            {
                output = new Vector2(x, y);
                return TypeAdapterResult.Pass();
            }

            output = default;
            return TypeAdapterResult.Fail($"Expected Vector2 as \"x y\", got \"{string.Join(" ", tokens ?? new List<string>())}\"");
        }
    }

    public class Vector3Adapter : BaseTypeAdapter<Vector3>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out Vector3 output)
        {
            if (cursor.TryConsume(3, out var tokens)
                && float.TryParse(tokens[0], out var x)
                && float.TryParse(tokens[1], out var y)
                && float.TryParse(tokens[2], out var z))
            {
                output = new Vector3(x, y, z);
                return TypeAdapterResult.Pass();
            }

            output = default;
            return TypeAdapterResult.Fail($"Expected Vector3 as \"x y z\", got \"{string.Join(" ", tokens ?? new List<string>())}\"");
        }
    }

    public class Vector4Adapter : BaseTypeAdapter<Vector4>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out Vector4 output)
        {
            if (cursor.TryConsume(4, out var tokens)
                && float.TryParse(tokens[0], out var x)
                && float.TryParse(tokens[1], out var y)
                && float.TryParse(tokens[2], out var z)
                && float.TryParse(tokens[3], out var w))
            {
                output = new Vector4(x, y, z, w);
                return TypeAdapterResult.Pass();
            }

            output = default;
            return TypeAdapterResult.Fail($"Expected Vector4 as \"x y z w\", got \"{string.Join(" ", tokens ?? new List<string>())}\"");
        }
    }

    public class Vector2IntAdapter : BaseTypeAdapter<Vector2Int>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out Vector2Int output)
        {
            if (cursor.TryConsume(2, out var tokens)
                && int.TryParse(tokens[0], out var x)
                && int.TryParse(tokens[1], out var y))
            {
                output = new Vector2Int(x, y);
                return TypeAdapterResult.Pass();
            }

            output = default;
            return TypeAdapterResult.Fail($"Expected Vector2Int as \"x y\", got \"{string.Join(" ", tokens ?? new List<string>())}\"");
        }
    }

    public class Vector3IntAdapter : BaseTypeAdapter<Vector3Int>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out Vector3Int output)
        {
            if (cursor.TryConsume(3, out var tokens)
                && int.TryParse(tokens[0], out var x)
                && int.TryParse(tokens[1], out var y)
                && int.TryParse(tokens[2], out var z))
            {
                output = new Vector3Int(x, y, z);
                return TypeAdapterResult.Pass();
            }

            output = default;
            return TypeAdapterResult.Fail($"Expected Vector3Int as \"x y z\", got \"{string.Join(" ", tokens ?? new List<string>())}\"");
        }
    }

    public class ColorAdapter : BaseTypeAdapter<Color>
    {
        protected override TypeAdapterResult TryConvertToType(TokenStream cursor, out Color output)
        {
            if (!cursor.TryPeek(out var first))
            {
                output = default;
                return TypeAdapterResult.Fail("Expected Color, got nothing");
            }

            if (first.StartsWith("#"))
            {
                cursor.TryConsume(out _);
                if (ColorUtility.TryParseHtmlString(first, out output))
                    return TypeAdapterResult.Pass();

                return TypeAdapterResult.Fail($"Expected hex color e.g. \"#FF0000\", got \"{first}\"");
            }

            if (cursor.TryConsume(4, out var tokens)
                && float.TryParse(tokens[0], out var r)
                && float.TryParse(tokens[1], out var g)
                && float.TryParse(tokens[2], out var b)
                && float.TryParse(tokens[3], out var a))
            {
                output = new Color(r, g, b, a);
                return TypeAdapterResult.Pass();
            }

            if (cursor.TryConsume(3, out tokens)
                && float.TryParse(tokens[0], out r)
                && float.TryParse(tokens[1], out g)
                && float.TryParse(tokens[2], out b))
            {
                output = new Color(r, g, b);
                return TypeAdapterResult.Pass();
            }

            output = default;
            return TypeAdapterResult.Fail($"Expected Color as \"r g b\" or \"r g b a\" or \"#RRGGBB\", got \"{first}\"");
        }
    }
}