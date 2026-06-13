using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CareerQuest
{
    // U3: DropSubmitResult moved to Scripts/Interaction/ToySubmissionResult.cs —
    // the shared interaction layer owns the drop seam types now.

    /// <summary>
    /// Design Build Studio — the flagship drag-and-drop room (U6).
    ///
    /// All gameplay flows through the programmatic seams (<see cref="TrySubmitDrop"/>,
    /// <see cref="IsPieceAccepted"/>, <see cref="IsDragLocked"/>, <see cref="DropRejected"/>);
    /// the pointer shell (DraggablePiece/DropZone) is a thin layer over them, and
    /// tests drive the seams directly — never synthetic pointer events.
    ///
    /// P22: in multiplayer, slot rendering AND result accuracy derive from the
    /// shared <see cref="DesignBuildNetworkState"/> (the old optimistic local
    /// dual-write is deleted). Solo keeps the local blueprint rules.
    /// P21: host rejects arrive on the sender only; handling defers one frame
    /// (the host's own rejects invoke synchronously inside the submit call) and
    /// a stale reject (old submission id) never bounces a newer drag.
    /// </summary>
    public class DesignBuildController : ActivityRoomController, IDragDropHost
    {
        public const string GentleWrongSlotFeedback = "Almost! That piece fits a different lot. Try another spot.";
        public const string GentleOccupiedFeedback = "That lot is already built. Try another contribution.";
        public const string GentleLockedFeedback = "The city is celebrating! Building starts again after the ceremony.";
        public const string GentleNoZoneFeedback = "No lot there. Drop a piece onto its matching lot.";

        private static readonly Color DesignAccent = new(0.969f, 0.424f, 0.369f); // Creative Coral

        private static readonly string[] BuilderCheers =
        {
            "Great fit!",
            "The city is growing!",
            "Perfect placement!",
            "Builders at work!",
            "Skyline complete!"
        };

        private readonly DesignBuildRoomState _state = new();
        private readonly Dictionary<string, DraggablePiece> _pieces = new();
        private readonly Dictionary<string, DropZone> _zones = new();
        private readonly HashSet<string> _renderedAccepted = new();

        private GameSession _session;
        private CareerQuestApp _app;
        private ResultSource _source;
        private DesignBuildNetworkState _networkState;
        private bool _networkSubscribed;

        private TextMeshProUGUI _feedbackText;
        private TextMeshProUGUI _statusText;
        private Coroutine _playfieldRoutine;
        private AvatarRuntimeView _builderNpc;
        private SpeechBubble _builderBubble;
        private string _partnerHeldPieceId;

        public DesignBuildRoomState State => _state;
        public FutureCityBlueprint Blueprint => _state.Blueprint;

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
            UsesNetworkState ? _networkState.Complete : _state.Blueprint.Complete;

        private int AuthoritativeAcceptedCount =>
            UsesNetworkState ? _networkState.AcceptedCount : _state.AcceptedPlacements;

        public void ResetActivity()
        {
            _state.ResetForAttempt();
            _renderedAccepted.Clear();
        }

        /// <summary>Local blueprint rules — the same path the buttons called.</summary>
        public bool TryPlacePiece(string pieceId)
        {
            var placed = _state.TryPlaceLocal(pieceId);
            SetFeedback(placed
                ? $"Accepted {pieceId.Replace('_', ' ')} into the Future City."
                : "That spot is already solved. Try another contribution.");
            return placed;
        }

        public bool IsPieceAccepted(string pieceId)
        {
            if (UsesNetworkState)
            {
                return _networkState.IsAccepted(pieceId);
            }

            foreach (var slot in _state.Blueprint.Slots)
            {
                if (slot.RequiredPieceId == pieceId)
                {
                    return slot.Filled;
                }
            }

            return false;
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
        public DropSubmitResult TrySubmitDrop(string pieceId, string slotId)
        {
            if (IsDragLocked)
            {
                SetFeedback(GentleLockedFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedLocked;
            }

            if (DesignBuildNetworkState.PieceIndexFor(pieceId) < 0)
            {
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedUnknownPiece;
            }

            if (!string.Equals(pieceId, slotId, StringComparison.Ordinal))
            {
                // Wrong-piece slot is deterministic content — bounce locally with
                // gentle teaching copy (never punish exploration).
                SetFeedback(GentleWrongSlotFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedWrongSlot;
            }

            if (IsPieceAccepted(pieceId))
            {
                SetFeedback(GentleOccupiedFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedOccupied;
            }

            if (UsesNetworkState)
            {
                var submissionId = _state.BeginSubmission(pieceId);
                _networkState.SubmitPlacement(pieceId, submissionId);

                // On the host the server RPC runs inline — the accept may have
                // already landed by the time SubmitPlacement returns.
                if (IsPieceAccepted(pieceId))
                {
                    _state.CompleteSubmission(pieceId);
                    return DropSubmitResult.Accepted;
                }

                return DropSubmitResult.Pending;
            }

            if (!TryPlacePiece(pieceId))
            {
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedOccupied;
            }

            HandlePieceAccepted(pieceId, celebrate: true);
            UpdateProgress();
            TryAutoComplete();
            return DropSubmitResult.Accepted;
        }

        /// <summary>
        /// Reject-channel handler core (public seam — the stale-reject scenario
        /// drives it directly). A reject only bounces the piece when it echoes
        /// that piece's CURRENT submission id.
        /// </summary>
        public void ProcessRejectedPlacement(string pieceId, int submissionId, DesignBuildRejectReason reason)
        {
            if (string.IsNullOrEmpty(pieceId) || !_state.IsCurrentSubmission(pieceId, submissionId))
            {
                return; // stale — a newer drag of the piece is in flight
            }

            _state.CompleteSubmission(pieceId);
            SetFeedback(reason == DesignBuildRejectReason.AlreadyPlaced
                ? GentleOccupiedFeedback
                : GentleWrongSlotFeedback);

            if (_pieces.TryGetValue(pieceId, out var piece) && piece != null)
            {
                piece.IsAwaitingResult = false;
                if (!piece.IsDragging)
                {
                    piece.SnapToHome();
                }
            }

            // P21 reject response: fires on the submitting client only (the
            // reject event lands on the sender; partners hear nothing).
            AudioCueCatalog.TryPlay(AudioCueIds.DropReject);
            RaiseRejected(pieceId);
        }

        public MiniGameResult CreateResult(ResultSource source)
        {
            var totalSlots = _state.Blueprint.Slots.Count;
            var acceptedCount = _state.AcceptedPlacements;
            var complete = _state.Blueprint.Complete;

            // P22: in multiplayer, result accuracy derives from network state.
            if (source == ResultSource.Multiplayer && _networkState != null && _networkState.IsSpawned)
            {
                acceptedCount = _networkState.AcceptedCount;
                complete = _networkState.Complete;
            }

            var tier = complete ? CompletionTier.Degree : CompletionTier.Practice;
            var accuracy = totalSlots == 0 ? 0f : (float)acceptedCount / totalSlots;
            return new MiniGameResult(
                CareerConfig.DesignBuildId,
                "Future City Design Build",
                tier,
                source,
                new[]
                {
                    new TraitDelta("Building", tier == CompletionTier.Degree ? 5 : 3),
                    new TraitDelta("Spatial Thinking", tier == CompletionTier.Degree ? 5 : 3),
                    new TraitDelta("Creativity", 4),
                    new TraitDelta("Reasoning", 3),
                    new TraitDelta("Collaboration", source == ResultSource.Multiplayer ? 3 : 1)
                },
                45f,
                accuracy,
                tier == CompletionTier.Degree
                    ? "Completed a skyline where helping, law, art, science, and invention fit together."
                    : "Practiced city design and found several strong matches.");
        }

        public void Render(Transform parent, GameSession session, CareerQuestApp app, ResultSource source)
        {
            BeginRoom(CareerConfig.DesignBuildId);
            _session = session;
            _app = app;
            _source = source;

            UnsubscribeNetwork();
            _networkState = FindAnyObjectByType<DesignBuildNetworkState>();

            _state.ResetForAttempt();
            _renderedAccepted.Clear();
            _pieces.Clear();
            _zones.Clear();
            _builderNpc = null;
            _builderBubble = null;
            _partnerHeldPieceId = null; // P17: room re-entry starts indicator-clean

            if (UsesNetworkState)
            {
                // Attempt lifecycle: fresh attempt after a completed one; joining
                // a partner's in-progress attempt never wipes it.
                _networkState.BeginAttempt();
                _state.SyncedAttemptNumber = _networkState.AttemptNumber;
                _networkState.Changed += HandleNetworkChanged;
                _networkState.PlacementRejected += HandleNetworkRejected;
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

            ToyInteractionKit.ApplyDropOutcome(piece, TrySubmitDrop(piece.PieceId, zone.ZoneId));
        }

        public bool WouldAcceptDrop(string pieceId, string zoneId)
        {
            return string.Equals(pieceId, zoneId, StringComparison.Ordinal)
                && !IsPieceAccepted(pieceId)
                && !IsDragLocked;
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private void BuildHud(Transform parent)
        {
            UiBuilder.FullPanel(parent, "DesignBuildPanel", new Color(0.88f, 0.95f, 1f, 0.04f));

            var refs = ActivityRoomChrome.MountQuestHud(
                parent,
                "DesignBuild",
                ActivityRoomChrome.DesignPaper,
                DesignAccent,
                "Future City Workshop",
                _state.Feedback,
                DesignBuildRoomState.DefaultProgress);
            _feedbackText = refs.Prompt;
            _statusText = refs.Status;

            var campus = UiBuilder.Button(parent, "DesignBuildCampusButton", "Campus", () => ExitToCampus(_app));
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 568f, -238f, 106f, 34f); // above the instruction strip band
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

            var existing = worldRoot.Find(DesignBuildStudioLayout.PlayfieldName);
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            var playfield = new GameObject(DesignBuildStudioLayout.PlayfieldName).transform;
            playfield.SetParent(worldRoot, false);

            _pieces.Clear();
            _zones.Clear();
            _renderedAccepted.Clear();

            var pieces = _state.Blueprint.Pieces;
            for (var i = 0; i < pieces.Count; i++)
            {
                var pieceId = pieces[i].Id;
                var slotPosition = ToyInteractionKit.AnchorPosition(
                    worldRoot,
                    DesignBuildStudioLayout.SlotAnchorPrefix + pieceId,
                    DesignBuildStudioLayout.SlotPosition(i));
                var trayPosition = ToyInteractionKit.AnchorPosition(
                    worldRoot,
                    DesignBuildStudioLayout.TrayAnchorPrefix + i,
                    DesignBuildStudioLayout.TrayPosition(i));

                var zoneObject = new GameObject($"DropZone_{pieceId}", typeof(BoxCollider2D), typeof(DropZone));
                zoneObject.transform.SetParent(playfield, false);
                zoneObject.transform.localPosition = slotPosition;
                var zoneCollider = zoneObject.GetComponent<BoxCollider2D>();
                zoneCollider.size = new Vector2(1.05f, 0.95f);
                var zone = zoneObject.GetComponent<DropZone>();
                zone.Configure(pieceId, 320);
                _zones[pieceId] = zone;

                var pieceObject = new GameObject($"Piece_{pieceId}", typeof(SpriteRenderer));
                pieceObject.transform.SetParent(playfield, false);
                pieceObject.transform.localPosition = trayPosition;
                var renderer = pieceObject.GetComponent<SpriteRenderer>();
                renderer.sprite = AssetCatalog.SpriteFor($"prop.city_piece_{pieceId}");
                renderer.sortingOrder = 330; // characters/props band
                ToyInteractionKit.ApplyWorldSize(pieceObject.transform, renderer.sprite, DesignBuildStudioLayout.PieceWorldSize);

                pieceObject.AddComponent<BoxCollider2D>();
                pieceObject.AddComponent<DragFeel>();
                var draggable = pieceObject.AddComponent<DraggablePiece>();
                draggable.Configure(pieceId, this, pieceObject.transform.position);
                _pieces[pieceId] = draggable;
            }

            EnsureBuilderNpc();
            SyncVisualsFromAuthority(celebrateNew: false);
            ApplyPartnerHeldPiece(PartnerHeldPieceIdFromState()); // P17: pre-existing hold renders on mount
            UpdateProgress();
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
                // Partner started a fresh attempt after completion — re-open the room.
                _state.SyncedAttemptNumber = _networkState.AttemptNumber;
                _state.ResetForAttempt();
            }

            SyncVisualsFromAuthority(celebrateNew: true);
            ApplyPartnerHeldPiece(PartnerHeldPieceIdFromState()); // P17: held list changed with the state
            UpdateProgress();
            TryAutoComplete();
        }

        private void HandleNetworkRejected(int pieceIndex, int submissionId, DesignBuildRejectReason reason)
        {
            // Host's own rejects invoke synchronously inside the submit call
            // stack — always defer one frame before reacting.
            StartCoroutine(DeferredReject(pieceIndex, submissionId, reason));
        }

        private IEnumerator DeferredReject(int pieceIndex, int submissionId, DesignBuildRejectReason reason)
        {
            yield return null;
            if (_feedbackText == null)
            {
                // Room torn down while the reject was in flight (player left the
                // route) — don't leak reject feedback/audio into the new route.
                UnsubscribeNetwork();
                yield break;
            }

            ProcessRejectedPlacement(DesignBuildNetworkState.PieceIdFor(pieceIndex), submissionId, reason);
        }

        /// <summary>Slot rendering derives from the authoritative source (P22).</summary>
        private void SyncVisualsFromAuthority(bool celebrateNew)
        {
            foreach (var piece in _state.Blueprint.Pieces)
            {
                var pieceId = piece.Id;
                var accepted = IsPieceAccepted(pieceId);
                if (accepted && !_renderedAccepted.Contains(pieceId))
                {
                    _state.CompleteSubmission(pieceId);
                    HandlePieceAccepted(pieceId, celebrateNew);
                }
                else if (!accepted && _renderedAccepted.Contains(pieceId))
                {
                    // Fresh attempt: the slot opened back up.
                    _renderedAccepted.Remove(pieceId);
                    if (_pieces.TryGetValue(pieceId, out var pieceView) && pieceView != null)
                    {
                        pieceView.UnlockAtHome();
                    }

                    if (_zones.TryGetValue(pieceId, out var zone) && zone != null)
                    {
                        zone.IsOccupied = false;
                        zone.HideGhost();
                    }
                }
            }
        }

        private void HandlePieceAccepted(string pieceId, bool celebrate)
        {
            _renderedAccepted.Add(pieceId);

            Vector3 slotPosition;
            if (_zones.TryGetValue(pieceId, out var zone) && zone != null)
            {
                zone.IsOccupied = true;
                zone.HideGhost();
                slotPosition = zone.transform.position;
            }
            else
            {
                var index = DesignBuildNetworkState.PieceIndexFor(pieceId);
                var fallback = DesignBuildStudioLayout.SlotPosition(Mathf.Max(0, index));
                slotPosition = new Vector3(fallback.x, fallback.y, 0f);
            }

            if (_pieces.TryGetValue(pieceId, out var piece) && piece != null)
            {
                piece.LockAtPosition(slotPosition);
                if (celebrate)
                {
                    var feel = piece.GetComponent<DragFeel>();
                    if (feel != null)
                    {
                        feel.PlayAcceptPunch(DesignAccent);
                    }
                }
            }

            SetFeedback($"Accepted {pieceId.Replace('_', ' ')} into the Future City.");

            if (celebrate)
            {
                CheerBuilderNpc();
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
            SetStatus("City complete! Badge ceremony starting...");

            var result = CreateResult(_source);
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
            _partnerHeldPieceId = ToyInteractionKit.ApplyPartnerHold(_pieces, _partnerHeldPieceId, pieceId);
        }

        private string PartnerHeldPieceIdFromState()
        {
            if (!UsesNetworkState)
            {
                return null;
            }

            var manager = Unity.Netcode.NetworkManager.Singleton;
            var localClientId = manager != null ? manager.LocalClientId : 0UL;
            return DesignBuildNetworkState.PieceIdFor(_networkState.HeldPieceIndexForPartner(localClientId));
        }

        /// <summary>P14: the builder partner cheers on accepted placements.</summary>
        private void CheerBuilderNpc()
        {
            EnsureBuilderNpc();
            if (_builderNpc == null)
            {
                return;
            }

            _builderNpc.TriggerCelebrate(1.2f);

            if (_builderBubble != null)
            {
                var index = Mathf.Clamp(AuthoritativeAcceptedCount - 1, 0, BuilderCheers.Length - 1);
                _builderBubble.Show(BuilderCheers[index], 2.2f);
            }
        }

        private void EnsureBuilderNpc()
        {
            if (_builderNpc != null)
            {
                return;
            }

            var npcObject = GameObject.Find(DesignBuildStudioLayout.BuilderNpcName);
            if (npcObject == null)
            {
                return;
            }

            _builderNpc = npcObject.GetComponent<AvatarRuntimeView>();
            if (_builderNpc != null && _builderBubble == null)
            {
                _builderBubble = SpeechBubble.Attach(npcObject.transform, new Vector3(0.15f, 1.2f, 0f), 2.4f);
            }
        }

        private void UpdateProgress()
        {
            var total = _state.Blueprint.Slots.Count;
            SetStatus(AuthoritativeComplete
                ? "City complete! Badge ceremony starting..."
                : $"{AuthoritativeAcceptedCount}/{total} city pieces placed.");
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
                _networkState.PlacementRejected -= HandleNetworkRejected;
            }

            _networkSubscribed = false;
        }

        private void OnDestroy()
        {
            UnsubscribeNetwork();
        }
    }
}
