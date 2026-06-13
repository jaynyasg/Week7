using System;
using System.Linq;
using Unity.Netcode;

namespace CareerQuest
{
    /// <summary>
    /// One compact, session-scoped reward fact: a completed station and the
    /// best tier earned. Indexes into <see cref="PartyStationDefinitions.All"/>
    /// — never names, free text, or persistent profile data (R17).
    /// </summary>
    public struct StationRewardFact : INetworkSerializeByMemcpy, IEquatable<StationRewardFact>
    {
        public int StationIndex;
        public int Tier;

        public bool Equals(StationRewardFact other)
        {
            return StationIndex == other.StationIndex && Tier == other.Tier;
        }

        public override bool Equals(object obj)
        {
            return obj is StationRewardFact other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (StationIndex, Tier).GetHashCode();
        }
    }

    /// <summary>
    /// Host-authoritative shared state for the generic Party Pack station layer
    /// (U3, R16/R17) — ONE network surface for all toy patterns instead of one
    /// bespoke network state per station. Canonical room pattern: submit RPC →
    /// host validates through the same <see cref="ToyPatternRules"/> the solo
    /// path runs → NetworkList/variable change → every peer re-renders.
    ///
    /// Replicates only: the selected station/seed (indexes), accepted progress
    /// (object indexes plus meter values), hint/highlight state, completion,
    /// the held-piece presence flags, and compact session reward facts. NEVER
    /// per-frame drag positions and never persistent profile or
    /// child-identifying data.
    ///
    /// P21 reject channel: the submit RPC captures the SENDER client id and
    /// replies with a SendTo.SpecifiedInParams reject RPC to that sender only,
    /// echoing the client's submission id; controllers ignore stale echoes.
    /// On the host the reject event invokes synchronously inside the submit
    /// call stack — handlers must defer one frame before reacting.
    ///
    /// Attempt lifecycle mirrors the rooms: <see cref="BeginAttempt"/> resets
    /// only AFTER a completed attempt; a player entering while the partner is
    /// mid-attempt joins the in-progress attempt. Post-completion submissions
    /// are ignored silently (completion guard, no reject spam).
    ///
    /// This behaviour rides the always-spawned CampusSessionState NetworkObject
    /// (EmoteRelay precedent): CampusSessionState.Awake adds it on every peer
    /// at scene load, before any network spawn.
    /// </summary>
    public class StationProgressNetworkState : NetworkBehaviour
    {
        private readonly NetworkVariable<int> _stationIndex = new(-1);
        private readonly NetworkVariable<int> _seedIndex = new(-1);
        private readonly NetworkVariable<int> _attemptNumber = new(1);
        private readonly NetworkVariable<bool> _complete = new(false);
        private readonly NetworkVariable<int> _hintLevel = new(0);
        private readonly NetworkVariable<int> _highlightObjectIndex = new(-1);
        private readonly NetworkList<int> _acceptedObjectIndexes = new();
        private readonly NetworkList<int> _meterValues = new();
        private readonly NetworkList<HeldPieceEntry> _heldPieces = new();
        private readonly NetworkList<StationRewardFact> _rewardFacts = new();

        public event Action Changed;

        /// <summary>
        /// Sender-side reject notification: (objectIndex, submissionId, reason).
        /// On the host this is invoked synchronously inside the submit call
        /// stack — client handlers must defer one frame before reacting.
        /// </summary>
        public event Action<int, int, ToyRejectReason> ActionRejected;

        // The rules mirror: mapping (ids <-> indexes) on every peer, and the
        // authoritative progress holder on the server. Rebuilt lazily whenever
        // the synced (station, seed, attempt) tuple changes, then replayed from
        // the replicated accepted/meter lists so it is consistent either way.
        private ToyPatternRules _rules;
        private int _rulesStationIndex = -1;
        private int _rulesSeedIndex = -1;
        private int _rulesAttemptNumber = -1;

