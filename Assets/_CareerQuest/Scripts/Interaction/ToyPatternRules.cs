using System;
using System.Collections.Generic;

namespace CareerQuest
{
    /// <summary>
    /// Pure rule logic for one station seed — accept/reject decisions, progress,
    /// and completion per <see cref="ToyPatternId"/> (U3, KTD5). No scene types
    /// and no Unity object state, so EditMode tests drive the full surface and
    /// the host validation core runs the exact same rules the solo path runs.
    ///
    /// Target id conventions (the renderer creates one DropZone per entry in
    /// <see cref="TargetIds"/>, and the rules validate against the same ids):
    /// - DragToSlot:       each chain toy goes to its own "slot.{objectId}".
    /// - SortToBin:        chain toys group into "bin.{group}" bins, where the
    ///                     group derives from the object's TraitHint (fallback:
    ///                     its own id) — wrong bin bounces gently.
    /// - PickMatchingTrio: shared "target.tray"; Clue toys are read first, then
    ///                     the core trio lands in any order.
    /// - SequenceCards:    shared "target.sequence"; chain toys must arrive in
    ///                     authored (definition) order.
    /// - ComposeSet:       shared "target.compose"; any order.
    /// - MatchAndCare:     each Clue is matched onto "mark.{its TargetId}" (the
    ///                     object it illuminates) before the core care toys go
    ///                     to the shared "target.care".
    /// - BalanceMeters:    core toys go to the shared "target.build"; every
    ///                     placement shifts the meters, and completion requires
    ///                     every meter back inside the green band.
    ///
    /// Meter rules apply to ANY pattern whose seed carries Meter-role objects
    /// (e.g. Music Remix is ComposeSet plus a tempo dial): meters start outside
    /// the green band, adjust through "meter.{objectId}" actions carrying the
    /// requested value (re-adjustable, clamped, never a fail state), and gate
    /// completion until they sit inside [MeterGreenMin..MeterGreenMax].
    ///
    /// Non-chain toys (Helper/Wildcard/Reaction/Bonus) are reaction pokes: they
    /// answer <see cref="ToySubmissionKind.ReactionOnly"/> so every listed toy
    /// does something visible (no-dead-toys rule) without advancing, blocking,
    /// or bouncing the core task.
    /// </summary>
    public sealed class ToyPatternRules
    {
        public const int MeterMin = 0;
        public const int MeterMax = 100;
        public const int MeterGreenMin = 40;
        public const int MeterGreenMax = 60;
        public const int MeterGreenTarget = 50;
        public const int MeterStartValue = 20;

        /// <summary>BalanceMeters only: each accepted placement pulls every meter down.</summary>
        public const int MeterShiftPerPlacement = -10;

        public const string TrioTrayTargetId = "target.tray";
        public const string SequenceTargetId = "target.sequence";
        public const string ComposeTargetId = "target.compose";
        public const string CareTargetId = "target.care";
        public const string BuildTargetId = "target.build";
        public const string GoalTargetId = "target.goal";
        public const string CrossTargetPrefix = "cross.";
        public const string SlotTargetPrefix = "slot.";
        public const string BinTargetPrefix = "bin.";
        public const string MarkTargetPrefix = "mark.";
        public const string MeterTargetPrefix = "meter.";
        public const string WaypointTargetPrefix = "waypoint.";

        private readonly List<PartyStationObjectDefinition> _objects = new();
        private readonly Dictionary<string, PartyStationObjectDefinition> _objectsById = new();
        private readonly List<string> _draggableOrder = new();   // golden order, chain non-meter
        private readonly List<string> _meterIds = new();
        private readonly List<string> _targetIds = new();
        private readonly Dictionary<string, string> _expectedTargetByObject = new();
        private readonly HashSet<string> _accepted = new();
        private readonly Dictionary<string, int> _meterValues = new();

        public ToyPatternId Pattern { get; }

        public ToyPatternRules(ToyPatternId pattern, IReadOnlyList<PartyStationObjectDefinition> objects)
        {
            Pattern = pattern;

            if (objects != null)
            {
                foreach (var definition in objects)
                {
                    if (definition == null || string.IsNullOrEmpty(definition.ObjectId)
                        || _objectsById.ContainsKey(definition.ObjectId))
                    {
                        continue;
                    }

                    _objects.Add(definition);
                    _objectsById[definition.ObjectId] = definition;
                }
            }

            BuildGoldenOrder();
            BuildTargets();
            Reset();
        }

