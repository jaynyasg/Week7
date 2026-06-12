using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Logic Court — U10 drag conversion (pure replication of the U6 Design
    /// Build flagship pattern). Game rules are unchanged: review the case first,
    /// sort the three evidence cards (bridge test and blueprint are helpful,
    /// paint opinion is not), exploration mistakes are counted gently, and
    /// success (Degree tier) still means at most one mistake. Only the
    /// interaction shell changed: case review is dragging the case file to the
    /// podium; each sort is dragging an evidence card to the Helpful or
    /// Not Helpful zone.
    ///
    /// All gameplay flows through the programmatic seams (<see cref="TrySubmitDrop"/>,
    /// <see cref="IsPieceAccepted"/>, <see cref="IsDragLocked"/>, <see cref="DropRejected"/>).
    ///
    /// P22: in multiplayer, sort rendering AND completion derive from the shared
    /// <see cref="LogicCourtNetworkState"/>. Solo keeps the local rules.
    /// P21: host rejects arrive on the sender only; handling defers one frame and
    /// a stale reject (old submission id) never bounces a newer drag.
    /// P13: the evidence tray order derives from the host-seeded shuffle seed.
    /// P14: the judge stamps the conclusion on completion (stamp punch + cue +
    /// judge celebrate/speech) and cheers each accepted sort.
    /// </summary>
    public class LogicCourtController : ActivityRoomController, IDragDropHost
    {
        public const string GentleLockedFeedback = "Court is celebrating! Sorting starts again after the ceremony.";
        public const string GentleOccupiedFeedback = "That evidence card is already sorted. Try another card.";
        public const string GentleNoZoneFeedback = "No zone there. Drop the card on Helpful or Not Helpful.";
        public const string CaseFileZoneFeedback = "The case file belongs on the podium. Review it first.";
        public const string CaseAlreadyReviewedFeedback = "The case is already reviewed. Sort the evidence cards.";
        public const string NeedReviewFeedback = "Review the case first — drag the case file to the podium.";
        public const string EvidenceOnPodiumFeedback = "Evidence goes in the sorting zones, not on the podium.";
        public const string CaseReviewedFeedback = "Case reviewed: only evidence that proves safety and fit should support the argument.";
        public const string StampLine = "Order! Case approved — strong argument!";

        private static readonly Color LogicAmber = new(0.949f, 0.639f, 0.231f); // Logic Amber #F2A33B

        private static readonly string[] JudgeCheers =
        {
            "Good sorting!",
            "The argument grows stronger!",
            "All evidence sorted!"
        };

        private readonly LogicCourtRoomState _state = new();
        private readonly Dictionary<string, DraggablePiece> _pieces = new();
        private readonly Dictionary<string, DropZone> _zones = new();
        private readonly HashSet<int> _renderedSteps = new();
        private bool _renderedCaseReviewed;

        private GameSession _session;
        private CareerQuestApp _app;
        private ResultSource _source;
        private LogicCourtNetworkState _networkState;
        private bool _networkSubscribed;

        private TextMeshProUGUI _feedbackText;
        private TextMeshProUGUI _statusText;
        private Coroutine _playfieldRoutine;
        private AvatarRuntimeView _judgeNpc;
        private SpeechBubble _judgeBubble;
        private string _partnerHeldPieceId;

        public LogicCourtRoomState State => _state;

        /// <summary>The authored evidence — unchanged game rules (EditMode rules tests pin it).</summary>
        public IReadOnlyList<EvidenceCard> Evidence { get; } = new[]
        {
            new EvidenceCard("The bridge model held 20 blocks.", true),
            new EvidenceCard("Someone liked the blue paint.", false),
            new EvidenceCard("The blueprint matched all safety slots.", true)
        };

        public event Action<MiniGameResult> Completed;

        /// <summary>Fired on the submitting client when a drop is rejected (pieceId).</summary>
        public event Action<string> DropRejected;

        /// <summary>P14 seam: true once the judge has stamped this attempt's conclusion.</summary>
        public bool HasStamped { get; private set; }

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

        /// <summary>P13 seam: the evidence tray order the active shuffle seed derives.</summary>
        public IReadOnlyList<string> EvidenceTrayOrder
        {
            get
            {
                var order = ContentShuffle.DeriveOrder(_state.ShuffleSeed, LogicCourtLayout.EvidencePieceIds.Length);
                var pieceIds = new string[order.Length];
                for (var i = 0; i < order.Length; i++)
                {
                    pieceIds[i] = LogicCourtLayout.EvidencePieceIds[order[i]];
                }

                return pieceIds;
            }
        }

        public bool SortEvidence(IEnumerable<bool> helpfulSelections)
        {
            return Evidence.Select(card => card.Helpful).SequenceEqual(helpfulSelections);
        }

        public void ResetActivity()
        {
            _state.ResetForAttempt();
            _renderedSteps.Clear();
            _renderedCaseReviewed = false;
            HasStamped = false;
        }

        /// <summary>Authoritative sort state (network in 2P, local in solo) — P22.</summary>
        public bool IsStepDone(int stepIndex)
        {
            return UsesNetworkState ? _networkState.IsStepComplete(stepIndex) : _state.IsStepCompleteLocal(stepIndex);
        }

        public bool IsPieceAccepted(string pieceId)
        {
            if (string.Equals(pieceId, LogicCourtLayout.CaseFilePieceId, StringComparison.Ordinal))
            {
                return _state.CaseReviewed;
            }

            var stepIndex = LogicCourtNetworkState.StepIndexFor(pieceId);
            return stepIndex >= 0 && IsStepDone(stepIndex);
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

            if (LogicCourtNetworkState.PieceIndexFor(pieceId) < 0)
            {
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedUnknownPiece;
            }

            if (string.Equals(pieceId, LogicCourtLayout.CaseFilePieceId, StringComparison.Ordinal))
            {
                return TrySubmitCaseFile(zoneId);
            }

            if (string.Equals(zoneId, LogicCourtLayout.PodiumZoneId, StringComparison.Ordinal))
            {
                SetFeedback(EvidenceOnPodiumFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedWrongSlot;
            }

            if (!_state.CaseReviewed)
            {
                // Same gate as the old "Review the case before sorting evidence."
                _state.CountMistake();
                SetFeedback(NeedReviewFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedWrongSlot;
            }

            var stepIndex = LogicCourtNetworkState.StepIndexFor(pieceId);
            if (IsStepDone(stepIndex))
            {
                SetFeedback(GentleOccupiedFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedOccupied;
            }

            if (!string.Equals(zoneId, LogicCourtLayout.CorrectZoneFor(pieceId), StringComparison.Ordinal))
            {
                // Wrong zone is deterministic content — bounce locally with
                // gentle teaching copy (never punish exploration).
                _state.CountMistake();
                SetFeedback(WrongZoneTeachingFeedback(pieceId));
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
            HandleSortAccepted(pieceId, celebrate: true);
            UpdateProgress();
            TryAutoComplete();
            return DropSubmitResult.Accepted;
        }

        private DropSubmitResult TrySubmitCaseFile(string zoneId)
        {
            var pieceId = LogicCourtLayout.CaseFilePieceId;
            if (!string.Equals(zoneId, LogicCourtLayout.PodiumZoneId, StringComparison.Ordinal))
            {
                SetFeedback(CaseFileZoneFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedWrongSlot;
            }

            if (_state.CaseReviewed)
            {
                SetFeedback(CaseAlreadyReviewedFeedback);
                RaiseRejected(pieceId);
                return DropSubmitResult.RejectedOccupied;
            }

            // Case review stays LOCAL per player (the old closure flag was local
            // too) — it never submits to the network state.
            _state.MarkCaseReviewed();
            HandleCaseReviewed(celebrate: true);
            UpdateProgress();
            return DropSubmitResult.Accepted;
        }

        /// <summary>
        /// Reject-channel handler core (public seam — the stale-reject scenario
        /// drives it directly). A reject only bounces the piece when it echoes
        /// that piece's CURRENT submission id.
        /// </summary>
        public void ProcessRejectedStep(string pieceId, int submissionId, LogicCourtRejectReason reason)
        {
            if (string.IsNullOrEmpty(pieceId) || !_state.IsCurrentSubmission(pieceId, submissionId))
            {
                return; // stale — a newer drag of the piece is in flight
            }

            _state.CompleteSubmission(pieceId);
            SetFeedback(reason == LogicCourtRejectReason.AlreadyDone
                ? GentleOccupiedFeedback
                : GentleNoZoneFeedback);

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
                CareerConfig.LogicCourtId,
                "Logic Court",
                success ? CompletionTier.Degree : CompletionTier.Practice,
                source,
                new[]
                {
                    new TraitDelta("Reasoning", success ? 5 : 3),
                    new TraitDelta("Communication", success ? 4 : 2),
                    new TraitDelta("Focus", 3),
                    new TraitDelta("Leadership", 2)
                },
                success ? 35f : 12f,
                success ? 0.94f : 0.58f,
                success
                    ? "Sorted useful evidence and made a strong closing argument."
                    : "Practiced spotting evidence that makes an argument stronger.");
        }

        public void Render(Transform parent, GameSession session, CareerQuestApp app, ResultSource source)
        {
            BeginRoom(CareerConfig.LogicCourtId);
            _session = session;
            _app = app;
            _source = source;

            UnsubscribeNetwork();
            _networkState = FindAnyObjectByType<LogicCourtNetworkState>();

            _state.ResetForAttempt();
            _renderedSteps.Clear();
            _renderedCaseReviewed = false;
            HasStamped = false;
            _pieces.Clear();
            _zones.Clear();
            _judgeNpc = null;
            _judgeBubble = null;
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
            // Ghost preview validity (P12) — paint-time only, never authoritative.
            if (IsDragLocked || IsPieceAccepted(pieceId))
            {
                return false;
            }

            if (string.Equals(pieceId, LogicCourtLayout.CaseFilePieceId, StringComparison.Ordinal))
            {
                return string.Equals(zoneId, LogicCourtLayout.PodiumZoneId, StringComparison.Ordinal);
            }

            return _state.CaseReviewed
                && string.Equals(zoneId, LogicCourtLayout.CorrectZoneFor(pieceId), StringComparison.Ordinal);
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private static string WrongZoneTeachingFeedback(string pieceId)
        {
            switch (pieceId)
            {
                case LogicCourtLayout.EvidencePaintPieceId:
                    return "Liking blue paint is an opinion — it does not prove the design works. Try Not Helpful.";
                case LogicCourtLayout.EvidenceTestPieceId:
                    return "The bridge test proves the design is strong — that evidence is helpful.";
                case LogicCourtLayout.EvidenceBlueprintPieceId:
                    return "Matching the safety slots proves the design fits — that evidence is helpful.";
                default:
                    return GentleNoZoneFeedback;
            }
        }

        private void BuildHud(Transform parent)
        {
            UiBuilder.FullPanel(parent, "LogicCourtPanel", new Color(0.97f, 0.93f, 1f, 0.04f));

            var refs = ActivityRoomChrome.MountQuestHud(
                parent,
                "LogicCourt",
                new Color(1f, 0.97f, 0.86f, 0.9f),
                new Color(LogicAmber.r, LogicAmber.g, LogicAmber.b, 0.95f),
                "Logic Court",
                _state.Feedback,
                LogicCourtRoomState.DefaultProgress);
            _feedbackText = refs.Prompt;
            _statusText = refs.Status;

            var campus = UiBuilder.Button(parent, "LogicCourtCampusButton", "Campus", () => ExitToCampus(_app));
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

            var existing = worldRoot.Find(LogicCourtLayout.PlayfieldName);
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            var playfield = new GameObject(LogicCourtLayout.PlayfieldName).transform;
            playfield.SetParent(worldRoot, false);

            _pieces.Clear();
            _zones.Clear();
            _renderedSteps.Clear();
            _renderedCaseReviewed = false;

            CreateZone(worldRoot, playfield, LogicCourtLayout.PodiumZoneId, LogicCourtLayout.PodiumZonePosition, LogicCourtLayout.PodiumZoneSize);
            CreateZone(worldRoot, playfield, LogicCourtLayout.HelpfulZoneId, LogicCourtLayout.HelpfulZonePosition, LogicCourtLayout.SortingZoneSize);
            CreateZone(worldRoot, playfield, LogicCourtLayout.NotHelpfulZoneId, LogicCourtLayout.NotHelpfulZonePosition, LogicCourtLayout.SortingZoneSize);

            // Tray slot 0 always holds the case file ("review first"); the
            // evidence order behind it derives from the shuffle seed (P13).
            CreatePiece(worldRoot, playfield, LogicCourtLayout.CaseFilePieceId, 0);
            var order = ContentShuffle.DeriveOrder(_state.ShuffleSeed, LogicCourtLayout.EvidencePieceIds.Length);
            for (var i = 0; i < order.Length; i++)
            {
                CreatePiece(worldRoot, playfield, LogicCourtLayout.EvidencePieceIds[order[i]], i + 1);
            }

            EnsureJudgeNpc();
            SyncVisualsFromAuthority(celebrateNew: false);
            ApplyPartnerHeldPiece(PartnerHeldPieceIdFromState()); // P17: pre-existing hold renders on mount
            UpdateProgress();
        }

        private void CreateZone(Transform worldRoot, Transform playfield, string zoneId, Vector2 fallbackPosition, Vector2 size)
        {
            var zoneObject = new GameObject($"DropZone_{zoneId}", typeof(BoxCollider2D), typeof(DropZone));
            zoneObject.transform.SetParent(playfield, false);
            zoneObject.transform.position = AnchorPosition(worldRoot, LogicCourtLayout.ZoneAnchorPrefix + zoneId, fallbackPosition);
            zoneObject.GetComponent<BoxCollider2D>().size = size;
            var zone = zoneObject.GetComponent<DropZone>();
            zone.Configure(zoneId, 320);
            _zones[zoneId] = zone;
        }

        private void CreatePiece(Transform worldRoot, Transform playfield, string pieceId, int trayIndex)
        {
            var trayPosition = AnchorPosition(
                worldRoot,
                LogicCourtLayout.TrayAnchorPrefix + trayIndex,
                LogicCourtLayout.TrayPosition(trayIndex));

            var pieceObject = new GameObject($"Piece_{pieceId}", typeof(SpriteRenderer));
            pieceObject.transform.SetParent(playfield, false);
            pieceObject.transform.position = trayPosition;
            var renderer = pieceObject.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetCatalog.SpriteFor($"prop.{pieceId}");
            renderer.sortingOrder = 330; // characters/props band
            ApplyWorldSize(pieceObject.transform, renderer.sprite, LogicCourtLayout.PieceWorldSize);

            pieceObject.AddComponent<BoxCollider2D>();
            pieceObject.AddComponent<DragFeel>();
            var draggable = pieceObject.AddComponent<DraggablePiece>();
            draggable.Configure(pieceId, this, pieceObject.transform.position);
            _pieces[pieceId] = draggable;
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
                _renderedCaseReviewed = false;
                HasStamped = false;
                RebuildPlayfieldIfMounted();
            }
            else if (UsesNetworkState
                && _networkState.ShuffleSeed != 0
                && _networkState.ShuffleSeed != _state.ShuffleSeed
                && AuthoritativeStepCount == 0)
            {
                // Late seed sync before anything is sorted — adopt and remount.
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

        private void HandleNetworkRejected(int stepIndex, int submissionId, LogicCourtRejectReason reason)
        {
            // Host's own rejects invoke synchronously inside the submit call
            // stack — always defer one frame before reacting.
            StartCoroutine(DeferredReject(stepIndex, submissionId, reason));
        }

        private IEnumerator DeferredReject(int stepIndex, int submissionId, LogicCourtRejectReason reason)
        {
            yield return null;
            ProcessRejectedStep(LogicCourtNetworkState.StepPieceIdFor(stepIndex), submissionId, reason);
        }

        /// <summary>Sort rendering derives from the authoritative source (P22).</summary>
        private void SyncVisualsFromAuthority(bool celebrateNew)
        {
            if (_state.CaseReviewed && !_renderedCaseReviewed)
            {
                HandleCaseReviewed(celebrate: false);
            }

            for (var stepIndex = 0; stepIndex < LogicCourtNetworkState.RequiredSteps; stepIndex++)
            {
                var pieceId = LogicCourtNetworkState.StepPieceIdFor(stepIndex);
                var done = IsStepDone(stepIndex);
                if (done && !_renderedSteps.Contains(stepIndex))
                {
                    _state.TryCompleteStepLocal(stepIndex);
                    _state.CompleteSubmission(pieceId);
                    HandleSortAccepted(pieceId, celebrateNew);
                }
                else if (!done && _renderedSteps.Contains(stepIndex))
                {
                    // Fresh attempt: the sort opened back up.
                    _renderedSteps.Remove(stepIndex);
                    if (_pieces.TryGetValue(pieceId, out var pieceView) && pieceView != null)
                    {
                        pieceView.UnlockAtHome();
                    }
                }
            }
        }

        private void HandleCaseReviewed(bool celebrate)
        {
            _renderedCaseReviewed = true;

            if (_pieces.TryGetValue(LogicCourtLayout.CaseFilePieceId, out var piece) && piece != null)
            {
                var lockPosition = LogicCourtLayout.LockPosition(LogicCourtLayout.CaseFilePieceId);
                piece.LockAtPosition(new Vector3(lockPosition.x, lockPosition.y, 0f));
            }

            if (_zones.TryGetValue(LogicCourtLayout.PodiumZoneId, out var podium) && podium != null)
            {
                podium.IsOccupied = true;
                podium.HideGhost();
            }

            SetFeedback(CaseReviewedFeedback);

            if (celebrate)
            {
                CheerJudgeNpc("Case opened. Sort the evidence!");
                AudioCueCatalog.TryPlay(AudioCueIds.DropAccept);
            }
        }

        private void HandleSortAccepted(string pieceId, bool celebrate)
        {
            var stepIndex = LogicCourtNetworkState.StepIndexFor(pieceId);
            _renderedSteps.Add(stepIndex);

            if (_pieces.TryGetValue(pieceId, out var piece) && piece != null)
            {
                var lockPosition = LogicCourtLayout.LockPosition(pieceId);
                piece.LockAtPosition(new Vector3(lockPosition.x, lockPosition.y, 0f));
                if (celebrate)
                {
                    var feel = piece.GetComponent<DragFeel>();
                    if (feel != null)
                    {
                        feel.PlayAcceptPunch(LogicAmber);
                    }
                }
            }

            SetFeedback(AcceptedFeedbackFor(pieceId));

            if (celebrate)
            {
                var index = Mathf.Clamp(AuthoritativeStepCount - 1, 0, JudgeCheers.Length - 1);
                CheerJudgeNpc(JudgeCheers[index]);
                AudioCueCatalog.TryPlay(AudioCueIds.DropAccept);
            }
        }

        private static string AcceptedFeedbackFor(string pieceId)
        {
            switch (pieceId)
            {
                case LogicCourtLayout.EvidenceTestPieceId:
                    return "Correct: bridge test results are useful evidence.";
                case LogicCourtLayout.EvidencePaintPieceId:
                    return "Correct: liking blue paint is not proof the design works.";
                case LogicCourtLayout.EvidenceBlueprintPieceId:
                    return "Correct: matching the safety slots supports the design.";
                default:
                    return CaseReviewedFeedback;
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
            SetStatus("Argument complete! Badge ceremony starting...");

            // P14: the judge stamps the conclusion before the ceremony routes.
            StampConclusion();

            // Unchanged success rule: at most one exploration mistake = Degree.
            var result = CreateResult(_state.Mistakes <= 1, _source);
            Completed?.Invoke(result);
            TryCompleteRoom(_session, _app, result);
        }

        /// <summary>P14: judge stamp — stamp punch animation + cue + judge celebrate/speech.</summary>
        private void StampConclusion()
        {
            HasStamped = true;
            AudioCueCatalog.TryPlay(AudioCueIds.BadgeStamp);
            CheerJudgeNpc(StampLine, 1.6f);

            var stamp = GameObject.Find(LogicCourtLayout.StampPropName);
            if (stamp != null && isActiveAndEnabled)
            {
                StartCoroutine(StampPunch(stamp.transform));
            }
        }

        private static IEnumerator StampPunch(Transform stamp)
        {
            if (stamp == null)
            {
                yield break;
            }

            var baseScale = stamp.localScale;
            var basePosition = stamp.localPosition;
            const float duration = 0.5f; // DESIGN.md completion beat 500-900ms
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (stamp == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var pulse = Mathf.Sin(t * Mathf.PI);
                stamp.localScale = baseScale * (1f + 0.3f * pulse);
                stamp.localPosition = basePosition + new Vector3(0f, -0.12f * pulse, 0f);
                yield return null;
            }

            if (stamp != null)
            {
                stamp.localScale = baseScale;
                stamp.localPosition = basePosition;
            }
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
            return LogicCourtNetworkState.PieceIdFor(_networkState.HeldPieceIndexForPartner(localClientId));
        }

        private void CheerJudgeNpc(string line, float celebrateSeconds = 1.2f)
        {
            EnsureJudgeNpc();
            if (_judgeNpc == null)
            {
                return;
            }

            _judgeNpc.TriggerCelebrate(celebrateSeconds);

            if (_judgeBubble != null)
            {
                _judgeBubble.Show(line, 2.2f);
            }
        }

        private void EnsureJudgeNpc()
        {
            if (_judgeNpc != null)
            {
                return;
            }

            var npcObject = GameObject.Find(LogicCourtLayout.JudgeNpcName);
            if (npcObject == null)
            {
                return;
            }

            _judgeNpc = npcObject.GetComponent<AvatarRuntimeView>();
            if (_judgeNpc != null && _judgeBubble == null)
            {
                _judgeBubble = SpeechBubble.Attach(npcObject.transform, new Vector3(0.15f, 1.2f, 0f), 2.4f);
            }
        }

        private void UpdateProgress()
        {
            SetStatus(AuthoritativeComplete
                ? "Argument complete! Badge ceremony starting..."
                : $"Evidence sorted: {AuthoritativeStepCount}/{LogicCourtNetworkState.RequiredSteps}.");
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
