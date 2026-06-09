using System;
using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    public class GameSession
    {
        private readonly Dictionary<string, MiniGameResult> _bestResults = new();

        public AppMode Mode { get; private set; } = AppMode.Entry;
        public ConnectionMode ConnectionMode { get; private set; } = ConnectionMode.None;
        public CareerDnaProfile CareerDna { get; } = new();
        public string DebugSourceSummary { get; private set; } = "Live";
        public string CurrentShowcaseStep { get; set; } = "None";
        public int PlayerCount { get; set; }

        public IReadOnlyCollection<MiniGameResult> BestResults => _bestResults.Values;
        public bool HasSeededResults => _bestResults.Values.Any(result => result.IsSeeded || result.Source == ResultSource.ShowcaseSeed);
        public bool RevealReady => _bestResults.Count >= 1;
        public string LastResultId { get; private set; } = "None";

        public event Action Changed;

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

        public bool RecordResult(MiniGameResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.ActivityId))
            {
                return false;
            }

            if (!_bestResults.TryGetValue(result.ActivityId, out var current) || result.IsBetterThan(current))
            {
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
                return "Keep exploring";
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
