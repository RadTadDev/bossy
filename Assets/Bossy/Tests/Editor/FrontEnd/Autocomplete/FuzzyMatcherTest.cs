using Bossy.Frontend.Autocomplete;
using NUnit.Framework;

namespace Bossy.Tests.FrontEnd.Autocomplete
{
    /// <summary>
    /// Tests the <see cref="FuzzyMatcher"/> class.
    /// </summary>
    internal class FuzzyMatcherTest
    {
        [Test]
        public void Rank_EmptyQuery_KeepsAllInOrder()
        {
            var ranked = FuzzyMatcher.Rank("", new[] { "b", "a", "c" }, s => s);

            Assert.That(ranked, Is.EqualTo(new[] { "b", "a", "c" }));
        }

        [Test]
        public void Rank_OrdersByTier()
        {
            var candidates = new[] { "fuzy", "has-fuz", "hasfuzz", "fuzzy-thing", "fuzz" };

            var ranked = FuzzyMatcher.Rank("fuzz", candidates, s => s);

            // exact, prefix, substring, fuzzy; "has-fuz" does not contain "fuzz" and is too far to be fuzzy
            Assert.That(ranked, Is.EqualTo(new[] { "fuzz", "fuzzy-thing", "hasfuzz", "fuzy" }));
        }

        [Test]
        public void Rank_IsCaseInsensitive()
        {
            Assert.That(FuzzyMatcher.Rank("LOAD", new[] { "load" }, s => s), Is.EqualTo(new[] { "load" }));
        }

        [Test]
        public void Rank_MatchesSegmentStarts()
        {
            Assert.That(FuzzyMatcher.Rank("ul", new[] { "unity-log" }, s => s), Is.EqualTo(new[] { "unity-log" }));
            Assert.That(FuzzyMatcher.Rank("sa", new[] { "SetActive" }, s => s), Is.EqualTo(new[] { "SetActive" }));
            Assert.That(FuzzyMatcher.Rank("log", new[] { "unity-log", "catalog" }, s => s), Is.EqualTo(new[] { "unity-log", "catalog" }));
        }

        [Test]
        public void Rank_SegmentStartRanksAboveSubstring()
        {
            var ranked = FuzzyMatcher.Rank("log", new[] { "catalog", "unity-log" }, s => s);

            Assert.That(ranked, Is.EqualTo(new[] { "unity-log", "catalog" }));
        }

        [Test]
        public void Rank_TiesKeepOriginalOrder()
        {
            var ranked = FuzzyMatcher.Rank("le", new[] { "level2", "level1", "lever" }, s => s);

            Assert.That(ranked, Is.EqualTo(new[] { "level2", "level1", "lever" }));
        }

        [Test]
        public void Rank_LeavesOutWeakFuzzyMatches()
        {
            Assert.That(FuzzyMatcher.Rank("get", new[] { "help" }, s => s), Is.Empty);
            Assert.That(FuzzyMatcher.Rank("laod", new[] { "load" }, s => s), Is.EqualTo(new[] { "load" }));
        }
    }
}