        public bool HasActiveStation => _stationIndex.Value >= 0;
        public int AttemptNumber => _attemptNumber.Value;
        public bool Complete => _complete.Value;
        public int AcceptedCount => _acceptedObjectIndexes.Count;
        public int HintLevel => _hintLevel.Value;
        public int HighlightObjectIndex => _highlightObjectIndex.Value;

        public string StationId
        {
            get
            {
                var index = _stationIndex.Value;
                return index >= 0 && index < PartyStationDefinitions.All.Count
                    ? PartyStationDefinitions.All[index].Id
                    : null;
            }
        }

        public string SeedId
        {
            get
            {
                var index = _stationIndex.Value;
                if (index < 0 || index >= PartyStationDefinitions.All.Count)
                {
                    return null;
                }

                var seeds = PartyStationDefinitions.All[index].Seeds;
                var seedIndex = _seedIndex.Value;
                return seedIndex >= 0 && seedIndex < seeds.Count ? seeds[seedIndex].SeedId : null;
            }
        }

        public string HighlightObjectId => ObjectIdFor(_highlightObjectIndex.Value);

        // Host-side seams: the last reject decision the validator made. Lets
        // host-only tests assert reject targeting without a second in-process
        // NetworkManager (true two-client delivery is manual 2P evidence).
        public ulong LastRejectClientId { get; private set; }
        public int LastRejectObjectIndex { get; private set; } = -1;
        public int LastRejectSubmissionId { get; private set; } = -1;
        public ToyRejectReason LastRejectReason { get; private set; } = ToyRejectReason.None;

        public override void OnNetworkSpawn()
        {
            _stationIndex.OnValueChanged += HandleIntChanged;
            _seedIndex.OnValueChanged += HandleIntChanged;
            _attemptNumber.OnValueChanged += HandleIntChanged;
            _complete.OnValueChanged += HandleBoolChanged;
            _hintLevel.OnValueChanged += HandleIntChanged;
            _highlightObjectIndex.OnValueChanged += HandleIntChanged;
            _acceptedObjectIndexes.OnListChanged += HandleIntListChanged;
            _meterValues.OnListChanged += HandleIntListChanged;
            _heldPieces.OnListChanged += HandleHeldPiecesChanged;
            _rewardFacts.OnListChanged += HandleRewardFactsChanged;
        }

        public override void OnNetworkDespawn()
        {
            _stationIndex.OnValueChanged -= HandleIntChanged;
            _seedIndex.OnValueChanged -= HandleIntChanged;
            _attemptNumber.OnValueChanged -= HandleIntChanged;
            _complete.OnValueChanged -= HandleBoolChanged;
            _hintLevel.OnValueChanged -= HandleIntChanged;
            _highlightObjectIndex.OnValueChanged -= HandleIntChanged;
            _acceptedObjectIndexes.OnListChanged -= HandleIntListChanged;
            _meterValues.OnListChanged -= HandleIntListChanged;
            _heldPieces.OnListChanged -= HandleHeldPiecesChanged;
            _rewardFacts.OnListChanged -= HandleRewardFactsChanged;
        }

        // ------------------------------------------------------------------
        // Read model (every peer renders accepted shared state from here)
        // ------------------------------------------------------------------

        public bool IsObjectAccepted(int objectIndex)
        {
            return objectIndex >= 0 && _acceptedObjectIndexes.Contains(objectIndex);
        }

        public bool IsObjectAccepted(string objectId)
        {
            return IsObjectAccepted(ObjectIndexFor(objectId));
        }

        /// <summary>Synced meter value by meter order (rules.MeterObjectIds order).</summary>
        public int MeterValueAt(int meterIndex)
        {
            return meterIndex >= 0 && meterIndex < _meterValues.Count
                ? _meterValues[meterIndex]
                : ToyPatternRules.MeterStartValue;
        }

        public int MeterValue(string meterId)
        {
            var rules = EnsureRules();
            if (rules == null)
            {
                return ToyPatternRules.MeterStartValue;
            }

            for (var i = 0; i < rules.MeterObjectIds.Count; i++)
            {
                if (rules.MeterObjectIds[i] == meterId)
                {
                    return MeterValueAt(i);
                }
            }

            return ToyPatternRules.MeterStartValue;
        }

