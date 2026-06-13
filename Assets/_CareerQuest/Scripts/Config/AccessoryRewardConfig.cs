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
    ///
    /// U11 accessory-fit pass: each definition now carries a tuned
    /// LocalOffset / LocalScale / SortingOffset (U6 left these as bare
    /// defaults). The values nudge each piece onto its body part on top of the
    /// slot-proportional anchor the <see cref="AvatarAccessoryLayer"/> derives
    /// from the host sprite extents — e.g. the chef hat sits a touch higher on
    /// the Head anchor, the back cape
    /// sorts BEHIND the body (negative SortingOffset) while a hand mic sorts in
    /// front. Bigger props (capes, robe) scale up; small pins/goggles scale
    /// down — so 3+ accessories read cleanly without floating or clipping.
    /// The geometric invariants (renderer bounds overlap the avatar, sort =
    /// host + offset, flip mirrors anchor-x) are asserted in
    /// AvatarAccessoryLayerPlayModeTests; final pixel sign-off stays an owner
    /// visual-review gate.
    /// </summary>
    public static class AccessoryRewardConfig
    {
        public static readonly int[] MilestoneThresholds = { 3, 5, 8, 10 };

        private static readonly AccessoryDefinition[] Definitions =
        {
            //       id / name / slot / station
            //       localOffset (avatar-local units, added to the slot anchor)
            //       localScale (× the normalized token height) / sortingOffset
            Station("accessory.tool_belt", "Tool Belt", AccessorySlot.Torso, CareerQuestCatalog.RoboticsGarageId,
                new Vector2(0f, -0.22f), 1.05f, 2),
            Station("accessory.lab_goggles", "Lab Goggles", AccessorySlot.Face, CareerQuestCatalog.AiLabId,
                new Vector2(-0.04f, 0.06f), 0.82f, 3),
            Station("accessory.chef_hat", "Chef Hat", AccessorySlot.Head, CareerQuestCatalog.CommunityKitchenId,
                new Vector2(0f, 0.12f), 1.1f, 3),
            Station("accessory.microphone", "Microphone", AccessorySlot.Hand, CareerQuestCatalog.MusicStudioId,
                new Vector2(0.02f, 0.02f), 0.78f, 3),
            Station("accessory.care_cape", "Care Cape", AccessorySlot.Back, CareerQuestCatalog.VetClinicId,
                new Vector2(0f, 0.04f), 1.25f, -2),
            Station("accessory.sketchbook", "Sketchbook", AccessorySlot.Hand, CareerQuestCatalog.GameStudioId,
                new Vector2(0.02f, -0.02f), 0.86f, 3),
            Station("accessory.weather_goggles", "Weather Goggles", AccessorySlot.Face, CareerQuestCatalog.WeatherLabId,
                new Vector2(-0.04f, 0.06f), 0.82f, 3),
            Station("accessory.mission_patch", "Mission Patch", AccessorySlot.Torso, CareerQuestCatalog.SpaceportId,
                new Vector2(-0.16f, 0.06f), 0.6f, 2),
            Station("accessory.press_badge", "Press Badge", AccessorySlot.Torso, CareerQuestCatalog.NewsroomId,
                new Vector2(-0.16f, 0.08f), 0.58f, 2),
            Station("accessory.green_hardhat", "Green Hardhat", AccessorySlot.Head, CareerQuestCatalog.GreenCityId,
                new Vector2(0f, 0.08f), 1.05f, 3),

            Milestone("accessory.badge_sash", "Badge Sash", AccessorySlot.Sash, 3, false,
                new Vector2(0f, 0f), 1.2f, 1),
            Milestone("accessory.explorer_cape", "Explorer Cape", AccessorySlot.Back, 5, false,
                new Vector2(0f, 0.04f), 1.3f, -2),
            Milestone("accessory.star_robe", "Star Robe", AccessorySlot.Torso, 8, true,
                new Vector2(0f, -0.12f), 1.35f, -1),
            Milestone("accessory.reveal_flourish", "Reveal Flourish", AccessorySlot.Back, 10, true,
                new Vector2(0f, 0.1f), 1.45f, -3)
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

        private static AccessoryDefinition Station(
            string id,
            string displayName,
            AccessorySlot slot,
            string stationId,
            Vector2 localOffset,
            float localScale,
            int sortingOffset)
        {
            return new AccessoryDefinition(id, displayName, slot, id, localOffset, localScale, sortingOffset, false, stationId, 0);
        }

        private static AccessoryDefinition Milestone(
            string id,
            string displayName,
            AccessorySlot slot,
            int completions,
            bool ceremonyOnly,
            Vector2 localOffset,
            float localScale,
            int sortingOffset)
        {
            return new AccessoryDefinition(id, displayName, slot, id, localOffset, localScale, sortingOffset, ceremonyOnly, string.Empty, completions);
        }
    }
}
