using System.Collections.Generic;

namespace CareerQuest
{
    /// <summary>
    /// Room-scoped Logic Court state (U10, mirroring <see cref="DesignBuildRoomState"/>).
    /// Replaces the old Render-closure locals (caseReviewed/sort flags, mistakes,
    /// feedback, completion latch). Case review stays LOCAL per player (as the
    /// old closure flag was) — only the three evidence sorts are shared state.
    ///
    /// Owns the client-side submission ids for the P21 reject channel and the
    /// local P13 shuffle seed (evidence order); multiplayer rooms adopt the
    /// host-synced seed via <see cref="UseSharedSeed"/>.
    /// </summary>
    public sealed class LogicCourtRoomState
    {
        public const string DefaultFeedback = "Review the case file on the podium, then sort each evidence card.";
        public const string DefaultProgress = "Evidence sorted: 0/3.";

        private readonly Dictionary<string, int> _pendingSubmissionByPiece = new();
        private readonly HashSet<int> _completedSteps = new();
        private int _nextSubmissionId = 1;

        public int Mistakes { get; private set; }
        public string Feedback { get; set; } = DefaultFeedback;
        public bool ResultEmitted { get; private set; }
        public bool CaseReviewed { get; private set; }

        /// <summary>Last shared attempt number this room state was synced against (2P).</summary>
        public int SyncedAttemptNumber { get; set; } = 1;

        /// <summary>Active P13 shuffle seed (local in solo, host-synced in 2P).</summary>
        public int ShuffleSeed { get; private set; }

        public int CompletedStepCount => _completedSteps.Count;
        public bool Complete => CompletedStepCount >= LogicCourtNetworkState.RequiredSteps;

        public bool IsStepCompleteLocal(int stepIndex)
        {
            return _completedSteps.Contains(stepIndex);
        }

        public void ResetForAttempt()
        {
            _completedSteps.Clear();
            Mistakes = 0;
            Feedback = DefaultFeedback;
            ResultEmitted = false;
            CaseReviewed = false;
            _pendingSubmissionByPiece.Clear();
            ShuffleSeed = ContentShuffle.NextSeed(ShuffleSeed, LogicCourtLayout.EvidencePieceIds.Length);
        }

        /// <summary>2P: adopt the host-seeded shuffle (P13). Zero means "not seeded yet".</summary>
        public void UseSharedSeed(int seed)
        {
            if (seed != 0)
            {
                ShuffleSeed = seed;
            }
        }

        public void MarkCaseReviewed()
        {
            CaseReviewed = true;
        }

        public bool TryCompleteStepLocal(int stepIndex)
        {
            return _completedSteps.Add(stepIndex);
        }

        public void CountMistake()
        {
            Mistakes++;
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
