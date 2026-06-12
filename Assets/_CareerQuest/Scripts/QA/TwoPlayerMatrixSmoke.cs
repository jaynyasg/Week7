using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Automated 2P evidence harness for the manual matrix rows (a)–(f) in
    /// docs/qa/2026-06-12-wow-pass-final.md. Two BUILT-PLAYER processes run on
    /// one machine: `-cq-smoke -cq-mode 2p-host` and `-cq-smoke -cq-mode
    /// 2p-client` (CareerQuestApp.RunCommandLineSmoke delegates here). Dead
    /// code in normal play — nothing runs unless that flag pair is present.
    ///
    /// Design rules:
    /// - Processes synchronize via OBSERVED NETWORK STATE (NetworkList /
    ///   NetworkVariable values, emote render seams), never fixed sleeps; every
    ///   wait has a generous timeout that fails the scenario LOUDLY on expiry.
    ///   The single intentional non-state wait is the f hold window (the host
    ///   has no back-channel to acknowledge its glow assert).
    /// - Each scenario emits exactly one
    ///   `CQ_2P_RESULT scenario=&lt;id&gt; pass=&lt;true|false&gt; detail=&lt;short&gt;` line;
    ///   the run ends with `CQ_2P_DONE pass=&lt;n&gt; fail=&lt;n&gt;` and
    ///   Application.Quit(0 on all-pass, 3 otherwise).
    /// - Scenario ownership: client logs a, b, f-client, e, c, d-client; host
    ///   logs f, e-host, d-host (the host observes its own side of f/e/d).
    /// - Cross-process signals ride the emote channel (fixed IDs, observed via
    ///   EmoteRelay render seams): Heart = scenario e itself; Star = "client is
    ///   at the reveal with its fallback latch already open" (so the host's
    ///   announcement can never race the fallback-latch evidence).
    /// - Reveal badge gating in a NETWORKED session: clients cannot record
    ///   results (GameSession.RecordResult is a no-op under the network read
    ///   model), so the HOST seeds ShowcaseSeedConfig results into its bound
    ///   session; CampusSessionState syncs UniqueCompletedGames and both peers
    ///   become RevealReady. The host also earns one real badge in (c).
    /// </summary>
    public class TwoPlayerMatrixSmoke : MonoBehaviour
    {
        public const string ResultPrefix = "CQ_2P_RESULT";
        public const string DonePrefix = "CQ_2P_DONE";

        private const float StepTimeoutSeconds = 15f;
        private const float JoinTimeoutSeconds = 90f;

        /// <summary>The piece the host places first (a/b evidence) and the client resubmits (c).</summary>
        private const string FirstPieceId = "clinic";

        /// <summary>The piece the client picks up for the partner-hold scenario (f).</summary>
        private const string HeldPieceId = "court";

        /// <summary>Host completes the attempt with the remaining blueprint pieces (c).</summary>
        private static readonly string[] RemainingPieceIds = { "court", "studio", "lab", "art_tower" };

        /// <summary>A submission id no real drag ever allocated — the wire reject echo is unmistakable.</summary>
        private const int WireRejectSubmissionId = 424242;

        private CareerQuestApp _app;
        private NetworkBootstrap _bootstrap;
        private int _passCount;
        private int _failCount;
        private bool _waitOk;

        /// <summary>Entry point — CareerQuestApp.RunCommandLineSmoke yields this.</summary>
        public IEnumerator Run(CareerQuestApp app, string mode)
        {
            _app = app;
            _bootstrap = app.GetComponent<NetworkBootstrap>();

            // Two windowed players share one machine; the unfocused window must
            // keep ticking (ProjectSettings ships runInBackground=0).
            Application.runInBackground = true;

            Debug.Log($"CQ_2P_START mode={mode}");

            if (string.Equals(mode, "2p-host", StringComparison.OrdinalIgnoreCase))
            {
                yield return RunHostLadder();
            }
            else
            {
                yield return RunClientLadder();
            }

            Debug.Log($"{DonePrefix} pass={_passCount} fail={_failCount}");

            var manager = NetworkManager.Singleton;
            if (manager != null && (manager.IsHost || manager.IsClient || manager.IsServer))
            {
                // Intentional shutdown — never surface the disconnect error UI.
                _bootstrap?.SuppressLocalDisconnectNotice();
                manager.Shutdown();
            }

            yield return null;
            Application.Quit(_failCount == 0 ? 0 : 3);
        }

        // ------------------------------------------------------------------
        // Host ladder (P1): place first piece → observe f → observe e →
        // complete attempt + ceremony (c actions) → seed badges → reveal
        // announce + skip (d-host) → hold session until the client finishes.
        // ------------------------------------------------------------------

        private IEnumerator RunHostLadder()
        {
            yield return _app.ConnectForQa(asHost: true);
            if (!_bootstrap.LastConnectionSucceeded)
            {
                LogResult("setup-host", false, "host-start-failed");
                yield break;
            }

            yield return WaitFor(
                () => NetworkManager.Singleton != null
                      && NetworkManager.Singleton.ConnectedClientsIds.Count >= 2
                      && CampusSessionState.Instance != null
                      && EmoteRelay.Instance != null
                      && EmoteRelay.Instance.IsSpawned,
                JoinTimeoutSeconds,
                "client-join");
            if (!_waitOk)
            {
                LogResult("setup-host", false, "client-never-joined");
                yield break;
            }

            var partnerId = PartnerClientId();
            var localId = NetworkManager.Singleton.LocalClientId;
            var relay = EmoteRelay.Instance;

            // --- a/b setup: enter Design Build, place the first piece (the
            // client owns the a/b result lines). ---
            _app.ShowDesignBuild(false);
            var controller = _app.GetComponent<DesignBuildController>();
            yield return WaitFor(() => controller.PieceFor(FirstPieceId) != null, StepTimeoutSeconds, "host-playfield");
            var network = FindAnyObjectByType<DesignBuildNetworkState>();
            if (!_waitOk || network == null || !network.IsSpawned)
            {
                LogResult("setup-host", false, "design-room-never-mounted");
                yield break;
            }

            var firstDrop = controller.TrySubmitDrop(FirstPieceId, FirstPieceId);
            yield return WaitFor(() => network.IsAccepted(FirstPieceId), StepTimeoutSeconds, "host-first-accept");
            if (firstDrop != DropSubmitResult.Accepted || !_waitOk)
            {
                LogResult("setup-host", false, $"first-drop={firstDrop}");
                yield break;
            }

            // --- f: the partner's held piece renders the soft glow here, and
            // clears when the partner releases. ---
            var heldIndex = DesignBuildNetworkState.PieceIndexFor(HeldPieceId);
            yield return WaitFor(() => network.HeldPieceIndexForPartner(localId) == heldIndex, 60f, "partner-held");
            var heldSeen = _waitOk;
            var heldPiece = controller.PieceFor(HeldPieceId);
            var glowShown = false;
            if (heldSeen)
            {
                yield return WaitFor(
                    () => heldPiece != null && PartnerHoldIndicator.IsShownOn(heldPiece.gameObject),
                    5f,
                    "partner-glow");
                glowShown = _waitOk;
            }

            yield return WaitFor(
                () => network.HeldPieceIndexForPartner(localId) == -1
                      && (heldPiece == null || !PartnerHoldIndicator.IsShownOn(heldPiece.gameObject)),
                30f,
                "partner-held-clear");
            var glowCleared = _waitOk;
            LogResult("f", heldSeen && glowShown && glowCleared, $"held={heldSeen},glow={glowShown},cleared={glowCleared}");

            // --- e-host: the client's emote renders above the CLIENT's avatar
            // on THIS screen. ---
            yield return WaitFor(
                () => relay.RenderedEmoteCount >= 1 && relay.LastRenderedClientId == partnerId,
                30f,
                "partner-emote");
            var emoteSeen = _waitOk;
            var bubbleOnPartner = false;
            if (emoteSeen)
            {
                var partnerAvatar = FindAvatarFor(partnerId);
                var bubble = partnerAvatar != null ? partnerAvatar.GetComponentInChildren<EmoteBubble>(true) : null;
                bubbleOnPartner = bubble != null && bubble.IsVisible && bubble.ShownEmote == EmoteId.Heart;
            }

            LogResult("e-host", emoteSeen && bubbleOnPartner, $"rendered={emoteSeen},bubbleOnClientAvatar={bubbleOnPartner}");

            // --- c (host-side actions; the client owns the c result line):
            // complete the attempt, ride the ceremony through the skip seam,
            // then return to campus so the client's re-entry resets the room. ---
            var completionOk = true;
            foreach (var pieceId in RemainingPieceIds)
            {
                var drop = controller.TrySubmitDrop(pieceId, pieceId);
                if (drop != DropSubmitResult.Accepted)
                {
                    completionOk = false;
                    Debug.Log($"CQ_2P_NOTE host-complete-drop piece={pieceId} result={drop}");
                }

                yield return null;
            }

            var attemptBefore = network.AttemptNumber;
            yield return WaitFor(() => _app.IsCeremonyActive, 10f, "host-ceremony-start");
            yield return WaitFor(() => _app.TrySkipCeremony(), 10f, "host-ceremony-skip");
            yield return WaitFor(() => !_app.IsCeremonyActive, 10f, "host-ceremony-end");
            _app.ShowCampus();
            yield return WaitFor(() => network.AttemptNumber > attemptBefore, 45f, "attempt-reset");
            var resetSeen = _waitOk;
            yield return WaitFor(() => network.AcceptedCount == 1, 30f, "client-resubmit");
            var resubmitSeen = _waitOk;
            Debug.Log($"CQ_2P_NOTE host-c-side complete={completionOk} reset={resetSeen} resubmit={resubmitSeen}");

            // --- d setup: badge seeding. Clients cannot record results in a
            // networked session (network read model), so the host seeds its
            // BOUND session — CampusSessionState syncs the unique-game count
            // and both peers become RevealReady. ---
            foreach (var seed in ShowcaseSeedConfig.CreativeTechnicalBuilderResults())
            {
                _app.Session.RecordResult(seed);
            }

            Debug.Log($"CQ_2P_NOTE host-seeded revealReady={_app.Session.RevealReady}");

            // --- d-host: wait for the client's at-reveal signal (Star) so the
            // announcement can never race the client's fallback-latch evidence,
            // then run our own reveal and skip at our own pace. ---
            yield return WaitFor(
                () => relay.RenderedEmoteCount >= 2 && relay.LastRenderedClientId == partnerId,
                45f,
                "client-at-reveal-signal");
            var signalSeen = _waitOk;

            _app.ShowReveal(); // announces the reveal start (host + RevealReady)
            var reveal = _app.GetComponent<CareerRevealController>();
            var director = reveal != null ? reveal.Director : null;
            yield return WaitFor(() => director != null && director.LatchOpened, 10f, "host-latch");
            var latchOpened = _waitOk;
            yield return WaitFor(
                () => director != null && director.CanSkip,
                RevealCinematicDirector.SkipDelaySeconds + 3f,
                "host-skip-arm");
            var skipArmed = _waitOk;
            var skipped = skipArmed && reveal != null && reveal.TrySkipReveal();
            var resolved = director != null && director.IsResolved;
            LogResult(
                "d-host",
                signalSeen && latchOpened && skipArmed && skipped && resolved,
                $"signal={signalSeen},latch={latchOpened},skipArmed={skipArmed},skipped={skipped},resolved={resolved}");

            // Hold the session open until the client's ladder finishes (its
            // shutdown drops the connected count) — quitting earlier would
            // disconnect the client mid-(d) and corrupt its cinematic.
            yield return WaitFor(
                () => NetworkManager.Singleton == null || NetworkManager.Singleton.ConnectedClientsIds.Count <= 1,
                90f,
                "client-finish");
        }

        // ------------------------------------------------------------------
        // Client ladder (P2): a duplicate reject → b shared render → f pickup/
        // release → e emote → c reset+resubmit → d fallback latch + continuity.
        // ------------------------------------------------------------------

        private IEnumerator RunClientLadder()
        {
            // The host process may still be booting — retry the localhost join.
            var connected = false;
            for (var attempt = 0; attempt < 3 && !connected; attempt++)
            {
                yield return _app.ConnectForQa(asHost: false);
                connected = _bootstrap.LastConnectionSucceeded;
                if (!connected)
                {
                    var manager = NetworkManager.Singleton;
                    if (manager != null && (manager.IsClient || manager.IsListening))
                    {
                        _bootstrap.SuppressLocalDisconnectNotice();
                        manager.Shutdown();
                    }

                    yield return new WaitForSecondsRealtime(2f);
                }
            }

            if (!connected)
            {
                LogResult("setup-client", false, "connect-failed");
                yield break;
            }

            yield return WaitFor(
                () => CampusSessionState.Instance != null
                      && EmoteRelay.Instance != null
                      && EmoteRelay.Instance.IsSpawned,
                StepTimeoutSeconds,
                "session-state");

            DesignBuildNetworkState network = null;
            yield return WaitFor(
                () => (network = FindAnyObjectByType<DesignBuildNetworkState>()) != null && network.IsSpawned,
                StepTimeoutSeconds,
                "design-network-state");
            if (!_waitOk)
            {
                LogResult("setup-client", false, "design-network-state-missing");
                yield break;
            }

            var localId = NetworkManager.Singleton.LocalClientId;
            var relay = EmoteRelay.Instance;

            // --- a: duplicate reject. Enter the room, observe the host's
            // placement (AcceptedCount==1), then (1) drop the SAME piece via
            // the drop seam — the synced accept makes the duplicate bounce
            // (Pending only when the accept has not synced yet) with the
            // DropRejected event and the occupied teaching copy — and
            // (2) drive the host reject CHANNEL directly (SubmitPlacement with
            // a fresh submission id) to prove two-client reject DELIVERY:
            // host validates AlreadyPlaced and replies to this sender only. ---
            _app.ShowDesignBuild(false);
            var controller = _app.GetComponent<DesignBuildController>();
            yield return WaitFor(() => controller.PieceFor(FirstPieceId) != null, StepTimeoutSeconds, "client-playfield");
            yield return WaitFor(
                () => network.AcceptedCount == 1 && controller.IsPieceAccepted(FirstPieceId),
                30f,
                "host-placement-synced");
            var hostPlacementSeen = _waitOk;

            var rejectedPieces = new List<string>();
            Action<string> onRejected = pieceId => rejectedPieces.Add(pieceId);
            controller.DropRejected += onRejected;

            var wireRejects = new List<(int PieceIndex, int SubmissionId, DesignBuildRejectReason Reason)>();
            Action<int, int, DesignBuildRejectReason> onWireReject =
                (pieceIndex, submissionId, reason) => wireRejects.Add((pieceIndex, submissionId, reason));
            network.PlacementRejected += onWireReject;

            var duplicate = controller.TrySubmitDrop(FirstPieceId, FirstPieceId);
            var duplicateRejected = duplicate == DropSubmitResult.RejectedOccupied || duplicate == DropSubmitResult.Pending;
            var localEventFired = rejectedPieces.Contains(FirstPieceId);

            network.SubmitPlacement(FirstPieceId, WireRejectSubmissionId);
            var expectedIndex = DesignBuildNetworkState.PieceIndexFor(FirstPieceId);
            yield return WaitFor(
                () => wireRejects.Exists(reject => reject.SubmissionId == WireRejectSubmissionId
                                                   && reject.Reason == DesignBuildRejectReason.AlreadyPlaced
                                                   && reject.PieceIndex == expectedIndex),
                StepTimeoutSeconds,
                "wire-reject-delivery");
            var wireRejectDelivered = _waitOk;

            controller.DropRejected -= onRejected;
            network.PlacementRejected -= onWireReject;

            // Nothing got locked by the rejects: an unplaced piece still drags,
            // and the authoritative accepted count never moved.
            var otherStillDraggable = controller.CanBeginDrag(HeldPieceId);
            var countStable = network.AcceptedCount == 1;
            LogResult(
                "a",
                hostPlacementSeen && duplicateRejected && localEventFired && wireRejectDelivered && otherStillDraggable && countStable,
                $"dup={duplicate},rejectEvent={localEventFired},wireReject={wireRejectDelivered},dragFree={otherStillDraggable},count={network.AcceptedCount}");

            // --- b: the host-accepted piece renders accepted on THIS side and
            // is not draggable here. ---
            var acceptedHere = controller.IsPieceAccepted(FirstPieceId);
            var dragBlocked = !controller.CanBeginDrag(FirstPieceId);
            var zone = controller.ZoneFor(FirstPieceId);
            var zoneOccupied = zone != null && zone.IsOccupied;
            var pieceView = controller.PieceFor(FirstPieceId);
            var pieceLocked = pieceView != null && !pieceView.CanDrag;
            LogResult(
                "b",
                acceptedHere && dragBlocked && zoneOccupied && pieceLocked,
                $"accepted={acceptedHere},dragBlocked={dragBlocked},zoneOccupied={zoneOccupied},pieceLocked={pieceLocked}");

            // --- f (client side): programmatic pickup raises the synced held
            // entry; hold a window for the host's glow assert; release clears. ---
            var heldPiece = controller.PieceFor(HeldPieceId);
            var dragStarted = heldPiece != null && heldPiece.BeginDragProgrammatic();
            var heldIndex = DesignBuildNetworkState.PieceIndexFor(HeldPieceId);
            yield return WaitFor(() => network.HeldPieceIndexFor(localId) == heldIndex, StepTimeoutSeconds, "own-held-synced");
            var heldSynced = _waitOk;

            // Hold window: the host asserts the glow event-driven while we
            // hold; there is no back-channel for its ack, so this is a hold,
            // not a sync (the host's own waits are generously timed).
            yield return new WaitForSecondsRealtime(5f);
            if (heldPiece != null && heldPiece.IsDragging)
            {
                heldPiece.CancelDrag();
            }

            yield return WaitFor(() => network.HeldPieceIndexFor(localId) == -1, StepTimeoutSeconds, "own-held-cleared");
            var heldCleared = _waitOk;
            LogResult("f-client", dragStarted && heldSynced && heldCleared, $"drag={dragStarted},synced={heldSynced},cleared={heldCleared}");

            // --- e: send one emote; the bubble renders above OUR avatar on
            // our own screen (the host asserts its side as e-host). ---
            var emoteBaseline = relay.RenderedEmoteCount;
            relay.SendEmote(EmoteId.Heart);
            yield return WaitFor(
                () => relay.RenderedEmoteCount > emoteBaseline && relay.LastRenderedClientId == localId,
                StepTimeoutSeconds,
                "own-emote-rendered");
            var emoteRendered = _waitOk;
            var ownAvatar = FindAvatarFor(localId);
            var ownBubble = ownAvatar != null ? ownAvatar.GetComponentInChildren<EmoteBubble>(true) : null;
            var bubbleVisible = ownBubble != null && ownBubble.IsVisible && ownBubble.ShownEmote == EmoteId.Heart;
            LogResult("e", emoteRendered && bubbleVisible, $"rendered={emoteRendered},bubbleOnOwnAvatar={bubbleVisible}");

            // --- c: observe completion + the host's ceremony phase, exit to
            // campus, re-enter (Render → BeginAttempt → host resets the
            // completed attempt), wait for the attempt bump + empty board,
            // then successfully submit one piece into the fresh attempt. ---
            yield return WaitFor(() => network.Complete, 60f, "host-completion");
            var completeSeen = _waitOk;
            var attemptBefore = network.AttemptNumber;
            yield return WaitFor(() => _app.Session.CurrentPhase == SessionPhase.Ceremony, 20f, "host-ceremony-phase");
            var ceremonyPhaseSeen = _waitOk;
            yield return WaitFor(() => _app.Session.CurrentPhase != SessionPhase.Ceremony, 30f, "host-ceremony-finished");

            // Safety: if this client ever ran its own local ceremony, clear it
            // through the same skip seam before navigating.
            yield return WaitFor(() => !_app.IsCeremonyActive || _app.TrySkipCeremony(), 10f, "client-ceremony-clear");
            yield return WaitFor(() => !_app.IsCeremonyActive, 10f, "client-ceremony-cleared");

            _app.ShowCampus();
            _app.ShowDesignBuild(false);
            yield return WaitFor(() => controller.PieceFor(FirstPieceId) != null, StepTimeoutSeconds, "client-replayfield");
            yield return WaitFor(
                () => network.AttemptNumber > attemptBefore && network.AcceptedCount == 0,
                StepTimeoutSeconds,
                "attempt-reset-synced");
            var resetSeen = _waitOk;
            var resubmit = controller.TrySubmitDrop(FirstPieceId, FirstPieceId);
            var resubmitPathOk = resubmit == DropSubmitResult.Pending || resubmit == DropSubmitResult.Accepted;
            yield return WaitFor(
                () => controller.IsPieceAccepted(FirstPieceId) && network.AcceptedCount == 1,
                StepTimeoutSeconds,
                "resubmit-accepted");
            var resubmitAccepted = _waitOk;
            LogResult(
                "c",
                completeSeen && ceremonyPhaseSeen && resetSeen && resubmitPathOk && resubmitAccepted,
                $"complete={completeSeen},ceremonyPhase={ceremonyPhaseSeen},reset={resetSeen},resubmit={resubmit},accepted={resubmitAccepted}");

            // --- d-client: navigate to the reveal WITHOUT any host
            // announcement — the latch must open via the fallback window and
            // Skip must arm; then signal the host (Star), and while the host
            // reveals + skips at its own pace, assert OUR beat/elapsed
            // continuity is unaffected through natural resolve. ---
            yield return WaitFor(() => _app.Session.RevealReady, 45f, "reveal-ready-synced");
            var revealReady = _waitOk;
            var campusState = CampusSessionState.Instance;
            var noEarlyAnnounce = campusState != null && campusState.RevealStartCount == 0;

            _app.ShowCampus();
            _app.ShowReveal();
            var reveal = _app.GetComponent<CareerRevealController>();
            var director = reveal != null ? reveal.Director : null;
            yield return WaitFor(
                () => director != null && director.LatchOpened,
                RevealCinematicDirector.LatchFallbackSeconds + 2f,
                "client-fallback-latch");
            var latchOpened = _waitOk;
            var stillUnannounced = campusState != null && campusState.RevealStartCount == 0;
            yield return WaitFor(
                () => director != null && director.CanSkip,
                RevealCinematicDirector.SkipDelaySeconds + 2f,
                "client-skip-arm");
            var skipArmed = _waitOk;

            relay.SendEmote(EmoteId.Star); // signal: fallback latch evidence is in
            yield return WaitFor(() => campusState != null && campusState.RevealStartCount > 0, 30f, "host-announce");
            var hostAnnounced = _waitOk;

            var latchStable = director != null && director.LatchOpened;
            var beatA = director != null ? director.CurrentBeat : RevealCinematicBeat.Idle;
            var elapsedA = director != null ? director.ElapsedSeconds : -1f;
            yield return new WaitForSecondsRealtime(1f);
            var beatB = director != null ? director.CurrentBeat : RevealCinematicBeat.Idle;
            var elapsedB = director != null ? director.ElapsedSeconds : -1f;
            var continuity = latchStable
                             && beatB != RevealCinematicBeat.WaitingForLatch
                             && beatB != RevealCinematicBeat.Idle
                             && (beatA == RevealCinematicBeat.Resolved
                                 ? beatB == RevealCinematicBeat.Resolved
                                 : elapsedB > elapsedA);
            yield return WaitFor(
                () => director != null && director.IsResolved,
                RevealCinematicDirector.MaxSeconds + 5f,
                "client-natural-resolve");
            var resolvedNaturally = _waitOk;
            LogResult(
                "d-client",
                revealReady && noEarlyAnnounce && latchOpened && stillUnannounced && skipArmed && hostAnnounced && continuity && resolvedNaturally,
                $"latch={latchOpened},unannounced={stillUnannounced},skipArm={skipArmed},announce={hostAnnounced},continuity={continuity},resolved={resolvedNaturally}");
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// State-based wait with a loud timeout: polls every frame on the
        /// real-time clock; on expiry logs a CQ_2P_WAIT_TIMEOUT diagnostic and
        /// leaves <see cref="_waitOk"/> false so the scenario fails visibly.
        /// </summary>
        private IEnumerator WaitFor(Func<bool> condition, float timeoutSeconds, string label)
        {
            _waitOk = false;
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (condition())
                {
                    _waitOk = true;
                    yield break;
                }

                yield return null;
            }

            Debug.Log($"CQ_2P_WAIT_TIMEOUT step={label} timeout={timeoutSeconds}");
        }

        private void LogResult(string scenario, bool pass, string detail)
        {
            if (pass)
            {
                _passCount++;
            }
            else
            {
                _failCount++;
            }

            var safeDetail = string.IsNullOrEmpty(detail) ? "ok" : detail.Replace(' ', '-');
            Debug.Log($"{ResultPrefix} scenario={scenario} pass={(pass ? "true" : "false")} detail={safeDetail}");
        }

        private static ulong PartnerClientId()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null)
            {
                return ulong.MaxValue;
            }

            foreach (var clientId in manager.ConnectedClientsIds)
            {
                if (clientId != manager.LocalClientId)
                {
                    return clientId;
                }
            }

            return ulong.MaxValue;
        }

        private static PlayerAvatarNetwork FindAvatarFor(ulong clientId)
        {
            foreach (var avatar in FindObjectsByType<PlayerAvatarNetwork>(FindObjectsSortMode.None))
            {
                if (avatar.IsSpawned && avatar.OwnerClientId == clientId)
                {
                    return avatar;
                }
            }

            return null;
        }
    }
}
