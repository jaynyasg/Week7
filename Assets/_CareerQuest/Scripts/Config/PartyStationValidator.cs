using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CareerQuest
{
    /// <summary>
    /// Loud validation gate for the Party Pack data spine (U1). Bad station
    /// data should fail here in EditMode tests, never at runtime.
    ///
    /// Encoded early-reader copy rules (design doc: Early-reader copy rule,
    /// Reveal Ceremony Target, Security And Child-Safety Notes):
    /// - No empty or whitespace-only copy, and no multi-line copy.
    /// - Guide-facing lines (intro, hint, escalation, success, reward preview,
    ///   NPC reaction) are at most MaxGuideLineLength (80) characters.
    /// - Prompts and target rules are at most MaxRuleLineLength (90) chars.
    /// - Result summaries are at most MaxResultSummaryLength (200) chars and
    ///   must use the strength framing "You practiced".
    /// - Deterministic career phrases are banned (case-insensitive substring).
    /// - Career jargon words are banned (case-insensitive word match).
    /// - Pretend-play-unsafe words (medical/legal/fear/shame pressure) are
    ///   banned (case-insensitive word match).
    /// </summary>
    public static class PartyStationValidator
    {
        public const int MaxGuideLineLength = 80;
        public const int MaxRuleLineLength = 90;
        public const int MaxResultSummaryLength = 200;
        public const int MinSeedObjects = 4;
        public const int MaxSeedObjects = 6;
        public const int MinChainObjects = 4;
        public const int MinCoreTaskObjects = 2;
        public const string RequiredSummaryFraming = "You practiced";

        /// <summary>Object sprite keys may use this intentional placeholder namespace until station art lands (U4/U5).</summary>
        public const string PlaceholderSpritePrefix = "prop.party.";

        /// <summary>Shared juice cue ids an object may declare as its visible reaction (no-dead-toys rule).</summary>
        public static readonly string[] KnownReactionKeys =
        {
            "react.pop",
            "react.sparkle",
            "react.bounce",
            "react.glow",
            "react.cheer",
            "react.wobble",
            "react.spin",
            "react.meter_shift"
        };

        public static readonly string[] BannedDeterministicPhrases =
        {
            "you will be",
            "you will become",
            "you must",
            "you should be",
            "you should become",
            "destined",
            "destiny",
            "this is your future",
            "born to be",
            "meant to be",
            "grow up to be",
            "one day you will"
        };

        public static readonly string[] BannedCareerJargon =
        {
            "algorithm",
            "algorithms",
            "neural",
            "optimization",
            "optimize",
            "infrastructure",
            "stakeholder",
            "synergy",
            "litigation",
            "jurisdiction",
            "metabolism",
            "certification",
            "psychometric",
            "curriculum"
        };

        public static readonly string[] BannedPretendPlayWords =
        {
            "sick",
            "illness",
            "disease",
            "medicine",
            "surgery",
            "blood",
            "pain",
            "injury",
            "injured",
            "die",
            "dies",
            "died",
            "dying",
            "death",
            "kill",
            "scared",
            "afraid",
            "fear",
            "guilty",
            "lawsuit",
            "arrest",
            "jail",
            "disaster",
            "deadly",
            "destroy",
            "destroyed",
            "allergy",
            "allergies",
            "diet",
            "weapon",
            "danger",
            "dangerous",
            "hurt",
            "shame",
            "stupid",
            "dumb"
        };

        private static readonly PartyStationObjectRole[] KnownRoles =
            (PartyStationObjectRole[])Enum.GetValues(typeof(PartyStationObjectRole));

        /// <summary>Validates every station, the accessory table, and the combo table. Empty result means the data spine is sound.</summary>
        public static IReadOnlyList<string> ValidateAll()
        {
            var issues = new List<string>();

            var stations = PartyStationDefinitions.All;
            AddDuplicates(issues, stations.Select(station => station.Id), "station id");
            AddDuplicates(
                issues,
                stations.SelectMany(station => station.Seeds ?? new List<PartyStationSeedDefinition>())
                    .Where(seed => seed != null)
                    .Select(seed => seed.SeedId),
                "seed id");

            foreach (var station in stations)
            {
                issues.AddRange(Validate(station));
            }

            ValidateAccessoryConfig(issues);
            ValidateComboConfig(issues);
            return issues;
        }

        /// <summary>Validates one station definition. Null-safe: a null definition is itself an issue.</summary>
        public static IReadOnlyList<string> Validate(PartyStationDefinition definition)
        {
            var issues = new List<string>();
            if (definition == null)
            {
                issues.Add("station: definition is null");
                return issues;
            }

            var context = string.IsNullOrWhiteSpace(definition.Id) ? "<missing id>" : definition.Id;
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                issues.Add($"{context}: station id is empty");
            }

            if (string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                issues.Add($"{context}: display name is empty");
            }

            if (definition.VerbTags == null || definition.VerbTags.Count == 0)
            {
                issues.Add($"{context}: verb tags are empty");
            }

            if (!Enum.IsDefined(typeof(ToyPatternId), definition.Pattern))
            {
                issues.Add($"{context}: unsupported toy pattern '{definition.Pattern}'");
            }

            ValidateGuideIdentity(issues, definition, context);
            ValidateCatalogAlignment(issues, definition, context);
            ValidateCareerTags(issues, definition, context);
            ValidateTraitDeltas(issues, definition, context);
            ValidateRewardAndArtKeys(issues, definition, context);
            ValidateSeeds(issues, definition, context);
            return issues;
        }

        /// <summary>Checks one short guide-facing copy line against the early-reader rules.</summary>
        public static IReadOnlyList<string> CheckGuideLine(string line, string context)
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(line))
            {
                issues.Add($"{context}: copy is empty");
                return issues;
            }

            if (line.IndexOf('\n') >= 0)
            {
                issues.Add($"{context}: copy spans multiple lines");
            }

            if (line.Length > MaxGuideLineLength)
            {
                issues.Add($"{context}: copy is {line.Length} chars (early-reader max {MaxGuideLineLength})");
            }

            issues.AddRange(CheckCopySafety(line, context));
            return issues;
        }

        /// <summary>Checks a prompt/target-rule line (slightly longer budget than guide lines).</summary>
        public static IReadOnlyList<string> CheckRuleLine(string line, string context)
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(line))
            {
                issues.Add($"{context}: copy is empty");
                return issues;
            }

            if (line.Length > MaxRuleLineLength)
            {
                issues.Add($"{context}: copy is {line.Length} chars (rule max {MaxRuleLineLength})");
            }

            issues.AddRange(CheckCopySafety(line, context));
            return issues;
        }

        /// <summary>Checks a station-end result summary (length + "You practiced" framing + safety).</summary>
        public static IReadOnlyList<string> CheckResultSummary(string summary, string context)
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(summary))
            {
                issues.Add($"{context}: result summary is empty");
                return issues;
            }

            if (summary.Length > MaxResultSummaryLength)
            {
                issues.Add($"{context}: result summary is {summary.Length} chars (max {MaxResultSummaryLength})");
            }

            if (!summary.Contains(RequiredSummaryFraming))
            {
                issues.Add($"{context}: result summary is missing the '{RequiredSummaryFraming}' strength framing");
            }

            issues.AddRange(CheckCopySafety(summary, context));
            return issues;
        }

        /// <summary>
        /// Scans any copy for deterministic career phrases, career jargon, and
        /// pretend-play-unsafe wording. Null/empty text yields no safety
        /// issues (emptiness is checked by the line validators).
        /// </summary>
        public static IReadOnlyList<string> CheckCopySafety(string text, string context)
        {
            var issues = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return issues;
            }

            foreach (var phrase in BannedDeterministicPhrases)
            {
                if (text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    issues.Add($"{context}: deterministic career phrase '{phrase}'");
                }
            }

            foreach (var word in BannedCareerJargon)
            {
                if (ContainsWord(text, word))
                {
                    issues.Add($"{context}: career jargon '{word}'");
                }
            }

            foreach (var word in BannedPretendPlayWords)
            {
                if (ContainsWord(text, word))
                {
                    issues.Add($"{context}: pretend-play-unsafe word '{word}'");
                }
            }

            return issues;
        }

        /// <summary>Resolved object set for a seed (overrides or station defaults). Null-safe, never null.</summary>
        public static IReadOnlyList<PartyStationObjectDefinition> ResolveSeedObjects(
            PartyStationDefinition definition,
            PartyStationSeedDefinition seed)
        {
            if (definition == null)
            {
                return Array.Empty<PartyStationObjectDefinition>();
            }

            return definition.ResolveObjects(seed) ?? Array.Empty<PartyStationObjectDefinition>();
        }

        private static void ValidateGuideIdentity(List<string> issues, PartyStationDefinition definition, string context)
        {
            if (string.IsNullOrWhiteSpace(definition.GuideName))
            {
                issues.Add($"{context}: guide name is empty");
            }
            else
            {
                issues.AddRange(CheckCopySafety(definition.GuideName, $"{context}.guide_name"));
            }

            if (string.IsNullOrWhiteSpace(definition.GuideVoice))
            {
                issues.Add($"{context}: guide voice is empty");
            }

            issues.AddRange(CheckRuleLine(definition.Prompt, $"{context}.prompt"));
            issues.AddRange(CheckRuleLine(definition.SuccessRule, $"{context}.success_rule"));
        }

        private static void ValidateCatalogAlignment(List<string> issues, PartyStationDefinition definition, string context)
        {
            if (!CareerQuestCatalog.TryGetById(definition.Id, out var entry))
            {
                issues.Add($"{context}: no CareerQuestCatalog entry for station id");
                return;
            }

            if (entry.BadgeArtKey != definition.BadgeArtKey)
            {
                issues.Add($"{context}: badge art key '{definition.BadgeArtKey}' does not match catalog '{entry.BadgeArtKey}'");
            }

            if (entry.CampusAssetId != definition.CampusArtKey)
            {
                issues.Add($"{context}: campus art key '{definition.CampusArtKey}' does not match catalog '{entry.CampusAssetId}'");
            }

            if (definition.CareerTags == null || !definition.CareerTags.Contains(entry.CareerTag))
            {
                issues.Add($"{context}: career tags are missing the catalog primary tag '{entry.CareerTag}'");
            }
        }

        private static void ValidateCareerTags(List<string> issues, PartyStationDefinition definition, string context)
        {
            if (definition.CareerTags == null || definition.CareerTags.Count == 0)
            {
                issues.Add($"{context}: career tags are empty");
                return;
            }

            foreach (var tag in definition.CareerTags)
            {
                if (!CareerConfig.TryGetCareer(tag, out _))
                {
                    issues.Add($"{context}: unknown career tag '{tag}'");
                }
            }
        }

        private static void ValidateTraitDeltas(List<string> issues, PartyStationDefinition definition, string context)
        {
            if (definition.TraitDeltas == null || definition.TraitDeltas.Count == 0)
            {
                issues.Add($"{context}: trait deltas are empty");
                return;
            }

            foreach (var delta in definition.TraitDeltas)
            {
                if (!CareerConfig.AllTraits.Contains(delta.Trait))
                {
                    issues.Add($"{context}: unknown trait '{delta.Trait}'");
                }

                if (delta.Delta <= 0)
                {
                    issues.Add($"{context}: trait '{delta.Trait}' has non-positive delta {delta.Delta}");
                }
            }
        }

        private static void ValidateRewardAndArtKeys(List<string> issues, PartyStationDefinition definition, string context)
        {
            if (!AccessoryRewardConfig.TryGetById(definition.AccessoryRewardId, out var accessory))
            {
                issues.Add($"{context}: unknown accessory reward '{definition.AccessoryRewardId}'");
            }
            else if (accessory.StationId != definition.Id)
            {
                issues.Add($"{context}: accessory '{accessory.Id}' belongs to station '{accessory.StationId}'");
            }

            if (!AssetCatalog.TryGetDefinition(definition.BadgeArtKey, out _))
            {
                issues.Add($"{context}: badge art key '{definition.BadgeArtKey}' is not cataloged");
            }

            if (!AssetCatalog.TryGetDefinition(definition.CampusArtKey, out _))
            {
                issues.Add($"{context}: campus art key '{definition.CampusArtKey}' is not cataloged");
            }

            if (!AssetCatalog.TryGetDefinition(definition.EvolutionPropAssetId, out _))
            {
                issues.Add($"{context}: evolution prop '{definition.EvolutionPropAssetId}' is not cataloged");
            }

            if (!CampusEvolutionController.TryGetEvolutionPropAssetId(definition.Id, out var evolutionProp))
            {
                issues.Add($"{context}: no campus evolution slot for station id");
            }
            else if (evolutionProp != definition.EvolutionPropAssetId)
            {
                issues.Add($"{context}: evolution prop '{definition.EvolutionPropAssetId}' does not match evolution layout '{evolutionProp}'");
            }
        }

        private static void ValidateSeeds(List<string> issues, PartyStationDefinition definition, string context)
        {
            if (definition.Seeds == null || definition.Seeds.Count == 0)
            {
                issues.Add($"{context}: station has no seeds");
                return;
            }

            if (definition.Seeds.Count != 2)
            {
                issues.Add($"{context}: expected exactly one default and one alternate seed, found {definition.Seeds.Count}");
            }

            var defaultCount = definition.Seeds.Count(seed => seed != null && seed.IsDefault);
            if (defaultCount != 1)
            {
                issues.Add($"{context}: expected exactly one default seed, found {defaultCount}");
            }

            AddDuplicates(issues, definition.Seeds.Where(seed => seed != null).Select(seed => seed.SeedId), $"{context} seed id");

            foreach (var seed in definition.Seeds)
            {
                if (seed == null)
                {
                    issues.Add($"{context}: seed entry is null");
                    continue;
                }

                ValidateSeed(issues, definition, seed, context);
            }
        }

        private static void ValidateSeed(
            List<string> issues,
            PartyStationDefinition definition,
            PartyStationSeedDefinition seed,
            string stationContext)
        {
            var context = string.IsNullOrWhiteSpace(seed.SeedId) ? $"{stationContext}.<missing seed id>" : seed.SeedId;
            if (string.IsNullOrWhiteSpace(seed.SeedId))
            {
                issues.Add($"{stationContext}: seed id is empty");
            }
            else if (!seed.SeedId.StartsWith($"{definition.Id}.", StringComparison.Ordinal))
            {
                issues.Add($"{context}: seed id does not use the '{definition.Id}.' prefix convention");
            }

            if (string.IsNullOrWhiteSpace(seed.DisplayName))
            {
                issues.Add($"{context}: seed display name is empty");
            }

            issues.AddRange(CheckRuleLine(definition.ResolvePrompt(seed), $"{context}.prompt"));
            issues.AddRange(CheckRuleLine(seed.TargetRule, $"{context}.target_rule"));
            issues.AddRange(CheckGuideLine(seed.IntroLine, $"{context}.intro"));
            issues.AddRange(CheckGuideLine(seed.HintLine, $"{context}.hint"));
            issues.AddRange(CheckGuideLine(seed.EscalationHintLine, $"{context}.escalation_hint"));
            issues.AddRange(CheckGuideLine(seed.SuccessLine, $"{context}.success"));
            issues.AddRange(CheckGuideLine(seed.RewardPreviewLine, $"{context}.reward_preview"));
            issues.AddRange(CheckGuideLine(seed.NpcReaction, $"{context}.npc_reaction"));
            issues.AddRange(CheckResultSummary(seed.ResultSummary, $"{context}.result_summary"));

            ValidateSeedObjects(issues, definition, seed, context);
        }

        private static void ValidateSeedObjects(
            List<string> issues,
            PartyStationDefinition definition,
            PartyStationSeedDefinition seed,
            string context)
        {
            var objects = ResolveSeedObjects(definition, seed);
            if (objects.Count < MinSeedObjects || objects.Count > MaxSeedObjects)
            {
                issues.Add($"{context}: seed has {objects.Count} interactables (expected {MinSeedObjects}-{MaxSeedObjects})");
            }

            var chainCount = objects.Count(item => item != null && item.IsChainRole);
            if (chainCount < MinChainObjects)
            {
                issues.Add($"{context}: only {chainCount} task/clue-chain objects (expected at least {MinChainObjects})");
            }

            var coreCount = objects.Count(item => item != null && item.Role == PartyStationObjectRole.CoreTask);
            if (coreCount < MinCoreTaskObjects)
            {
                issues.Add($"{context}: only {coreCount} core task objects (expected at least {MinCoreTaskObjects})");
            }

            AddDuplicates(
                issues,
                objects.Where(item => item != null).Select(item => item.ObjectId),
                $"{context} object id");

            var knownIds = new HashSet<string>(objects.Where(item => item != null).Select(item => item.ObjectId));
            foreach (var item in objects)
            {
                if (item == null)
                {
                    issues.Add($"{context}: object entry is null");
                    continue;
                }

                var objectContext = $"{context}.{item.ObjectId}";
                if (string.IsNullOrWhiteSpace(item.ObjectId))
                {
                    issues.Add($"{context}: object id is empty");
                }

                if (string.IsNullOrWhiteSpace(item.DisplayName))
                {
                    issues.Add($"{objectContext}: object display name is empty");
                }
                else
                {
                    issues.AddRange(CheckCopySafety(item.DisplayName, objectContext));
                }

                if (!KnownRoles.Contains(item.Role))
                {
                    issues.Add($"{objectContext}: unknown object role '{item.Role}'");
                }

                // No-dead-toys rule: every listed interactable declares a
                // known visible reaction cue.
                if (string.IsNullOrWhiteSpace(item.ReactionKey) || !KnownReactionKeys.Contains(item.ReactionKey))
                {
                    issues.Add($"{objectContext}: reaction key '{item.ReactionKey}' is not a known shared cue");
                }

                if (string.IsNullOrWhiteSpace(item.SpriteKey)
                    || (!AssetCatalog.TryGetDefinition(item.SpriteKey, out _)
                        && !item.SpriteKey.StartsWith(PlaceholderSpritePrefix, StringComparison.Ordinal)))
                {
                    issues.Add($"{objectContext}: sprite key '{item.SpriteKey}' is neither cataloged nor a '{PlaceholderSpritePrefix}' placeholder");
                }

                if (!string.IsNullOrEmpty(item.TargetId) && !knownIds.Contains(item.TargetId))
                {
                    issues.Add($"{objectContext}: target '{item.TargetId}' does not reference a known object in this seed");
                }

                if (!string.IsNullOrEmpty(item.TraitHint) && !CareerConfig.AllTraits.Contains(item.TraitHint))
                {
                    issues.Add($"{objectContext}: unknown trait hint '{item.TraitHint}'");
                }
            }
        }

        private static void ValidateAccessoryConfig(List<string> issues)
        {
            AddDuplicates(issues, AccessoryRewardConfig.All.Select(definition => definition.Id), "accessory id");

            foreach (var accessory in AccessoryRewardConfig.All)
            {
                var context = $"accessory.{accessory.Id}";
                if (string.IsNullOrWhiteSpace(accessory.DisplayName))
                {
                    issues.Add($"{context}: display name is empty");
                }
                else
                {
                    issues.AddRange(CheckCopySafety(accessory.DisplayName, context));
                }

                if (!Enum.IsDefined(typeof(AccessorySlot), accessory.Slot))
                {
                    issues.Add($"{context}: unknown accessory slot '{accessory.Slot}'");
                }

                if (!AssetCatalog.TryGetDefinition(accessory.SpriteAssetId, out _))
                {
                    issues.Add($"{context}: sprite asset '{accessory.SpriteAssetId}' is not cataloged");
                }

                if (accessory.IsMilestone == !string.IsNullOrEmpty(accessory.StationId))
                {
                    issues.Add($"{context}: accessory must unlock from exactly one of station or milestone");
                }

                if (!accessory.IsMilestone && !CareerQuestCatalog.TryGetById(accessory.StationId, out _))
                {
                    issues.Add($"{context}: unknown station id '{accessory.StationId}'");
                }

                if (accessory.IsMilestone && !AccessoryRewardConfig.MilestoneThresholds.Contains(accessory.MilestoneCompletions))
                {
                    issues.Add($"{context}: milestone count {accessory.MilestoneCompletions} is not a known threshold");
                }
            }

            foreach (var stationId in CareerQuestCatalog.PartyStationIds)
            {
                if (!AccessoryRewardConfig.TryGetForStation(stationId, out _))
                {
                    issues.Add($"accessory: station '{stationId}' has no core accessory");
                }
            }

            foreach (var threshold in AccessoryRewardConfig.MilestoneThresholds)
            {
                if (!AccessoryRewardConfig.TryGetForMilestone(threshold, out _))
                {
                    issues.Add($"accessory: milestone threshold {threshold} has no accessory");
                }
            }
        }

        private static void ValidateComboConfig(List<string> issues)
        {
            if (CareerComboConfig.All.Count < 12)
            {
                issues.Add($"combo: expected at least 12 starter combo cards, found {CareerComboConfig.All.Count}");
            }

            AddDuplicates(issues, CareerComboConfig.All.Select(definition => definition.Id), "combo id");
            AddDuplicates(issues, CareerComboConfig.All.Select(definition => definition.AuthoredPriority.ToString()), "combo priority");

            foreach (var combo in CareerComboConfig.All)
            {
                var context = $"combo.{combo.Id}";
                if (string.IsNullOrWhiteSpace(combo.DisplayName))
                {
                    issues.Add($"{context}: display name is empty");
                }

                if (combo.RequiredStationIds.Count != 2 || combo.RequiredStationIds.Distinct().Count() != 2)
                {
                    issues.Add($"{context}: combo must pair two distinct stations");
                }

                foreach (var stationId in combo.RequiredStationIds)
                {
                    if (!CareerQuestCatalog.TryGetById(stationId, out _))
                    {
                        issues.Add($"{context}: unknown station id '{stationId}'");
                    }
                }

                foreach (var family in combo.FamilyBlend)
                {
                    if (!CareerFamilies.All.Contains(family))
                    {
                        issues.Add($"{context}: unknown career family '{family}'");
                    }
                }

                issues.AddRange(CheckGuideLine(combo.RevealCopy, $"{context}.reveal_copy"));
            }
        }

        private static void AddDuplicates(List<string> issues, IEnumerable<string> values, string label)
        {
            var duplicates = values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .GroupBy(value => value)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

            foreach (var duplicate in duplicates)
            {
                issues.Add($"duplicate {label} '{duplicate}'");
            }
        }

        private static bool ContainsWord(string text, string word)
        {
            return Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase);
        }
    }
}
