using NUnit.Framework;
using Bossy.Frontend.Parsing;

namespace Bossy.Tests.FrontEnd.Parsing
{
    /// <summary>
    /// Tests the primitive type adatpers.
    /// </summary>
    public class PrimitiveAdapterTest
    {
        // ── Bool ────────────────────────────────────────────────────────────

        [Test] public void Bool_BareFlag_ReturnsTrue()
        {
            var cursor = new TokenStream("");
            var adapter = new BoolAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.True);
        }

        [Test] public void Bool_ExplicitTrue()
        {
            var cursor = new TokenStream("true");
            var adapter = new BoolAdapter();
            adapter.TryConvert(cursor, null, out var output);
            Assert.That(output, Is.True);
        }

        [Test] public void Bool_ExplicitFalse()
        {
            var cursor = new TokenStream("false");
            var adapter = new BoolAdapter();
            adapter.TryConvert(cursor, null, out var output);
            Assert.That(output, Is.False);
        }

        [Test] public void Bool_NextTokenIsFlag_ReturnsTrueWithoutConsuming()
        {
            var cursor = new TokenStream("--other");
            var adapter = new BoolAdapter();
            adapter.TryConvert(cursor, null, out var output);
            Assert.That(output, Is.True);
            Assert.That(cursor.TryPeek(out var remaining), Is.True);
            Assert.That(remaining, Is.EqualTo("--other"));
        }

        [Test] public void Bool_NonBoolToken_ReturnsTrueWithoutConsuming()
        {
            var cursor = new TokenStream("hello");
            var adapter = new BoolAdapter();
            adapter.TryConvert(cursor, null, out var output);
            Assert.That(output, Is.True);
            Assert.That(cursor.TryPeek(out var remaining), Is.True);
            Assert.That(remaining, Is.EqualTo("hello"));
        }

        // ── Byte ────────────────────────────────────────────────────────────

