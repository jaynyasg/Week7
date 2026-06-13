using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    /// <summary>
    /// U11 passport polish: the gallery reads as a sticker/passport book per
    /// DESIGN.md — paper page with a stitched spine and stacked page edges,
    /// badge chips as stickers (earned = full career color + gold stamp ring,
    /// locked = dimmed slot), and skill tallies styled as paper pills. All
    /// object names and the render contract are unchanged from the pre-polish
    /// controller (AchievementGalleryPanel, RevealButton, GalleryCampusButton).
    /// </summary>
    public class AchievementGalleryController : MonoBehaviour
    {
        // U11 (Gate B simplify Finding 4): locked-slot colors moved to the shared
        // QuestStageUi tokens (they were identical here and in PassportController).

        public void Render(Transform parent, GameSession session, CareerQuestApp app)
        {
            var panel = UiBuilder.FullPanel(parent, "AchievementGalleryPanel", new Color(0.92f, 0.86f, 0.72f, 0.35f));

            // Stacked page edges behind the book sell the "book" silhouette.
            var pageEdgeB = UiBuilder.Panel(panel, "GalleryPageEdgeB", QuestStageUi.PaperShadow);
            UiBuilder.Place(pageEdgeB, 10f, 0f, 920f, 560f);
            var pageEdgeA = UiBuilder.Panel(panel, "GalleryPageEdgeA", Color.Lerp(QuestStageUi.Paper, QuestStageUi.PaperShadow, 0.35f));
            UiBuilder.Place(pageEdgeA, 5f, 5f, 920f, 560f);

            var book = UiBuilder.Panel(panel, "GalleryPassportBook", QuestStageUi.Paper);
            UiBuilder.Place(book, 0f, 10f, 920f, 560f);

            var spine = UiBuilder.Panel(book, "GalleryPassportSpine", QuestStageUi.PaperShadow);
            UiBuilder.Place(spine, -430f, 0f, 36f, 540f);

            // Stitch dots down the spine (handmade sticker-book feel).
            for (var stitch = 0; stitch < 7; stitch++)
            {
                UiBuilder.Circle(book, $"GallerySpineStitch{stitch}", Color.Lerp(QuestStageUi.PaperShadow, QuestStageUi.Ink, 0.35f), -430f, 228f - stitch * 76f, 8f, 8f);
            }

            var title = UiBuilder.Text(book, "GalleryTitle", "Quest Passport", 44, TextAnchor.MiddleCenter, QuestStageUi.Ink, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(title.rectTransform, 40f, 230f, 760f, 56f);

            // Passport seal, top-right of the page.
            UiBuilder.Circle(book, "GallerySealRing", QuestStageUi.PathGold, 372f, 228f, 64f, 64f);
            UiBuilder.Circle(book, "GallerySealFace", QuestStageUi.Paper, 372f, 228f, 50f, 50f);
            var seal = UiBuilder.Text(book, "GallerySealCount", $"{session.UniqueCompletedGames}", 22, TextAnchor.MiddleCenter, QuestStageUi.Ink, TypeRole.Display, TypeWeight.Bold);
            UiBuilder.Place(seal.rectTransform, 372f, 228f, 50f, 32f);

            var headerBand = UiBuilder.Panel(book, "GalleryHeaderBand", QuestStageUi.PathGold);
            UiBuilder.Place(headerBand, 0f, 196f, 820f, 6f);

            var subtitle = UiBuilder.Text(book, "GallerySubtitle", "Sticker badges from every career room you tried", 18, TextAnchor.MiddleCenter, new Color(0.25f, 0.32f, 0.36f));
            UiBuilder.Place(subtitle.rectTransform, 40f, 172f, 720f, 32f);

            MountBadgeGrid(book, session);
            MountSkillTallies(book, session);
            MountGearRow(book, session);

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
            UiBuilder.Place(reveal.GetComponent<RectTransform>(), -250f, -250f, 250f, 58f);
            QuestStageUi.StylePrimaryButton(reveal);
            reveal.interactable = session.RevealReady;

            // U6: the gallery cross-links to the tabbed passport (Gear/Combos/
            // Results) — the same session-derived surface, richer view.
            var passport = UiBuilder.Button(book, "GalleryPassportButton", "Passport", () => app.ShowPassport());
            UiBuilder.Place(passport.GetComponent<RectTransform>(), 8f, -250f, 220f, 58f);
            QuestStageUi.StyleSecondaryButton(passport);

            var campus = UiBuilder.Button(book, "GalleryCampusButton", "Campus", app.ShowCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 250f, -250f, 220f, 58f);
            QuestStageUi.StyleSecondaryButton(campus);
        }

        /// <summary>
        /// U6 gear row: a compact strip of EARNED accessories (resolver-derived,
        /// newest-first), each an identity chip with the real accessory sprite
        /// (U11 art pass). Empty until the first completion derives gear.
        /// Presentation only (KTD8) — reads the session, never scoring.
        /// </summary>
        private static void MountGearRow(RectTransform book, GameSession session)
        {
            // Newest-first, de-duplicated for a clean strip (the gallery surfaces
            // the most recent gear first). U11 Finding 2: one shared resolver
            // helper, not a hand-rolled dedup loop duplicated across surfaces.
            var distinct = AccessoryResolver.DistinctEarned(session, newestFirst: true);
            if (distinct.Count == 0)
            {
                return;
            }

            // Right-margin vertical stack (clear of the badge grid and the trait
            // pills): a small "gear earned" tally of newest-first token chips.
            var label = UiBuilder.Text(book, "GalleryGearLabel", "Gear", 14, TextAnchor.MiddleCenter, QuestStageUi.Ink, TypeRole.Body, TypeWeight.SemiBold);
            UiBuilder.Place(label.rectTransform, 392f, 150f, 80f, 22f);

            var shown = Mathf.Min(distinct.Count, 4);
            var step = -52f;
            for (var i = 0; i < shown; i++)
            {
                var accessory = distinct[i];
                var chipColor = AssetCatalog.TryGetDefinition(accessory.SpriteAssetId, out var definition)
                    ? definition.PrimaryColor
                    : QuestStageUi.PathGold;
                var y = 118f + i * step;
                UiBuilder.Circle(book, $"GalleryGearChip{i}", Color.Lerp(chipColor, QuestStageUi.Paper, 0.55f), 392f, y, 40f, 40f);

                // U11: the real accessory art sits on the chip (final art landed —
                // CareerQuestAccessoryArtBuilder), so the gear strip reads as the
                // actual gear. Final-art-only (DESIGN: no fallback art reaches a
                // player surface) — the identity chip alone stands in until art.
                var sprite = AssetCatalog.SpriteFor(accessory.SpriteAssetId);
                if (sprite != null && AssetCatalog.IsFinalArtSprite(sprite))
                {
                    var iconObject = new GameObject($"GalleryGearIcon{i}", typeof(RectTransform), typeof(Image));
                    iconObject.transform.SetParent(book, false);
                    var icon = iconObject.GetComponent<Image>();
                    icon.sprite = sprite;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    UiBuilder.Place(icon.rectTransform, 392f, y, 32f, 32f);
                }
            }
        }

        private static void MountBadgeGrid(RectTransform book, GameSession session)
        {
            var entries = CareerQuestCatalog.All.ToList();
            var columns = 4;
            var startX = -250f;
            var startY = 108f;
            var stepX = 170f;
            var stepY = -136f;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var column = i % columns;
                var row = i / columns;
                var x = startX + column * stepX;
                var y = startY + row * stepY;
                var earned = session.GetBestResult(entry.Id) != null;

                // Per-entry sticker group: a slight alternating tilt makes the
                // chips read as hand-placed stickers, not a UI grid.
                var groupObject = new GameObject($"{entry.Id}ChipGroup", typeof(RectTransform));
                groupObject.transform.SetParent(book, false);
                var group = groupObject.GetComponent<RectTransform>();
                UiBuilder.Place(group, x, y, 150f, 130f);
                group.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 2.5f : -2.5f);

                var careerColor = AssetCatalog.TryGetDefinition(entry.BadgeArtKey, out var badgeDefinition)
                    ? badgeDefinition.PrimaryColor
                    : QuestStageUi.PathGold;

                if (earned)
                {
                    // Earned sticker: gold stamp ring → career ring → paper face.
                    UiBuilder.Circle(group, $"{entry.Id}ChipStamp", QuestStageUi.PathGold, 0f, 8f, 106f, 106f);
                    UiBuilder.Circle(group, $"{entry.Id}ChipRing", careerColor, 0f, 8f, 96f, 96f);
                    UiBuilder.Circle(group, $"{entry.Id}Chip", QuestStageUi.Paper, 0f, 8f, 82f, 82f);

                    var sprite = AssetCatalog.SpriteFor(entry.BadgeArtKey);
                    if (sprite != null)
                    {
                        var iconObject = new GameObject($"{entry.Id}ChipIcon", typeof(RectTransform), typeof(Image));
                        iconObject.transform.SetParent(group, false);
                        var icon = iconObject.GetComponent<Image>();
                        icon.sprite = sprite;
                        icon.preserveAspect = true;
                        icon.raycastTarget = false;
                        UiBuilder.Place(icon.rectTransform, 0f, 8f, 64f, 64f);
                    }
                }
                else
                {
                    // Locked slot: dimmed circle waiting for its sticker.
                    UiBuilder.Circle(group, $"{entry.Id}ChipRing", QuestStageUi.LockedRing, 0f, 8f, 96f, 96f);
                    UiBuilder.Circle(group, $"{entry.Id}Chip", QuestStageUi.LockedFace, 0f, 8f, 82f, 82f);
                    var hint = UiBuilder.Text(group, $"{entry.Id}ChipHint", "?", 30, TextAnchor.MiddleCenter, QuestStageUi.LockedInk, TypeRole.Display, TypeWeight.SemiBold);
                    UiBuilder.Place(hint.rectTransform, 0f, 8f, 44f, 44f);
                }

                var label = UiBuilder.Text(
                    group,
                    $"{entry.Id}Badge",
                    earned ? entry.BadgeName : "Locked",
                    12,
                    TextAnchor.MiddleCenter,
                    earned ? QuestStageUi.Ink : QuestStageUi.LockedInk);
                UiBuilder.Place(label.rectTransform, 0f, -56f, 140f, 28f);
            }
        }

        private static void MountSkillTallies(RectTransform book, GameSession session)
        {
            var traits = session.CareerDna.TopTraits(5).ToList();
            if (traits.Count == 0)
            {
                var empty = UiBuilder.Text(book, "TraitSummary", "Play one activity to build Career DNA.", 18, TextAnchor.MiddleCenter, QuestStageUi.Ink);
                UiBuilder.Place(empty.rectTransform, 40f, -150f, 760f, 48f);
                return;
            }

            // Skill tallies as paper pills with a gold count.
            var container = new GameObject("TraitSummary", typeof(RectTransform));
            container.transform.SetParent(book, false);
            var containerRect = container.GetComponent<RectTransform>();
            UiBuilder.Place(containerRect, 40f, -150f, 760f, 44f);

            var pillWidth = 138f;
            var step = 146f;
            var firstX = -(traits.Count - 1) * step * 0.5f;
            for (var i = 0; i < traits.Count; i++)
            {
                var trait = traits[i];
                var pill = UiBuilder.Panel(containerRect, $"TraitPill{i}", Color.Lerp(QuestStageUi.Paper, QuestStageUi.PaperShadow, 0.45f));
                UiBuilder.Place(pill, firstX + i * step, 0f, pillWidth, 34f);

                var pillLabel = UiBuilder.Text(pill, $"TraitPill{i}Label", $"{trait.Trait}  +{trait.Delta}", 15, TextAnchor.MiddleCenter, QuestStageUi.Ink, TypeRole.Body, TypeWeight.SemiBold);
                UiBuilder.Stretch(pillLabel.rectTransform, 6f, 2f);
            }
        }
    }
}
