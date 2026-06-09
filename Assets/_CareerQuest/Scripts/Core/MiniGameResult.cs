using System;
using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    public enum CompletionTier
    {
        Practice = 0,
        Degree = 1
    }

    public enum ResultSource
    {
        Multiplayer,
        Solo,
        SoloFallback,
        ShowcaseSeed
    }

    [Serializable]
    public struct TraitDelta
    {
        public string Trait;
        public int Delta;

        public TraitDelta(string trait, int delta)
        {
            Trait = trait;
            Delta = delta;
        }
    }

    [Serializable]
    public class MiniGameResult
    {
        public string ActivityId;
        public string DisplayName;
        public CompletionTier Tier;
        public ResultSource Source;
        public float TimeRemaining;
        public float Accuracy;
        public string Summary;
        public bool IsSeeded;
        public List<TraitDelta> TraitDeltas = new();

        public MiniGameResult()
        {
        }

        public MiniGameResult(
            string activityId,
            string displayName,
            CompletionTier tier,
            ResultSource source,
            IEnumerable<TraitDelta> traitDeltas,
            float timeRemaining,
            float accuracy,
            string summary,
            bool isSeeded = false)
        {
            ActivityId = activityId;
            DisplayName = displayName;
            Tier = tier;
            Source = source;
            TraitDeltas = traitDeltas?.ToList() ?? new List<TraitDelta>();
            TimeRemaining = timeRemaining;
            Accuracy = accuracy;
            Summary = summary;
            IsSeeded = isSeeded;
        }

        public bool IsBetterThan(MiniGameResult other)
        {
            if (other == null)
            {
                return true;
            }

            if (Tier != other.Tier)
            {
                return Tier > other.Tier;
            }

            if (!Approximately(TimeRemaining, other.TimeRemaining))
            {
                return TimeRemaining > other.TimeRemaining;
            }

            if (!Approximately(Accuracy, other.Accuracy))
            {
                return Accuracy > other.Accuracy;
            }

            return false;
        }

        public int TraitValue(string trait)
        {
            return TraitDeltas.Where(delta => delta.Trait == trait).Sum(delta => delta.Delta);
        }

        private static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) < 0.001f;
        }
    }
}
