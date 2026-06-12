using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>Avatar accessory slots (design doc: head, face, torso, back, hand, sash).</summary>
    public enum AccessorySlot
    {
        Head,
        Face,
        Torso,
        Back,
        Hand,
        Sash
    }

    /// <summary>
    /// One derived accessory reward. Accessories are visual/story rewards only
    /// and never modify career scoring (KTD8). Station accessories unlock from
    /// the station's best result; milestone accessories derive from unique
    /// completion counts. Offsets/scale/sorting feed the U6
    /// AvatarAccessoryLayer anchors.
    /// </summary>
    public sealed class AccessoryDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public AccessorySlot Slot { get; }
        public string SpriteAssetId { get; }
        public Vector2 LocalOffset { get; }
        public float LocalScale { get; }
        public int SortingOffset { get; }
        public bool CeremonyOnly { get; }

        /// <summary>Station that unlocks this accessory, or empty for milestone accessories.</summary>
        public string StationId { get; }

        /// <summary>Unique-completion count that unlocks this accessory, or 0 for station accessories.</summary>
        public int MilestoneCompletions { get; }

        public AccessoryDefinition(
            string id,
            string displayName,
            AccessorySlot slot,
            string spriteAssetId,
            Vector2 localOffset,
            float localScale,
            int sortingOffset,
            bool ceremonyOnly,
            string stationId,
            int milestoneCompletions)
        {
            Id = id;
            DisplayName = displayName;
            Slot = slot;
            SpriteAssetId = spriteAssetId;
            LocalOffset = localOffset;
            LocalScale = localScale;
            SortingOffset = sortingOffset;
            CeremonyOnly = ceremonyOnly;
            StationId = stationId;
            MilestoneCompletions = milestoneCompletions;
        }

        public bool IsMilestone => MilestoneCompletions > 0;
    }

    /// <summary>
    /// Static accessory reward table: one core accessory per Party Pack
    /// station plus milestone/ceremony accessories at 3/5/8/10 unique
    /// completions (R12). The star robe and the 10-completion reveal flourish
    /// are ceremony-only so normal campus play stays uncluttered.
    /// </summary>
    public static class AccessoryRewardConfig
    {
        public static readonly int[] MilestoneThresholds = { 3, 5, 8, 10 };

        private static readonly AccessoryDefinition[] Definitions =
        {
            Station("accessory.tool_belt", "Tool Belt", AccessorySlot.Torso, CareerQuestCatalog.RoboticsGarageId),
            Station("accessory.lab_goggles", "Lab Goggles", AccessorySlot.Face, CareerQuestCatalog.AiLabId),
            Station("accessory.chef_hat", "Chef Hat", AccessorySlot.Head, CareerQuestCatalog.CommunityKitchenId),
            Station("accessory.microphone", "Microphone", AccessorySlot.Hand, CareerQuestCatalog.MusicStudioId),
            Station("accessory.care_cape", "Care Cape", AccessorySlot.Back, CareerQuestCatalog.VetClinicId),
            Station("accessory.sketchbook", "Sketchbook", AccessorySlot.Hand, CareerQuestCatalog.GameStudioId),
            Station("accessory.weather_goggles", "Weather Goggles", AccessorySlot.Face, CareerQuestCatalog.WeatherLabId),
            Station("accessory.mission_patch", "Mission Patch", AccessorySlot.Torso, CareerQuestCatalog.SpaceportId),
            Station("accessory.press_badge", "Press Badge", AccessorySlot.Torso, CareerQuestCatalog.NewsroomId),
            Station("accessory.green_hardhat", "Green Hardhat", AccessorySlot.Head, CareerQuestCatalog.GreenCityId),

            Milestone("accessory.badge_sash", "Badge Sash", AccessorySlot.Sash, 3, false),
            Milestone("accessory.explorer_cape", "Explorer Cape", AccessorySlot.Back, 5, false),
            Milestone("accessory.star_robe", "Star Robe", AccessorySlot.Torso, 8, true),
            Milestone("accessory.reveal_flourish", "Reveal Flourish", AccessorySlot.Back, 10, true)
        };

        public static IReadOnlyList<AccessoryDefinition> All => Definitions;

        public static IEnumerable<AccessoryDefinition> StationAccessories =>
            Definitions.Where(definition => !definition.IsMilestone);

        public static IEnumerable<AccessoryDefinition> MilestoneAccessories =>
            Definitions.Where(definition => definition.IsMilestone);

        public static bool TryGetById(string id, out AccessoryDefinition definition)
        {
            definition = Definitions.FirstOrDefault(candidate => candidate.Id == id);
            return definition != null;
        }

        public static bool TryGetForStation(string stationId, out AccessoryDefinition definition)
        {
            definition = Definitions.FirstOrDefault(candidate => candidate.StationId == stationId);
            return definition != null;
        }

        public static bool TryGetForMilestone(int uniqueCompletions, out AccessoryDefinition definition)
        {
            definition = Definitions.FirstOrDefault(candidate => candidate.MilestoneCompletions == uniqueCompletions);
            return definition != null;
        }

        private static AccessoryDefinition Station(string id, string displayName, AccessorySlot slot, string stationId)
        {
            return new AccessoryDefinition(id, displayName, slot, id, Vector2.zero, 1f, 1, false, stationId, 0);
        }

        private static AccessoryDefinition Milestone(string id, string displayName, AccessorySlot slot, int completions, bool ceremonyOnly)
        {
            return new AccessoryDefinition(id, displayName, slot, id, Vector2.zero, 1f, 1, ceremonyOnly, string.Empty, completions);
        }
    }
}
