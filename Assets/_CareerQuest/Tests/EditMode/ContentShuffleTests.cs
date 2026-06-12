using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    /// <summary>
    /// P13 derivation contract: the synced seed yields an identical order on
    /// every client (pure function), and the per-attempt reseed always yields
    /// a DIFFERENT order than the previous attempt's.
    /// </summary>
    public class ContentShuffleTests
    {
        [Test]
        public void SameSeedYieldsIdenticalOrderEveryTime()
        {
            // The 2P contract: both clients derive the order from the same
            // host-synced seed — same input, same permutation, always.
            foreach (var seed in new[] { 1, 42, 1337, int.MaxValue - 1 })
            {
                Assert.That(ContentShuffle.DeriveOrder(seed, 4), Is.EqualTo(ContentShuffle.DeriveOrder(seed, 4)), $"seed {seed} (count 4)");
                Assert.That(ContentShuffle.DeriveOrder(seed, 3), Is.EqualTo(ContentShuffle.DeriveOrder(seed, 3)), $"seed {seed} (count 3)");
            }
        }

        [Test]
        public void DerivedOrderIsAlwaysACompletePermutation()
        {
            foreach (var seed in new[] { 0, 7, 99, 123456 })
            {
                foreach (var count in new[] { 1, 3, 4, 5 })
                {
                    var order = ContentShuffle.DeriveOrder(seed, count);
                    Assert.That(order.OrderBy(value => value), Is.EqualTo(Enumerable.Range(0, count)),
                        $"seed {seed}, count {count} must permute every index exactly once");
                }
            }
        }

        [Test]
        public void NextSeedProducesADifferentOrderingThanThePrevious()
        {
            // Consecutive attempts must present different content orderings.
            foreach (var count in new[] { 3, 4 })
            {
                var seed = 0;
                for (var attempt = 0; attempt < 10; attempt++)
                {
                    var next = ContentShuffle.NextSeed(seed, count);
                    Assert.That(next, Is.Not.Zero, "seeds are never zero (zero means 'unseeded')");
                    Assert.That(
                        ContentShuffle.DeriveOrder(next, count),
                        Is.Not.EqualTo(ContentShuffle.DeriveOrder(seed, count)),
                        $"attempt {attempt} (count {count}) must change the ordering");
                    seed = next;
                }
            }
        }

        [Test]
        public void NextSeedHandlesDegenerateSingleItemDomain()
        {
            // A one-item order can never differ — NextSeed must still terminate
            // with a fresh non-zero seed.
            Assert.That(ContentShuffle.NextSeed(5, 1), Is.Not.Zero);
            Assert.That(ContentShuffle.NextSeed(0, 0), Is.Not.Zero);
        }
    }
}
