using TMPro;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// U4 reward preview chip (design doc Station intro rule: a visible reward
    /// preview before AND during play, in every pacing mode). Shows the seed's
    /// reward-preview line plus the station's core accessory name from
    /// <see cref="AccessoryRewardConfig"/>; accessory ART is intentionally not
    /// rendered here until the U6 accessory pass lands final sprites — the
    /// chip uses shape + copy so no fallback art ever reaches the screen.
    /// </summary>
    public sealed class StationRewardPreview
    {
        public const string PanelName = "StationRewardPreview";
        public const string LineTextName = "StationRewardPreviewLine";
        public const string AccessoryTextName = "StationRewardAccessoryName";
        public const string EarnedStampName = "StationRewardEarnedStamp";

        private static readonly Color PaperColor = new(1f, 0.97f, 0.86f, 0.92f);
        private static readonly Color InkColor = new(0.098f, 0.196f, 0.235f);
        private static readonly Color PathGold = new(0.953f, 0.769f, 0.357f);

        private readonly RectTransform _earnedStamp;

        private StationRewardPreview(RectTransform earnedStamp)
        {
            _earnedStamp = earnedStamp;
        }

        public bool IsEarnedStampVisible => _earnedStamp != null && _earnedStamp.gameObject.activeSelf;

        public static StationRewardPreview Mount(
            Transform parent,
            PartyStationDefinition definition,
            PartyStationSeedDefinition seed,
            Color accent)
        {
            if (parent == null || definition == null || seed == null)
            {
                return null;
            }

            var accessoryName = AccessoryRewardConfig.TryGetForStation(definition.Id, out var accessory)
                ? accessory.DisplayName
                : "Surprise gear";

            var panel = UiBuilder.Panel(parent, PanelName, PaperColor);
            UiBuilder.Place(panel, 470f, 248f, 300f, 64f);
            UiBuilder.Shape(panel, "StationRewardStripe", accent, 0f, -29f, 300f, 6f);
            UiBuilder.Circle(panel, "StationRewardBadge", accent, -126f, 0f, 36f, 36f);

            var name = UiBuilder.Text(panel, AccessoryTextName, accessoryName, 15, TextAnchor.MiddleLeft, InkColor, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(name.rectTransform, 18f, 14f, 240f, 22f);

            var line = UiBuilder.Text(panel, LineTextName, seed.RewardPreviewLine, 11, TextAnchor.MiddleLeft, InkColor);
            UiBuilder.Place(line.rectTransform, 18f, -10f, 240f, 22f);
            line.enableAutoSizing = true;
            line.fontSizeMin = 9;
            line.fontSizeMax = 11;

            // Earned stamp: hidden until completion derives the accessory.
            var stamp = UiBuilder.Circle(panel, EarnedStampName, PathGold, 126f, 16f, 26f, 26f);
            var stampLabel = UiBuilder.Text(stamp, $"{EarnedStampName}Label", "!", 14, TextAnchor.MiddleCenter, InkColor, TypeRole.Display, TypeWeight.Bold);
            UiBuilder.Stretch(stampLabel.rectTransform);
            stamp.gameObject.SetActive(false);

            return new StationRewardPreview(stamp);
        }

        /// <summary>Completion beat: the tease becomes an earned stamp (full spotlight is U6).</summary>
        public void MarkEarned()
        {
            if (_earnedStamp != null)
            {
                _earnedStamp.gameObject.SetActive(true);
            }
        }
    }
}
