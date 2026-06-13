using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// U6 station-end accessory spotlight beat (design doc Reward Spotlight
    /// rule): the brief "You unlocked X!" card shown after a station completes,
    /// reading the just-appended <see cref="RewardEvent"/> and the earned
    /// accessory. It is presentation only (KTD8) — it never derives or writes
    /// session state; it renders the accessory the resolver already earned.
    ///
    /// The accessory ART is still placeholder (AssetCatalog prop tokens until
    /// the U6/U11 art pass), so the spotlight shows a paper card with a tinted
    /// token chip + copy rather than fallback art — no fallback sprite ever
    /// reaches the screen.
    ///
    /// Quiet-mode seam (U9): <see cref="QuietMode"/> suppresses the pulse/auto
    /// timing so U9 can gate motion for the calm party-run path. The card still
    /// renders; only the animated beat is held. The deterministic
    /// <see cref="Tick"/> clock drives the auto-dismiss so tests need no reals.
    /// </summary>
    public sealed class AccessorySpotlightController : MonoBehaviour
    {
        public const string PanelName = "AccessorySpotlightPanel";
        public const string TitleName = "AccessorySpotlightTitle";
        public const string AccessoryNameName = "AccessorySpotlightAccessoryName";
        public const string PracticedLineName = "AccessorySpotlightPracticedLine";
        public const string TokenChipName = "AccessorySpotlightTokenChip";
        public const string ComboSparkName = "AccessorySpotlightComboSpark";

        /// <summary>Spotlight hold before it auto-dismisses (presentation pacing only).</summary>
        public const float HoldSeconds = 3.2f;

        private RectTransform _panel;
        private RectTransform _tokenChip;
        private float _elapsed;
        private bool _active;
        private float _chipBaseScale = 1f;

        /// <summary>U9 seam: when true, the pulse and auto-dismiss timing are held (calm run).</summary>
        public bool QuietMode { get; set; }

        /// <summary>Real-time clock toggle (house idiom). Tests set false and drive Tick.</summary>
        public bool AutoTick { get; set; } = true;

        public bool IsActive => _active;
        public string ShownAccessoryId { get; private set; }

        /// <summary>Test/QA seam: the accessory display name currently shown, or null.</summary>
        public string ShownAccessoryName { get; private set; }

        /// <summary>Test/QA seam: how many combo sparks the beat surfaced (0 when none).</summary>
        public int ShownComboSparkCount { get; private set; }

        /// <summary>
        /// Shows the spotlight for one reward event. The earned accessory is the
        /// event's <see cref="RewardEvent.AccessoryRewardId"/> (already derived by
        /// the station/resolver). Quiet mode is honored from the moment it shows.
        /// </summary>
        public void Show(Transform parent, RewardEvent rewardEvent, bool quietMode = false)
        {
            if (parent == null || rewardEvent == null)
            {
                return;
            }

            QuietMode = quietMode;
            Dismiss();
            _active = true;
            _elapsed = 0f;
            ShownAccessoryId = rewardEvent.AccessoryRewardId;
            ShownComboSparkCount = rewardEvent.ComboSparkIds.Count;

            var accent = AccentFor(rewardEvent.StationId);
            _panel = UiBuilder.Panel(parent, PanelName, QuestStageUi.Paper);
            UiBuilder.Place(_panel, 0f, 150f, 460f, 168f);
            UiBuilder.Shape(_panel, "AccessorySpotlightStripe", accent, 0f, 78f, 460f, 8f);

            var title = UiBuilder.Text(_panel, TitleName, "New gear unlocked!", 22, TextAnchor.MiddleCenter, QuestStageUi.Ink, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(title.rectTransform, 0f, 54f, 420f, 30f);

            // Placeholder accessory token: a tinted chip stands in for the art
            // until the U6/U11 accessory pass lands final sprites.
            _tokenChip = UiBuilder.Circle(_panel, TokenChipName, accent, -168f, -6f, 72f, 72f);
            _chipBaseScale = 1f;

            ShownAccessoryName = AccessoryNameFor(rewardEvent.AccessoryRewardId);
            var accessoryName = UiBuilder.Text(_panel, AccessoryNameName, ShownAccessoryName, 19, TextAnchor.MiddleLeft, QuestStageUi.Ink, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(accessoryName.rectTransform, 30f, 8f, 296f, 26f);

            var practiced = UiBuilder.Text(_panel, PracticedLineName, rewardEvent.PracticedLine(), 14, TextAnchor.MiddleLeft, new Color(0.27f, 0.36f, 0.4f));
            UiBuilder.Place(practiced.rectTransform, 30f, -20f, 296f, 22f);
            practiced.enableAutoSizing = true;
            practiced.fontSizeMin = 11;
            practiced.fontSizeMax = 14;

            // Combo spark hint: a once-per-session "you sparked a combo!" line
            // (full combo reveal is U7). Placeholder copy only; no card art.
            if (rewardEvent.ComboSparkIds.Count > 0)
            {
                var spark = UiBuilder.Text(_panel, ComboSparkName, ComboSparkLine(rewardEvent.ComboSparkIds.Count), 13, TextAnchor.MiddleCenter, QuestStageUi.PathGold, TypeRole.Body, TypeWeight.SemiBold);
                UiBuilder.Place(spark.rectTransform, 0f, -62f, 420f, 22f);
            }
        }

        /// <summary>Deterministic clock seam — pulse + auto-dismiss (held in quiet mode).</summary>
        public void Tick(float deltaSeconds)
        {
            if (!_active || _panel == null || deltaSeconds <= 0f)
            {
                return;
            }

            if (QuietMode)
            {
                // Calm run: the card stays put with no motion and no auto-pop.
                return;
            }

            _elapsed += deltaSeconds;

            if (_tokenChip != null)
            {
                var pulse = _chipBaseScale + Mathf.Sin(_elapsed * 6f) * 0.06f;
                _tokenChip.localScale = new Vector3(pulse, pulse, 1f);
            }

            if (_elapsed >= HoldSeconds)
            {
                Dismiss();
            }
        }

        /// <summary>Tears down the spotlight card (auto-dismiss, route change, or replace).</summary>
        public void Dismiss()
        {
            if (_panel != null)
            {
                Destroy(_panel.gameObject);
                _panel = null;
            }

            _tokenChip = null;
            _active = false;
        }

        private static string AccessoryNameFor(string accessoryId)
        {
            return AccessoryRewardConfig.TryGetById(accessoryId, out var accessory)
                ? accessory.DisplayName
                : "Surprise gear";
        }

        private static string ComboSparkLine(int sparkCount)
        {
            return sparkCount == 1 ? "You sparked a new combo!" : "You sparked new combos!";
        }

        private static Color AccentFor(string stationId)
        {
            if (!string.IsNullOrEmpty(stationId)
                && CareerQuestCatalog.TryGetById(stationId, out var entry)
                && AssetCatalog.TryGetDefinition(entry.BadgeArtKey, out var badge))
            {
                return badge.PrimaryColor;
            }

            return QuestStageUi.PathGold;
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            Dismiss();
        }
    }
}
