using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// U9 (R18) the guided "Party Run" presenter. It renders the demo cadence
    /// over a campus visit when a run is active: the round intro (current
    /// station + seed premise/reward preview), the progress strip, the previous
    /// round's reward preview, the campus-evolution beat hook, and the
    /// Continue / Quit controls — handing off to reveal once the run is ready.
    ///
    /// It is a PRESENTER over session-only state (KTD7): it reads
    /// <see cref="PartyRunState"/>, the <see cref="RewardEventLog"/>, and the
    /// <see cref="RevealSynthesis"/> snapshot, and never RE-derives reward,
    /// family, combo, or readiness facts (the reveal handoff mirrors the
    /// existing >= 3 unique-completion gate via the synthesis snapshot). It
    /// never forces in-game station order: Continue routes to the run's current
    /// round, but the campus doors stay free-choice the whole time.
    ///
    /// Mounting is idempotent and a campus-only surface (it lives under the
    /// canvas root the campus rebuilds, so a route change tears it down). All
    /// control paths are seams the buttons AND the PlayMode tests share.
    /// </summary>
    public sealed class PartyRunPresenter : MonoBehaviour
    {
        public const string PanelName = "PartyRunPanel";
        public const string RoundIntroName = "PartyRunRoundIntro";
        public const string RewardPreviewName = "PartyRunRewardPreview";
        public const string ProgressStripName = "PartyRunProgressStrip";
        public const string StepCellPrefix = "PartyRunStep_";
        public const string ContinueButtonName = "PartyRunContinueButton";
        public const string QuitButtonName = "PartyRunQuitButton";
        public const string RevealButtonName = "PartyRunRevealButton";

        private static readonly Color Paper = new(1f, 0.969f, 0.878f, 0.97f);
        private static readonly Color Ink = new(0.098f, 0.196f, 0.235f);
        private static readonly Color DoneGreen = new(0.22f, 0.55f, 0.34f);
        private static readonly Color CurrentGold = new(0.953f, 0.769f, 0.357f);
        private static readonly Color UpcomingGrey = new(0.7f, 0.72f, 0.74f);

        private CareerQuestApp _app;
        private GameSession _session;
        private RectTransform _panel;

        public bool IsMounted => _panel != null;

        /// <summary>House attach idiom — CareerQuestApp routes here.</summary>
        public static PartyRunPresenter AttachTo(GameObject host)
        {
            var controller = host.GetComponent<PartyRunPresenter>();
            if (controller == null)
            {
                controller = host.AddComponent<PartyRunPresenter>();
            }

            return controller;
        }

        public void Bind(CareerQuestApp app, GameSession session)
        {
            _app = app;
            _session = session;
        }

        /// <summary>
        /// Mounts the campus run panel under <paramref name="parent"/> when a run
        /// is active; a no-op (and teardown) when no run is active, so the
        /// campus shows nothing extra during normal free-choice play.
        /// </summary>
        public void MountOnCampus(RectTransform parent)
        {
            Unmount();
            var run = _session?.PartyRun;
            if (parent == null || run == null || !run.IsActive)
            {
                return;
            }

            _panel = UiBuilder.Panel(parent, PanelName, Paper);
            UiBuilder.Place(_panel, 0f, -150f, 760f, 196f);
            UiBuilder.Shape(_panel, "PartyRunStripe", CurrentGold, 0f, 90f, 760f, 8f);

            var heading = UiBuilder.Text(_panel, "PartyRunHeading", "Party Run", 20, TextAnchor.MiddleLeft, Ink, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(heading.rectTransform, -340f, 68f, 360f, 28f);

            MountRoundIntro(run);
            MountProgressStrip(run);
            MountRewardPreview();
            MountControls(run);
        }

        public void Unmount()
        {
            if (_panel != null)
            {
                Destroy(_panel.gameObject);
                _panel = null;
            }
        }

        // ------------------------------------------------------------------
        // Control seams (buttons + PlayMode tests share these).
        // ------------------------------------------------------------------

        /// <summary>
        /// Continue the run: route to the current round's station (its selected
        /// seed). Presenter only — it does not advance the run here; the station
        /// completion does. Returns false when nothing is pending.
        /// </summary>
        public bool Continue()
        {
            return _app != null && _app.ContinuePartyRun();
        }

        /// <summary>Quit the run: clears ONLY guided sequencing (app preserves earned state).</summary>
        public void Quit()
        {
            _app?.QuitPartyRun();
        }

        /// <summary>Hand off to the reveal ceremony (only meaningful once ready).</summary>
        public void RevealHandoff()
        {
            _app?.ShowReveal();
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private void MountRoundIntro(PartyRunState run)
        {
            string introText;
            if (run.IsComplete)
            {
                introText = "Run complete — open your reveal!";
            }
            else
            {
                var stationId = run.CurrentStationId;
                var roundNumber = run.CurrentRound + 1;
                if (PartyStationDefinitions.TryGetById(stationId, out var definition))
                {
                    var seed = ResolveSeed(definition, run.CurrentSeedId);
                    var preview = seed != null && !string.IsNullOrWhiteSpace(seed.RewardPreviewLine)
                        ? seed.RewardPreviewLine
                        : "A new toy challenge is ready.";
                    introText = $"Round {roundNumber}/{run.RoundCount}: {definition.DisplayName}\n{preview}";
                }
                else
                {
                    introText = $"Round {roundNumber}/{run.RoundCount}";
                }
            }

            var intro = UiBuilder.Text(_panel, RoundIntroName, introText, 16, TextAnchor.MiddleLeft, Ink);
            UiBuilder.Place(intro.rectTransform, -160f, 64f, 560f, 44f);
            intro.enableAutoSizing = true;
            intro.fontSizeMin = 12;
            intro.fontSizeMax = 16;
        }

        /// <summary>
        /// Progress strip: one cell per round. Each cell carries TWO signals so
        /// it never relies on color alone (R19 non-color cue) — a state glyph
        /// (check / dot / number) AND the round number label, plus the color.
        /// </summary>
        private void MountProgressStrip(PartyRunState run)
        {
            var strip = UiBuilder.Panel(_panel, ProgressStripName, new Color(1f, 1f, 1f, 0f));
            UiBuilder.Place(strip, -160f, 14f, 560f, 40f);

            var rows = run.ProgressStrip;
            var count = Mathf.Max(1, rows.Count);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var x = -((count - 1) * 0.5f) * 60f + i * 60f;
                var color = row.State switch
                {
                    PartyRunStepState.Done => DoneGreen,
                    PartyRunStepState.Current => CurrentGold,
                    _ => UpcomingGrey
                };

                UiBuilder.Circle(strip, $"{StepCellPrefix}{i}", color, x, 0f, 40f, 40f);

                // Non-color glyph: completed shows a check, current a ring dot,
                // upcoming the round number — readable without distinguishing hue.
                var glyph = row.State switch
                {
                    PartyRunStepState.Done => "✓",
                    PartyRunStepState.Current => "●",
                    _ => (i + 1).ToString()
                };
                var label = UiBuilder.Text(strip, $"{StepCellPrefix}{i}Label", glyph, 18, TextAnchor.MiddleCenter, Color.white, TypeRole.Body, TypeWeight.SemiBold);
                UiBuilder.Place(label.rectTransform, x, 0f, 40f, 40f);
            }
        }

        /// <summary>
        /// Reward preview of the most recent completed round, READ from the
        /// session reward log (U6) — never re-derived. Hidden until the run has
        /// at least one completion.
        /// </summary>
        private void MountRewardPreview()
        {
            var recent = _session.RewardLog.Recent(1);
            if (recent.Count == 0)
            {
                return;
            }

            var rewardEvent = recent[0];
            var accessoryName = AccessoryRewardConfig.TryGetById(rewardEvent.AccessoryRewardId, out var accessory)
                ? accessory.DisplayName
                : "new gear";
            var text = $"Last round: {rewardEvent.Summary}  ·  Earned {accessoryName}.";

            var preview = UiBuilder.Text(_panel, RewardPreviewName, text, 13, TextAnchor.MiddleLeft, new Color(0.27f, 0.36f, 0.4f));
            UiBuilder.Place(preview.rectTransform, -160f, -34f, 560f, 36f);
            preview.enableAutoSizing = true;
            preview.fontSizeMin = 10;
            preview.fontSizeMax = 13;
        }

        private void MountControls(PartyRunState run)
        {
            // Continue while the run has a pending round; otherwise the run is
            // complete and the primary action is the reveal handoff.
            if (!run.IsComplete && run.CurrentStationId != null)
            {
                var continueButton = UiBuilder.Button(_panel, ContinueButtonName, "Continue Party Run", () => Continue());
                UiBuilder.Place(continueButton.GetComponent<RectTransform>(), 250f, 24f, 220f, 52f);
                QuestStageUi.StylePrimaryButton(continueButton);
            }

            // Reveal handoff appears as soon as the run is reveal-ready (mirrors
            // the normal gate through the synthesis snapshot — never a second
            // readiness rule). It stays available through the rest of the run.
            var synthesis = RevealSynthesis.Resolve(_session);
            if (synthesis.IsRevealReady)
            {
                var revealButton = UiBuilder.Button(_panel, RevealButtonName, "Reveal", () => RevealHandoff());
                UiBuilder.Place(revealButton.GetComponent<RectTransform>(), 250f, -36f, 220f, 46f);
                if (run.IsComplete)
                {
                    QuestStageUi.StylePrimaryButton(revealButton);
                }
                else
                {
                    QuestStageUi.StyleSecondaryButton(revealButton);
                }
            }

            var quit = UiBuilder.SmallButton(_panel, QuitButtonName, "Quit run", () => Quit());
            UiBuilder.Place(quit.GetComponent<RectTransform>(), -340f, -64f, 150f, 40f);
            QuestStageUi.StyleSecondaryButton(quit);
        }

        private static PartyStationSeedDefinition ResolveSeed(PartyStationDefinition definition, string seedId)
        {
            if (definition == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(seedId) && definition.TryGetSeed(seedId, out var seed))
            {
                return seed;
            }

            return definition.DefaultSeed;
        }
    }
}
