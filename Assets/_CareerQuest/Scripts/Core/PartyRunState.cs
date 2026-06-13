using System.Collections.Generic;

namespace CareerQuest
{
    /// <summary>
    /// U9 (R18, KTD7): the session-only guided "Party Run" sequence state. It is
    /// a PRESENTER layer over session results, NEVER progression law — starting,
    /// resuming, or quitting a run changes only the guided ordering, never the
    /// earned <see cref="GameSession"/> results, accessories, badges, traits, or
    /// evolution pieces. Normal free-choice campus play ignores this object
    /// entirely (a station entered outside the run never advances it).
    ///
    /// Shape (design doc: "lightweight guided-run state"):
    /// - <see cref="StationIds"/>: the ordered round station ids,
    /// - <see cref="SeedIds"/>: the selected seed id per round (parallel list),
    /// - <see cref="CurrentRound"/>: the round index the run is waiting on,
    /// - <see cref="CompletedStationIds"/>: rounds finished IN this run,
    /// - <see cref="IsActive"/>/<see cref="IsComplete"/>: run flags,
    /// - the progress-strip rows derive from the above (<see cref="ProgressStrip"/>).
    ///
    /// It survives route changes (campus, gallery, non-run rooms) so "Continue
    /// Party Run" resumes after a detour; it is reset on <see cref="Clear"/>
    /// (explicit Quit), which wipes ONLY this object.
    /// </summary>
    public sealed class PartyRunState
    {
        private readonly List<string> _stationIds = new();
        private readonly List<string> _seedIds = new();
        private readonly List<string> _completedStationIds = new();

        /// <summary>The ordered round station ids (empty when no run is active).</summary>
        public IReadOnlyList<string> StationIds => _stationIds;

        /// <summary>The selected seed id per round, parallel to <see cref="StationIds"/>.</summary>
        public IReadOnlyList<string> SeedIds => _seedIds;

        /// <summary>Station ids completed within this guided run, in completion order.</summary>
        public IReadOnlyList<string> CompletedStationIds => _completedStationIds;

        /// <summary>True between Start and Quit (resumable across route changes).</summary>
        public bool IsActive { get; private set; }

        /// <summary>True once every round has been completed (run reached its end).</summary>
        public bool IsComplete { get; private set; }

        /// <summary>The round index the run is currently waiting on (== count when done).</summary>
        public int CurrentRound { get; private set; }

        /// <summary>Total rounds in the active run.</summary>
        public int RoundCount => _stationIds.Count;

        /// <summary>The station id the current round expects, or null when none/complete.</summary>
        public string CurrentStationId =>
            IsActive && CurrentRound >= 0 && CurrentRound < _stationIds.Count
                ? _stationIds[CurrentRound]
                : null;

        /// <summary>The selected seed id for the current round, or null.</summary>
        public string CurrentSeedId =>
            IsActive && CurrentRound >= 0 && CurrentRound < _seedIds.Count
                ? _seedIds[CurrentRound]
                : null;

        /// <summary>
        /// Starts a guided run over the given ordered station ids and parallel
        /// seed ids. A null/short seed list pads with nulls (the station picks
        /// its default seed). Re-starting replaces any prior run cleanly. Returns
        /// false for an empty station list.
        /// </summary>
        public bool Start(IReadOnlyList<string> stationIds, IReadOnlyList<string> seedIds = null)
        {
            if (stationIds == null || stationIds.Count == 0)
            {
                return false;
            }

            _stationIds.Clear();
            _seedIds.Clear();
            _completedStationIds.Clear();

            for (var i = 0; i < stationIds.Count; i++)
            {
                _stationIds.Add(stationIds[i]);
                _seedIds.Add(seedIds != null && i < seedIds.Count ? seedIds[i] : null);
            }

            IsActive = true;
            IsComplete = false;
            CurrentRound = 0;
            return true;
        }

        /// <summary>
        /// Records a station completion against the active run. Only the run's
        /// CURRENT-round station advances it (KTD7: a free-choice completion of
        /// an out-of-order or non-run station never moves the guided sequence).
        /// Returns true when this completion advanced the run.
        /// </summary>
        public bool NoteStationCompleted(string stationId)
        {
            if (!IsActive || IsComplete || string.IsNullOrWhiteSpace(stationId))
            {
                return false;
            }

            if (!string.Equals(stationId, CurrentStationId, System.StringComparison.Ordinal))
            {
                return false;
            }

            _completedStationIds.Add(stationId);
            CurrentRound++;
            if (CurrentRound >= _stationIds.Count)
            {
                CurrentRound = _stationIds.Count;
                IsComplete = true;
            }

            return true;
        }

        /// <summary>
        /// True when the run has finished enough unique completions to route to
        /// reveal. Mirrors the normal reveal gate (design doc: a guided run must
        /// not bypass reveal-readiness — it routes only at >= 3 completions).
        /// </summary>
        public bool ReadyForRevealHandoff => IsActive && _completedStationIds.Count >= 3;

        /// <summary>
        /// Quit/reset: clears ONLY the guided sequencing state. Callers must
        /// never touch <see cref="GameSession"/> results here — earned rewards
        /// persist (design doc: "Quit Party Run clears only guided-run state").
        /// </summary>
        public void Clear()
        {
            _stationIds.Clear();
            _seedIds.Clear();
            _completedStationIds.Clear();
            IsActive = false;
            IsComplete = false;
            CurrentRound = 0;
        }

        /// <summary>
        /// The progress-strip rows (one per round) for the presenter + debug
        /// overlay. Derived, not stored — the strip can never disagree with the
        /// round/completion state above.
        /// </summary>
        public IReadOnlyList<PartyRunStepStatus> ProgressStrip
        {
            get
            {
                var rows = new List<PartyRunStepStatus>(_stationIds.Count);
                for (var i = 0; i < _stationIds.Count; i++)
                {
                    var state = i < CurrentRound
                        ? PartyRunStepState.Done
                        : i == CurrentRound && IsActive && !IsComplete
                            ? PartyRunStepState.Current
                            : PartyRunStepState.Upcoming;
                    rows.Add(new PartyRunStepStatus(_stationIds[i], _seedIds[i], state));
                }

                return rows;
            }
        }
    }

    /// <summary>One progress-strip cell's render state (non-color cue: shape/label too).</summary>
    public enum PartyRunStepState
    {
        Upcoming,
        Current,
        Done
    }

    /// <summary>A single guided-run progress-strip row (station id + seed + state).</summary>
    public readonly struct PartyRunStepStatus
    {
        public PartyRunStepStatus(string stationId, string seedId, PartyRunStepState state)
        {
            StationId = stationId;
            SeedId = seedId;
            State = state;
        }

        public string StationId { get; }
        public string SeedId { get; }
        public PartyRunStepState State { get; }
    }
}
