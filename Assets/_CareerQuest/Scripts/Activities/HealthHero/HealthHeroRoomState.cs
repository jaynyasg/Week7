using System.Collections.Generic;

namespace CareerQuest
{
    /// <summary>
    /// Room-scoped Health Hero state (U10, mirroring <see cref="DesignBuildRoomState"/>).
    /// Replaces the old Render-closure locals (step flags, mistakes counter,
    /// feedback string, completion latch) with an object whose replay/reset
    /// semantics match the old closure lifetime: every fresh room entry calls
    /// <see cref="ResetForAttempt"/>.
    ///
    /// Owns the client-side submission ids for the P21 reject channel and the
    /// local P13 shuffle seed: solo rooms reseed per attempt through
    /// <see cref="ContentShuffle.NextSeed"/> (guaranteed different tray order);
    /// multiplayer rooms adopt the host-synced seed via <see cref="UseSharedSeed"/>.
    /// </summary>
    public sealed class HealthHeroRoomState
    {
        public const string DefaultFeedback = "Help the patient feel better. Bring the symptom clipboard to the patient first.";
        public const string DefaultProgress = "Care steps done: 0/3.";

        private readonly Dictionary<string, int> _pendingSubmissionByPiece = new();
        private readonly HashSet<int> _completedSteps = new();
        private int _nextSubmissionId = 1;

        public int Mistakes { get; private set; }
        public string Feedback { get; set; } = DefaultFeedback;
        public bool ResultEmitted { get; private set; }

        /// <summary>Last shared attempt number this room state was synced against (2P).</summary>
        public int SyncedAttemptNumber { get; set; } = 1;

        /// <summary>Active P13 shuffle seed (local in solo, host-synced in 2P).</summary>
        public int ShuffleSeed { get; private set; }

        public int CompletedStepCount => _completedSteps.Count;
        public bool Complete => CompletedStepCount >= HealthHeroNetworkState.RequiredSteps;

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
            _pendingSubmissionByPiece.Clear();
            ShuffleSeed = ContentShuffle.NextSeed(ShuffleSeed, HealthHeroClinicLayout.PieceIds.Length);
        }

        /// <summary>2P: adopt the host-seeded shuffle (P13). Zero means "not seeded yet".</summary>
        public void UseSharedSeed(int seed)
        {
            if (seed != 0)
            {
                ShuffleSeed = seed;
            }
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
