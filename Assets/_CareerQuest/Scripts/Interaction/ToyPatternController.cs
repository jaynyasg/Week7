using System;

namespace CareerQuest
{
    /// <summary>
    /// Per-pattern play orchestration for one station seed (U3): attempt
    /// lifecycle, submission ids for the 2P reject channel, the recoverable
    /// hint ladder, and completion detection over <see cref="ToyPatternRules"/>.
    /// Plain C# (no scene types) so EditMode tests drive the full surface; the
    /// U4 PartyStationController owns rendering, networking wiring, and result
    /// emission on top of this.
    ///
    /// Mirrors the room-state contracts the three converted drag rooms proved:
    /// - Submission ids: <see cref="BeginSubmission"/> /
    ///   <see cref="IsCurrentSubmission"/> make stale rejects recognizable so
    ///   they never bounce a newer drag (P21).
    /// - Hint ladder (design doc): a wrong attempt or idle time raises a text
    ///   clue (level 1); a second raises the object highlight (level 2); an
    ///   accepted action recovers the ladder back to no hint. Never a fail state.
    /// - Lock semantics: completion or an emitted result locks the surface; a
    ///   locked submission bounces gently as <see cref="ToyRejectReason.Locked"/>.
    /// </summary>
    public sealed class ToyPatternController
    {
        public const float IdleHintSeconds = 8f;
        public const int MaxHintLevel = 2;

        private readonly ToySubmissionTracker _submissions = new();
        private float _idleSeconds;

        public PartyStationDefinition Definition { get; }
        public PartyStationSeedDefinition Seed { get; }
        public ToyPatternRules Rules { get; private set; }

        public ToyPatternId Pattern => Definition.Pattern;
        public string StationId => Definition.Id;
        public string SeedId => Seed.SeedId;

        public bool Complete => Rules.Complete;
        public bool ResultEmitted { get; private set; }

        /// <summary>External lock input (ceremony, route transition) — U4 sets it.</summary>
        public bool ExternalLock { get; set; }

        public bool IsLocked => ResultEmitted || Complete || ExternalLock;

        /// <summary>Last shared attempt number this controller was synced against (2P).</summary>
        public int SyncedAttemptNumber { get; set; } = 1;

        /// <summary>0 = no hint, 1 = text clue, 2 = clue plus object highlight.</summary>
        public int HintLevel { get; private set; }

        public int WrongAttempts { get; private set; }

        /// <summary>The toy a level-2 hint should pulse, or null below level 2 / when done.</summary>
        public string HighlightObjectId => HintLevel >= MaxHintLevel ? Rules.NextExpectedObjectId : null;

        /// <summary>Seed copy for the current hint level (level 2 escalates the line).</summary>
        public string CurrentHintLine =>
            HintLevel <= 0 ? null
            : HintLevel >= MaxHintLevel ? Seed.EscalationHintLine
            : Seed.HintLine;

        /// <summary>Fired exactly once per attempt, on the accept that completed the seed.</summary>
        public event Action Completed;

        /// <summary>Fired on rejected actions: (objectId, reason). Submitting surface only.</summary>
        public event Action<string, ToyRejectReason> ActionRejected;

        public ToyPatternController(PartyStationDefinition definition, PartyStationSeedDefinition seed)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Seed = seed ?? definition.DefaultSeed ?? throw new ArgumentNullException(nameof(seed));
            Rules = ToyPatternRules.ForSeed(Definition, Seed);
        }

        /// <summary>
        /// THE action seam (solo path; the 2P path submits to the host and
        /// mirrors accepted shared state through <see cref="ApplyAuthoritativeAccept"/>).
        /// </summary>
        public ToySubmissionResult TrySubmitAction(ToyAction action)
        {
            if (IsLocked)
            {
                var locked = ToySubmissionResult.Rejected(ToyRejectReason.Locked);
                ActionRejected?.Invoke(action.ObjectId, ToyRejectReason.Locked);
                return locked;
            }

            var result = Rules.Submit(action);
            switch (result.Kind)
            {
                case ToySubmissionKind.Accepted:
                    RecoverHintLadder();
                    if (result.StationCompleted)
                    {
                        Completed?.Invoke();
                    }

                    break;
                case ToySubmissionKind.Rejected:
                    NoteWrongAttempt();
                    ActionRejected?.Invoke(action.ObjectId, result.RejectReason);
                    break;
            }

            return result;
        }

        /// <summary>Marks the single result emission for the attempt (raises the lock).</summary>
        public void MarkResultEmitted()
        {
            ResultEmitted = true;
        }

        /// <summary>
        /// Fresh attempt: clears progress, hint ladder, submission ids, and the
        /// result latch — the same replay semantics the room states use.
        /// </summary>
        public void ResetForAttempt()
        {
            Rules.Reset();
            ResultEmitted = false;
            HintLevel = 0;
            WrongAttempts = 0;
            _idleSeconds = 0f;
            _submissions.Reset();
        }

        // ------------------------------------------------------------------
        // Hint ladder (design doc: gentle, recoverable, never a fail state)
        // ------------------------------------------------------------------

        /// <summary>Accumulates idle time; enough idle raises the next hint level.</summary>
        public void NoteIdle(float deltaSeconds)
        {
            if (IsLocked || deltaSeconds <= 0f || HintLevel >= MaxHintLevel)
            {
                return;
            }

            _idleSeconds += deltaSeconds;
            if (_idleSeconds >= IdleHintSeconds)
            {
                _idleSeconds = 0f;
                HintLevel++;
            }
        }

        /// <summary>2P mirror: adopt the host-synced hint state on every peer.</summary>
        public void ApplyAuthoritativeHint(int hintLevel)
        {
            HintLevel = hintLevel < 0 ? 0 : hintLevel > MaxHintLevel ? MaxHintLevel : hintLevel;
        }

        private void NoteWrongAttempt()
        {
            WrongAttempts++;
            _idleSeconds = 0f;
            if (HintLevel < MaxHintLevel)
            {
                HintLevel++;
            }
        }

        private void RecoverHintLadder()
        {
            HintLevel = 0;
            _idleSeconds = 0f;
        }

        // ------------------------------------------------------------------
        // 2P client mirror (clients render accepted shared state, never
        // optimistic local-only completion)
        // ------------------------------------------------------------------

        public void ApplyAuthoritativeAccept(string objectId)
        {
            Rules.ForceAccept(objectId);
        }

        public void ApplyAuthoritativeMeter(string meterId, int value)
        {
            Rules.ForceMeterValue(meterId, value);
        }

        // ------------------------------------------------------------------
        // Submission ids (P21 reject channel)
        // ------------------------------------------------------------------

        public int BeginSubmission(string objectId)
        {
            return _submissions.Begin(objectId);
        }

        public void InvalidatePendingSubmission(string objectId)
        {
            _submissions.Invalidate(objectId);
        }

        public bool IsCurrentSubmission(string objectId, int submissionId)
        {
            return _submissions.IsCurrent(objectId, submissionId);
        }

        public void CompleteSubmission(string objectId)
        {
            _submissions.Complete(objectId);
        }

        /// <summary>
        /// Teardown discipline: drops every subscriber and clears transient hint
        /// state so a torn-down surface can never re-fire into a new route.
        /// </summary>
        public void Teardown()
        {
            Completed = null;
            ActionRejected = null;
            HintLevel = 0;
            _idleSeconds = 0f;
            _submissions.Reset();
        }
    }
}
