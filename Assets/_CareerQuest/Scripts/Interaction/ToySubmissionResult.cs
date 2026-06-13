using System.Collections.Generic;

namespace CareerQuest
{
    /// <summary>
    /// Drop seam outcome shared by every drag room and station surface (U3:
    /// promoted out of DesignBuildController where it was born in U6 — Health
    /// Hero and Logic Court already consumed it, so the type now lives with the
    /// interaction layer it describes). Values are unchanged; every existing
    /// TrySubmitDrop seam and test keeps compiling against the same names.
    /// </summary>
    public enum DropSubmitResult
    {
        Accepted,
        Pending,
        RejectedWrongSlot,
        RejectedOccupied,
        RejectedLocked,
        RejectedUnknownPiece
    }

    /// <summary>
    /// Why a toy pattern rule rejected an action. Byte-backed so the station
    /// reject channel can echo it over the wire exactly like the per-room
    /// reject reasons do.
    /// </summary>
    public enum ToyRejectReason : byte
    {
        None = 0,
        UnknownObject = 1,
        WrongTarget = 2,
        AlreadyAccepted = 3,
        OutOfOrder = 4,
        Locked = 5
    }

    /// <summary>How a toy pattern rule resolved a submitted action.</summary>
    public enum ToySubmissionKind
    {
        /// <summary>Progress (or a meter adjustment) was accepted.</summary>
        Accepted,

        /// <summary>
        /// A non-chain toy (Helper/Wildcard/Reaction/Bonus role) was poked:
        /// it reacts visibly (no-dead-toys rule) but never advances or blocks
        /// the core task, never occupies, and never bounces as a reject.
        /// </summary>
        ReactionOnly,

        /// <summary>The action bounced — see <see cref="ToySubmissionResult.RejectReason"/>.</summary>
        Rejected
    }

    /// <summary>
    /// Host-side validation outcome for a station action submission (the
    /// station-layer analog of <see cref="PlacementSubmissionResult"/>).
    /// </summary>
    public enum ToyActionSubmissionResult
    {
        Accepted,
        ReactionApplied,
        Rejected,
        IgnoredComplete,
        IgnoredNoStation,
        IgnoredNotServer
    }

    /// <summary>
    /// One player action against a station seed: drop ObjectId onto TargetId.
    /// Value is only meaningful for Meter-role objects (the requested meter
    /// value); zero otherwise.
    /// </summary>
    public readonly struct ToyAction
    {
        public string ObjectId { get; }
        public string TargetId { get; }
        public int Value { get; }

        public ToyAction(string objectId, string targetId, int value = 0)
        {
            ObjectId = objectId;
            TargetId = targetId;
            Value = value;
        }
    }

    /// <summary>
    /// The rule layer's answer to one <see cref="ToyAction"/>. Pure data — no
    /// scene types — so EditMode tests drive the full accept/reject surface.
    /// </summary>
    public readonly struct ToySubmissionResult
    {
        public ToySubmissionKind Kind { get; }
        public ToyRejectReason RejectReason { get; }

        /// <summary>True when this accept was the one that completed the station.</summary>
        public bool StationCompleted { get; }

        private ToySubmissionResult(ToySubmissionKind kind, ToyRejectReason rejectReason, bool stationCompleted)
        {
            Kind = kind;
            RejectReason = rejectReason;
            StationCompleted = stationCompleted;
        }

        public bool IsAccepted => Kind == ToySubmissionKind.Accepted;
        public bool IsRejected => Kind == ToySubmissionKind.Rejected;

        public static ToySubmissionResult Accepted(bool stationCompleted)
        {
            return new ToySubmissionResult(ToySubmissionKind.Accepted, ToyRejectReason.None, stationCompleted);
        }

        public static ToySubmissionResult Reaction()
        {
            return new ToySubmissionResult(ToySubmissionKind.ReactionOnly, ToyRejectReason.None, false);
        }

        public static ToySubmissionResult Rejected(ToyRejectReason reason)
        {
            return new ToySubmissionResult(ToySubmissionKind.Rejected, reason, false);
        }

        /// <summary>
        /// Drag-shell mapping for the shared drop outcome handling. ReactionOnly
        /// maps to Accepted (it is an acknowledged interaction, never a bounce);
        /// the station surface plays the reaction and returns the toy home itself.
        /// </summary>
        public DropSubmitResult ToDropSubmitResult()
        {
            if (Kind != ToySubmissionKind.Rejected)
            {
                return DropSubmitResult.Accepted;
            }

            switch (RejectReason)
            {
                case ToyRejectReason.UnknownObject:
                    return DropSubmitResult.RejectedUnknownPiece;
                case ToyRejectReason.AlreadyAccepted:
                    return DropSubmitResult.RejectedOccupied;
                case ToyRejectReason.Locked:
                    return DropSubmitResult.RejectedLocked;
                default:
                    return DropSubmitResult.RejectedWrongSlot;
            }
        }
    }

    /// <summary>
    /// Client-side submission ids for the station reject channel — the same
    /// contract the three room states implement individually (P21): each
    /// multiplayer submission gets a monotonically increasing id; a reject
    /// response only bounces the object when it echoes that object's CURRENT
    /// submission id, so a stale reject can never bounce a newer drag, and a
    /// double-delivered reject for an already-resolved submission is ignored.
    /// </summary>
    public sealed class ToySubmissionTracker
    {
        private readonly Dictionary<string, int> _pendingSubmissionByObject = new();
        private int _nextSubmissionId = 1;

        /// <summary>Allocates the submission id for a new submission of this object.</summary>
        public int Begin(string objectId)
        {
            var id = _nextSubmissionId++;
            _pendingSubmissionByObject[objectId] = id;
            return id;
        }

        /// <summary>
        /// A new pickup of the object invalidates any in-flight submission, so a
        /// late reject for the old submission is recognizably stale.
        /// </summary>
        public void Invalidate(string objectId)
        {
            _pendingSubmissionByObject.Remove(objectId);
        }

        public bool IsCurrent(string objectId, int submissionId)
        {
            return objectId != null
                && _pendingSubmissionByObject.TryGetValue(objectId, out var current)
                && current == submissionId;
        }

        public void Complete(string objectId)
        {
            if (objectId != null)
            {
                _pendingSubmissionByObject.Remove(objectId);
            }
        }

        public void Reset()
        {
            _pendingSubmissionByObject.Clear();
        }
    }
}
