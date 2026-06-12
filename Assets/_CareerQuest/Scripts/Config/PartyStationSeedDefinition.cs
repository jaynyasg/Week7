using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    /// <summary>
    /// One remix seed for a Party Pack station (design doc: Station Seed
    /// Bible). Exactly one seed per station is the default; alternates change
    /// the prompt, objects, target rule, and copy without creating extra badge
    /// identities or reveal progress. Empty PromptOverride/ObjectOverrides fall
    /// back to the station-level Prompt/Objects.
    /// </summary>
    public sealed class PartyStationSeedDefinition
    {
        public string SeedId { get; }
        public string DisplayName { get; }
        public bool IsDefault { get; }
        public string PromptOverride { get; }
        public IReadOnlyList<PartyStationObjectDefinition> ObjectOverrides { get; }
        public string TargetRule { get; }
        public string IntroLine { get; }
        public string HintLine { get; }
        public string EscalationHintLine { get; }
        public string SuccessLine { get; }
        public string RewardPreviewLine { get; }
        public string ResultSummary { get; }
        public string NpcReaction { get; }

        public PartyStationSeedDefinition(
            string seedId,
            string displayName,
            bool isDefault,
            string promptOverride,
            IEnumerable<PartyStationObjectDefinition> objectOverrides,
            string targetRule,
            string introLine,
            string hintLine,
            string escalationHintLine,
            string successLine,
            string rewardPreviewLine,
            string resultSummary,
            string npcReaction)
        {
            SeedId = seedId;
            DisplayName = displayName;
            IsDefault = isDefault;
            PromptOverride = promptOverride;
            ObjectOverrides = objectOverrides?.ToList() ?? new List<PartyStationObjectDefinition>();
            TargetRule = targetRule;
            IntroLine = introLine;
            HintLine = hintLine;
            EscalationHintLine = escalationHintLine;
            SuccessLine = successLine;
            RewardPreviewLine = rewardPreviewLine;
            ResultSummary = resultSummary;
            NpcReaction = npcReaction;
        }
    }
}