        /// <summary>The canonical constructor: one rules instance per resolved seed.</summary>
        public static ToyPatternRules ForSeed(PartyStationDefinition definition, PartyStationSeedDefinition seed)
        {
            return new ToyPatternRules(definition.Pattern, definition.ResolveObjects(seed));
        }

        /// <summary>All resolved seed objects, definition order.</summary>
        public IReadOnlyList<PartyStationObjectDefinition> Objects => _objects;

        /// <summary>Chain toys the player drags to completion, in golden order.</summary>
        public IReadOnlyList<string> DraggableObjectIds => _draggableOrder;

        /// <summary>Meter-role objects, definition order.</summary>
        public IReadOnlyList<string> MeterObjectIds => _meterIds;

        /// <summary>Distinct drop-target ids the renderer must create zones for.</summary>
        public IReadOnlyList<string> TargetIds => _targetIds;

        public int RequiredCount => _draggableOrder.Count;
        public int AcceptedCount => _accepted.Count;

        public bool Complete
        {
            get
            {
                if (_accepted.Count < _draggableOrder.Count)
                {
                    return false;
                }

                foreach (var meterId in _meterIds)
                {
                    if (!IsMeterInGreen(meterId))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsAccepted(string objectId)
        {
            return objectId != null && _accepted.Contains(objectId);
        }

        public bool IsMeterObject(string objectId)
        {
            return objectId != null && _meterValues.ContainsKey(objectId);
        }

        public int MeterValue(string meterId)
        {
            return _meterValues.TryGetValue(meterId, out var value) ? value : MeterStartValue;
        }

        public bool IsMeterInGreen(string meterId)
        {
            var value = MeterValue(meterId);
            return value >= MeterGreenMin && value <= MeterGreenMax;
        }

        /// <summary>
        /// The next toy a hint highlight should point at: the first unaccepted
        /// chain toy in golden order, then the first meter outside the green
        /// band, then null once everything is satisfied.
        /// </summary>
        public string NextExpectedObjectId
        {
            get
            {
                foreach (var objectId in _draggableOrder)
                {
                    if (!_accepted.Contains(objectId))
                    {
                        return objectId;
                    }
                }

                foreach (var meterId in _meterIds)
                {
                    if (!IsMeterInGreen(meterId))
                    {
                        return meterId;
                    }
                }

                return null;
            }
        }

        /// <summary>The one target id this object is accepted onto (null for non-chain toys).</summary>
        public string ExpectedTargetFor(string objectId)
        {
            return objectId != null && _expectedTargetByObject.TryGetValue(objectId, out var targetId)
                ? targetId
                : null;
        }

        /// <summary>
        /// THE rule seam. Validates one action against the current progress and
        /// mutates progress on accept. Pattern teardown/replay calls
        /// <see cref="Reset"/>; clients mirroring host-accepted progress use
        /// <see cref="ForceAccept"/> / <see cref="ForceMeterValue"/> instead.
        /// </summary>
        public ToySubmissionResult Submit(ToyAction action)
        {
            if (Complete)
            {
                return ToySubmissionResult.Rejected(ToyRejectReason.Locked);
            }

            if (string.IsNullOrEmpty(action.ObjectId)
                || !_objectsById.TryGetValue(action.ObjectId, out var definition))
            {
                return ToySubmissionResult.Rejected(ToyRejectReason.UnknownObject);
            }

            if (definition.Role == PartyStationObjectRole.Meter)
            {
                return SubmitMeter(definition, action);
            }

            if (!definition.IsChainRole)
            {
                // Helper/Wildcard/Reaction/Bonus: react visibly, never progress.
                return ToySubmissionResult.Reaction();
            }

            if (_accepted.Contains(definition.ObjectId))
            {
                return ToySubmissionResult.Rejected(ToyRejectReason.AlreadyAccepted);
            }

            var expectedTarget = ExpectedTargetFor(definition.ObjectId);
            if (!string.Equals(action.TargetId, expectedTarget, StringComparison.Ordinal))
            {
                return ToySubmissionResult.Rejected(ToyRejectReason.WrongTarget);
            }

            if (!IsOrderSatisfied(definition))
            {
                return ToySubmissionResult.Rejected(ToyRejectReason.OutOfOrder);
            }

            _accepted.Add(definition.ObjectId);

            if (Pattern == ToyPatternId.BalanceMeters && definition.Role == PartyStationObjectRole.CoreTask)
            {
                ShiftAllMeters(MeterShiftPerPlacement);
            }

            return ToySubmissionResult.Accepted(Complete);
        }

        /// <summary>Clears progress and returns every meter to its start value.</summary>
        public void Reset()
        {
            _accepted.Clear();
            _meterValues.Clear();
            foreach (var meterId in _meterIds)
            {
                _meterValues[meterId] = MeterStartValue;
            }
        }

        /// <summary>
        /// Client mirror of a host-accepted chain toy (no validation — the host
        /// already validated; clients only render accepted shared state).
        /// </summary>
        public void ForceAccept(string objectId)
        {
            if (objectId != null && _expectedTargetByObject.ContainsKey(objectId) && !IsMeterObject(objectId))
            {
                _accepted.Add(objectId);
            }
        }

        /// <summary>Client mirror of a host-accepted meter value.</summary>
        public void ForceMeterValue(string meterId, int value)
        {
            if (IsMeterObject(meterId))
            {
                _meterValues[meterId] = Clamp(value);
            }
        }

        /// <summary>
        /// The golden action sequence that completes this seed — drives the
        /// per-pattern golden tests now and quick/demo completion later.
        /// </summary>
        public IReadOnlyList<ToyAction> BuildGoldenActionSequence()
        {
            var actions = new List<ToyAction>(_draggableOrder.Count + _meterIds.Count);
            foreach (var objectId in _draggableOrder)
            {
                actions.Add(new ToyAction(objectId, ExpectedTargetFor(objectId)));
            }

            foreach (var meterId in _meterIds)
            {
                actions.Add(new ToyAction(meterId, ExpectedTargetFor(meterId), MeterGreenTarget));
            }

            return actions;
        }

        // ------------------------------------------------------------------
        // Network index mapping (compact wire encoding: indexes, never strings)
        // ------------------------------------------------------------------

        public int ObjectIndexFor(string objectId)
        {
            if (string.IsNullOrEmpty(objectId))
            {
                return -1;
            }

            for (var i = 0; i < _objects.Count; i++)
            {
                if (_objects[i].ObjectId == objectId)
                {
                    return i;
                }
            }

            return -1;
        }

        public string ObjectIdFor(int objectIndex)
        {
            return objectIndex >= 0 && objectIndex < _objects.Count ? _objects[objectIndex].ObjectId : null;
        }

        public int TargetIndexFor(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                return -1;
            }

            for (var i = 0; i < _targetIds.Count; i++)
            {
                if (_targetIds[i] == targetId)
                {
                    return i;
                }
            }

            return -1;
        }

        public string TargetIdFor(int targetIndex)
        {
            return targetIndex >= 0 && targetIndex < _targetIds.Count ? _targetIds[targetIndex] : null;
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private ToySubmissionResult SubmitMeter(PartyStationObjectDefinition meter, ToyAction action)
        {
            var expectedTarget = ExpectedTargetFor(meter.ObjectId);
            if (!string.Equals(action.TargetId, expectedTarget, StringComparison.Ordinal))
            {
                return ToySubmissionResult.Rejected(ToyRejectReason.WrongTarget);
            }

            // Meters are re-adjustable and clamped — never occupied, never a
            // harsh fail; an out-of-green value just keeps completion gated.
            _meterValues[meter.ObjectId] = Clamp(action.Value);
            return ToySubmissionResult.Accepted(Complete);
        }

        private bool IsOrderSatisfied(PartyStationObjectDefinition definition)
        {
            switch (Pattern)
            {
                case ToyPatternId.SequenceCards:
                case ToyPatternId.TracePath:
                    // Strict authored order: only the next unaccepted chain toy
                    // lands. TracePath traces the same ordered chain — the tracer
                    // can only reach the next waypoint, never skip ahead.
                    foreach (var objectId in _draggableOrder)
                    {
                        if (!_accepted.Contains(objectId))
                        {
                            return objectId == definition.ObjectId;
                        }
                    }

                    return false;
                case ToyPatternId.PickMatchingTrio:
                case ToyPatternId.MatchAndCare:
                    // Read/match every clue before the core toys land.
                    if (definition.Role != PartyStationObjectRole.CoreTask)
                    {
                        return true;
                    }

                    foreach (var objectId in _draggableOrder)
                    {
                        if (_objectsById[objectId].Role == PartyStationObjectRole.Clue
                            && !_accepted.Contains(objectId))
                        {
                            return false;
                        }
                    }

                    return true;
                default:
                    return true;
            }
        }

        private void ShiftAllMeters(int delta)
        {
            foreach (var meterId in _meterIds)
            {
                _meterValues[meterId] = Clamp(_meterValues[meterId] + delta);
            }
        }

        private void BuildGoldenOrder()
        {
            // Clue-first patterns read their clues before the core toys; every
            // other pattern keeps authored definition order. Meters never join
            // the draggable order — they are adjusted, not placed.
            var cluesFirst = Pattern == ToyPatternId.PickMatchingTrio || Pattern == ToyPatternId.MatchAndCare;

            if (Pattern == ToyPatternId.DeduceAnswer)
            {
                // Deduction by elimination: only the FALSE candidates (CoreTask)
                // are the eliminate-chain. The one true answer is a Clue, kept
                // OUT of the chain so it never needs crossing — it survives.
                AppendChainObjects(PartyStationObjectRole.CoreTask);
            }
            else if (cluesFirst)
            {
                AppendChainObjects(PartyStationObjectRole.Clue);
                AppendChainObjects(PartyStationObjectRole.CoreTask);
            }
            else
            {
                foreach (var definition in _objects)
                {
                    if (definition.IsChainRole && definition.Role != PartyStationObjectRole.Meter)
                    {
                        _draggableOrder.Add(definition.ObjectId);
                    }
                }
            }

            foreach (var definition in _objects)
            {
                if (definition.Role == PartyStationObjectRole.Meter)
                {
                    _meterIds.Add(definition.ObjectId);
                }
            }
        }

        private void AppendChainObjects(PartyStationObjectRole role)
        {
            foreach (var definition in _objects)
            {
                if (definition.Role == role)
                {
                    _draggableOrder.Add(definition.ObjectId);
                }
            }
        }

        private void BuildTargets()
        {
            foreach (var objectId in _draggableOrder)
            {
                var targetId = ComputeExpectedTarget(_objectsById[objectId]);
                _expectedTargetByObject[objectId] = targetId;
                if (!_targetIds.Contains(targetId))
                {
                    _targetIds.Add(targetId);
                }
            }

            foreach (var meterId in _meterIds)
            {
                var targetId = MeterTargetPrefix + meterId;
                _expectedTargetByObject[meterId] = targetId;
                _targetIds.Add(targetId);
            }
        }

        private string ComputeExpectedTarget(PartyStationObjectDefinition definition)
        {
            switch (Pattern)
            {
                case ToyPatternId.DragToSlot:
                    return SlotTargetPrefix + definition.ObjectId;
                case ToyPatternId.SortToBin:
                    return BinTargetPrefix + GroupKeyFor(definition);
                case ToyPatternId.PickMatchingTrio:
                    return TrioTrayTargetId;
                case ToyPatternId.SequenceCards:
                    return SequenceTargetId;
                case ToyPatternId.TracePath:
                    // Each waypoint is its own positioned zone along the route
                    // (vs SequenceCards' single shared target) so the path reads
                    // spatially and the tracer submits each as it passes through.
                    return WaypointTargetPrefix + definition.ObjectId;
                case ToyPatternId.ComposeSet:
                    return ComposeTargetId;
                case ToyPatternId.ShootTarget:
                    // One shared goal (the rescue spot): every shot lands in the
                    // same target in any order. Distinct from SequenceCards/Trace
                    // (ordered) and from per-toy slots — the variety is the launch
                    // verb, validated onto a single goal zone.
                    return GoalTargetId;
                case ToyPatternId.DeduceAnswer:
                    // Only the false candidates reach here (the answer Clue is out
                    // of the eliminate-chain). Each crosses out onto its own zone;
                    // tapping the answer hits no cross zone -> WrongTarget bounce.
                    return CrossTargetPrefix + definition.ObjectId;
                case ToyPatternId.MatchAndCare:
                    return definition.Role == PartyStationObjectRole.Clue
                        && !string.IsNullOrEmpty(definition.TargetId)
                        ? MarkTargetPrefix + definition.TargetId
                        : CareTargetId;
                case ToyPatternId.BalanceMeters:
                    return BuildTargetId;
                default:
                    return SlotTargetPrefix + definition.ObjectId;
            }
        }

        private static string GroupKeyFor(PartyStationObjectDefinition definition)
        {
            return string.IsNullOrWhiteSpace(definition.TraitHint)
                ? definition.ObjectId
                : definition.TraitHint.ToLowerInvariant().Replace(' ', '_');
        }

        private static int Clamp(int value)
        {
            return value < MeterMin ? MeterMin : value > MeterMax ? MeterMax : value;
        }
    }
}
