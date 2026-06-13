using System;
using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    public class GameSession
    {
        private readonly Dictionary<string, MiniGameResult> _bestResults = new();
        private readonly List<string> _completionOrder = new();

        // U6 (R11): the session reward-event log is owned here so the session
        // layer can append one event per station completion (host) and so the
        // passport/gallery read the same derived list on host and client. It is
        // presentation-only (KTD8) — nothing here feeds Career DNA, ranking, or
        // reveal readiness.
        private readonly RewardEventLog _rewardLog = new();

        // U6: the replicated completed-activity order + tier for the 2P read
        // model. On a client (network read model) CompletedActivityIds returns
        // this list so accessory/combo derivation matches the host exactly.
        private readonly List<string> _networkCompletionOrder = new();
        private readonly Dictionary<string, CompletionTier> _networkBestTier = new();

        // U9: session-only guided-run sequencing (R18/KTD7) and classroom access
        // settings (R19). Both are hosted here so they survive route changes, but
        // neither is scoring: the party run is a PRESENTER over results, and the
        // access settings only soften presentation. A full session wipe
        // (ResetResults / new game) clears the run; the access settings are
        // stickier session preferences and only reset on explicit request.
        private readonly PartyRunState _partyRun = new();
        private readonly ClassroomAccessSettings _classroomAccess = new();

        public AppMode Mode { get; private set; } = AppMode.Entry;
        public ConnectionMode ConnectionMode { get; private set; } = ConnectionMode.None;
        public CareerDnaProfile CareerDna { get; } = new();
        public string DebugSourceSummary { get; private set; } = "Live";
        public string CurrentShowcaseStep { get; set; } = "None";
        public int PlayerCount { get; set; }
        public AvatarDefinition SelectedAvatar { get; private set; } = AvatarConfig.DefaultAvatar;
        public ActivityRoute CurrentRoute { get; private set; } = ActivityRoute.Entry;
        public SessionPhase CurrentPhase { get; private set; } = SessionPhase.Hub;

        public IReadOnlyCollection<MiniGameResult> BestResults => _bestResults.Values;
        public bool HasSeededResults => _bestResults.Values.Any(result => result.IsSeeded || result.Source == ResultSource.ShowcaseSeed);
        public int UniqueCompletedGames => _networkReadModel ? _networkUniqueCompletedGames : _bestResults.Count;
        public int GamesNeededForReveal => Math.Max(0, 3 - UniqueCompletedGames);
        public bool RevealReady => UniqueCompletedGames >= 3;
        public string LastResultId { get; private set; } = "None";

        /// <summary>
        /// Completed activity ids in first-completion order (U6). On the host
        /// this is the live completion order; in network-read-model mode it is
        /// the replicated completed-activity order, so accessory and combo
        /// derivation match the host on every client (newest-per-slot wins).
        /// </summary>
        public IReadOnlyList<string> CompletedActivityIds =>
            _networkReadModel ? _networkCompletionOrder : _completionOrder;

        /// <summary>
        /// The session reward-event log (U6, R11). Read-only to callers: the
        /// passport Results/Combos pages and the spotlight read recent events;
        /// only <see cref="AppendStationRewardEvent"/> writes to it.
        /// </summary>
        public RewardEventLog RewardLog => _rewardLog;

        /// <summary>
        /// The session-only guided "Party Run" sequence (U9, R18). A presenter
        /// over results (KTD7) — it never gates scoring or reveal readiness.
        /// </summary>
        public PartyRunState PartyRun => _partyRun;

        /// <summary>The session-only classroom access settings (U9, R19), local and resettable.</summary>
        public ClassroomAccessSettings ClassroomAccess => _classroomAccess;

        public event Action Changed;

        private bool _networkReadModel;
        private int _networkUniqueCompletedGames;

        public void StartMode(AppMode mode)
        {
            ResetResults();
            Mode = mode;
            ConnectionMode = mode == AppMode.SoloFallback ? ConnectionMode.SoloFallback : ConnectionMode.None;
            DebugSourceSummary = mode == AppMode.Showcase ? "Showcase pending" : "Live";
            CurrentShowcaseStep = "None";
            NotifyChanged();
        }

        public void SetConnectionMode(ConnectionMode mode)
        {
            ConnectionMode = mode;
            Mode = mode == ConnectionMode.SoloFallback ? AppMode.SoloFallback : AppMode.Play;
            DebugSourceSummary = mode == ConnectionMode.SoloFallback ? "Solo Fallback" : "Live";
            NotifyChanged();
        }

        public void SelectAvatar(string avatarId)
        {
            SelectedAvatar = AvatarConfig.GetAvatar(avatarId);
            NotifyChanged();
        }

        public void SetRoute(ActivityRoute route)
        {
            if (CurrentRoute == route)
            {
                return;
            }

            CurrentRoute = route;
            var routedPhase = PhaseFromRoute(route);
            if (CurrentPhase != SessionPhase.Ceremony || routedPhase == SessionPhase.Gallery)
            {
                CurrentPhase = routedPhase;
            }

            NotifyChanged();
        }

        public void SetSessionPhase(SessionPhase phase)
        {
            if (CurrentPhase == phase)
            {
                return;
            }

            CurrentPhase = phase;
            NotifyChanged();
        }

        public static SessionPhase PhaseFromRoute(ActivityRoute route)
        {
            switch (route)
            {
                case ActivityRoute.Gallery:
                    return SessionPhase.Gallery;
                case ActivityRoute.DesignBuild:
                case ActivityRoute.HealthHero:
                case ActivityRoute.LogicCourt:
                case ActivityRoute.AiLab:
                case ActivityRoute.MusicStudio:
                case ActivityRoute.RoboticsGarage:
                case ActivityRoute.CommunityKitchen:
                case ActivityRoute.PartyStation:
                    return SessionPhase.InRoom;
                default:
                    return SessionPhase.Hub;
            }
        }

        public void ApplyNetworkSnapshot(SessionPhase phase, ActivityRoute route, int playerCount, int uniqueCompletedGames)
        {
            ApplyNetworkSnapshot(phase, route, playerCount, uniqueCompletedGames, null);
        }

        /// <summary>
        /// U6 2P read model: clients adopt the host's phase/route/player count and
        /// unique completion count PLUS the compact completed-activity snapshots
        /// (first-completion order + best tier). The snapshots feed
        /// <see cref="CompletedActivityIds"/> and <see cref="CompletedTier"/> so
        /// accessory slots, combo eligibility, and passport/gallery entries derive
        /// identically to the host. Never names, free text, or profile data (R17).
        /// </summary>
        public void ApplyNetworkSnapshot(
            SessionPhase phase,
            ActivityRoute route,
            int playerCount,
            int uniqueCompletedGames,
            IReadOnlyList<CompletedActivitySnapshot> completedActivities)
        {
            _networkReadModel = true;
            _networkUniqueCompletedGames = uniqueCompletedGames;

            _networkCompletionOrder.Clear();
            _networkBestTier.Clear();
            if (completedActivities != null)
            {
                foreach (var snapshot in completedActivities)
                {
                    if (string.IsNullOrWhiteSpace(snapshot.ActivityId) || _networkBestTier.ContainsKey(snapshot.ActivityId))
                    {
                        continue;
                    }

                    _networkCompletionOrder.Add(snapshot.ActivityId);
                    _networkBestTier[snapshot.ActivityId] = snapshot.Tier;
                }
            }

            CurrentPhase = phase;
            CurrentRoute = route;
            PlayerCount = playerCount;
            NotifyChanged();
        }

        public void ClearNetworkReadModel()
        {
            _networkReadModel = false;
            _networkUniqueCompletedGames = 0;
            _networkCompletionOrder.Clear();
            _networkBestTier.Clear();
        }

        /// <summary>
        /// Best tier earned for a completed activity (U6). On the host it reads
        /// the best result's tier; on a client it reads the replicated compact
        /// fact. Returns false for activities not completed in this read model.
        /// </summary>
        public bool CompletedTier(string activityId, out CompletionTier tier)
        {
            if (_networkReadModel)
            {
                return _networkBestTier.TryGetValue(activityId, out tier);
            }

            if (_bestResults.TryGetValue(activityId, out var result))
            {
                tier = result.Tier;
                return true;
            }

            tier = default;
            return false;
        }

        /// <summary>
        /// U6 (R11): appends one reward event for a station completion to the
        /// session log, using <see cref="CompletedActivityIds"/> for combo-spark
        /// eligibility. Replays append even when the best result does not change
        /// (the Results page always shows the latest seed-aware micro-result).
        /// Presentation only (KTD8) — never touches best results or scoring.
        /// </summary>
        public RewardEvent AppendStationRewardEvent(StationRewardEvent stationEvent)
        {
            var rewardEvent = _rewardLog.Append(stationEvent, CompletedActivityIds);
            NotifyChanged();
            return rewardEvent;
        }

        public bool RecordResult(MiniGameResult result)
        {
            if (_networkReadModel)
            {
                return false;
            }

            if (result == null || string.IsNullOrWhiteSpace(result.ActivityId))
            {
                return false;
            }

            if (!_bestResults.TryGetValue(result.ActivityId, out var current) || result.IsBetterThan(current))
            {
                // First completion of this id joins the completion order exactly
                // once — a better-result replacement of an already-present id
                // never re-appends (so unique count and earn order stay stable).
                if (current == null)
                {
                    _completionOrder.Add(result.ActivityId);
                }

                _bestResults[result.ActivityId] = result;
                LastResultId = result.ActivityId;
                DebugSourceSummary = result.IsSeeded || result.Source == ResultSource.ShowcaseSeed ? "Showcase seeded" : result.Source.ToString();
                Recompute();
                NotifyChanged();
                return true;
            }

            return false;
        }

        public MiniGameResult GetBestResult(string activityId)
        {
            return _bestResults.TryGetValue(activityId, out var result) ? result : null;
        }

        public string ConfidencePhrase()
        {
            if (!RevealReady)
            {
                return GamesNeededForReveal == 1 ? "One more game" : $"{GamesNeededForReveal} games to go";
            }

            var degreeCount = _bestResults.Values.Count(result => result.Tier == CompletionTier.Degree);

            if (_bestResults.Count >= 3 && degreeCount >= 2)
            {
                return "Very strong match";
            }

            if (_bestResults.Count >= 2 || degreeCount >= 2)
            {
                return "Strong match";
            }

            return "Good match";
        }

        public IReadOnlyList<CareerMatch> CareerMatches()
        {
            return CareerConfig.RankCareers(CareerDna);
        }

        public IReadOnlyList<CareerMatch> CoLeadMatches()
        {
            var ranked = CareerMatches();

            if (ranked.Count == 0)
            {
                return Array.Empty<CareerMatch>();
            }

            var topScore = ranked[0].Score;
            return ranked.Where(match => topScore - match.Score <= 5).Take(3).ToList();
        }

        public void SeedShowcase()
        {
            StartMode(AppMode.Showcase);

            foreach (var result in ShowcaseSeedConfig.CreativeTechnicalBuilderResults())
            {
                RecordResult(result);
            }

            DebugSourceSummary = "Showcase seeded";
            CurrentShowcaseStep = "Seeded route ready";
            NotifyChanged();
        }

        public void ResetResults()
        {
            _bestResults.Clear();
            _completionOrder.Clear();
            _rewardLog.Clear();
            // U9: a full session wipe (new game / mode start) has no guided run
            // in flight — clear ONLY the sequencing state. This is the session
            // reset path, NOT the in-run Quit path (CareerQuestApp calls
            // PartyRun.Clear directly for an explicit Quit, preserving results).
            _partyRun.Clear();
            LastResultId = "None";
            CareerDna.Recompute(_bestResults.Values);
        }

        private void Recompute()
        {
            CareerDna.Recompute(_bestResults.Values);
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
