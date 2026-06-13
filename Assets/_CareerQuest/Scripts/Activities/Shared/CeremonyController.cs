namespace CareerQuest
{
    public enum CeremonySubPhase
    {
        Celebration,
        Feedback,
        Transition
    }

    public sealed class CeremonyController
    {
        public const float SkipDelaySeconds = 3f;
        public const float TotalBudgetSeconds = 12f;

        private const float CelebrationDuration = 3f;
        private const float FeedbackDuration = 6f;
        private const float TransitionDuration = 3f;

        private float _elapsed;
        private bool _skipped;

        public CeremonyController(MiniGameResult result)
            : this(result, null)
        {
        }

        /// <summary>
        /// U7: the station-end ceremony flow can carry the
        /// <see cref="RevealSynthesisResult"/> snapshot so its Feedback beat can
        /// surface the strength preview (top traits/paths/family/superpower) that
        /// the full reveal will expand — one resolver feeds both (KTD9). Pacing
        /// is unchanged; the snapshot is optional (null for plain station fanfare).
        /// </summary>
        public CeremonyController(MiniGameResult result, RevealSynthesisResult revealPreview)
        {
            Result = result;
            RevealPreview = revealPreview;
        }

        public MiniGameResult Result { get; }

        /// <summary>Synthesis snapshot for the strength preview, or null. Presentation only (KTD8).</summary>
        public RevealSynthesisResult RevealPreview { get; }

        /// <summary>True when this ceremony can show a synthesis-backed strength preview.</summary>
        public bool HasRevealPreview => RevealPreview != null;

        public CeremonySubPhase CurrentSubPhase { get; private set; } = CeremonySubPhase.Celebration;

        public bool IsComplete { get; private set; }

        public bool CanSkip => _elapsed >= SkipDelaySeconds && !IsComplete;

        public float ElapsedSeconds => _elapsed;

        public void Skip()
        {
            if (!CanSkip)
            {
                return;
            }

            _skipped = true;
            IsComplete = true;
        }

        public void Tick(float deltaSeconds)
        {
            if (IsComplete)
            {
                return;
            }

            _elapsed += deltaSeconds;
            CurrentSubPhase = ResolveSubPhase(_elapsed);

            if (_elapsed >= TotalBudgetSeconds)
            {
                IsComplete = true;
            }
        }

        private static CeremonySubPhase ResolveSubPhase(float elapsed)
        {
            if (elapsed < CelebrationDuration)
            {
                return CeremonySubPhase.Celebration;
            }

            if (elapsed < CelebrationDuration + FeedbackDuration)
            {
                return CeremonySubPhase.Feedback;
            }

            return CeremonySubPhase.Transition;
        }
    }
}
