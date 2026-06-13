using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    /// <summary>
    /// One session reward event (U6, R11): the presentation record of a single
    /// station completion — recent micro-result copy, the selected seed id,
    /// the seed-aware summary, the top trait highlights, the unlocked
    /// accessory id, and any combo spark ids that became eligible at this
    /// completion. Events are session-only, append-only, and presentation-only
    /// (KTD8): they never feed Career DNA, ranking, or reveal readiness —
    /// best results in <see cref="GameSession"/> stay the scoring truth.
    /// </summary>
    public sealed class RewardEvent
    {
        /// <summary>Micro-result rule (design doc): top 2-3 practiced traits.</summary>
        public const int MaxTraitHighlights = 3;

        public RewardEvent(
            string stationId,
            string seedId,
            CompletionTier tier,
            ResultSource source,
            string summary,
            IEnumerable<TraitDelta> topTraitHighlights,
            string accessoryRewardId,
            IEnumerable<string> comboSparkIds)
        {
            StationId = stationId;
            SeedId = seedId;
            Tier = tier;
            Source = source;
            Summary = summary;
            TopTraitHighlights = topTraitHighlights?.ToList() ?? new List<TraitDelta>();
            AccessoryRewardId = accessoryRewardId;
            ComboSparkIds = comboSparkIds?.ToList() ?? new List<string>();
        }

        public string StationId { get; }

        /// <summary>The seed the player actually chose for this attempt.</summary>
        public string SeedId { get; }

        public CompletionTier Tier { get; }
        public ResultSource Source { get; }

        /// <summary>Seed-aware "what you did" line (the seed's result summary).</summary>
        public string Summary { get; }

        /// <summary>"What you practiced": the attempt's strongest trait deltas.</summary>
        public IReadOnlyList<TraitDelta> TopTraitHighlights { get; }

        /// <summary>"What you unlocked": the station's core accessory id.</summary>
        public string AccessoryRewardId { get; }

        /// <summary>Combo cards that became spark-eligible at this completion (first time only).</summary>
        public IReadOnlyList<string> ComboSparkIds { get; }

        /// <summary>"You practiced Building + Reasoning." (micro-result line two).</summary>
        public string PracticedLine()
        {
            if (TopTraitHighlights.Count == 0)
            {
                return "You practiced something new today.";
            }

            var names = TopTraitHighlights.Select(delta => delta.Trait);
            return $"You practiced {string.Join(" + ", names)}.";
        }
    }

    /// <summary>
    /// One compact completed-activity fact for the multiplayer read model
    /// (R17): which activity completed and the best tier — never names, free
    /// text, or persistent profile data. Order matters: snapshots arrive in
    /// first-completion order so clients derive the same "newest per slot"
    /// accessory choices the host does.
    /// </summary>
    public readonly struct CompletedActivitySnapshot
    {
        public CompletedActivitySnapshot(string activityId, CompletionTier tier)
        {
            ActivityId = activityId;
            Tier = tier;
        }

        public string ActivityId { get; }
        public CompletionTier Tier { get; }
    }
}