        public int ObjectIndexFor(string objectId)
        {
            var rules = EnsureRules();
            return rules != null ? rules.ObjectIndexFor(objectId) : -1;
        }

        public string ObjectIdFor(int objectIndex)
        {
            var rules = EnsureRules();
            return rules != null ? rules.ObjectIdFor(objectIndex) : null;
        }

        public int TargetIndexFor(string targetId)
        {
            var rules = EnsureRules();
            return rules != null ? rules.TargetIndexFor(targetId) : -1;
        }

        public string TargetIdFor(int targetIndex)
        {
            var rules = EnsureRules();
            return rules != null ? rules.TargetIdFor(targetIndex) : null;
        }

        // ------------------------------------------------------------------
        // Host station lifecycle
        // ------------------------------------------------------------------

        /// <summary>
        /// Host-only: opens a station surface on a validated seed selection.
        /// Every mount is a fresh attempt; reward facts persist (session log).
        /// </summary>
        public void ServerBeginStation(string stationId, string seedId)
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            var stationIndex = StationIndexFor(stationId);
            if (stationIndex < 0)
            {
                return;
            }

            var seeds = PartyStationDefinitions.All[stationIndex].Seeds;
            var seedIndex = 0;
            for (var i = 0; i < seeds.Count; i++)
            {
                if (seeds[i].SeedId == seedId)
                {
                    seedIndex = i;
                    break;
                }
            }

            _stationIndex.Value = stationIndex;
            _seedIndex.Value = seedIndex;
            _attemptNumber.Value = _attemptNumber.Value + 1;
            ResetProgressState();
        }

        /// <summary>Host-only: closes the station surface (route change). Reward facts persist.</summary>
        public void ServerEndStation()
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            _stationIndex.Value = -1;
            _seedIndex.Value = -1;
            ResetProgressState();
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
            if (!IsSpawned || !IsServer || !HasActiveStation)
            {
                return;
            }

