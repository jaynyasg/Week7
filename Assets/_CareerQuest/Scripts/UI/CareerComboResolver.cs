using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    /// <summary>
    /// Primary combo selection for the reveal (U7, R15). Combos are session-only
    /// ceremony flavor and add NO score (KTD8) — this resolver only DECIDES which
    /// eligible combo leads the ceremony as the hybrid identity; eligibility
    /// itself is the pure pair check that already lives in
    /// <see cref="RewardEventLog.EligibleComboIds"/>.
    ///
    /// Selection order (design doc: Starter Combo Cards primary-selection rule):
    ///   1. strongest traits — the combo whose family blend best matches the
    ///      player's strongest Career DNA traits (highest trait-fit score),
    ///   2. most recently completed station — the combo whose later required
    ///      station appears latest in completion order,
    ///   3. authored priority — the stable handcrafted tiebreak.
    /// Every comparison is deterministic, so host and 2P clients pick the same
    /// primary from the same completion facts.
    ///
    /// Pure/static: no scene types, no session mutation. The reveal layer passes
    /// derived inputs in; tests drive it with plain fixtures.
    /// </summary>
    public static class CareerComboResolver
    {
        /// <summary>
        /// The primary combo for the ceremony, or null when no station pair is
        /// eligible. Inputs: trait totals (Career DNA), completed station ids in
        /// first-completion order (recency tiebreak). Eligibility is recomputed
        /// here from the completed set so callers cannot pass a stale list.
        /// </summary>
        public static CareerComboDefinition SelectPrimary(
            IReadOnlyDictionary<string, int> traitTotals,
            IReadOnlyList<string> completedActivityIdsInOrder)
        {
            return RankEligible(traitTotals, completedActivityIdsInOrder).FirstOrDefault();
        }

        /// <summary>
        /// All eligible combos in primary-selection order (strongest traits →
        /// most recent station → authored priority). The first element is the
        /// ceremony primary; the rest are the "also unlocked" list for the
        /// gallery/passport/debug overlay.
        /// </summary>
        public static IReadOnlyList<CareerComboDefinition> RankEligible(
            IReadOnlyDictionary<string, int> traitTotals,
            IReadOnlyList<string> completedActivityIdsInOrder)
        {
            var completedOrder = completedActivityIdsInOrder ?? new List<string>();
            var eligibleIds = new HashSet<string>(RewardEventLog.EligibleComboIds(completedOrder));
            if (eligibleIds.Count == 0)
            {
                return new List<CareerComboDefinition>();
            }

            // Recency index: position of an id in first-completion order. A
            // combo's recency is its LATER required station — the completion
            // that actually unlocked the pair.
            var recencyByStation = new Dictionary<string, int>();
            for (var index = 0; index < completedOrder.Count; index++)
            {
                if (!string.IsNullOrWhiteSpace(completedOrder[index]) && !recencyByStation.ContainsKey(completedOrder[index]))
                {
                    recencyByStation[completedOrder[index]] = index;
                }
            }

            return CareerComboConfig.All
                .Where(combo => eligibleIds.Contains(combo.Id))
                .OrderByDescending(combo => TraitFit(combo, traitTotals))
                .ThenByDescending(combo => UnlockRecency(combo, recencyByStation))
                .ThenBy(combo => combo.AuthoredPriority)
                .ToList();
        }

        /// <summary>
        /// Trait-fit score: how strongly the player's Career DNA leans into the
        /// combo's two families. Each family contributes its signature trait
        /// weights (summed from the careers whose primary family it is); the
        /// score multiplies those weights by the player's trait totals. A combo
        /// whose families align with the player's strongest traits scores
        /// highest. Public so RevealSynthesis/tests can explain the choice.
        /// </summary>
        public static int TraitFit(CareerComboDefinition combo, IReadOnlyDictionary<string, int> traitTotals)
        {
            if (combo == null || traitTotals == null)
            {
                return 0;
            }

            var score = 0;
            foreach (var family in combo.FamilyBlend)
            {
                foreach (var pair in FamilySignatureWeights(family))
                {
                    if (traitTotals.TryGetValue(pair.Key, out var total))
                    {
                        score += total * pair.Value;
                    }
                }
            }

            return score;
        }

        /// <summary>
        /// The unlock recency of a combo: the later first-completion index of
        /// its two required stations. Combos whose pair completed more recently
        /// rank ahead on the recency tiebreak. -1 when a required station is not
        /// in the order list (should not happen for an eligible combo).
        /// </summary>
        private static int UnlockRecency(CareerComboDefinition combo, IReadOnlyDictionary<string, int> recencyByStation)
        {
            var latest = -1;
            foreach (var stationId in combo.RequiredStationIds)
            {
                if (recencyByStation.TryGetValue(stationId, out var index))
                {
                    latest = index > latest ? index : latest;
                }
            }

            return latest;
        }

        // Signature trait weights per family, derived once from CareerConfig so
        // the combo trait-fit knows what each family "is about". Sums the trait
        // weights of every career whose PRIMARY family is this one. (RevealSynthesis
        // ranks the ceremony family from the top shown paths instead; this stays
        // the combo-fit signal so a combo's two families weigh the player's DNA.)
        private static readonly Dictionary<string, Dictionary<string, int>> SignatureCache = new();

        public static IReadOnlyDictionary<string, int> FamilySignatureWeights(string family)
        {
            if (SignatureCache.TryGetValue(family, out var cached))
            {
                return cached;
            }

            var weights = new Dictionary<string, int>();
            foreach (var career in CareerConfig.Careers.Where(candidate => candidate.PrimaryFamily == family))
            {
                foreach (var weight in career.TraitWeights)
                {
                    weights[weight.Key] = (weights.TryGetValue(weight.Key, out var existing) ? existing : 0) + weight.Value;
                }
            }

            SignatureCache[family] = weights;
            return weights;
        }
    }
}
