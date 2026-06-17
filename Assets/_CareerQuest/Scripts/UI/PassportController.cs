using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    /// <summary>
    /// U6 Quest Passport: a four-tab book — Badges, Gear, Combos, Results — all
    /// rendered from session-DERIVED state (best results, the accessory
    /// resolver, combo eligibility, and the reward-event log). It is
    /// presentation only (KTD8): it reads the session, never writes scoring.
    ///
    /// Locked-entry rule (R12/privacy): a station a kid has not completed shows
    /// a dimmed slot and NEVER a seed choice — replay seed remixes only exist
    /// for stations already completed. Completed entries replay through the
    /// app's normal routing (the same entry points the hub doors use), so the
    /// passport adds no second navigation path.
    ///
    /// The Combos tab shows once-per-session SPARK placeholders for combos that
    /// became eligible (pure pair check over completed stations); the full
    /// CareerComboResolver primary-combo reveal is U7. All copy follows the
    /// early-reader rules (PartyStationValidator-safe, short, strength-framed).
    /// </summary>
    public sealed class PassportController : MonoBehaviour
    {
        public const string PanelName = "PassportPanel";
        public const string BookName = "PassportBook";
        public const string TabPrefix = "PassportTab_";
        public const string EntryPrefix = "PassportEntry_";
        public const string ReplayButtonPrefix = "PassportReplay_";
        public const string SeedChoiceObjectSuffix = "SeedChoice";
        public const string CampusButtonName = "PassportCampusButton";

        public enum PassportPage
        {
            Badges,
            Gear,
            Combos,
            Results
        }

        private static readonly PassportPage[] Pages =
        {
            PassportPage.Badges,
            PassportPage.Gear,
            PassportPage.Combos,
            PassportPage.Results
        };

        private const float BadgeGearGridStartY = 116f;

        // U11 (Gate B simplify Finding 4): locked-slot colors moved to the shared
        // QuestStageUi tokens (they were identical here and in the gallery).

        private Transform _parent;
        private GameSession _session;
        private CareerQuestApp _app;
        private RectTransform _book;
        private RectTransform _pageRoot;

        public PassportPage CurrentPage { get; private set; } = PassportPage.Badges;

        public void Render(Transform parent, GameSession session, CareerQuestApp app)
        {
            Render(parent, session, app, PassportPage.Badges);
        }

        public void Render(Transform parent, GameSession session, CareerQuestApp app, PassportPage page)
        {
            if (parent == null || session == null)
            {
                return;
            }

            _parent = parent;
            _session = session;
            _app = app;
            CurrentPage = page;

            var panel = UiBuilder.FullPanel(parent, PanelName, new Color(0.92f, 0.86f, 0.72f, 0.35f));

            _book = UiBuilder.Panel(panel, BookName, QuestStageUi.Paper);
            UiBuilder.Place(_book, 0f, 10f, 940f, 560f);

            var spine = UiBuilder.Panel(_book, "PassportSpine", QuestStageUi.PaperShadow);
            UiBuilder.Place(spine, -440f, 0f, 32f, 540f);

            var title = UiBuilder.Text(_book, "PassportTitle", "Quest Passport", 38, TextAnchor.MiddleLeft, QuestStageUi.Ink, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(title.rectTransform, -150f, 232f, 560f, 48f);

            var seal = UiBuilder.Text(_book, "PassportSeal", $"{_session.UniqueCompletedGames} done", 18, TextAnchor.MiddleRight, new Color(0.25f, 0.32f, 0.36f));
            UiBuilder.Place(seal.rectTransform, 320f, 232f, 160f, 30f);

            MountTabs();

            _pageRoot = UiBuilder.Panel(_book, "PassportPageRoot", new Color(1f, 1f, 1f, 0f));
            UiBuilder.Place(_pageRoot, 16f, -16f, 860f, 420f);
            RenderPage();

            var campus = UiBuilder.Button(_book, CampusButtonName, "Campus", () => _app?.ShowCampus());
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 360f, -250f, 200f, 52f);
            QuestStageUi.StyleSecondaryButton(campus);
        }

        /// <summary>
        /// Tab switch seam (the tab buttons and tests share it). Routes through
        /// the app so the full surface teardown (ResetRoot) runs and the debug
        /// overlay re-attaches — a stale page's buttons/entries never leak into
        /// the next page. Falls back to a self-contained re-render (clearing the
        /// parent first) when there is no app (direct-render/test usage).
        /// </summary>
        public void ShowPage(PassportPage page)
        {
            if (_session == null)
            {
                return;
            }

            if (_app != null)
            {
                _app.ShowPassport(page);
                return;
            }

            if (_parent == null)
            {
                return;
            }

            UiBuilder.Clear(_parent);
            Render(_parent, _session, null, page);
        }

        private void MountTabs()
        {
            for (var i = 0; i < Pages.Length; i++)
            {
                var page = Pages[i];
                var x = -250f + i * 165f;
                var button = UiBuilder.Button(_book, $"{TabPrefix}{page}", PageLabel(page), () => ShowPage(page));
                UiBuilder.Place(button.GetComponent<RectTransform>(), x, 188f, 150f, 40f);
                if (page == CurrentPage)
                {
                    QuestStageUi.StylePrimaryButton(button);
                }
                else
                {
                    QuestStageUi.StyleSecondaryButton(button);
                }
            }
        }

        private void RenderPage()
        {
            switch (CurrentPage)
            {
                case PassportPage.Badges:
                    RenderBadges();
                    break;
                case PassportPage.Gear:
                    RenderGear();
                    break;
                case PassportPage.Combos:
                    RenderCombos();
                    break;
                case PassportPage.Results:
                    RenderResults();
                    break;
            }
        }

        // ------------------------------------------------------------------
        // Badges: every catalog station; completed entries replay, locked hide.
        // ------------------------------------------------------------------

        private void RenderBadges()
        {
            var entries = CareerQuestCatalog.AllWithPartyStations.ToList();
            var columns = 5;
            var startX = -340f;
            var startY = BadgeGearGridStartY;
            var stepX = 170f;
            var stepY = -132f;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var column = i % columns;
                var row = i / columns;
                var x = startX + column * stepX;
                var y = startY + row * stepY;
                MountBadgeEntry(entry, x, y);
            }
        }

        private void MountBadgeEntry(CatalogEntry entry, float x, float y)
        {
            var earned = _session.GetBestResult(entry.Id) != null
                || _session.CompletedActivityIds.Contains(entry.Id);

            var group = MountEntryGroup($"{EntryPrefix}{entry.Id}", x, y);

            var careerColor = AssetCatalog.TryGetDefinition(entry.BadgeArtKey, out var badge)
                ? badge.PrimaryColor
                : QuestStageUi.PathGold;

            if (earned)
            {
                UiBuilder.Circle(group, $"{entry.Id}BadgeRing", careerColor, 0f, 32f, 66f, 66f);
                UiBuilder.Circle(group, $"{entry.Id}BadgeFace", QuestStageUi.Paper, 0f, 32f, 54f, 54f);

                var sprite = AssetCatalog.SpriteFor(entry.BadgeArtKey);
                if (sprite != null)
                {
                    AddIcon(group, $"{entry.Id}BadgeIcon", sprite, 0f, 32f, 42f);
                }

                var label = UiBuilder.Text(group, $"{entry.Id}BadgeLabel", entry.BadgeName, 12, TextAnchor.MiddleCenter, QuestStageUi.Ink);
                UiBuilder.Place(label.rectTransform, 0f, -18f, 156f, 24f);

                // Completed entries replay through the SAME routing the hub doors
                // use — never a second navigation path, never a seed picker here.
                var replay = UiBuilder.Button(group, $"{ReplayButtonPrefix}{entry.Id}", "Play again", () => Replay(entry));
                UiBuilder.Place(replay.GetComponent<RectTransform>(), 0f, -46f, 140f, 30f);
                QuestStageUi.StyleSecondaryButton(replay);
            }
            else
            {
                // Locked slot: dimmed, no badge name, and explicitly NO seed
                // choice (a kid cannot peek at remixes for an unplayed station).
                UiBuilder.Circle(group, $"{entry.Id}BadgeRing", QuestStageUi.LockedRing, 0f, 32f, 66f, 66f);
                UiBuilder.Circle(group, $"{entry.Id}BadgeFace", QuestStageUi.LockedFace, 0f, 32f, 54f, 54f);
                var hint = UiBuilder.Text(group, $"{entry.Id}BadgeLockHint", "?", 28, TextAnchor.MiddleCenter, QuestStageUi.LockedInk, TypeRole.Display, TypeWeight.SemiBold);
                UiBuilder.Place(hint.rectTransform, 0f, 32f, 40f, 40f);

                var label = UiBuilder.Text(group, $"{entry.Id}BadgeLabel", "Locked", 12, TextAnchor.MiddleCenter, QuestStageUi.LockedInk);
                UiBuilder.Place(label.rectTransform, 0f, -18f, 156f, 24f);
            }
        }

        // ------------------------------------------------------------------
        // Gear: earned accessories (resolver), campus-visible vs. ceremony-only.
        // ------------------------------------------------------------------

        private void RenderGear()
        {
            var earned = AccessoryResolver.ResolveEarned(_session);
            if (earned.Count == 0)
            {
                MountEmpty("PassportGearEmpty", "Finish a quest to earn your first gear.");
                return;
            }

            // Campus-visible set (newest per slot, no ceremony-only) marks which
            // earned pieces actually show on the avatar in normal play.
            var campusVisibleIds = new HashSet<string>(
                AccessoryResolver.ResolveVisible(earned, ceremonyContext: false).Select(accessory => accessory.Id));

            // Earn-order, de-duplicated (U11 Finding 2: one shared resolver helper
            // instead of a hand-rolled dedup loop duplicated across surfaces).
            var distinct = AccessoryResolver.DistinctEarned(_session, newestFirst: false);

            var columns = 5;
            var startX = -340f;
            var startY = BadgeGearGridStartY;
            var stepX = 170f;
            var stepY = -132f;
            for (var i = 0; i < distinct.Count; i++)
            {
                var accessory = distinct[i];
                var column = i % columns;
                var row = i / columns;
                var x = startX + column * stepX;
                var y = startY + row * stepY;
                MountGearEntry(accessory, campusVisibleIds.Contains(accessory.Id), x, y);
            }
        }

        private void MountGearEntry(AccessoryDefinition accessory, bool campusVisible, float x, float y)
        {
            var group = MountEntryGroup($"{EntryPrefix}{accessory.Id}", x, y);

            // U11: the accessory art pass landed (CareerQuestAccessoryArtBuilder),
            // so the gear page shows the REAL accessory sprite on a soft identity
            // chip — not a placeholder token.
            var chipColor = AssetCatalog.TryGetDefinition(accessory.SpriteAssetId, out var definition)
                ? definition.PrimaryColor
                : QuestStageUi.PathGold;
            UiBuilder.Circle(group, $"{accessory.Id}GearChip", Color.Lerp(chipColor, QuestStageUi.Paper, 0.55f), 0f, 32f, 62f, 62f);

            // Final-art-only (DESIGN: no fallback art on a player surface) — the
            // identity chip alone stands in until the art pass has run.
            var sprite = AssetCatalog.SpriteFor(accessory.SpriteAssetId);
            if (sprite != null && AssetCatalog.IsFinalArtSprite(sprite))
            {
                AddIcon(group, $"{accessory.Id}GearIcon", sprite, 0f, 32f, 46f);
            }

            var name = UiBuilder.Text(group, $"{accessory.Id}GearName", accessory.DisplayName, 13, TextAnchor.MiddleCenter, QuestStageUi.Ink, TypeRole.Body, TypeWeight.SemiBold);
            UiBuilder.Place(name.rectTransform, 0f, -20f, 158f, 24f);

            var tag = accessory.CeremonyOnly
                ? "Reveal only"
                : campusVisible ? "Worn now" : "In your bag";
            var tagLabel = UiBuilder.Text(group, $"{accessory.Id}GearTag", tag, 11, TextAnchor.MiddleCenter, new Color(0.27f, 0.36f, 0.4f));
            UiBuilder.Place(tagLabel.rectTransform, 0f, -44f, 158f, 22f);
        }

        // ------------------------------------------------------------------
        // Combos: spark placeholders from eligibility (full reveal is U7).
        // ------------------------------------------------------------------

        private void RenderCombos()
        {
            var eligible = RewardEventLog.EligibleComboIds(_session.CompletedActivityIds);
            if (eligible.Count == 0)
            {
                MountEmpty("PassportCombosEmpty", "Finish two matching quests to spark a combo.");
                return;
            }

            var shownSparks = _session.RewardLog.ShownComboSparkIds;
            var startY = 150f;
            var stepY = -64f;
            var maxRows = 6;
            for (var i = 0; i < eligible.Count && i < maxRows; i++)
            {
                if (!CareerComboConfig.TryGetById(eligible[i], out var combo))
                {
                    continue;
                }

                var y = startY + i * stepY;
                var row = UiBuilder.Panel(_pageRoot, $"PassportComboRow{i}", Color.Lerp(QuestStageUi.Paper, QuestStageUi.PaperShadow, 0.4f));
                UiBuilder.Place(row, 0f, y, 760f, 54f);

                var sparked = shownSparks.Contains(combo.Id);
                UiBuilder.Circle(row, $"PassportComboSpark{i}", sparked ? QuestStageUi.PathGold : QuestStageUi.LockedRing, -344f, 0f, 30f, 30f);

                var name = UiBuilder.Text(row, $"PassportComboName{i}", combo.DisplayName, 17, TextAnchor.MiddleLeft, QuestStageUi.Ink, TypeRole.Display, TypeWeight.SemiBold);
                UiBuilder.Place(name.rectTransform, -150f, 10f, 420f, 24f);

                var blurb = UiBuilder.Text(row, $"PassportComboBlurb{i}", sparked ? "Sparked! See it at the reveal." : "Ready to spark at the reveal.", 12, TextAnchor.MiddleLeft, new Color(0.27f, 0.36f, 0.4f));
                UiBuilder.Place(blurb.rectTransform, -150f, -12f, 480f, 22f);
            }
        }

        // ------------------------------------------------------------------
        // Results: recent reward events (seed-aware micro-results).
        // ------------------------------------------------------------------

        private void RenderResults()
        {
            var recent = _session.RewardLog.Recent(6);
            if (recent.Count == 0)
            {
                MountEmpty("PassportResultsEmpty", "Your finished quests will show up here.");
                return;
            }

            var startY = 150f;
            var stepY = -64f;
            for (var i = 0; i < recent.Count; i++)
            {
                var rewardEvent = recent[i];
                var y = startY + i * stepY;
                var row = UiBuilder.Panel(_pageRoot, $"PassportResultRow{i}", Color.Lerp(QuestStageUi.Paper, QuestStageUi.PaperShadow, 0.4f));
                UiBuilder.Place(row, 0f, y, 760f, 54f);

                var stationName = StationDisplayName(rewardEvent.StationId);
                var name = UiBuilder.Text(row, $"PassportResultName{i}", stationName, 16, TextAnchor.MiddleLeft, QuestStageUi.Ink, TypeRole.Display, TypeWeight.SemiBold);
                UiBuilder.Place(name.rectTransform, -150f, 10f, 420f, 24f);

                var practiced = UiBuilder.Text(row, $"PassportResultPracticed{i}", rewardEvent.PracticedLine(), 12, TextAnchor.MiddleLeft, new Color(0.27f, 0.36f, 0.4f));
                UiBuilder.Place(practiced.rectTransform, -150f, -12f, 480f, 22f);
                practiced.enableAutoSizing = true;
                practiced.fontSizeMin = 10;
                practiced.fontSizeMax = 12;

                var tier = UiBuilder.Text(row, $"PassportResultTier{i}", rewardEvent.Tier == CompletionTier.Degree ? "Quest done" : "Practiced", 12, TextAnchor.MiddleRight, rewardEvent.Tier == CompletionTier.Degree ? QuestStageUi.PathGold : new Color(0.27f, 0.36f, 0.4f), TypeRole.Body, TypeWeight.SemiBold);
                UiBuilder.Place(tier.rectTransform, 320f, 0f, 120f, 24f);
            }
        }

        // ------------------------------------------------------------------
        // Shared helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Replays a completed entry through the app's normal routing — Party
        /// Pack stations by station id, core rooms by their dedicated entry —
        /// exactly the paths the hub doors use (no second navigation path).
        /// </summary>
        private void Replay(CatalogEntry entry)
        {
            if (_app == null || entry == null)
            {
                return;
            }

            if (CareerQuestCatalog.IsPartyStationId(entry.Id))
            {
                _app.ShowPartyStation(entry.Id);
                return;
            }

            switch (entry.Route)
            {
                case ActivityRoute.DesignBuild:
                    _app.ShowDesignBuild(false);
                    break;
                case ActivityRoute.HealthHero:
                    _app.ShowHealthHero();
                    break;
                case ActivityRoute.LogicCourt:
                    _app.ShowLogicCourt();
                    break;
                default:
                    _app.ShowCampus();
                    break;
            }
        }

        private RectTransform MountEntryGroup(string name, float x, float y)
        {
            var group = UiBuilder.Panel(_pageRoot, name, new Color(1f, 1f, 1f, 0f));
            UiBuilder.Place(group, x, y, 160f, 128f);
            return group;
        }

        private void MountEmpty(string name, string message)
        {
            var empty = UiBuilder.Text(_pageRoot, name, message, 18, TextAnchor.MiddleCenter, QuestStageUi.Ink);
            UiBuilder.Place(empty.rectTransform, 0f, 60f, 720f, 60f);
        }

        private static void AddIcon(Transform parent, string name, Sprite sprite, float x, float y, float size)
        {
            var iconObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(parent, false);
            var icon = iconObject.GetComponent<Image>();
            icon.sprite = sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UiBuilder.Place(icon.rectTransform, x, y, size, size);
        }

        private static string StationDisplayName(string stationId)
        {
            if (!string.IsNullOrEmpty(stationId) && CareerQuestCatalog.TryGetById(stationId, out var entry))
            {
                return entry.DisplayName;
            }

            return PartyStationDefinitions.TryGetById(stationId, out var definition)
                ? definition.DisplayName
                : "Quest";
        }

        private static string PageLabel(PassportPage page)
        {
            return page switch
            {
                PassportPage.Badges => "Badges",
                PassportPage.Gear => "Gear",
                PassportPage.Combos => "Combos",
                _ => "Results"
            };
        }
    }
}
