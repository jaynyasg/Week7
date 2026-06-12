using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    internal static class QuestStageUi
    {
        internal static readonly Color Ink = new(0.098f, 0.196f, 0.235f);
        internal static readonly Color Paper = new(1f, 0.969f, 0.878f);
        internal static readonly Color PaperShadow = new(0.851f, 0.714f, 0.435f);
        internal static readonly Color PathGold = new(0.953f, 0.769f, 0.357f);
        internal static readonly Color StageNight = new(0.08f, 0.11f, 0.22f);
        internal static readonly Color Spotlight = new(1f, 0.922f, 0.639f, 0.35f);
        internal static readonly Color WorkshopTeal = new(0.055f, 0.42f, 0.435f);

        internal static void MountStageBackdrop(RectTransform parent, bool unlocked)
        {
            var curtainLeft = UiBuilder.Panel(parent, "StageCurtainLeft", new Color(0.45f, 0.12f, 0.18f, 0.92f));
            UiBuilder.Place(curtainLeft, -520f, 0f, 240f, 720f);

            var curtainRight = UiBuilder.Panel(parent, "StageCurtainRight", new Color(0.45f, 0.12f, 0.18f, 0.92f));
            UiBuilder.Place(curtainRight, 520f, 0f, 240f, 720f);

            var floor = UiBuilder.Panel(parent, "StageFloor", new Color(0.18f, 0.14f, 0.1f, 0.88f));
            UiBuilder.Place(floor, 0f, -280f, 760f, 120f);

            UiBuilder.Circle(parent, "StageSpot", unlocked ? PathGold : new Color(0.35f, 0.32f, 0.4f), 0f, -210f, 420f, 48f);

            if (unlocked)
            {
                var beamLeft = UiBuilder.Panel(parent, "StageBeamLeft", Spotlight);
                UiBuilder.Place(beamLeft, -180f, 120f, 120f, 360f);
                beamLeft.localRotation = Quaternion.Euler(0f, 0f, 18f);

                var beamRight = UiBuilder.Panel(parent, "StageBeamRight", Spotlight);
                UiBuilder.Place(beamRight, 180f, 120f, 120f, 360f);
                beamRight.localRotation = Quaternion.Euler(0f, 0f, -18f);
            }
        }

        internal static void MountBadgeSlots(RectTransform parent, GameSession session, float y)
        {
            var earned = CareerQuestCatalog.All
                .Where(entry => session.GetBestResult(entry.Id) != null)
                .Take(3)
                .ToList();

            for (var slot = 0; slot < 3; slot++)
            {
                var x = -180f + slot * 180f;
                var filled = slot < session.UniqueCompletedGames;
                var entry = slot < earned.Count ? earned[slot] : null;
                MountBadgeSlot(parent, slot, x, y, filled, entry);
            }

            var progressLabel = UiBuilder.Text(
                parent,
                "RevealBadgeProgress",
                $"{session.UniqueCompletedGames}/3 quest badges collected",
                22,
                TextAnchor.MiddleCenter,
                filledProgressColor(session));
            UiBuilder.Place(progressLabel.rectTransform, 0f, y - 78f, 520f, 36f);

            var progressTrack = UiBuilder.Panel(parent, "RevealProgressTrack", new Color(0.2f, 0.22f, 0.28f, 0.55f));
            UiBuilder.Place(progressTrack, 0f, y - 108f, 420f, 14f);

            var fillWidth = Mathf.Max(24f, 420f * (session.UniqueCompletedGames / 3f));
            var progressFill = UiBuilder.Panel(parent, "RevealProgressFill", PathGold);
            UiBuilder.Place(progressFill, -210f + fillWidth * 0.5f, y - 108f, fillWidth, 14f);
        }

        internal static void StylePrimaryButton(Button button)
        {
            button.GetComponent<Image>().color = WorkshopTeal;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontSize = 24;
                label.color = Color.white;
            }
        }

        internal static void StyleSecondaryButton(Button button)
        {
            button.GetComponent<Image>().color = new Color(0.09f, 0.31f, 0.42f);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontSize = 22;
            }
        }

        private static Color filledProgressColor(GameSession session)
        {
            // U11 owner-review fix: the in-progress state used a pale blue that
            // disappeared on the cream locked card — Ink keeps it readable.
            return session.RevealReady ? PathGold : Ink;
        }

        private static void MountBadgeSlot(RectTransform parent, int index, float x, float y, bool filled, CatalogEntry entry)
        {
            var slotName = $"RevealBadgeSlot{index}";
            var ringColor = filled ? PathGold : new Color(0.55f, 0.58f, 0.64f);
            UiBuilder.Circle(parent, $"{slotName}Ring", ringColor, x, y, 96f, 96f);

            UiBuilder.Circle(parent, slotName, filled ? Paper : new Color(0.28f, 0.3f, 0.36f), x, y, 78f, 78f);

            if (filled && entry != null)
            {
                var badgeSprite = AssetCatalog.SpriteFor(entry.BadgeArtKey);
                if (badgeSprite != null)
                {
                    var iconObject = new GameObject($"{slotName}Icon", typeof(RectTransform), typeof(Image));
                    iconObject.transform.SetParent(parent, false);
                    var icon = iconObject.GetComponent<Image>();
                    icon.sprite = badgeSprite;
                    icon.preserveAspect = true;
                    icon.color = Color.white;
                    UiBuilder.Place(icon.rectTransform, x, y, 56f, 56f);
                }

                var stamp = UiBuilder.Text(parent, $"{slotName}Label", entry.BadgeName, 12, TextAnchor.MiddleCenter, Ink);
                UiBuilder.Place(stamp.rectTransform, x, y - 58f, 120f, 28f);
            }
            else
            {
                var lockLabel = UiBuilder.Text(parent, $"{slotName}Lock", "?", 34, TextAnchor.MiddleCenter, new Color(0.75f, 0.78f, 0.82f));
                UiBuilder.Place(lockLabel.rectTransform, x, y, 40f, 40f);
            }
        }
    }
}
