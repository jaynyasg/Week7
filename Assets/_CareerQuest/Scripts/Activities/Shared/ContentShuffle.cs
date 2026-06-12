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
