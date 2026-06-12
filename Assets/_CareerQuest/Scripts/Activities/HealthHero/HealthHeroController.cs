using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Health Hero Clinic — U10 drag conversion (pure replication of the U6
    /// Design Build flagship pattern). Game rules are unchanged: the authored
    /// case (sore throat → thermometer → warm tea and rest) plays as three
    /// ordered care steps, exploration mistakes are counted gently, and success
    /// (Degree tier) still means at most one mistake. Only the interaction shell
    /// changed: each step's choice is a drag of its care tool onto the patient
    /// zone; the bandage is the wrong tool and always bounces with teaching copy.
    ///
    /// All gameplay flows through the programmatic seams (<see cref="TrySubmitDrop"/>,
    /// <see cref="IsPieceAccepted"/>, <see cref="IsDragLocked"/>, <see cref="DropRejected"/>);
    /// the pointer shell (DraggablePiece/DropZone) is a thin layer over them.
    ///
    /// P22: in multiplayer, step rendering AND completion derive from the shared
    /// <see cref="HealthHeroNetworkState"/>. Solo keeps the local rules.
    /// P21: host rejects arrive on the sender only; handling defers one frame and
    /// a stale reject (old submission id) never bounces a newer drag.
    /// P13: the tool tray order derives from the host-seeded shuffle seed.
    /// P14: the patient brightens (celebrate + speech bubble) on each accepted step.
    /// </summary>
    public class HealthHeroController : ActivityRoomController, IDragDropHost
    {
        public const string GentleWrongZoneFeedback = "Bring care to the patient. Drop each tool onto the patient's bed.";
        public const string GentleOccupiedFeedback = "That care step is already done. Try the next tool.";
        public const string GentleLockedFeedback = "The clinic is celebrating! Care starts again after the ceremony.";
        public const string GentleNoZoneFeedback = "No patient there. Drop the tool onto the patient's bed.";
        public const string BandageFeedback = "A bandage will not help a sore throat. Try the tool that measures temperature.";
        public const string NeedSymptomsFirstFeedback = "Check symptoms first — bring the clipboard to the patient.";
        public const string NeedToolFirstFeedback = "Pick the right tool before the care plan — try the thermometer.";

        private static readonly Color HealthMint = new(0.345f, 0.784f, 0.580f); // Health Mint #58C894

        private static readonly string[] StepAcceptedFeedback =
        {
            "You found a sore throat and a warm forehead. Choose a useful tool.",
            "Good tool choice! Now bring the patient a kind care plan.",
            "Care plan ready! The patient feels much better."
        };

        private static readonly string[] PatientCheers =
        {
            "That helps!",
            "I feel a little better!",
            "Thank you, Health Hero!"
        };

        private readonly HealthHeroRoomState _state = new();
        private readonly Dictionary<string, DraggablePiece> _pieces = new();
        private readonly Dictionary<string, DropZone> _zones = new();
        private readonly Dictionary<string, Vector3> _appliedPositions = new();
        private readonly HashSet<int> _renderedSteps = new();

        private GameSession _session;
        private CareerQuestApp _app;
        private ResultSource _source;
        private HealthHeroNetworkState _networkState;
        private bool _networkSubscribed;

        private TextMeshProUGUI _feedbackText;
        private TextMeshProUGUI _statusText;
        private Coroutine _playfieldRoutine;
        private AvatarRuntimeView _patientNpc;
        private SpeechBubble _patientBubble;
        private string _partnerHeldPieceId;

        public HealthHeroRoomState State => _state;

        /// <summary>The authored case — unchanged game rules (EditMode rules tests pin it).</summary>
        public HealthHeroCase CurrentCase { get; private set; } = new("sore throat", "thermometer", "warm tea and rest");

        public event Action<MiniGameResult> Completed;

        /// <summary>Fired on the submitting client when a drop is rejected (pieceId).</summary>
        public event Action<string> DropRejected;

        /// <summary>
        /// Single drag-lock flag: raised by attempt completion and by the
        /// ceremony. Drag handlers check it client-side; the host submission
        /// guard (completion check in the network state) covers the server side.
        /// </summary>
        public bool IsDragLocked =>
            _state.ResultEmitted
            || AuthoritativeComplete
            || (_app != null && _app.IsCeremonyActive);

        private bool UsesNetworkState =>
            _source == ResultSource.Multiplayer && _networkState != null && _networkState.IsSpawned;

        private bool AuthoritativeComplete =>
            UsesNetworkState ? _networkState.Complete : _state.Complete;

        private int AuthoritativeStepCount =>
            UsesNetworkState ? _networkState.CompletedSteps : _state.CompletedStepCount;

        /// <summary>P13 seam: the tool tray order the active shuffle seed derives.</summary>
        public IReadOnlyList<string> TrayPieceOrder
        {
            get
            {
                var order = ContentShuffle.DeriveOrder(_state.ShuffleSeed, HealthHeroClinicLayout.PieceIds.Length);
                var pieceIds = new string[order.Length];
                for (var i = 0; i < order.Length; i++)
                {
                    pieceIds[i] = HealthHeroClinicLayout.PieceIds[order[i]];
                }

                return pieceIds;
            }
        }

        public bool CheckMatch(string symptom, string tool, string treatment)
        {
            return symptom == CurrentCase.Symptom && tool == CurrentCase.Tool && treatment == CurrentCase.Treatment;
        }

        public void ResetActivity()
        {
            _state.ResetForAttempt();
            _renderedSteps.Clear();
        }

        /// <summary>Authoritative step state (network in 2P, local in solo) — P22.</summary>
        public bool IsStepDone(int stepIndex)
        {
            return UsesNetworkState ? _networkState.IsStepComplete(stepIndex) : _state.IsStepCompleteLocal(stepIndex);
        }

        public bool IsPieceAccepted(string pieceId)
        {
            var stepIndex = HealthHeroNetworkState.StepIndexFor(pieceId);
            return stepIndex >= 0 && IsStepDone(stepIndex);
        }

        /// <summary>First incomplete care step (the order gate's expectation).</summary>
        public int NextStepIndex
        {
            get
            {
                for (var stepIndex = 0; stepIndex < HealthHeroNetworkState.RequiredSteps; stepIndex++)
                {
                    if (!IsStepDone(stepIndex))
                    {
                        return stepIndex;
                    }
                }

                return HealthHeroNetworkState.RequiredSteps;
            }
        }

        /// <summary>Test/QA seam: the live piece object for a piece id (post-mount).</summary>
        public DraggablePiece PieceFor(string pieceId)
        {
            return _pieces.TryGetValue(pieceId, out var piece) ? piece : null;
        }

        /// <summary>Test/QA seam: the live drop zone for a zone id (post-mount).</summary>
        public DropZone ZoneFor(string zoneId)
        {
            return _zones.TryGetValue(zoneId, out var zone) ? zone : null;
        }

        /// <summary>
        /// THE drop seam. Drops resolve here in solo and multiplayer; the pointer
        /// shell and the tests both call it.
        /// </summary>
        public DropSubmitResult TrySubmitDrop(string pieceId, string zoneId)
        {
            if (IsDragLocked)
            {
                SetFeedback(GentleLockedFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedLocked;
            }

            if (HealthHeroNetworkState.PieceIndexFor(pieceId) < 0)
            {
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedUnknownPiece;
            }

            if (!string.Equals(zoneId, HealthHeroClinicLayout.PatientZoneId, StringComparison.Ordinal))
            {
                SetFeedback(GentleWrongZoneFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedWrongSlot;
            }

            if (string.Equals(pieceId, HealthHeroClinicLayout.BandagePieceId, StringComparison.Ordinal))
            {
                // The wrong tool is deterministic content — bounce locally with
                // gentle teaching copy (never punish exploration), count the
                // exploration mistake exactly as the old button did.
                _state.CountMistake();
                SetFeedback(BandageFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedWrongSlot;
            }

            var stepIndex = HealthHeroNetworkState.StepIndexFor(pieceId);
            if (IsStepDone(stepIndex))
            {
                SetFeedback(GentleOccupiedFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedOccupied;
            }

            if (stepIndex != NextStepIndex)
            {
                // Step order is deterministic local content (mirrors the old
                // "check symptoms before choosing tools" gates).
                _state.CountMistake();
                SetFeedback(NextStepIndex == 0 ? NeedSymptomsFirstFeedback : NeedToolFirstFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedWrongSlot;
            }

            if (UsesNetworkState)
            {
                var submissionId = _state.BeginSubmission(pieceId);
                _networkState.SubmitStep(pieceId, submissionId);

                // On the host the server RPC runs inline — the accept may have
                // already landed by the time SubmitStep returns.
                if (IsStepDone(stepIndex))
                {
                    _state.CompleteSubmission(pieceId);
                    return DropSubmitResult.Accepted;
                }

                return DropSubmitResult.Pending;
            }

            _state.TryCompleteStepLocal(stepIndex);
            HandleStepAccepted(pieceId, celebrate: true);
            UpdateProgress();
            TryAutoComplete();
            return DropSubmitResult.Accepted;
        }

        /// <summary>
        /// Reject-channel handler core (public seam — the stale-reject scenario
        /// drives it directly). A reject only bounces the piece when it echoes
        /// that piece's CURRENT submission id.
        /// </summary>
        public void ProcessRejectedStep(string pieceId, int submissionId, HealthHeroRejectReason reason)
        {
            if (string.IsNullOrEmpty(pieceId) || !_state.IsCurrentSubmission(pieceId, submissionId))
            {
                return; // stale — a newer drag of the piece is in flight
            }

            _state.CompleteSubmission(pieceId);
            SetFeedback(reason == HealthHeroRejectReason.AlreadyDone
                ? GentleOccupiedFeedback
                : GentleWrongZoneFeedback);

            if (_pieces.TryGetValue(pieceId, out var piece) && piece != null)
            {
                piece.IsAwaitingResult = false;
                if (!piece.IsDragging)
                {
                    piece.SnapToHome();
                }
            }

            // P21 reject response: fires on the submitting client only.
            AudioCueCatalog.TryPlay(AudioCueIds.DropReject);
            RaiseRejected(pieceId);
        }

        public MiniGameResult CreateResult(bool success, ResultSource source)
        {
            return new MiniGameResult(
                CareerConfig.HealthHeroId,
                "Health Hero Clinic",
                success ? CompletionTier.Degree : CompletionTier.Practice,
                source,
                new[]
                {
                    new TraitDelta("Helping", success ? 5 : 3),
                    new TraitDelta("Science", success ? 4 : 2),
                    new TraitDelta("Focus", 3),
                    new TraitDelta("Communication", 3)
                },
                success ? 42f : 15f,
                success ? 0.92f : 0.65f,
                success
                    ? "Matched symptoms to the right tool and care plan."
                    : "Practiced reading symptoms and choosing helpful care.");
        }

        public void Render(Transform parent, GameSession session, CareerQuestApp app, ResultSource source)
        {
            BeginRoom(CareerConfig.HealthHeroId);
            _session = session;
            _app = app;
            _source = source;

            UnsubscribeNetwork();
            _networkState = FindAnyObjectByType<HealthHeroNetworkState>();

            _state.ResetForAttempt();
            _renderedSteps.Clear();
            _pieces.Clear();
            _zones.Clear();
            _appliedPositions.Clear();
            _patientNpc = null;
            _patientBubble = null;
            _partnerHeldPieceId = null; // P17: room re-entry starts indicator-clean

            if (UsesNetworkState)
            {
                // Attempt lifecycle: fresh attempt after a completed one; joining
                // a partner's in-progress attempt never wipes it.
                _networkState.BeginAttempt();
                _state.SyncedAttemptNumber = _networkState.AttemptNumber;
                _state.UseSharedSeed(_networkState.ShuffleSeed); // P13: host-seeded order
                _networkState.Changed += HandleNetworkChanged;
                _networkState.StepRejected += HandleNetworkRejected;
                _networkSubscribed = true;
            }

            BuildHud(parent);

            if (_playfieldRoutine != null)
            {
                StopCoroutine(_playfieldRoutine);
            }

            _playfieldRoutine = StartCoroutine(MountPlayfieldWhenRoomRevealed());
        }

        // ------------------------------------------------------------------
        // IDragDropHost — the pointer shell delegates every decision here.
        // ------------------------------------------------------------------

        public bool CanBeginDrag(string pieceId)
        {
            return !IsDragLocked && !IsPieceAccepted(pieceId);
        }

        public void NotifyPickUp(string pieceId)
        {
            // A new pickup invalidates any in-flight submission so a late reject
            // for the old submission reads as stale.
            _state.InvalidatePendingSubmission(pieceId);

            if (UsesNetworkState)
            {
                _networkState.SetHeldPiece(pieceId); // P17 plumbing
            }

            AudioCueCatalog.TryPlay(AudioCueIds.DragPickup);
        }

        public void NotifyRelease(string pieceId)
        {
            if (UsesNetworkState)
            {
                _networkState.ClearHeldPiece(); // P17 plumbing
            }
        }

        public void HandleDrop(DraggablePiece piece, DropZone zone)
        {
            if (piece == null)
            {
                return;
            }

            if (zone == null)
            {
                SetFeedback(GentleNoZoneFeedback);
                piece.SnapToHome();
                return;
            }

            var result = TrySubmitDrop(piece.PieceId, zone.ZoneId);
            switch (result)
            {
                case DropSubmitResult.Accepted:
                    // Visuals were applied by the accept path (local or network).
                    break;
                case DropSubmitResult.Pending:
                    piece.IsAwaitingResult = true;
                    break;
                default:
                    piece.SnapToHome();
                    break;
            }
        }

        public bool WouldAcceptDrop(string pieceId, string zoneId)
        {
            // Ghost preview only for the NEXT expected care step (P12 paint-time).
            return string.Equals(zoneId, HealthHeroClinicLayout.PatientZoneId, StringComparison.Ordinal)
                && !IsDragLocked
                && HealthHeroNetworkState.StepIndexFor(pieceId) == NextStepIndex;
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private void BuildHud(Transform parent)
        {
            UiBuilder.FullPanel(parent, "HealthHeroPanel", new Color(0.92f, 1f, 0.92f, 0.04f));

            var refs = ActivityRoomChrome.MountQuestHud(
                parent,
                "HealthHero",
                new Color(1f, 0.97f, 0.86f, 0.9f),
                new Color(HealthMint.r, HealthMint.g, HealthMint.b, 0.95f),
                "Health Hero Clinic",
                _state.Feedback,
                HealthHeroRoomState.DefaultProgress);
            _feedbackText = refs.Prompt;
            _statusText = refs.Status;

            var campus = UiBuilder.Button(parent, "HealthHeroCampusButton", "Campus", () => ExitToCampus(_app));
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 568f, -322f, 106f, 34f);
            ActivityRoomChrome.StyleButton(campus, ActivityRoomChrome.ButtonPrimary, 14);
        }

        private IEnumerator MountPlayfieldWhenRoomRevealed()
        {
            var world = CampusWorldController.Ensure();
            var safety = 0;
            while (world.IsRoomVeilActive && _feedbackText != null && safety++ < 600)
            {
                yield return null;
            }

            if (_feedbackText == null)
            {
                _playfieldRoutine = null;
                yield break; // route changed before the room revealed
            }

            BuildPlayfield(world.WorldRoot);
            _playfieldRoutine = null;
        }

        private void BuildPlayfield(Transform worldRoot)
        {
            if (worldRoot == null)
            {
                return;
            }

            DraggablePiece.EnsureInputShell();

            var existing = worldRoot.Find(HealthHeroClinicLayout.PlayfieldName);
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            var playfield = new GameObject(HealthHeroClinicLayout.PlayfieldName).transform;
            playfield.SetParent(worldRoot, false);

            _pieces.Clear();
            _zones.Clear();
            _appliedPositions.Clear();
            _renderedSteps.Clear();

            var zoneObject = new GameObject($"DropZone_{HealthHeroClinicLayout.PatientZoneId}", typeof(BoxCollider2D), typeof(DropZone));
            zoneObject.transform.SetParent(playfield, false);
            zoneObject.transform.position = AnchorPosition(
                worldRoot,
                HealthHeroClinicLayout.ZoneAnchorPrefix + HealthHeroClinicLayout.PatientZoneId,
                HealthHeroClinicLayout.PatientZonePosition);
            var zoneCollider = zoneObject.GetComponent<BoxCollider2D>();
            zoneCollider.size = HealthHeroClinicLayout.PatientZoneSize;
            var zone = zoneObject.GetComponent<DropZone>();
            zone.Configure(HealthHeroClinicLayout.PatientZoneId, 320);
            _zones[HealthHeroClinicLayout.PatientZoneId] = zone;

            // P13: tool tray order derives from the (host-seeded or local) seed.
            var order = ContentShuffle.DeriveOrder(_state.ShuffleSeed, HealthHeroClinicLayout.PieceIds.Length);
            for (var trayIndex = 0; trayIndex < order.Length; trayIndex++)
            {
                var pieceId = HealthHeroClinicLayout.PieceIds[order[trayIndex]];
                var trayPosition = AnchorPosition(
                    worldRoot,
                    HealthHeroClinicLayout.TrayAnchorPrefix + trayIndex,
                    HealthHeroClinicLayout.TrayPosition(trayIndex));

                var pieceObject = new GameObject($"Piece_{pieceId}", typeof(SpriteRenderer));
                pieceObject.transform.SetParent(playfield, false);
                pieceObject.transform.position = trayPosition;
                var renderer = pieceObject.GetComponent<SpriteRenderer>();
                renderer.sprite = AssetCatalog.SpriteFor($"prop.{pieceId}");
                renderer.sortingOrder = 330; // characters/props band
                ApplyWorldSize(pieceObject.transform, renderer.sprite, HealthHeroClinicLayout.PieceWorldSize);

                pieceObject.AddComponent<BoxCollider2D>();
                pieceObject.AddComponent<DragFeel>();
                var draggable = pieceObject.AddComponent<DraggablePiece>();
                draggable.Configure(pieceId, this, pieceObject.transform.position);
                _pieces[pieceId] = draggable;
            }

            foreach (var stepPieceId in HealthHeroClinicLayout.StepPieceIds)
            {
                _appliedPositions[stepPieceId] = AnchorPosition(
                    worldRoot,
                    HealthHeroClinicLayout.AppliedAnchorPrefix + stepPieceId,
                    HealthHeroClinicLayout.AppliedPosition(stepPieceId));
            }

            EnsurePatientNpc();
            SyncVisualsFromAuthority(celebrateNew: false);
            ApplyPartnerHeldPiece(PartnerHeldPieceIdFromState()); // P17: pre-existing hold renders on mount
            UpdateProgress();
        }

        private static Vector3 AnchorPosition(Transform worldRoot, string anchorName, Vector2 fallback)
        {
            foreach (var child in worldRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == anchorName)
                {
                    return child.position;
                }
            }

            return new Vector3(fallback.x, fallback.y, 0f);
        }

        private static void ApplyWorldSize(Transform target, Sprite sprite, Vector2 worldSize)
        {
            if (sprite == null)
            {
                return;
            }

            var bounds = sprite.bounds.size;
            var width = Mathf.Approximately(bounds.x, 0f) ? 1f : bounds.x;
            var height = Mathf.Approximately(bounds.y, 0f) ? 1f : bounds.y;
            target.localScale = new Vector3(worldSize.x / width, worldSize.y / height, 1f);
        }

        private void HandleNetworkChanged()
        {
            if (!_networkSubscribed)
            {
                return;
            }

            if (_feedbackText == null)
            {
                // Room torn down (route change) — drop the subscription lazily.
                UnsubscribeNetwork();
                return;
            }

            if (UsesNetworkState && _networkState.AttemptNumber != _state.SyncedAttemptNumber)
            {
                // Partner started a fresh attempt after completion — re-open the
                // room and remount the tray with the freshly synced shuffle order.
                _state.SyncedAttemptNumber = _networkState.AttemptNumber;
                _state.ResetForAttempt();
                _state.UseSharedSeed(_networkState.ShuffleSeed);
                _renderedSteps.Clear();
                RebuildPlayfieldIfMounted();
            }
            else if (UsesNetworkState
                && _networkState.ShuffleSeed != 0
                && _networkState.ShuffleSeed != _state.ShuffleSeed
                && AuthoritativeStepCount == 0)
            {
                // Late seed sync before anything is placed — adopt and remount.
                _state.UseSharedSeed(_networkState.ShuffleSeed);
                RebuildPlayfieldIfMounted();
            }

            SyncVisualsFromAuthority(celebrateNew: true);
            ApplyPartnerHeldPiece(PartnerHeldPieceIdFromState()); // P17: held list changed with the state
            UpdateProgress();
            TryAutoComplete();
        }

        private void RebuildPlayfieldIfMounted()
        {
            if (_pieces.Count == 0)
            {
                return;
            }

            BuildPlayfield(CampusWorldController.Ensure().WorldRoot);
        }

        private void HandleNetworkRejected(int stepIndex, int submissionId, HealthHeroRejectReason reason)
        {
            // Host's own rejects invoke synchronously inside the submit call
            // stack — always defer one frame before reacting.
            StartCoroutine(DeferredReject(stepIndex, submissionId, reason));
        }

        private IEnumerator DeferredReject(int stepIndex, int submissionId, HealthHeroRejectReason reason)
        {
            yield return null;
            ProcessRejectedStep(HealthHeroNetworkState.StepPieceIdFor(stepIndex), submissionId, reason);
        }

        /// <summary>Step rendering derives from the authoritative source (P22).</summary>
        private void SyncVisualsFromAuthority(bool celebrateNew)
        {
            for (var stepIndex = 0; stepIndex < HealthHeroNetworkState.RequiredSteps; stepIndex++)
            {
                var pieceId = HealthHeroNetworkState.StepPieceIdFor(stepIndex);
                var done = IsStepDone(stepIndex);
                if (done && !_renderedSteps.Contains(stepIndex))
                {
                    _state.TryCompleteStepLocal(stepIndex);
                    _state.CompleteSubmission(pieceId);
                    HandleStepAccepted(pieceId, celebrateNew);
                }
                else if (!done && _renderedSteps.Contains(stepIndex))
                {
                    // Fresh attempt: the step opened back up.
                    _renderedSteps.Remove(stepIndex);
                    if (_pieces.TryGetValue(pieceId, out var pieceView) && pieceView != null)
                    {
                        pieceView.UnlockAtHome();
                    }
                }
            }

            if (_zones.TryGetValue(HealthHeroClinicLayout.PatientZoneId, out var zone) && zone != null)
            {
                zone.IsOccupied = AuthoritativeComplete;
                if (zone.IsOccupied)
                {
                    zone.HideGhost();
                }
            }
        }

        private void HandleStepAccepted(string pieceId, bool celebrate)
        {
            var stepIndex = HealthHeroNetworkState.StepIndexFor(pieceId);
            _renderedSteps.Add(stepIndex);

            var appliedPosition = _appliedPositions.TryGetValue(pieceId, out var anchored)
                ? anchored
                : (Vector3)HealthHeroClinicLayout.AppliedPosition(pieceId);

            if (_pieces.TryGetValue(pieceId, out var piece) && piece != null)
            {
                piece.LockAtPosition(appliedPosition);
                if (celebrate)
                {
                    var feel = piece.GetComponent<DragFeel>();
                    if (feel != null)
                    {
                        feel.PlayAcceptPunch(HealthMint);
                    }
                }
            }

            SetFeedback(StepAcceptedFeedback[Mathf.Clamp(stepIndex, 0, StepAcceptedFeedback.Length - 1)]);

            if (celebrate)
            {
                CheerPatientNpc();
                AudioCueCatalog.TryPlay(AudioCueIds.DropAccept);
            }
        }

        private void TryAutoComplete()
        {
            if (_state.ResultEmitted || !AuthoritativeComplete)
            {
                return;
            }

            if (_session == null || _app == null)
            {
                return; // seam-only usage without a rendered room
            }

            _state.MarkResultEmitted(); // raises the drag lock with completion
            SetStatus("Case complete! Badge ceremony starting...");

            // Unchanged success rule: at most one exploration mistake = Degree.
            var result = CreateResult(_state.Mistakes <= 1, _source);
            Completed?.Invoke(result);
            TryCompleteRoom(_session, _app, result);
        }

        /// <summary>P17 read seam: which piece the partner currently holds (or null).</summary>
        public string PartnerHeldPieceId => _partnerHeldPieceId;

        /// <summary>
        /// P17 render seam: soft highlight on the piece the PARTNER holds —
        /// gentle pulse on the tray piece, never drag-position mirroring. The
        /// network path drives it from the held-piece list; tests drive it
        /// directly. Null clears (drop/reject/accept/disconnect).
        /// </summary>
        public void ApplyPartnerHeldPiece(string pieceId)
        {
            if (_partnerHeldPieceId != null
                && !string.Equals(_partnerHeldPieceId, pieceId, StringComparison.Ordinal)
                && _pieces.TryGetValue(_partnerHeldPieceId, out var previous)
                && previous != null)
            {
                PartnerHoldIndicator.Clear(previous.gameObject);
            }

            _partnerHeldPieceId = pieceId;
            if (pieceId != null && _pieces.TryGetValue(pieceId, out var piece) && piece != null)
            {
                PartnerHoldIndicator.Show(piece.gameObject);
            }
        }

        private string PartnerHeldPieceIdFromState()
        {
            if (!UsesNetworkState)
            {
                return null;
            }

            var manager = Unity.Netcode.NetworkManager.Singleton;
            var localClientId = manager != null ? manager.LocalClientId : 0UL;
            return HealthHeroNetworkState.PieceIdFor(_networkState.HeldPieceIndexForPartner(localClientId));
        }

        /// <summary>P14: the patient brightens on accepted care steps.</summary>
        private void CheerPatientNpc()
        {
            EnsurePatientNpc();
            if (_patientNpc == null)
            {
                return;
            }

            _patientNpc.TriggerCelebrate(1.2f);

            if (_patientBubble != null)
            {
                var index = Mathf.Clamp(AuthoritativeStepCount - 1, 0, PatientCheers.Length - 1);
                _patientBubble.Show(PatientCheers[index], 2.2f);
            }
        }

        private void EnsurePatientNpc()
        {
            if (_patientNpc != null)
            {
                return;
            }

            var npcObject = GameObject.Find(HealthHeroClinicLayout.PatientNpcName);
            if (npcObject == null)
            {
                return;
            }

            _patientNpc = npcObject.GetComponent<AvatarRuntimeView>();
            if (_patientNpc != null && _patientBubble == null)
            {
                _patientBubble = SpeechBubble.Attach(npcObject.transform, new Vector3(0.15f, 1.2f, 0f), 2.4f);
            }
        }

        private void UpdateProgress()
        {
            SetStatus(AuthoritativeComplete
                ? "Case complete! Badge ceremony starting..."
                : $"Care steps done: {AuthoritativeStepCount}/{HealthHeroNetworkState.RequiredSteps}.");
        }

        private void SetFeedback(string message)
        {
            _state.Feedback = message;
            if (_feedbackText != null)
            {
                _feedbackText.text = message;
            }
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }

        private void RaiseRejected(string pieceId)
        {
            DropRejected?.Invoke(pieceId);
        }

        private void UnsubscribeNetwork()
        {
            if (_networkSubscribed && _networkState != null)
            {
                _networkState.Changed -= HandleNetworkChanged;
                _networkState.StepRejected -= HandleNetworkRejected;
            }

            _networkSubscribed = false;
        }

        private void OnDestroy()
        {
            UnsubscribeNetwork();
        }
    }
}
