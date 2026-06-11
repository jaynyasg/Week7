using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    public class AchievementGalleryController : MonoBehaviour
    {
        public void Render(Transform parent, GameSession session, CareerQuestApp app)
        {
            var panel = UiBuilder.FullPanel(parent, "AchievementGalleryPanel", new Color(0.92f, 0.86f, 0.72f, 0.35f));

            var book = UiBuilder.Panel(panel, "GalleryPassportBook", QuestStageUi.Paper);
            UiBuilder.Place(book, 0f, 10f, 920f, 560f);

            var spine = UiBuilder.Panel(book, "GalleryPassportSpine", QuestStageUi.PaperShadow);
            UiBuilder.Place(spine, -430f, 0f, 36f, 540f);

            var title = UiBuilder.Text(book, "GalleryTitle", "Quest Passport", 44, TextAnchor.MiddleCenter, QuestStageUi.Ink);
            UiBuilder.Place(title.rectTransform, 40f, 230f, 760f, 56f);

            var subtitle = UiBuilder.Text(book, "GallerySubtitle", "Sticker badges from every career room you tried", 18, TextAnchor.MiddleCenter, new Color(0.25f, 0.32f, 0.36f));
            UiBuilder.Place(subtitle.rectTransform, 40f, 190f, 720f, 32f);

            MountBadgeGrid(book, session);

            var traits = string.Join("   ·   ", session.CareerDna.TopTraits(5).Select(trait => $"{trait.Trait} +{trait.Delta}"));
            if (string.IsNullOrWhiteSpace(traits))
            {
                traits = "Play one activity to build Career DNA.";
            }

            var traitText = UiBuilder.Text(book, "TraitSummary", traits, 18, TextAnchor.MiddleCenter, QuestStageUi.Ink);
            UiBuilder.Place(traitText.rectTransform, 40f, -150f, 760f, 48f);

            var revealProgress = UiBuilder.Text(
                book,
                "RevealProgress",
                session.RevealReady
                    ? "Reveal unlocked! All 3 quest badges collected."
                    : $"Reveal unlock: {session.UniqueCompletedGames}/3 unique quest badges",
                20,
                TextAnchor.MiddleCenter,
                session.RevealReady ? QuestStageUi.PathGold : QuestStageUi.Ink);
            UiBuilder.Place(revealProgress.rectTransform, 40f, -195f, 680f, 36f);

            var reveal = UiBuilder.Button(book, "RevealButton", session.RevealReady ? "Reveal Careers!" : "Reveal (Locked)", app.ShowReveal);
            UiBuilder.Place(reveal.GetComponent<RectTransform>(), -120f, -250f, 260f, 58f);
            QuestStageUi.StylePrimaryButton(reveal);
            reveal.interactable = session.RevealReady;

            var campus = UiBuilder.Button(book, "GalleryCampusButton", "Campus", app.ShowCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 160f, -250f, 220f, 58f);
            QuestStageUi.StyleSecondaryButton(campus);
        }

        private static void MountBadgeGrid(RectTransform book, GameSession session)
        {
            var entries = CareerQuestCatalog.All.ToList();
            var columns = 4;
            var startX = -250f;
            var startY = 120f;
            var stepX = 170f;
            var stepY = -130f;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var column = i % columns;
                var row = i / columns;
                var x = startX + column * stepX;
                var y = startY + row * stepY;
                var result = session.GetBestResult(entry.Id);
                var earned = result != null;

                var chip = UiBuilder.Circle(book, $"{entry.Id}Chip", earned ? QuestStageUi.Paper : new Color(0.88f, 0.9f, 0.93f), x, y, 88f, 88f);
                var ring = UiBuilder.Circle(book, $"{entry.Id}ChipRing", earned ? QuestStageUi.PathGold : new Color(0.7f, 0.74f, 0.78f), x, y, 96f, 96f);
                ring.SetAsFirstSibling();

                if (earned)
                {
                    var sprite = AssetCatalog.SpriteFor(entry.BadgeArtKey);
                    if (sprite != null)
                    {
                        var iconObject = new GameObject($"{entry.Id}ChipIcon", typeof(RectTransform), typeof(Image));
                        iconObject.transform.SetParent(book, false);
                        var icon = iconObject.GetComponent<Image>();
                        icon.sprite = sprite;
                        icon.preserveAspect = true;
                        UiBuilder.Place(icon.rectTransform, x, y, 52f, 52f);
                    }
                }

                var label = UiBuilder.Text(
                    book,
                    $"{entry.Id}Badge",
                    earned ? entry.BadgeName : "Locked",
                    12,
                    TextAnchor.MiddleCenter,
                    earned ? QuestStageUi.Ink : new Color(0.45f, 0.48f, 0.52f));
                UiBuilder.Place(label.rectTransform, x, y - 62f, 140f, 28f);
            }
        }
    }
}
