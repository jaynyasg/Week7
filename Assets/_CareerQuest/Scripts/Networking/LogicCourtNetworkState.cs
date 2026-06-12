using System;
using System.Linq;
using Unity.Netcode;

namespace CareerQuest
{
    public enum LogicCourtRejectReason : byte
    {
        None = 0,
        UnknownStep = 1,
        AlreadyDone = 2
    }

    /// <summary>
    /// Host-authoritative Logic Court shared state (U10 — pure replication of
    /// <see cref="DesignBuildNetworkState"/>): submit RPC → host validates →
    /// NetworkList change → both clients re-render.
    ///
    /// P21 reject channel: the submit RPC captures the SENDER client id from
    /// RpcParams and replies with a SendTo.SpecifiedInParams reject RPC to that
    /// sender only — never owner-targeted (this object is server-owned). The
    /// reject echoes the client's submission id; the controller ignores rejects
    /// whose echo is stale. Host accepts fire <see cref="Changed"/> exactly once
    /// (via the list event only).
    ///
    /// Attempt lifecycle mirrors Design Build: <see cref="BeginAttempt"/> resets
    /// only AFTER a completed attempt; a player joining a partner's in-progress
    /// attempt never wipes it. Submissions arriving host-side after completion
    /// are ignored silently (completion guard).
    ///
    /// P13: the host seeds <see cref="ShuffleSeed"/> (evidence order) at spawn
    /// and reseeds on every attempt reset; clients derive the identical order
    /// from the synced seed via <see cref="ContentShuffle.DeriveOrder"/>.
    ///
    /// Zone correctness (helpful vs not helpful) is deterministic LOCAL content,
    /// enforced client-side like Design Build's wrong-slot rule; case review is
    /// local per player. The host validates step identity, duplicates, and
    /// completion only — the same SubmitStep(0/1/2) semantics as before U10.
    /// </summary>
    public class LogicCourtNetworkState : NetworkBehaviour
    {
        public const int RequiredSteps = 3;

        private readonly NetworkList<int> _completedSteps = new();
        private readonly NetworkList<HeldPieceEntry> _heldPieces = new();
        private readonly NetworkVariable<int> _attemptNumber = new(1);
        private readonly NetworkVariable<int> _shuffleSeed = new(0);

        public event Action Changed;

        /// <summary>
        /// Sender-side reject notification: (stepIndex, submissionId, reason).
        /// On the host this is invoked synchronously inside the submit call stack
        /// — client handlers must defer one frame before reacting.
        /// </summary>
        public event Action<int, int, LogicCourtRejectReason> StepRejected;

        public int CompletedSteps => _completedSteps.Count;
        public bool Complete => CompletedSteps >= RequiredSteps;
        public int AttemptNumber => _attemptNumber.Value;

        /// <summary>P13 host-seeded shuffle seed (0 = not seeded yet).</summary>
        public int ShuffleSeed => _shuffleSeed.Value;

        // Host-side seams: the last reject decision the validator made. Lets
        // host-only tests assert reject targeting without a second in-process
        // NetworkManager (true two-client delivery is manual 2P evidence).
        public ulong LastRejectClientId { get; private set; }
        public int LastRejectStepIndex { get; private set; } = -1;
        public int LastRejectSubmissionId { get; private set; } = -1;
        public LogicCourtRejectReason LastRejectReason { get; private set; } = LogicCourtRejectReason.None;

        public override void OnNetworkSpawn()
        {
            _completedSteps.OnListChanged += HandleStepsChanged;
            _heldPieces.OnListChanged += HandleHeldPiecesChanged;
            _attemptNumber.OnValueChanged += HandleAttemptNumberChanged;
            _shuffleSeed.OnValueChanged += HandleShuffleSeedChanged;

            if (IsServer && _shuffleSeed.Value == 0)
            {
                _shuffleSeed.Value = ContentShuffle.NextSeed(0, LogicCourtLayout.EvidencePieceIds.Length);
            }
        }

