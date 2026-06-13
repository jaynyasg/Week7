using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    /// <summary>
    /// U9 (R19) facilitator controls — the local-session classroom/demo controls
    /// that live INSIDE the existing pause surface (design doc: "visible only in
    /// existing pause/debug/demo surfaces ... not a separate educator product").
    /// No accounts, rosters, analytics, or saved profiles (KTD12).
    ///
    /// Four controls, and the "start over" vs "reset run" distinction is the
    /// core contract:
    /// - Reset current run / Restart demo route: clear ONLY the guided
    ///   sequencing (PartyRun) and return to campus — every earned result,
    ///   accessory, badge, trait, and evolution piece is preserved.
    /// - Return to campus: leave the current room/run detour without clearing
    ///   anything (a resumable run stays resumable).
    /// - Quiet mode: the reduced-motion + quiet-audio classroom toggle.
    /// - Start over: the ONLY control that wipes session-earned results, and it
    ///   says so explicitly (it routes the app's new-session reset).
    ///
    /// The buttons are a thin shell over the CareerQuestApp seams; the same seams
    /// back the PlayMode assertions, so the controls are testable without pixels.
    /// </summary>
    public sealed class FacilitatorControlsController : MonoBehaviour
    {
        public const string PanelName = "FacilitatorControlsPanel";
        public const string ResetRunButtonName = "FacilitatorResetRunButton";
        public const string ReturnToCampusButtonName = "FacilitatorReturnToCampusButton";
        public const string QuietToggleButtonName = "FacilitatorQuietToggleButton";
        public const string RestartDemoButtonName = "FacilitatorRestartDemoButton";
        public const string StartOverButtonName = "FacilitatorStartOverButton";

        private CareerQuestApp _app;
        private RectTransform _panel;
        private TMPro.TextMeshProUGUI _quietLabel;

        /// <summary>House attach idiom — the pause menu routes here.</summary>
        public static FacilitatorControlsController AttachTo(GameObject host)
        {
            var controller = host.GetComponent<FacilitatorControlsController>();
            if (controller == null)
            {
                controller = host.AddComponent<FacilitatorControlsController>();
            }

            return controller;
        }

        public void Bind(CareerQuestApp app)
        {
            _app = app;
        }

        /// <summary>
        /// Mounts the facilitator control row under a parent (the pause card).
        /// Idempotent re-mount: a prior panel is dropped first.
        /// </summary>
        public void Mount(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            Unmount();

            // Sits below the pause card (card bottom ~ -260) under the overlay.
            _panel = UiBuilder.Panel(parent, PanelName, new Color(0.97f, 0.94f, 0.86f, 0.96f));
            UiBuilder.Place(_panel, 0f, -300f, 560f, 124f);

            var title = UiBuilder.Text(_panel, "FacilitatorControlsTitle", "Facilitator controls", 16, TextAnchor.MiddleCenter, new Color(0.098f, 0.196f, 0.235f), TypeRole.Body, TypeWeight.SemiBold);
            UiBuilder.Place(title.rectTransform, 0f, 48f, 500f, 24f);

            var resetRun = UiBuilder.SmallButton(_panel, ResetRunButtonName, "Reset run", ResetCurrentRun);
            UiBuilder.Place(resetRun.GetComponent<RectTransform>(), -186f, 10f, 152f, 44f);
            QuestStageUi.StyleSecondaryButton(resetRun);

            var campus = UiBuilder.SmallButton(_panel, ReturnToCampusButtonName, "To campus", ReturnToCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), -18f, 10f, 152f, 44f);
            QuestStageUi.StyleSecondaryButton(campus);

            var quiet = UiBuilder.SmallButton(_panel, QuietToggleButtonName, QuietLabel(), ToggleQuietMode);
            UiBuilder.Place(quiet.GetComponent<RectTransform>(), 150f, 10f, 168f, 44f);
            QuestStageUi.StyleSecondaryButton(quiet);
            _quietLabel = quiet.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            var restart = UiBuilder.SmallButton(_panel, RestartDemoButtonName, "Restart demo", RestartDemoRoute);
            UiBuilder.Place(restart.GetComponent<RectTransform>(), -102f, -38f, 200f, 44f);
            QuestStageUi.StyleSecondaryButton(restart);

            // "Start over" is the ONLY destructive control: it clears earned
            // results, and its label says so plainly. Tinted apart from the rest.
            var startOver = UiBuilder.SmallButton(_panel, StartOverButtonName, "Start over (clear results)", StartOver);
            UiBuilder.Place(startOver.GetComponent<RectTransform>(), 132f, -38f, 240f, 44f);
            startOver.GetComponent<Image>().color = new Color(0.78f, 0.36f, 0.3f);
        }

        public void Unmount()
        {
            if (_panel != null)
            {
                Destroy(_panel.gameObject);
                _panel = null;
            }

            _quietLabel = null;
        }

        // ------------------------------------------------------------------
        // Control seams — the buttons AND the PlayMode tests share these.
        // ------------------------------------------------------------------

        /// <summary>
        /// Reset current run: clears ONLY guided sequencing and returns to
        /// campus. Earned results/accessories/badges/traits/evolution persist.
        /// </summary>
        public void ResetCurrentRun()
        {
            _app?.QuitPartyRun();
        }

        /// <summary>Leave the current detour to campus (a resumable run stays resumable).</summary>
        public void ReturnToCampus()
        {
            _app?.ReturnToCampus();
        }

        /// <summary>Toggle the reduced-motion + quiet-audio classroom mode.</summary>
        public void ToggleQuietMode()
        {
            if (_app == null)
            {
                return;
            }

            _app.SetQuietMode(!_app.Session.ClassroomAccess.QuietMode);
            if (_quietLabel != null)
            {
                _quietLabel.text = QuietLabel();
            }
        }

        /// <summary>
        /// Restart the guided demo route: re-seeds the standard demo run from
        /// round one WITHOUT clearing earned results (a fresh pass over the same
        /// session). Equivalent to reset-run + start-demo.
        /// </summary>
        public void RestartDemoRoute()
        {
            _app?.RestartDemoRoute();
        }

        /// <summary>
        /// Start over: the explicit destructive control — wipes session-earned
        /// results (and the guided run) back to a fresh play session. This is the
        /// ONLY facilitator control that clears earned state.
        /// </summary>
        public void StartOver()
        {
            _app?.StartOver();
        }

        private string QuietLabel()
        {
            var on = _app != null && _app.Session.ClassroomAccess.QuietMode;
            return on ? "Quiet: On" : "Quiet: Off";
        }
    }
}
