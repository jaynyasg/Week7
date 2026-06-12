using System;
using System.Linq;
using Unity.Netcode;

namespace CareerQuest
{
    public enum DesignBuildRejectReason : byte
    {
        None = 0,
        UnknownPiece = 1,
        AlreadyPlaced = 2
    }

    public enum PlacementSubmissionResult
    {
        Accepted,
        Rejected,
        IgnoredComplete,
        IgnoredNotServer
    }

    /// <summary>P17 groundwork: which piece a client is currently holding.</summary>
    public struct HeldPieceEntry : INetworkSerializeByMemcpy, IEquatable<HeldPieceEntry>
    {
        public ulong ClientId;
        public int PieceIndex;

        public bool Equals(HeldPieceEntry other)
        {
            return ClientId == other.ClientId && PieceIndex == other.PieceIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is HeldPieceEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (ClientId, PieceIndex).GetHashCode();
        }
    }

    /// <summary>
    /// Host-authoritative Design Build shared state (canonical room pattern):
    /// submit RPC → host validates → NetworkList change → both clients re-render.
    ///
    /// P21 reject channel: the submit RPC captures the SENDER client id from
    /// RpcParams and replies with a SendTo.SpecifiedInParams reject RPC to that
    /// sender only — never owner-targeted (this object is server-owned, so owner
    /// addressing would always hit the host). The reject echoes the client's
    /// submission id; the controller ignores rejects whose echo is stale.
    /// Host accepts fire <see cref="Changed"/> exactly once (via the list event —
    /// the old duplicate manual invoke is removed).
    ///
    /// Attempt lifecycle: the list historically never reset. Now the host resets
    /// the room state when a NEW attempt begins AFTER completion
    /// (<see cref="BeginAttempt"/> / <see cref="ResetForAttempt"/>); a player
    /// entering while the partner is mid-attempt joins the in-progress attempt —
    /// the reset request is ignored unless the previous attempt completed.
    /// Submissions arriving host-side after completion are ignored (completion
    /// guard, no reject spam).
    /// </summary>
    public class DesignBuildNetworkState : NetworkBehaviour
    {
        private readonly NetworkList<int> _acceptedPieceIndexes = new();
        private readonly NetworkList<HeldPieceEntry> _heldPieces = new();
        private readonly NetworkVariable<int> _attemptNumber = new(1);

        public event Action Changed;

        /// <summary>
        /// Sender-side reject notification: (pieceIndex, submissionId, reason).
        /// On the host this is invoked synchronously inside the submit call stack
        /// — client handlers must defer one frame before reacting.
        /// </summary>
        public event Action<int, int, DesignBuildRejectReason> PlacementRejected;

        // Cached once: Complete and the bounds check run per Changed event per
        // piece per client — rebuilding the default blueprint there is waste.
        private static readonly int RequiredPieceCount = FutureCityBlueprint.CreateDefault().Pieces.Count;

        public int AcceptedCount => _acceptedPieceIndexes.Count;
        public bool Complete => AcceptedCount >= RequiredPieceCount;
        public int AttemptNumber => _attemptNumber.Value;

        // Host-side seams: the last reject decision the validator made. Lets
        // host-only tests assert reject targeting without a second in-process
        // NetworkManager (true two-client delivery is manual 2P evidence).
        public ulong LastRejectClientId { get; private set; }
        public int LastRejectPieceIndex { get; private set; } = -1;
        public int LastRejectSubmissionId { get; private set; } = -1;
        public DesignBuildRejectReason LastRejectReason { get; private set; } = DesignBuildRejectReason.None;

        public override void OnNetworkSpawn()
        {
            _acceptedPieceIndexes.OnListChanged += HandleAcceptedPiecesChanged;
            _heldPieces.OnListChanged += HandleHeldPiecesChanged;
            _attemptNumber.OnValueChanged += HandleAttemptNumberChanged;
        }

        public override void OnNetworkDespawn()
        {
            _acceptedPieceIndexes.OnListChanged -= HandleAcceptedPiecesChanged;
            _heldPieces.OnListChanged -= HandleHeldPiecesChanged;
            _attemptNumber.OnValueChanged -= HandleAttemptNumberChanged;
        }

        public bool IsAccepted(string pieceId)
        {
            var pieceIndex = PieceIndexFor(pieceId);
            return pieceIndex >= 0 && _acceptedPieceIndexes.Contains(pieceIndex);
        }

        public void SubmitPlacement(string pieceId, int submissionId)
        {
            if (!IsSpawned)
            {
                return;
            }

            var pieceIndex = PieceIndexFor(pieceId);
            if (pieceIndex < 0)
            {
                return;
            }

            SubmitPlacementRpc(pieceIndex, submissionId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitPlacementRpc(int pieceIndex, int submissionId, RpcParams rpcParams = default)
        {
            ApplySubmission(pieceIndex, submissionId, rpcParams.Receive.SenderClientId);
        }

        /// <summary>
        /// Host-side validation core. Also the 2P test seam: host-only tests call
        /// it with a simulated partner client id to exercise accept/reject races.
        /// </summary>
        public PlacementSubmissionResult ApplySubmission(int pieceIndex, int submissionId, ulong senderClientId)
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

            if (pieceIndex < 0 || pieceIndex >= RequiredPieceCount)
            {
                SendReject(senderClientId, pieceIndex, submissionId, DesignBuildRejectReason.UnknownPiece);
                return PlacementSubmissionResult.Rejected;
            }

            if (_acceptedPieceIndexes.Contains(pieceIndex))
            {
                SendReject(senderClientId, pieceIndex, submissionId, DesignBuildRejectReason.AlreadyPlaced);
                return PlacementSubmissionResult.Rejected;
            }

            // The NetworkList change event fires Changed exactly once — no manual
            // invoke here (the old double fire on host accepts is removed).
            _acceptedPieceIndexes.Add(pieceIndex);
            ApplyHeldPiece(-1, senderClientId);
            return PlacementSubmissionResult.Accepted;
        }

        private void SendReject(ulong senderClientId, int pieceIndex, int submissionId, DesignBuildRejectReason reason)
        {
            LastRejectClientId = senderClientId;
            LastRejectPieceIndex = pieceIndex;
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

            PlacementRejectedRpc(
                pieceIndex,
                submissionId,
                (byte)reason,
                RpcTarget.Single(senderClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        private void PlacementRejectedRpc(int pieceIndex, int submissionId, byte reason, RpcParams rpcParams = default)
        {
            PlacementRejected?.Invoke(pieceIndex, submissionId, (DesignBuildRejectReason)reason);
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

        /// <summary>Server-side reset when a new attempt begins after completion.</summary>
        public void ResetForAttempt()
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            _acceptedPieceIndexes.Clear();
            _heldPieces.Clear();
            _attemptNumber.Value = _attemptNumber.Value + 1;
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

        public static int PieceIndexFor(string pieceId)
        {
            if (string.IsNullOrWhiteSpace(pieceId))
            {
                return -1;
            }

            var pieces = FutureCityBlueprint.CreateDefault().Pieces;
            for (var i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].Id == pieceId)
                {
                    return i;
                }
            }

            return -1;
        }

        public static string PieceIdFor(int pieceIndex)
        {
            var pieces = FutureCityBlueprint.CreateDefault().Pieces;
            return pieceIndex >= 0 && pieceIndex < pieces.Count ? pieces[pieceIndex].Id : null;
        }

        private void HandleAcceptedPiecesChanged(NetworkListEvent<int> change)
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
    }
}
