using System.Collections.Generic;

namespace CareerQuest
{
    /// <summary>
    /// Room-scoped Design Build state. Replaces the old Render-closure locals
    /// (blueprint progress, feedback strings, completion latch) with an object
    /// whose replay/reset semantics match the old closure lifetime exactly:
    /// every fresh room entry (Render) calls <see cref="ResetForAttempt"/>, just
    /// as the old closure variables were recreated per Render.
    ///
    /// Also owns the client-side submission ids for the P21 reject channel: each
    /// multiplayer drop submission gets a monotonically increasing id; a reject
    /// response only bounces the piece when it echoes the piece's CURRENT
    /// submission id, so a stale reject can never bounce a newer drag.
    /// </summary>
    public sealed class DesignBuildRoomState
    {
        public const string DefaultFeedback = "Drag each city piece onto its matching lot.";
        public const string DefaultProgress = "Helper clue: care, fairness, art, science, invention.";

        private readonly Dictionary<string, int> _pendingSubmissionByPiece = new();
        private int _nextSubmissionId = 1;

        public FutureCityBlueprint Blueprint { get; private set; } = FutureCityBlueprint.CreateDefault();
        public int AcceptedPlacements { get; private set; }
        public string Feedback { get; set; } = DefaultFeedback;
        public bool ResultEmitted { get; private set; }

        /// <summary>Last shared attempt number this room state was synced against (2P).</summary>
        public int SyncedAttemptNumber { get; set; } = 1;

        /// <summary>
        /// Local seed for the tray derangement: each fresh attempt reshuffles
        /// which tray slot every city piece rests in, so a piece is never sitting
        /// in the slot directly under its matching lot. Tray order is cosmetic and
        /// id-matched, so this stays local (never networked).
        /// </summary>
        public int TrayShuffleSeed { get; private set; }

        public void ResetForAttempt()
        {
            Blueprint = FutureCityBlueprint.CreateDefault();
            TrayShuffleSeed = ContentShuffle.NextSeed(TrayShuffleSeed, Blueprint.Pieces.Count);
            AcceptedPlacements = 0;
            Feedback = DefaultFeedback;
            ResultEmitted = false;
            _pendingSubmissionByPiece.Clear();
        }

        public bool TryPlaceLocal(string pieceId)
        {
            var placed = Blueprint.TryPlace(pieceId);
            if (placed)
            {
                AcceptedPlacements++;
            }

            return placed;
        }

        public void MarkResultEmitted()
        {
            ResultEmitted = true;
        }

        /// <summary>Allocates the submission id for a new multiplayer drop of this piece.</summary>
        public int BeginSubmission(string pieceId)
        {
            var id = _nextSubmissionId++;
            _pendingSubmissionByPiece[pieceId] = id;
            return id;
        }

        /// <summary>
        /// A new pickup of the piece invalidates any in-flight submission, so a
        /// late reject for the old submission is recognizably stale.
        /// </summary>
        public void InvalidatePendingSubmission(string pieceId)
        {
            _pendingSubmissionByPiece.Remove(pieceId);
        }

        public bool IsCurrentSubmission(string pieceId, int submissionId)
        {
            return _pendingSubmissionByPiece.TryGetValue(pieceId, out var current) && current == submissionId;
        }

        public void CompleteSubmission(string pieceId)
        {
            _pendingSubmissionByPiece.Remove(pieceId);
        }
    }
}