            _attemptNumber.Value = _attemptNumber.Value + 1;
            ResetProgressState();
        }

        private void ResetProgressState()
        {
            _acceptedObjectIndexes.Clear();
            _heldPieces.Clear();
            _meterValues.Clear();
            _complete.Value = false;
            _hintLevel.Value = 0;
            _highlightObjectIndex.Value = -1;

            var rules = EnsureRules();
            if (rules != null)
            {
                rules.Reset();
                foreach (var meterId in rules.MeterObjectIds)
                {
                    _meterValues.Add(rules.MeterValue(meterId));
                }
            }
        }

        // ------------------------------------------------------------------
        // Submissions (clients submit actions; host validates; everyone renders)
        // ------------------------------------------------------------------

        public void SubmitAction(string objectId, string targetId, int value, int submissionId)
        {
            if (!IsSpawned || !HasActiveStation)
            {
                return;
            }

            var objectIndex = ObjectIndexFor(objectId);
            if (objectIndex < 0)
            {
                return;
            }

            SubmitActionRpc(objectIndex, TargetIndexFor(targetId), value, submissionId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitActionRpc(int objectIndex, int targetIndex, int value, int submissionId, RpcParams rpcParams = default)
        {
            ApplySubmission(objectIndex, targetIndex, value, submissionId, rpcParams.Receive.SenderClientId);
        }

        /// <summary>
        /// Host-side validation core. Also the 2P test seam: host-only tests call
        /// it with a simulated partner client id to exercise accept/reject races.
        /// </summary>
        public ToyActionSubmissionResult ApplySubmission(
            int objectIndex,
            int targetIndex,
            int value,
            int submissionId,
            ulong senderClientId)
        {
            if (!IsSpawned || !IsServer)
            {
                return ToyActionSubmissionResult.IgnoredNotServer;
            }

            if (!HasActiveStation)
            {
                return ToyActionSubmissionResult.IgnoredNoStation;
            }

            // Completion guard: post-completion stragglers are ignored silently.
            if (Complete)
            {
                return ToyActionSubmissionResult.IgnoredComplete;
            }

            var rules = EnsureRules();
            var objectId = rules != null ? rules.ObjectIdFor(objectIndex) : null;
            if (objectId == null)
            {
                SendReject(senderClientId, objectIndex, submissionId, ToyRejectReason.UnknownObject);
                return ToyActionSubmissionResult.Rejected;
            }

            var result = rules.Submit(new ToyAction(objectId, rules.TargetIdFor(targetIndex), value));
            switch (result.Kind)
            {
                case ToySubmissionKind.Rejected:
                    SendReject(senderClientId, objectIndex, submissionId, result.RejectReason);
                    return ToyActionSubmissionResult.Rejected;
                case ToySubmissionKind.ReactionOnly:
                    // Local flair on the submitting surface — no shared state change.
                    return ToyActionSubmissionResult.ReactionApplied;
                default:
                    if (!rules.IsMeterObject(objectId))
                    {
                        _acceptedObjectIndexes.Add(objectIndex);
                    }

                    SyncMeterValuesFrom(rules);
                    _complete.Value = rules.Complete;
                    ApplyHeldPiece(-1, senderClientId);
                    return ToyActionSubmissionResult.Accepted;
            }
        }

        private void SyncMeterValuesFrom(ToyPatternRules rules)
        {
            for (var i = 0; i < rules.MeterObjectIds.Count && i < _meterValues.Count; i++)
            {
                var current = rules.MeterValue(rules.MeterObjectIds[i]);
                if (_meterValues[i] != current)
                {
                    _meterValues[i] = current;
                }
            }
        }

        private void SendReject(ulong senderClientId, int objectIndex, int submissionId, ToyRejectReason reason)
        {
            LastRejectClientId = senderClientId;
            LastRejectObjectIndex = objectIndex;
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

            ActionRejectedRpc(
                objectIndex,
                submissionId,
                (byte)reason,
                RpcTarget.Single(senderClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        private void ActionRejectedRpc(int objectIndex, int submissionId, byte reason, RpcParams rpcParams = default)
        {
            ActionRejected?.Invoke(objectIndex, submissionId, (ToyRejectReason)reason);
        }

        // ------------------------------------------------------------------
        // Hint ladder sync (accepted hints/highlights are visible to BOTH players)
        // ------------------------------------------------------------------

        /// <summary>Any peer asks for the next hint; the host escalates and syncs it.</summary>
        public void RequestHint()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsServer)
            {
                ServerEscalateHint();
                return;
            }

            RequestHintRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestHintRpc(RpcParams rpcParams = default)
        {
            ServerEscalateHint();
        }

        /// <summary>Host-side hint escalation: level 1 clue, then level 2 highlight.</summary>
        public void ServerEscalateHint()
        {
            if (!IsSpawned || !IsServer || !HasActiveStation || Complete)
            {
                return;
            }

            var level = _hintLevel.Value >= ToyPatternController.MaxHintLevel
                ? ToyPatternController.MaxHintLevel
                : _hintLevel.Value + 1;
            var rules = EnsureRules();
            var highlight = level >= ToyPatternController.MaxHintLevel && rules != null
                ? rules.ObjectIndexFor(rules.NextExpectedObjectId)
                : -1;
            ServerSetHint(level, highlight);
        }

        public void ServerSetHint(int hintLevel, int highlightObjectIndex)
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            _hintLevel.Value = hintLevel;
            _highlightObjectIndex.Value = highlightObjectIndex;
        }

        // ------------------------------------------------------------------
        // Compact session reward facts (R17 read model: completion facts only)
        // ------------------------------------------------------------------

        public int RewardFactCount => _rewardFacts.Count;

        /// <summary>Host-only: records (or upgrades) the best tier for a completed station.</summary>
        public void ServerRecordRewardFact(string stationId, CompletionTier tier)
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            var stationIndex = StationIndexFor(stationId);
            if (stationIndex < 0)
            {
                return;
            }

            for (var i = 0; i < _rewardFacts.Count; i++)
            {
                if (_rewardFacts[i].StationIndex == stationIndex)
                {
                    if ((int)tier > _rewardFacts[i].Tier)
                    {
                        _rewardFacts[i] = new StationRewardFact { StationIndex = stationIndex, Tier = (int)tier };
                    }

                    return;
                }
            }

            _rewardFacts.Add(new StationRewardFact { StationIndex = stationIndex, Tier = (int)tier });
        }

        public bool TryGetRewardFact(string stationId, out CompletionTier tier)
        {
            tier = default;
            var stationIndex = StationIndexFor(stationId);
            if (stationIndex < 0)
            {
                return false;
            }

            foreach (var fact in _rewardFacts)
            {
                if (fact.StationIndex == stationIndex)
                {
                    tier = (CompletionTier)fact.Tier;
                    return true;
                }
            }

            return false;
        }

        public string RewardFactStationIdAt(int index)
        {
            if (index < 0 || index >= _rewardFacts.Count)
            {
                return null;
            }

            var stationIndex = _rewardFacts[index].StationIndex;
            return stationIndex >= 0 && stationIndex < PartyStationDefinitions.All.Count
                ? PartyStationDefinitions.All[stationIndex].Id
                : null;
        }

        // ------------------------------------------------------------------
        // P17 held-piece plumbing (presence flag, never drag-position mirroring)
        // ------------------------------------------------------------------

        public void SetHeldPiece(string objectId)
        {
            if (!IsSpawned)
            {
                return;
            }

            SetHeldPieceRpc(ObjectIndexFor(objectId));
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

        /// <summary>Object index the client currently holds, or -1.</summary>
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

        /// <summary>The object index held by any client OTHER than the local one, or -1.</summary>
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

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        public static int StationIndexFor(string stationId)
        {
            if (string.IsNullOrWhiteSpace(stationId))
            {
                return -1;
            }

            var stations = PartyStationDefinitions.All;
            for (var i = 0; i < stations.Count; i++)
            {
                if (stations[i].Id == stationId)
                {
                    return i;
                }
            }

            return -1;
        }

        private ToyPatternRules EnsureRules()
        {
            var stationIndex = _stationIndex.Value;
            if (stationIndex < 0 || stationIndex >= PartyStationDefinitions.All.Count)
            {
                _rules = null;
                _rulesStationIndex = -1;
                return null;
            }

            var seedIndex = _seedIndex.Value;
            var attemptNumber = _attemptNumber.Value;
            if (_rules != null
                && _rulesStationIndex == stationIndex
                && _rulesSeedIndex == seedIndex
                && _rulesAttemptNumber == attemptNumber)
            {
                return _rules;
            }

            var definition = PartyStationDefinitions.All[stationIndex];
            var seed = seedIndex >= 0 && seedIndex < definition.Seeds.Count
                ? definition.Seeds[seedIndex]
                : definition.DefaultSeed;
            _rules = ToyPatternRules.ForSeed(definition, seed);
            _rulesStationIndex = stationIndex;
            _rulesSeedIndex = seedIndex;
            _rulesAttemptNumber = attemptNumber;

            // Replay replicated progress into the fresh mirror so server-side
            // validation and client-side reads agree no matter when the mirror
            // was (re)built.
            foreach (var objectIndex in _acceptedObjectIndexes)
            {
                _rules.ForceAccept(_rules.ObjectIdFor(objectIndex));
            }

            for (var i = 0; i < _rules.MeterObjectIds.Count && i < _meterValues.Count; i++)
            {
                _rules.ForceMeterValue(_rules.MeterObjectIds[i], _meterValues[i]);
            }

            return _rules;
        }

        private void HandleIntChanged(int previous, int current)
        {
            Changed?.Invoke();
        }

        private void HandleBoolChanged(bool previous, bool current)
        {
            Changed?.Invoke();
        }

        private void HandleIntListChanged(NetworkListEvent<int> change)
        {
            Changed?.Invoke();
        }

        private void HandleHeldPiecesChanged(NetworkListEvent<HeldPieceEntry> change)
        {
            Changed?.Invoke();
        }

        private void HandleRewardFactsChanged(NetworkListEvent<StationRewardFact> change)
        {
            Changed?.Invoke();
        }
    }
}
