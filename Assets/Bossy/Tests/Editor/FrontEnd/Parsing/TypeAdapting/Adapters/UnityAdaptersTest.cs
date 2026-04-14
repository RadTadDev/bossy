using NUnit.Framework;
using Bossy.FrontEnd.Parsing;
using UnityEngine;

namespace Bossy.Tests.Editor.FrontEnd.Parsing.TypeAdapting.Adapters
{
    /// <summary>
    /// Tests the Unity adapters.
    /// </summary>
    public class UnityAdaptersTest
    {
        // ── Vector2 ─────────────────────────────────────────────────────────

        [Test] public void Vector2_Valid()
        {
            var cursor = new TokenStream("1.5 2.5");
            var adapter = new Vector2Adapter();
            var result = adapter.TryConvert(cursor, out var output);
            var typed = (Vector2)output;
            Assert.That(result.Success, Is.True);
            Assert.That(typed.x, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(typed.y, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test] public void Vector2_Negative()
        {
            var cursor = new TokenStream("-1.0 -2.0");
            var adapter = new Vector2Adapter();
            var result = adapter.TryConvert(cursor, out var output);
            var typed = (Vector2)output;
            Assert.That(result.Success, Is.True);
            Assert.That(typed.x, Is.EqualTo(-1.0f).Within(0.0001f));
            Assert.That(typed.y, Is.EqualTo(-2.0f).Within(0.0001f));
        }

        [Test] public void Vector2_NotEnoughTokens_Fails()
        {
            var cursor = new TokenStream("1.0");
            var adapter = new Vector2Adapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Vector2_BadFloat_Fails()
        {
            var cursor = new TokenStream("1.0 abc");
            var adapter = new Vector2Adapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Vector2_OnlyConsumesTwoTokens()
        {
            var cursor = new TokenStream("1.0 2.0 3.0");
            var adapter = new Vector2Adapter();
            adapter.TryConvert(cursor, out _);
            Assert.That(cursor.TryPeek(out var next), Is.True);
            Assert.That(next, Is.EqualTo("3.0"));
        }

        // ── Vector3 ─────────────────────────────────────────────────────────

        [Test] public void Vector3_Valid()
        {
            var cursor = new TokenStream("1.0 2.0 3.0");
            var adapter = new Vector3Adapter();
            var result = adapter.TryConvert(cursor, out var output);
            var typed = (Vector3)output;
            Assert.That(result.Success, Is.True);
            Assert.That(typed.x, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(typed.y, Is.EqualTo(2.0f).Within(0.0001f));
            Assert.That(typed.z, Is.EqualTo(3.0f).Within(0.0001f));
        }

        [Test] public void Vector3_NotEnoughTokens_Fails()
        {
            var cursor = new TokenStream("1.0 2.0");
            var adapter = new Vector3Adapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Vector3_BadFloat_Fails()
        {
            var cursor = new TokenStream("1.0 abc 3.0");
            var adapter = new Vector3Adapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Vector3_OnlyConsumesThreeTokens()
        {
            var cursor = new TokenStream("1.0 2.0 3.0 4.0");
            var adapter = new Vector3Adapter();
            adapter.TryConvert(cursor, out _);
            Assert.That(cursor.TryPeek(out var next), Is.True);
            Assert.That(next, Is.EqualTo("4.0"));
        }

        // ── Vector4 ─────────────────────────────────────────────────────────

        [Test] public void Vector4_Valid()
        {
            var cursor = new TokenStream("1.0 2.0 3.0 4.0");
            var adapter = new Vector4Adapter();
            var result = adapter.TryConvert(cursor, out var output);
            var typed = (Vector4)output;
            Assert.That(result.Success, Is.True);
            Assert.That(typed.x, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(typed.y, Is.EqualTo(2.0f).Within(0.0001f));
            Assert.That(typed.z, Is.EqualTo(3.0f).Within(0.0001f));
            Assert.That(typed.w, Is.EqualTo(4.0f).Within(0.0001f));
        }

        [Test] public void Vector4_NotEnoughTokens_Fails()
        {
            var cursor = new TokenStream("1.0 2.0 3.0");
            var adapter = new Vector4Adapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Vector4_BadFloat_Fails()
        {
            var cursor = new TokenStream("1.0 2.0 abc 4.0");
            var adapter = new Vector4Adapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Vector4_OnlyConsumesFourTokens()
        {
            var cursor = new TokenStream("1.0 2.0 3.0 4.0 5.0");
            var adapter = new Vector4Adapter();
            adapter.TryConvert(cursor, out _);
            Assert.That(cursor.TryPeek(out var next), Is.True);
            Assert.That(next, Is.EqualTo("5.0"));
        }

        // ── Vector2Int ──────────────────────────────────────────────────────

        [Test] public void Vector2Int_Valid()
        {
            var cursor = new TokenStream("3 7");
            var adapter = new Vector2IntAdapter();
            var result = adapter.TryConvert(cursor, out var output);
            var typed = (Vector2Int)output;
            Assert.That(result.Success, Is.True);
            Assert.That(typed.x, Is.EqualTo(3));
            Assert.That(typed.y, Is.EqualTo(7));
        }

        [Test] public void Vector2Int_Negative()
        {
            var cursor = new TokenStream("-3 -7");
            var adapter = new Vector2IntAdapter();
            var result = adapter.TryConvert(cursor, out var output);
            var typed = (Vector2Int)output;
            Assert.That(result.Success, Is.True);
            Assert.That(typed.x, Is.EqualTo(-3));
            Assert.That(typed.y, Is.EqualTo(-7));
        }

        [Test] public void Vector2Int_Float_Fails()
        {
            var cursor = new TokenStream("1.5 2.5");
            var adapter = new Vector2IntAdapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Vector2Int_NotEnoughTokens_Fails()
        {
            var cursor = new TokenStream("1");
            var adapter = new Vector2IntAdapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        // ── Vector3Int ──────────────────────────────────────────────────────

        [Test] public void Vector3Int_Valid()
        {
            var cursor = new TokenStream("1 2 3");
            var adapter = new Vector3IntAdapter();
            var result = adapter.TryConvert(cursor, out var output);
            var typed = (Vector3Int)output;
            Assert.That(result.Success, Is.True);
            Assert.That(typed.x, Is.EqualTo(1));
            Assert.That(typed.y, Is.EqualTo(2));
            Assert.That(typed.z, Is.EqualTo(3));
        }

        [Test] public void Vector3Int_Float_Fails()
        {
            var cursor = new TokenStream("1.0 2.0 3.0");
            var adapter = new Vector3IntAdapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Vector3Int_NotEnoughTokens_Fails()
        {
            var cursor = new TokenStream("1 2");
            var adapter = new Vector3IntAdapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Vector3Int_OnlyConsumesThreeTokens()
        {
            var cursor = new TokenStream("1 2 3 4");
            var adapter = new Vector3IntAdapter();
            adapter.TryConvert(cursor, out _);
            Assert.That(cursor.TryPeek(out var next), Is.True);
            Assert.That(next, Is.EqualTo("4"));
        }

        // ── Color ───────────────────────────────────────────────────────────

        [Test] public void Color_HexRGB()
        {
            var cursor = new TokenStream("#FF0000");
            var adapter = new ColorAdapter();
            var result = adapter.TryConvert(cursor, out var output);
            var typed = (Color)output;
            Assert.That(result.Success, Is.True);
            Assert.That(typed.r, Is.EqualTo(1.0f).Within(0.01f));
            Assert.That(typed.g, Is.EqualTo(0.0f).Within(0.01f));
            Assert.That(typed.b, Is.EqualTo(0.0f).Within(0.01f));
        }

        [Test] public void Color_HexRGBA()
        {
            var cursor = new TokenStream("#FF0000FF");
            var adapter = new ColorAdapter();
            var result = adapter.TryConvert(cursor, out var output);
            var typed = (Color)output;
            Assert.That(result.Success, Is.True);
            Assert.That(typed.a, Is.EqualTo(1.0f).Within(0.01f));
        }

        [Test] public void Color_BadHex_Fails()
        {
            var cursor = new TokenStream("#ZZZZZZ");
            var adapter = new ColorAdapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Color_ThreeComponents()
        {
            var cursor = new TokenStream("1.0 0.5 0.0");
            var adapter = new ColorAdapter();
            var result = adapter.TryConvert(cursor, out var output);
            var typed = (Color)output;
            Assert.That(result.Success, Is.True);
            Assert.That(typed.r, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(typed.g, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(typed.b, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(typed.a, Is.EqualTo(1.0f).Within(0.0001f));
        }

        [Test] public void Color_FourComponents()
        {
            var cursor = new TokenStream("1.0 0.5 0.0 0.75");
            var adapter = new ColorAdapter();
            var result = adapter.TryConvert(cursor, out var output);
            var typed = (Color)output;
            Assert.That(result.Success, Is.True);
            Assert.That(typed.a, Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test] public void Color_NotEnoughComponents_Fails()
        {
            var cursor = new TokenStream("1.0 0.5");
            var adapter = new ColorAdapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Color_BadFloat_Fails()
        {
            var cursor = new TokenStream("1.0 abc 0.0");
            var adapter = new ColorAdapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Color_FourComponents_OnlyConsumesFourTokens()
        {
            var cursor = new TokenStream("1.0 0.5 0.0 0.75 99");
            var adapter = new ColorAdapter();
            adapter.TryConvert(cursor, out _);
            Assert.That(cursor.TryPeek(out var next), Is.True);
            Assert.That(next, Is.EqualTo("99"));
        }

        [Test] public void Color_Empty_Fails()
        {
            var cursor = new TokenStream("");
            var adapter = new ColorAdapter();
            var result = adapter.TryConvert(cursor, out _);
            Assert.That(result.Success, Is.False);
        }
    }
}