        public override void OnNetworkDespawn()
        {
            _completedSteps.OnListChanged -= HandleStepsChanged;
            _heldPieces.OnListChanged -= HandleHeldPiecesChanged;
            _attemptNumber.OnValueChanged -= HandleAttemptNumberChanged;
            _shuffleSeed.OnValueChanged -= HandleShuffleSeedChanged;
        }

        public bool IsStepComplete(int stepIndex)
        {
            return _completedSteps.Contains(stepIndex);
        }

        public void SubmitStep(string pieceId, int submissionId)
        {
            if (!IsSpawned)
            {
                return;
            }

            var stepIndex = StepIndexFor(pieceId);
            if (stepIndex < 0)
            {
                return;
            }

            SubmitStepRpc(stepIndex, submissionId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitStepRpc(int stepIndex, int submissionId, RpcParams rpcParams = default)
        {
            ApplySubmission(stepIndex, submissionId, rpcParams.Receive.SenderClientId);
        }

        /// <summary>
        /// Host-side validation core. Also the 2P test seam: host-only tests call
        /// it with a simulated partner client id to exercise accept/reject races.
        /// </summary>
        public PlacementSubmissionResult ApplySubmission(int stepIndex, int submissionId, ulong senderClientId)
        {
            if (!IsSpawned || !IsServer)
            {
                return PlacementSubmissionResult.IgnoredNotServer;
            }

            // Completion guard: post-completion stragglers are ignored silently.
            if (Complete)
            {
                return PlacementSubmissionResult.IgnoredComplete;
            }

            if (stepIndex < 0 || stepIndex >= RequiredSteps)
            {
                SendReject(senderClientId, stepIndex, submissionId, LogicCourtRejectReason.UnknownStep);
                return PlacementSubmissionResult.Rejected;
            }

            if (_completedSteps.Contains(stepIndex))
            {
                SendReject(senderClientId, stepIndex, submissionId, LogicCourtRejectReason.AlreadyDone);
                return PlacementSubmissionResult.Rejected;
            }

            // The NetworkList change event fires Changed exactly once — no manual
            // invoke (mirrors Design Build's single-fire contract).
            _completedSteps.Add(stepIndex);
            ApplyHeldPiece(-1, senderClientId);
            return PlacementSubmissionResult.Accepted;
        }

        private void SendReject(ulong senderClientId, int stepIndex, int submissionId, LogicCourtRejectReason reason)
        {
            LastRejectClientId = senderClientId;
            LastRejectStepIndex = stepIndex;
            LastRejectSubmissionId = submissionId;
            LastRejectReason = reason;

            // Only dispatch to a live target: the sender may have disconnected
            // while the submission was in flight (and host-only seam tests
            // simulate partner ids that are not connected).
            var manager = NetworkManager;
            var targetIsLive = manager != null
                && (senderClientId == manager.LocalClientId || manager.ConnectedClientsIds.Contains(senderClientId));
            if (!targetIsLive)
            {
                return;
            }

            StepRejectedRpc(
                stepIndex,
                submissionId,
                (byte)reason,
                RpcTarget.Single(senderClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        private void StepRejectedRpc(int stepIndex, int submissionId, byte reason, RpcParams rpcParams = default)
        {
            StepRejected?.Invoke(stepIndex, submissionId, (LogicCourtRejectReason)reason);
        }

        /// <summary>
        /// Room-entry hook: begins a fresh attempt only when the previous attempt
        /// completed. A player joining a partner's in-progress attempt never wipes it.
        /// </summary>
        public void BeginAttempt()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsServer)
            {
                ServerBeginAttempt();
                return;
            }

            RequestBeginAttemptRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestBeginAttemptRpc(RpcParams rpcParams = default)
        {
            ServerBeginAttempt();
        }

        private void ServerBeginAttempt()
        {
            if (!Complete)
            {
                return;
            }

            ResetForAttempt();
        }

        /// <summary>Server-side reset when a new attempt begins after completion (reseeds the shuffle — P13).</summary>
        public void ResetForAttempt()
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            _completedSteps.Clear();
            _heldPieces.Clear();
            _attemptNumber.Value = _attemptNumber.Value + 1;
            _shuffleSeed.Value = ContentShuffle.NextSeed(_shuffleSeed.Value, LogicCourtLayout.EvidencePieceIds.Length);
        }

        // ------------------------------------------------------------------
        // P17 groundwork: held-piece plumbing (set on pickup, cleared on
        // drop/reject/accept). Partner-highlight RENDERING lands in U12.
        // ------------------------------------------------------------------

        public void SetHeldPiece(string pieceId)
        {
            if (!IsSpawned)
            {
                return;
            }

            SetHeldPieceRpc(PieceIndexFor(pieceId));
        }

        public void ClearHeldPiece()
        {
            if (!IsSpawned)
            {
                return;
            }

            SetHeldPieceRpc(-1);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SetHeldPieceRpc(int pieceIndex, RpcParams rpcParams = default)
        {
            ApplyHeldPiece(pieceIndex, rpcParams.Receive.SenderClientId);
        }

        /// <summary>Server-side held-piece core (also a host-only test seam).</summary>
        public void ApplyHeldPiece(int pieceIndex, ulong clientId)
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            for (var i = _heldPieces.Count - 1; i >= 0; i--)
            {
                if (_heldPieces[i].ClientId == clientId)
                {
                    _heldPieces.RemoveAt(i);
                }
            }

            if (pieceIndex >= 0)
            {
                _heldPieces.Add(new HeldPieceEntry { ClientId = clientId, PieceIndex = pieceIndex });
            }
        }

        /// <summary>Piece index the client currently holds, or -1.</summary>
        public int HeldPieceIndexFor(ulong clientId)
        {
            foreach (var entry in _heldPieces)
            {
                if (entry.ClientId == clientId)
                {
                    return entry.PieceIndex;
                }
            }

            return -1;
        }

        /// <summary>
        /// U12 P17: the piece index held by any client OTHER than
        /// <paramref name="localClientId"/>, or -1. Readable on every peer (the
        /// list syncs everywhere) — this is the partner-indicator read seam.
        /// </summary>
        public int HeldPieceIndexForPartner(ulong localClientId)
        {
            foreach (var entry in _heldPieces)
            {
                if (entry.ClientId != localClientId && entry.PieceIndex >= 0)
                {
                    return entry.PieceIndex;
                }
            }

            return -1;
        }

        /// <summary>Evidence-step index for a piece id (-1 for the case file).</summary>
        public static int StepIndexFor(string pieceId)
        {
            return IndexIn(LogicCourtLayout.EvidencePieceIds, pieceId);
        }

        public static string StepPieceIdFor(int stepIndex)
        {
            var steps = LogicCourtLayout.EvidencePieceIds;
            return stepIndex >= 0 && stepIndex < steps.Length ? steps[stepIndex] : null;
        }

        /// <summary>Draggable-piece index (held-piece domain), or -1.</summary>
        public static int PieceIndexFor(string pieceId)
        {
            return IndexIn(LogicCourtLayout.PieceIds, pieceId);
        }

        public static string PieceIdFor(int pieceIndex)
        {
            var pieces = LogicCourtLayout.PieceIds;
            return pieceIndex >= 0 && pieceIndex < pieces.Length ? pieces[pieceIndex] : null;
        }

        private static int IndexIn(string[] ids, string pieceId)
        {
            if (string.IsNullOrWhiteSpace(pieceId))
            {
                return -1;
            }

            for (var i = 0; i < ids.Length; i++)
            {
                if (ids[i] == pieceId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void HandleStepsChanged(NetworkListEvent<int> change)
        {
            Changed?.Invoke();
        }

        private void HandleHeldPiecesChanged(NetworkListEvent<HeldPieceEntry> change)
        {
            Changed?.Invoke();
        }

        private void HandleAttemptNumberChanged(int previous, int current)
        {
            Changed?.Invoke();
        }

        private void HandleShuffleSeedChanged(int previous, int current)
        {
            Changed?.Invoke();
        }
    }
}
