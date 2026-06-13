using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    /// <summary>
    /// Reveal presentation style, chosen by unique-completion bucket (design
    /// doc: Reveal richness thresholds). One resolver drives all of them
    /// (KTD9) — these are content/copy/flourish tokens, never separate cinematic
    /// controllers. PreReveal is the &lt;3 teaser state below the reveal gate.
    /// </summary>
    public enum RevealStyle
    {
        PreReveal,        // 0-2 unique completions: teaser, not yet unlocked
        Simple,           // 3-4: superpower, top traits, top paths, family, accessories
        Rich,             // 5-7: stronger confidence, milestone accessory spotlight
        BigExplorer,      // 8-9: bigger flourish, broader explorer copy
        Completionist     // 10: reveal-only completion flourish
    }

    /// <summary>
    /// The shape <see cref="CareerRevealController"/> and the ceremony consume.
    /// Everything here is derived from the session read model — nothing feeds
    /// back into scoring (KTD8). Combo fields are null/empty when no station
    /// pair is eligible; the reveal still renders (empty-combo path).
    /// </summary>
    public sealed class RevealSynthesisResult
    {
        public int UniqueCompletions { get; }
        public bool IsRevealReady { get; }

        /// <summary>Bucketed presentation style (drives flourish depth + copy).</summary>
        public RevealStyle Style { get; }

        /// <summary>Top 3 Career DNA traits, strongest first.</summary>
        public IReadOnlyList<TraitDelta> TopTraits { get; }

        /// <summary>Top 5 career paths by weighted Career DNA scoring.</summary>
        public IReadOnlyList<CareerMatch> TopPaths { get; }

        /// <summary>Primary career family key (top family in the trait blend), or empty.</summary>
        public string PrimaryFamily { get; }

        /// <summary>Secondary career family key (second in the blend), or empty.</summary>
        public string SecondaryFamily { get; }

        /// <summary>Family presentation row for the primary family, or null.</summary>
        public CareerFamilyPresentation FamilyPresentation { get; }

        /// <summary>Ceremony headline: the superpower phrase (leads the reveal).</summary>
        public string Superpower { get; }

        /// <summary>Ceremony subhead: the family blend label (e.g. "Future Tech + Care + Community").</summary>
        public string FamilySubhead { get; }

        /// <summary>Primary combo / hybrid identity for the spotlight, or null.</summary>
        public CareerComboDefinition PrimaryCombo { get; }

        /// <summary>Other eligible combos (gallery/passport/debug), primary excluded.</summary>
        public IReadOnlyList<CareerComboDefinition> AdditionalCombos { get; }

        /// <summary>True when a primary combo exists — the spotlight layers on any style.</summary>
        public bool HasComboSpotlight => PrimaryCombo != null;

        public RevealSynthesisResult(
            int uniqueCompletions,
            bool isRevealReady,
            RevealStyle style,
            IReadOnlyList<TraitDelta> topTraits,
            IReadOnlyList<CareerMatch> topPaths,
            string primaryFamily,
            string secondaryFamily,
            CareerFamilyPresentation familyPresentation,
            string superpower,
            string familySubhead,
            CareerComboDefinition primaryCombo,
            IReadOnlyList<CareerComboDefinition> additionalCombos)
        {
            UniqueCompletions = uniqueCompletions;
            IsRevealReady = isRevealReady;
            Style = style;
            TopTraits = topTraits ?? new List<TraitDelta>();
            TopPaths = topPaths ?? new List<CareerMatch>();
            PrimaryFamily = primaryFamily ?? string.Empty;
            SecondaryFamily = secondaryFamily ?? string.Empty;
            FamilyPresentation = familyPresentation;
            Superpower = superpower ?? string.Empty;
            FamilySubhead = familySubhead ?? string.Empty;
            PrimaryCombo = primaryCombo;
            AdditionalCombos = additionalCombos ?? new List<CareerComboDefinition>();
        }
    }

    /// <summary>
    /// The ONE reveal resolver (KTD9, R14). From shared session inputs it
    /// produces the full ceremony content: top traits, top 5 paths, career
    /// family, superpower, hybrid/combo identity, and the presentation style
    /// bucket. No bespoke per-outcome path — the bucket is a token the same
    /// CareerRevealController / RevealCinematicDirector render.
    ///
    /// Priority (design doc: RevealSynthesis priority rules):
    ///   1. top paths from weighted Career DNA scoring (top 5),
    ///   2. family from the top 2 career categories (trait blend, not one station),
    ///   3. superpower from the top family,
    ///   4. hybrid identity from combo cards only when the pair is earned,
    ///   5. accessories are visual proof (handled by AvatarAccessoryLayer),
    ///   6. headline leads superpower → family → paths → hybrid.
    ///
    /// Pure core: the <see cref="Resolve(IReadOnlyList{CareerMatch},
    /// IReadOnlyDictionary{string,int}, IReadOnlyList{string}, int)"/> overload
    /// takes plain inputs so the selection is fully unit-testable without a
    /// scene; the <see cref="Resolve(GameSession)"/> convenience wires the
    /// session read model in.
    /// </summary>
    public static class RevealSynthesis
    {
        public const int TopTraitCount = 3;
        public const int TopPathCount = 5;

        /// <summary>Convenience over the session read model (host best results or 2P facts).</summary>
        public static RevealSynthesisResult Resolve(GameSession session)
        {
            if (session == null)
            {
                return Resolve(
                    new List<CareerMatch>(),
                    new Dictionary<string, int>(),
                    new List<string>(),
                    0);
            }

            return Resolve(
                session.CareerMatches(),
                session.CareerDna.TraitTotals,
                session.CompletedActivityIds,
                session.UniqueCompletedGames);
        }

        /// <summary>
        /// Pure core. <paramref name="rankedCareers"/> is the full 30-path
        /// ranking (best first); <paramref name="traitTotals"/> is the Career
        /// DNA; <paramref name="completedActivityIdsInOrder"/> drives combo
        /// recency; <paramref name="uniqueCompletions"/> drives the style bucket.
        /// </summary>
        public static RevealSynthesisResult Resolve(
            IReadOnlyList<CareerMatch> rankedCareers,
            IReadOnlyDictionary<string, int> traitTotals,
            IReadOnlyList<string> completedActivityIdsInOrder,
            int uniqueCompletions)
        {
            var ranked = rankedCareers ?? new List<CareerMatch>();
            var traits = traitTotals ?? new Dictionary<string, int>();
            var completed = completedActivityIdsInOrder ?? new List<string>();

            var style = StyleFor(uniqueCompletions);
            var isReady = uniqueCompletions >= 3;

            var topTraits = TopTraits(traits);
            var topPaths = ranked.Take(TopPathCount).ToList();

            // Only families actually represented in the top paths blend the
            // subhead (design: avoid showing more than two families, and never
            // a family with no shown path). A degenerate empty ranking still
            // yields a safe primary fallback below.
            var families = RankFamilies(topPaths);
            var primaryFamily = families.ElementAtOrDefault(0) ?? string.Empty;
            var secondaryFamily = families.ElementAtOrDefault(1) ?? string.Empty;
            CareerFamilyConfig.TryGet(primaryFamily, out var familyPresentation);

            var superpower = familyPresentation != null ? familyPresentation.Superpower : "Campus Explorer";
            var subhead = BuildSubhead(primaryFamily, secondaryFamily);

            var rankedCombos = CareerComboResolver.RankEligible(traits, completed);
            var primaryCombo = rankedCombos.FirstOrDefault();
            var additionalCombos = rankedCombos.Skip(1).ToList();

            return new RevealSynthesisResult(
                uniqueCompletions,
                isReady,
                style,
                topTraits,
                topPaths,
                primaryFamily,
                secondaryFamily,
                familyPresentation,
                superpower,
                subhead,
                primaryCombo,
                additionalCombos);
        }

        /// <summary>The presentation style for a unique-completion count (design buckets).</summary>
        public static RevealStyle StyleFor(int uniqueCompletions)
        {
            if (uniqueCompletions >= 10)
            {
                return RevealStyle.Completionist;
            }

            if (uniqueCompletions >= 8)
            {
                return RevealStyle.BigExplorer;
            }

            if (uniqueCompletions >= 5)
            {
                return RevealStyle.Rich;
            }

            if (uniqueCompletions >= 3)
            {
                return RevealStyle.Simple;
            }

            return RevealStyle.PreReveal;
        }

        private static IReadOnlyList<TraitDelta> TopTraits(IReadOnlyDictionary<string, int> traitTotals)
        {
            return traitTotals
                .Where(pair => pair.Value > 0)
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key, System.StringComparer.Ordinal)
                .Take(TopTraitCount)
                .Select(pair => new TraitDelta(pair.Key, pair.Value))
                .ToList();
        }

        /// <summary>
        /// Family ranking from the player's TOP career categories (design rule
        /// 2: career family comes from the top 2 career categories, not one
        /// station). Each family scores by the summed match score of the top-5
        /// shown paths whose primary family it is — so the subhead always
        /// reflects the careers actually on screen, and small families (Justice,
        /// Nature) can lead when they dominate the top paths. Ties break by
        /// ordinal family name for determinism. A family with no top-path
        /// representation scores 0 and sinks to the end.
        /// </summary>
        private static IReadOnlyList<string> RankFamilies(IReadOnlyList<CareerMatch> topPaths)
        {
            var scoreByFamily = CareerFamilies.All.ToDictionary(family => family, _ => 0);
            foreach (var match in topPaths)
            {
                var family = match.Career.PrimaryFamily;
                if (!string.IsNullOrEmpty(family) && scoreByFamily.ContainsKey(family))
                {
                    scoreByFamily[family] += System.Math.Max(0, match.Score);
                }
            }

            var represented = CareerFamilies.All
                .Where(family => scoreByFamily[family] > 0)
                .OrderByDescending(family => scoreByFamily[family])
                .ThenBy(family => family, System.StringComparer.Ordinal)
                .ToList();

            // Degenerate guard: with no scored paths at all, fall back to a
            // stable ordinal family list so the reveal still names a family.
            return represented.Count > 0
                ? represented
                : CareerFamilies.All.OrderBy(family => family, System.StringComparer.Ordinal).ToList();
        }

        private static string BuildSubhead(string primaryFamily, string secondaryFamily)
        {
            var primary = CareerFamilyConfig.DisplayNameFor(primaryFamily);
            if (string.IsNullOrEmpty(primaryFamily))
            {
                return "Campus Explorer";
            }

            if (string.IsNullOrEmpty(secondaryFamily) || secondaryFamily == primaryFamily)
            {
                return primary;
            }

            return $"{primary} + {CareerFamilyConfig.DisplayNameFor(secondaryFamily)}";
        }
    }
}
