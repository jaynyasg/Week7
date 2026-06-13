using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    /// <summary>
    /// Session-only append log of station reward events (U6, R11). The session
    /// layer feeds it from <see cref="PartyStationController.RewardEventEmitted"/>
    /// via <see cref="GameSession.AppendStationRewardEvent"/>; replays append
    /// even when they do not replace the best result, so the passport Results
    /// page always shows the most recent seed-aware micro-result.
    ///
    /// The log also owns the once-per-session combo SPARK memory (design doc:
    /// each combo spark appears once per session even if stations replay).
    /// Combo eligibility itself is a pure pair check over completed station
    /// ids against <see cref="CareerComboConfig"/> — the full
    /// CareerComboResolver (primary-combo selection for reveal) is U7.
    ///
    /// Never persisted, never a second scoring channel (KTD8): nothing here
    /// reads back into Career DNA, ranking, or reveal readiness.
    /// </summary>
    public sealed class RewardEventLog
    {
        /// <summary>Recent-events cap — a session log, never a history archive.</summary>
        public const int MaxEvents = 32;

        private readonly List<RewardEvent> _events = new();
        private readonly HashSet<string> _shownComboSparkIds = new();

        /// <summary>All retained events, oldest first.</summary>
        public IReadOnlyList<RewardEvent> Events => _events;

        /// <summary>Combo ids whose spark beat already played this session.</summary>
        public IReadOnlyCollection<string> ShownComboSparkIds => _shownComboSparkIds;

        /// <summary>
        /// Appends one station reward event. `completedActivityIds` is the
        /// session's completed set BEFORE/AFTER this completion (the event's
        /// own station is unioned in, so call order around RecordResult does
        /// not matter). Returns the appended event.
        /// </summary>
        public RewardEvent Append(StationRewardEvent stationEvent, IEnumerable<string> completedActivityIds)
        {
            var completed = new HashSet<string>(completedActivityIds ?? Enumerable.Empty<string>());
            if (!string.IsNullOrWhiteSpace(stationEvent.StationId))
            {
                completed.Add(stationEvent.StationId);
            }

            var sparkIds = new List<string>();
            foreach (var comboId in EligibleComboIds(completed))
            {
                if (_shownComboSparkIds.Add(comboId))
                {
                    sparkIds.Add(comboId);
                }
            }

            var rewardEvent = new RewardEvent(
                stationEvent.StationId,
                stationEvent.SeedId,
                stationEvent.Tier,
                stationEvent.Source,
                stationEvent.Summary,
                TopTraits(stationEvent.TraitDeltas),
                stationEvent.AccessoryRewardId,
                sparkIds);

            _events.Add(rewardEvent);
            if (_events.Count > MaxEvents)
            {
                _events.RemoveAt(0);
            }

            return rewardEvent;
        }

        /// <summary>The most recent events, newest first (passport Results page).</summary>
        public IReadOnlyList<RewardEvent> Recent(int count)
        {
            if (count <= 0 || _events.Count == 0)
            {
                return new List<RewardEvent>();
            }

            return _events.AsEnumerable().Reverse().Take(count).ToList();
        }

        public void Clear()
        {
            _events.Clear();
            _shownComboSparkIds.Clear();
        }

        /// <summary>
        /// U6 combo-eligibility fact: combos whose required station pair is
        /// fully inside the completed set, in authored priority order. Pure
        /// derivation over <see cref="CareerComboConfig"/> — no stored unlock
        /// state, so host and 2P clients agree from the same completion facts.
        /// </summary>
        public static IReadOnlyList<string> EligibleComboIds(IEnumerable<string> completedActivityIds)
        {
            var completed = completedActivityIds as ISet<string> ?? new HashSet<string>(completedActivityIds ?? Enumerable.Empty<string>());
            return CareerComboConfig.All
                .Where(combo => combo.RequiredStationIds.Count > 0 && combo.RequiredStationIds.All(completed.Contains))
                .OrderBy(combo => combo.AuthoredPriority)
                .Select(combo => combo.Id)
                .ToList();
        }

        private static IEnumerable<TraitDelta> TopTraits(IReadOnlyList<TraitDelta> traitDeltas)
        {
            if (traitDeltas == null)
            {
                return Enumerable.Empty<TraitDelta>();
            }

            return traitDeltas
                .OrderByDescending(delta => delta.Delta)
                .ThenBy(delta => delta.Trait)
                .Take(RewardEvent.MaxTraitHighlights);
        }
    }
}
