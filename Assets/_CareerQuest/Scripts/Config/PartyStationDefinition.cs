using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    /// <summary>
    /// Supported first-pass toy interaction patterns (design doc schema
    /// contract). Each station maps to exactly one pattern; the U3
    /// ToyInteractionKit provides the matching controller.
    /// </summary>
    public enum ToyPatternId
    {
        DragToSlot,
        SortToBin,
        PickMatchingTrio,
        SequenceCards,
        ComposeSet,
        MatchAndCare,
        BalanceMeters,

        /// <summary>
        /// Design-review #3 (distinct verbs): the player drags ONE tracer
        /// continuously through ordered waypoint zones laid along a route, in
        /// order — not N separate drops. Completion order is identical to
        /// SequenceCards (strict authored order); only the input verb and the
        /// route layout differ, so it routes the same host-validated action seam.
        /// </summary>
        TracePath,

        /// <summary>
        /// Design-review #3 (distinct verbs): the player pulls a toy back from a
        /// launch pad and releases to LAUNCH it at one shared goal (the rescue
        /// spot) — aim + power, not placement. Each chain toy lands in the goal
        /// in any order (acceptance is any-order like ComposeSet); the spatial
        /// "did this shot reach the goal?" skill lives in the launcher input
        /// component, while the rules just validate the toy onto the shared goal
        /// target — so it routes the same host-validated action seam.
        /// </summary>
        ShootTarget,

        /// <summary>
        /// Design-review #3 (distinct verbs): the player taps to CROSS OUT the
        /// candidate answers that break a clue until one survives — deduction by
        /// elimination, NOT drag-to-bin. The false candidates are the CoreTask
        /// eliminate-chain (each crosses out onto its own "cross.{id}"); the one
        /// true answer is a Clue, excluded from the chain, so tapping it bounces
        /// gently ("that one's true, keep it!"). Completion is the same accepted-
        /// equals-required machinery (required = the false set), routed through
        /// the same host-validated action seam.
        /// </summary>
        DeduceAnswer
    }

    /// <summary>
    /// Static identity + content contract for one Party Pack station (KTD2:
    /// static C# definitions, no ScriptableObjects/JSON yet). The station Id is
    /// the same string used by the catalog entry, result activity id, badge
    /// art key suffix, and campus evolution metadata. Objects is the default
    /// seed's object set; alternate seeds replace it via ObjectOverrides.
    /// </summary>
    public sealed class PartyStationDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> VerbTags { get; }
        public ToyPatternId Pattern { get; }
        public string GuideName { get; }
        public string GuideVoice { get; }
        public string Prompt { get; }
        public IReadOnlyList<PartyStationObjectDefinition> Objects { get; }
        public string SuccessRule { get; }
        public IReadOnlyList<TraitDelta> TraitDeltas { get; }
        public string AccessoryRewardId { get; }
        public IReadOnlyList<string> CareerTags { get; }
        public string BadgeArtKey { get; }
        public string CampusArtKey { get; }
        public string EvolutionPropAssetId { get; }
        public IReadOnlyList<PartyStationSeedDefinition> Seeds { get; }

        public PartyStationDefinition(
            string id,
            string displayName,
            IEnumerable<string> verbTags,
            ToyPatternId pattern,
            string guideName,
            string guideVoice,
            string prompt,
            IEnumerable<PartyStationObjectDefinition> objects,
            string successRule,
            IEnumerable<TraitDelta> traitDeltas,
            string accessoryRewardId,
            IEnumerable<string> careerTags,
            string badgeArtKey,
            string campusArtKey,
            string evolutionPropAssetId,
            IEnumerable<PartyStationSeedDefinition> seeds)
        {
            Id = id;
            DisplayName = displayName;
            VerbTags = verbTags?.ToList() ?? new List<string>();
            Pattern = pattern;
            GuideName = guideName;
            GuideVoice = guideVoice;
            Prompt = prompt;
            Objects = objects?.ToList() ?? new List<PartyStationObjectDefinition>();
            SuccessRule = successRule;
            TraitDeltas = traitDeltas?.ToList() ?? new List<TraitDelta>();
            AccessoryRewardId = accessoryRewardId;
            CareerTags = careerTags?.ToList() ?? new List<string>();
            BadgeArtKey = badgeArtKey;
            CampusArtKey = campusArtKey;
            EvolutionPropAssetId = evolutionPropAssetId;
            Seeds = seeds?.ToList() ?? new List<PartyStationSeedDefinition>();
        }

        public PartyStationSeedDefinition DefaultSeed => Seeds.FirstOrDefault(seed => seed.IsDefault);

        public IReadOnlyList<PartyStationSeedDefinition> AlternateSeeds =>
            Seeds.Where(seed => !seed.IsDefault).ToList();

        public bool TryGetSeed(string seedId, out PartyStationSeedDefinition seed)
        {
            seed = Seeds.FirstOrDefault(candidate => candidate.SeedId == seedId);
            return seed != null;
        }

        /// <summary>Seed object set: the seed's overrides when present, otherwise the station defaults.</summary>
        public IReadOnlyList<PartyStationObjectDefinition> ResolveObjects(PartyStationSeedDefinition seed)
        {
            return seed != null && seed.ObjectOverrides.Count > 0 ? seed.ObjectOverrides : Objects;
        }

        /// <summary>Seed prompt: the seed's override when present, otherwise the station prompt.</summary>
        public string ResolvePrompt(PartyStationSeedDefinition seed)
        {
            return seed != null && !string.IsNullOrWhiteSpace(seed.PromptOverride) ? seed.PromptOverride : Prompt;
        }
    }
}
