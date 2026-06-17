using System;

namespace CareerQuest
{
    /// <summary>
    /// P13 host-seeded content shuffle. The HOST owns the seed (a NetworkVariable
    /// on the room network state, reseeded per attempt); every client derives the
    /// identical presentation order from the same seed via <see cref="DeriveOrder"/>
    /// — a pure function, so tests drive it directly. Solo rooms seed locally
    /// through the same <see cref="NextSeed"/> path.
    ///
    /// <see cref="NextSeed"/> guarantees two consecutive attempts present
    /// DIFFERENT orderings: candidates whose derived order matches the previous
    /// seed's order are skipped.
    /// </summary>
    public static class ContentShuffle
    {
        private static readonly Random SeedSource = new();

        /// <summary>
        /// Deterministic permutation of 0..count-1 (Fisher-Yates over
        /// System.Random(seed)). Same seed + count always yields the same order.
        /// </summary>
        public static int[] DeriveOrder(int seed, int count)
        {
            var order = new int[Math.Max(0, count)];
            for (var i = 0; i < order.Length; i++)
            {
                order[i] = i;
            }

            var random = new Random(seed);
            for (var i = order.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            return order;
        }

        /// <summary>
        /// Like <see cref="DeriveOrder"/> but guarantees a derangement when
        /// <paramref name="count"/> is >= 2: no element keeps its home index
        /// (order[i] != i for every i). Use this where an item resting in its
        /// own "answer" position would give the puzzle away (e.g. a Design Build
        /// piece must never sit in the tray slot directly under its matching
        /// lot). A single item (or none) cannot be deranged, so it is returned
        /// unchanged.
        /// </summary>
        public static int[] DeriveDerangement(int seed, int count)
        {
            var order = DeriveOrder(seed, count);

            // Repair any fixed point by swapping it with its neighbour. Swapping
            // order[i]==i with order[j] (j != i) can never re-fix i (no other
            // slot holds value i) nor fix j (it receives i != j), so one pass
            // clears every fixed point without creating new ones.
            for (var i = 0; i < order.Length; i++)
            {
                if (order[i] == i)
                {
                    var j = i == order.Length - 1 ? 0 : i + 1;
                    (order[i], order[j]) = (order[j], order[i]);
                }
            }

            return order;
        }

        /// <summary>
        /// A fresh non-zero seed whose derived order differs from the previous
        /// seed's order (count permitting — a single-item order can never differ).
        /// </summary>
        public static int NextSeed(int previousSeed, int count)
        {
            var previous = DeriveOrder(previousSeed, count);
            var candidate = previousSeed;
            for (var attempt = 0; attempt < 64; attempt++)
            {
                candidate = SeedSource.Next(1, int.MaxValue);
                if (count <= 1 || !SameOrder(previous, DeriveOrder(candidate, count)))
                {
                    return candidate;
                }
            }

            // Pathologically unlucky draw streak: walk forward until the order
            // differs (with >= 2 items some nearby seed always differs).
            for (var step = 1; step < 256; step++)
            {
                var probe = candidate == int.MaxValue - step ? step : candidate + step;
                if (probe != 0 && (count <= 1 || !SameOrder(previous, DeriveOrder(probe, count))))
                {
                    return probe;
                }
            }

            return candidate == 0 ? 1 : candidate;
        }

        private static bool SameOrder(int[] left, int[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