        [Test] public void Byte_Valid()
        {
            var cursor = new TokenStream("255");
            var adapter = new ByteAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo((byte)255));
        }

        [Test] public void Byte_Overflow_Fails()
        {
            var cursor = new TokenStream("256");
            var adapter = new ByteAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Byte_Negative_Fails()
        {
            var cursor = new TokenStream("-1");
            var adapter = new ByteAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        // ── SByte ───────────────────────────────────────────────────────────

        [Test] public void SByte_Valid()
        {
            var cursor = new TokenStream("-128");
            var adapter = new SByteAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo((sbyte)-128));
        }

        [Test] public void SByte_Overflow_Fails()
        {
            var cursor = new TokenStream("128");
            var adapter = new SByteAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        // ── Short ───────────────────────────────────────────────────────────

        [Test] public void Short_Valid()
        {
            var cursor = new TokenStream("-32768");
            var adapter = new ShortAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo((short)-32768));
        }

        [Test] public void Short_Overflow_Fails()
        {
            var cursor = new TokenStream("32768");
            var adapter = new ShortAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        // ── UShort ──────────────────────────────────────────────────────────

        [Test] public void UShort_Valid()
        {
            var cursor = new TokenStream("65535");
            var adapter = new UShortAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo((ushort)65535));
        }

        [Test] public void UShort_Negative_Fails()
        {
            var cursor = new TokenStream("-1");
            var adapter = new UShortAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        // ── Int ─────────────────────────────────────────────────────────────

        [Test] public void Int_Valid()
        {
            var cursor = new TokenStream("42");
            var adapter = new IntAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo(42));
        }

        [Test] public void Int_Negative()
        {
            var cursor = new TokenStream("-100");
            var adapter = new IntAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo(-100));
        }

        [Test] public void Int_NotANumber_Fails()
        {
            var cursor = new TokenStream("abc");
            var adapter = new IntAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("abc"));
        }

        [Test] public void Int_Empty_Fails()
        {
            var cursor = new TokenStream("");
            var adapter = new IntAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("nothing"));
        }

        [Test] public void Int_OnlyConsumesOneToken()
        {
            var cursor = new TokenStream("1 2 3");
            var adapter = new IntAdapter();
            adapter.TryConvert(cursor, null, out _);
            Assert.That(cursor.TryPeek(out var next), Is.True);
            Assert.That(next, Is.EqualTo("2"));
        }

        // ── UInt ────────────────────────────────────────────────────────────

        [Test] public void UInt_Valid()
        {
            var cursor = new TokenStream("4294967295");
            var adapter = new UIntAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo(uint.MaxValue));
        }

        [Test] public void UInt_Negative_Fails()
        {
            var cursor = new TokenStream("-1");
            var adapter = new UIntAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        // ── Long ────────────────────────────────────────────────────────────

        [Test] public void Long_Valid()
        {
            var cursor = new TokenStream("-9223372036854775808");
            var adapter = new LongAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo(long.MinValue));
        }

        [Test] public void Long_NotANumber_Fails()
        {
            var cursor = new TokenStream("abc");
            var adapter = new LongAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        // ── ULong ───────────────────────────────────────────────────────────

        [Test] public void ULong_Valid()
        {
            var cursor = new TokenStream("18446744073709551615");
            var adapter = new ULongAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo(ulong.MaxValue));
        }

        [Test] public void ULong_Negative_Fails()
        {
            var cursor = new TokenStream("-1");
            var adapter = new ULongAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        // ── Float ───────────────────────────────────────────────────────────

        [Test] public void Float_Valid()
        {
            var cursor = new TokenStream("3.14");
            var adapter = new FloatAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo(3.14f).Within(0.0001f));
        }

        [Test] public void Float_Negative()
        {
            var cursor = new TokenStream("-1.5");
            var adapter = new FloatAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo(-1.5f).Within(0.0001f));
        }

        [Test] public void Float_NotANumber_Fails()
        {
            var cursor = new TokenStream("abc");
            var adapter = new FloatAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        // ── Double ──────────────────────────────────────────────────────────

        [Test] public void Double_Valid()
        {
            var cursor = new TokenStream("3.141592653589793");
            var adapter = new DoubleAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo(3.141592653589793).Within(0.000000000000001));
        }

        [Test] public void Double_NotANumber_Fails()
        {
            var cursor = new TokenStream("abc");
            var adapter = new DoubleAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        // ── Char ────────────────────────────────────────────────────────────

        [Test] public void Char_Valid()
        {
            var cursor = new TokenStream("A");
            var adapter = new CharAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo('A'));
        }

        [Test] public void Char_MultipleChars_Fails()
        {
            var cursor = new TokenStream("AB");
            var adapter = new CharAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void Char_Empty_Fails()
        {
            var cursor = new TokenStream("");
            var adapter = new CharAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        // ── String ──────────────────────────────────────────────────────────

        [Test] public void String_Valid()
        {
            var cursor = new TokenStream("hello");
            var adapter = new StringAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo("hello"));
        }

        [Test] public void String_QuotedPhrase()
        {
            var cursor = new TokenStream("\"hello world\"");
            var adapter = new StringAdapter();
            var result = adapter.TryConvert(cursor, null, out var output);
            Assert.That(result.Success, Is.True);
            Assert.That(output, Is.EqualTo("hello world"));
        }

        [Test] public void String_Empty_Fails()
        {
            var cursor = new TokenStream("");
            var adapter = new StringAdapter();
            var result = adapter.TryConvert(cursor, null, out _);
            Assert.That(result.Success, Is.False);
        }

        [Test] public void String_OnlyConsumesOneToken()
        {
            var cursor = new TokenStream("foo bar");
            var adapter = new StringAdapter();
            adapter.TryConvert(cursor, null, out _);
            Assert.That(cursor.TryPeek(out var next), Is.True);
            Assert.That(next, Is.EqualTo("bar"));
        }
        
        // ── Enum ──────────────────────────────────────────────────────────
        private enum Animal
        {
            Dog = 10,
            Cat = 20
        }
        
        [Test] public void Enum_Valid()
        {
            var cursor = new TokenStream("Dog");
            var adapter = new EnumAdapter<Animal>();
            adapter.TryConvert(cursor, null, out var output);
            Assert.That(output, Is.EqualTo(Animal.Dog));
            Assert.That(cursor.TryPeek(out _), Is.False);
        }
        
        [Test] public void Enum_OtherCase_Valid()
        {
            var cursor = new TokenStream("dog");
            var adapter = new EnumAdapter<Animal>();
            adapter.TryConvert(cursor, null, out var output);
            Assert.That(output, Is.EqualTo(Animal.Dog));
            Assert.That(cursor.TryPeek(out _), Is.False);
        }
        
        [Test] public void Enum_Numeric_Valid()
        {
            var cursor = new TokenStream("10");
            var adapter = new EnumAdapter<Animal>();
            adapter.TryConvert(cursor, null, out var output);
            Assert.That(output, Is.EqualTo(Animal.Dog));
            Assert.That(cursor.TryPeek(out _), Is.False);
        }
        
        [Test] public void Enum_Numeric_Invalid()
        {
            var cursor = new TokenStream("5");
            var adapter = new EnumAdapter<Animal>();
            var result = adapter.TryConvert(cursor, null, out _);
            
            Assert.That(result.Success, Is.False);
            Assert.That(cursor.TryPeek(out _), Is.False);
        }
        
        [Test] public void Enum_Fails()
        {
            var cursor = new TokenStream("Bad");
            var adapter = new EnumAdapter<Animal>();
            var result = adapter.TryConvert(cursor, null, out _);
            
            Assert.That(result.Success, Is.False);
            Assert.That(cursor.TryPeek(out _), Is.False);
        }
    }
}