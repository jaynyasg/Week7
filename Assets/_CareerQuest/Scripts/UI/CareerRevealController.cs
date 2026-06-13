using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    /// <summary>
    /// U7 rewrite: the reveal is a world event. The camera moves to the
    /// authored stage, badge tokens travel to slots, the light sweep and unlock
    /// burst play in-world (RevealCinematicDirector owns the beats); the UI is
    /// reduced to result copy + actions, mounted ONLY after the sequence
    /// resolves or skip fires.
    ///
    /// Locked branch (&lt;3 unique badges — R22 gate semantics unchanged): short
    /// settle shot, locked slots showing the earned/3 state, no Skip button, no
    /// full cinematic. A client whose synced unique-game count is stale renders
    /// the locked branch and self-corrects on the next state change.
    ///
    /// Every exit path (skip → exit actions, natural completion, Campus/Gallery
    /// action, CancelCeremony on disconnect) routes through the single
    /// <see cref="CancelCinematic"/> teardown: beats stop, an active drag is
    /// cancelled, and the camera restores via CameraDirector (P23).
    /// </summary>
    public class CareerRevealController : MonoBehaviour
    {
        public const string SkipButtonName = "RevealSkipButton";

        private GameSession _session;
        private CareerQuestApp _app;
        private CampusWorldController _world;
        private CameraDirector _cameraDirector;
        private RevealCinematicDirector _director;
        private RectTransform _panel;
        private Button _skipButton;
        private bool _mounted;
        private bool _lockedBranchShown;
        private bool _sessionSubscribed;
        private bool _upgradeQueued;

        /// <summary>
        /// U7 synthesis snapshot for this render: top traits, top 5 paths,
        /// family, superpower, combo spotlight, and the completion-count style.
        /// One resolver (KTD9) — the unlocked card and the cinematic both read
        /// from it instead of bespoke per-outcome logic.
        /// </summary>
        public RevealSynthesisResult Synthesis { get; private set; }

        /// <summary>Test/QA seam: the live beat sequencer (null before first render).</summary>
        public RevealCinematicDirector Director => _director;

        public bool IsCinematicActive => _director != null && _director.IsRunning;

        public void Render(Transform parent, GameSession session, CareerQuestApp app)
        {
            CancelCinematic(); // idempotent — fresh render always starts clean

            _session = session;
            _app = app;

            // One synthesis pass per render (KTD9): every reveal beat and copy
            // line below reads from this snapshot, never re-derives.
            Synthesis = RevealSynthesis.Resolve(_session);

            _world = CampusWorldController.Ensure();
            _cameraDirector = _world.CameraDirector;
            _director = GetComponent<RevealCinematicDirector>();
            if (_director == null)
            {
                _director = gameObject.AddComponent<RevealCinematicDirector>();
            }

            _mounted = true;

            // World-first: the panel is a transparent container so the stage
            // diorama carries the screen (non-blocking per the U6 defaults).
            _panel = UiBuilder.FullPanel(parent, "CareerRevealPanel", Color.clear);

            // Stale-count guard (R22): readiness is checked against the synced
            // unique-game count at trigger time. GameSession reads the network
            // read model on clients, so a stale snapshot lands in the locked
            // branch and self-corrects when the next state change arrives.
            if (_session.RevealReady)
            {
                BeginUnlockedCinematic();
            }
            else
            {
                BeginLockedBranch();
            }
        }

        /// <summary>UI button + test seam share this guarded path (skip after 3s, per-client).</summary>
        public bool TrySkipReveal()
        {
            return _director != null && _director.TrySkip();
        }

        /// <summary>
        /// THE single teardown. Stops beats, cancels any active drag, restores
        /// the camera via CameraDirector, and drops subscriptions. Routed from
        /// every exit: CareerQuestApp.ResetRoot (all route changes, including
        /// the disconnect path) and CancelCeremony.
        /// </summary>
        public void CancelCinematic()
        {
            // Camera restoration applies only when beats are mid-flight (skip,
            // disconnect, early route change). After the sequence resolves, the
            // next route's SetRouteShot is the restoration guarantee (U3) — a
            // blind reset here would cancel the camera state the new route just
            // mounted (e.g. the hub follow that ShowCampus begins before
            // ResetRoot runs).
            var cinematicMidFlight = _director != null && _director.IsRunning;

            if (_director != null)
            {
                _director.StopImmediate();
            }

            DraggablePiece.CancelActiveDrag();

            if (cinematicMidFlight && _cameraDirector != null)
            {
                _cameraDirector.ResetToRouteShot();
            }

            UnsubscribeSessionChanged();
            _mounted = false;
            _lockedBranchShown = false;
            _upgradeQueued = false;
            _skipButton = null;
            _panel = null;
        }

        private void Update()
        {
            if (_skipButton != null && _director != null)
            {
                _skipButton.interactable = _director.CanSkip;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeSessionChanged();
        }

        // ------------------------------------------------------------------
        // Unlocked branch — full in-world cinematic
        // ------------------------------------------------------------------

        private void BeginUnlockedCinematic()
        {
            _lockedBranchShown = false;

            // Skip control is the only UI during the cinematic; it arms after
            // 3 seconds (Update polls CanSkip) and acts per-client.
            _skipButton = UiBuilder.Button(_panel, SkipButtonName, "Skip", () => TrySkipReveal());
            UiBuilder.Place(_skipButton.GetComponent<RectTransform>(), 0f, -310f, 180f, 48f);
            QuestStageUi.StyleSecondaryButton(_skipButton);
            _skipButton.interactable = false;

            _director.Begin(new RevealCinematicContext
            {
                Unlocked = true,
                Style = Synthesis != null ? Synthesis.Style : RevealStyle.Simple,
                EarnedCount = Mathf.Clamp(_session.UniqueCompletedGames, 0, RevealStageLayout.SlotCount),
                EarnedEntries = EarnedEntries(),
                WorldRoot = _world.WorldRoot,
                Camera = _cameraDirector,
                StageShot = RevealStageAnchors.ResolveStageShot(),
                SettleShot = RevealStageLayout.SettleShot,
                IsStageMounted = () => _world == null || !_world.IsRoomVeilActive,
                RequireRevealStartSync = RequiresRevealStartSync(),
                HasRevealStartSync = () =>
                {
                    var state = CampusSessionState.Instance;
                    return state == null || state.RevealStartCount > 0;
                },
                OnResolved = MountUnlockedResultCopy
            });
        }

        /// <summary>
        /// The host-synced start moment matters only on connected clients: the
        /// host announces as it shows the reveal, and solo play has no peer to
        /// wait for. The latch is max(sync observed, local stage mounted).
        /// </summary>
        private static bool RequiresRevealStartSync()
        {
            var network = NetworkManager.Singleton;
            return network != null
                && network.IsConnectedClient
                && !network.IsServer
                && CampusSessionState.Instance != null;
        }

        /// <summary>
        /// Earned badge entries for token art. On multiplayer clients results
        /// live host-side, so this list may be shorter than the synced count —
        /// the director falls back to generic badge art for missing entries.
        /// </summary>
        private IReadOnlyList<CatalogEntry> EarnedEntries()
        {
            return CareerQuestCatalog.All
                .Where(entry => _session.GetBestResult(entry.Id) != null)
                .Take(RevealStageLayout.SlotCount)
                .ToList();
        }

        /// <summary>
        /// Result copy + exit actions — mounted only after the sequence
        /// resolves or skip fires (R12/R13; copy stays strength-based — R22).
        /// </summary>
        private void MountUnlockedResultCopy()
        {
            if (!_mounted || _panel == null)
            {
                return;
            }

            if (_skipButton != null)
            {
                Destroy(_skipButton.gameObject);
                _skipButton = null;
            }

            // Lower-third card: the stage (tokens in slots, glow at full sweep)
            // stays the hero of the shot.
            var card = UiBuilder.Panel(_panel, "RevealResultCard", QuestStageUi.Paper);
            UiBuilder.Place(card, 0f, -188f, 1020f, 320f);

            var stripe = UiBuilder.Panel(card, "RevealResultStripe", QuestStageUi.PathGold);
            UiBuilder.Place(stripe, 0f, 152f, 1020f, 10f);

            var synthesis = Synthesis ?? RevealSynthesis.Resolve(_session);

            var banner = UiBuilder.Text(
                card,
                "RevealUnlockBanner",
                "REVEAL UNLOCKED!",
                28,
                TextAnchor.MiddleCenter,
                QuestStageUi.WorkshopTeal,
                TypeRole.Display,
                TypeWeight.Bold);
            UiBuilder.Place(banner.rectTransform, 0f, 128f, 560f, 38f);

            // Headline leads with the superpower (design rule 6), then the
            // family subhead, then the top paths — one synthesis snapshot, not a
            // pile of bespoke labels.
            var lead = UiBuilder.Text(card, "RevealLead", synthesis.Superpower, 36, TextAnchor.MiddleCenter, new Color(0.05f, 0.35f, 0.28f), TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(lead.rectTransform, 0f, 86f, 940f, 46f);

            var subhead = UiBuilder.Text(card, "RevealSubhead", synthesis.FamilySubhead + " strengths", 22, TextAnchor.MiddleCenter, QuestStageUi.WorkshopTeal);
            UiBuilder.Place(subhead.rectTransform, 0f, 50f, 880f, 32f);

            var pathNames = string.Join("   •   ", synthesis.TopPaths.Take(RevealSynthesis.TopPathCount).Select(match => match.Career.DisplayName));
            var paths = UiBuilder.Text(card, "RevealPaths", "You might like: " + pathNames, 18, TextAnchor.MiddleCenter, QuestStageUi.Ink);
            UiBuilder.Place(paths.rectTransform, 0f, 18f, 960f, 30f);

            var confidence = UiBuilder.Text(card, "RevealConfidence", _session.ConfidencePhrase(), 20, TextAnchor.MiddleCenter, QuestStageUi.WorkshopTeal);
            UiBuilder.Place(confidence.rectTransform, 0f, -12f, 640f, 28f);

            // Hybrid spotlight layers on top of any completion-count style
            // (design: Hybrid Spotlight) — only when a combo pair is eligible.
            var bodyText = "This is a strength clue from your quest badges — not a life assignment.";
            if (synthesis.HasComboSpotlight)
            {
                var combo = synthesis.PrimaryCombo;
                bodyText = $"Hybrid spark: {combo.DisplayName}. {combo.RevealCopy}\n{bodyText}";
            }

            var body = UiBuilder.Text(
                card,
                "RevealBody",
                bodyText,
                18,
                TextAnchor.MiddleCenter,
                QuestStageUi.Ink);
            UiBuilder.Place(body.rectTransform, 0f, -52f, 940f, 64f);

            MountExitActions(card, -120f);
        }

        // ------------------------------------------------------------------
        // Locked branch — settle shot, earned/3 state, no Skip
        // ------------------------------------------------------------------

        private void BeginLockedBranch()
        {
            _lockedBranchShown = true;
            SubscribeSessionChanged();

            _director.Begin(new RevealCinematicContext
            {
                Unlocked = false,
                EarnedCount = Mathf.Clamp(_session.UniqueCompletedGames, 0, RevealStageLayout.SlotCount),
                EarnedEntries = EarnedEntries(),
                WorldRoot = _world.WorldRoot,
                Camera = _cameraDirector,
                StageShot = RevealStageAnchors.ResolveStageShot(),
                SettleShot = RevealStageLayout.SettleShot,
                IsStageMounted = () => _world == null || !_world.IsRoomVeilActive,
                RequireRevealStartSync = false, // locked progress is always local-visible
                OnResolved = MountLockedCard
            });
        }

        private void MountLockedCard()
        {
            if (!_mounted || _panel == null)
            {
                return;
            }

            var card = UiBuilder.Panel(_panel, "RevealLockedCard", QuestStageUi.Paper);
            UiBuilder.Place(card, 0f, -110f, 920f, 430f);

            var stripe = UiBuilder.Panel(card, "RevealLockedStripe", QuestStageUi.PathGold);
            UiBuilder.Place(stripe, 0f, 207f, 920f, 10f);

            var title = UiBuilder.Text(
                card,
                "RevealTitle",
                "Career Reveal Stage",
                34,
                TextAnchor.MiddleCenter,
                QuestStageUi.Ink,
                TypeRole.Display,
                TypeWeight.Bold);
            UiBuilder.Place(title.rectTransform, 0f, 168f, 700f, 48f);

            // Locked badge slots + the clear "X/3 badges" state (DESIGN: Gallery
            // And Reveal). Counts read the synced unique-game count.
            QuestStageUi.MountBadgeSlots(card, _session, 60f);

            var locked = UiBuilder.Text(
                card,
                "RevealLocked",
                "Complete 3 unique quest badges to unlock your career reveal.\n" + _session.ConfidencePhrase() + ".",
                22,
                TextAnchor.MiddleCenter,
                QuestStageUi.Ink);
            UiBuilder.Place(locked.rectTransform, 0f, -98f, 760f, 70f);

            var hint = UiBuilder.Text(
                card,
                "RevealHint",
                "Walk to another career door on campus to earn your next badge.",
                17,
                TextAnchor.MiddleCenter,
                new Color(0.22f, 0.32f, 0.36f));
            UiBuilder.Place(hint.rectTransform, 0f, -140f, 700f, 34f);

            MountExitActions(card, -180f);
        }

        private void MountExitActions(RectTransform parent, float y)
        {
            var gallery = UiBuilder.Button(parent, "RevealGalleryButton", "Gallery", () => _app.ShowGallery());
            UiBuilder.Place(gallery.GetComponent<RectTransform>(), -130f, y, 220f, 54f);
            QuestStageUi.StyleSecondaryButton(gallery);

            var campus = UiBuilder.Button(parent, "RevealCampusButton", "Campus", () => _app.ShowCampus());
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 130f, y, 220f, 54f);
            QuestStageUi.StylePrimaryButton(campus);
        }

        // ------------------------------------------------------------------
        // Stale-count self-correction
        // ------------------------------------------------------------------

        private void SubscribeSessionChanged()
        {
            if (_sessionSubscribed || _session == null)
            {
                return;
            }

            _session.Changed += HandleSessionChanged;
            _sessionSubscribed = true;
        }

        private void UnsubscribeSessionChanged()
        {
            if (!_sessionSubscribed || _session == null)
            {
                _sessionSubscribed = false;
                return;
            }

            _session.Changed -= HandleSessionChanged;
            _sessionSubscribed = false;
        }

        private void HandleSessionChanged()
        {
            if (!_mounted || !_lockedBranchShown || _upgradeQueued || _session == null || _app == null)
            {
                return;
            }

            if (!_session.RevealReady || _app.CurrentRoute != ActivityRoute.Reveal)
            {
                return;
            }

            // The synced count crossed the gate while we showed the locked
            // branch — re-render into the unlocked cinematic. Deferred one
            // frame: the change may arrive inside a network callback.
            _upgradeQueued = true;
            StartCoroutine(DeferredUpgradeToUnlocked());
        }

        private IEnumerator DeferredUpgradeToUnlocked()
        {
            yield return null;
            _upgradeQueued = false;

            if (_mounted && _session != null && _app != null
                && _session.RevealReady && _app.CurrentRoute == ActivityRoute.Reveal)
            {
                _app.ShowReveal();
            }
        }
    }
}
