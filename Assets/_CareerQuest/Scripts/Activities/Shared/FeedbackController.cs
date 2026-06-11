using UnityEngine;

namespace CareerQuest
{
    public sealed class CeremonyPresentation
    {
        public CeremonyPresentation(string title, string message, string badgeLabel, Color accentColor, string cueId)
        {
            Title = title;
            Message = message;
            BadgeLabel = badgeLabel;
            AccentColor = accentColor;
            CueId = cueId;
        }

        public string Title { get; }
        public string Message { get; }
        public string BadgeLabel { get; }
        public Color AccentColor { get; }
        public string CueId { get; }
    }

    public static class FeedbackController
    {
        private static readonly Color DesignBuildAccent = new(247f / 255f, 108f / 255f, 94f / 255f);
        private static readonly Color HealthHeroAccent = new(88f / 255f, 200f / 255f, 148f / 255f);
        private static readonly Color LogicCourtAccent = new(242f / 255f, 163f / 255f, 59f / 255f);
        private static readonly Color DefaultAccent = new(74f / 255f, 144f / 255f, 226f / 255f);

        public static CeremonyPresentation ForResult(MiniGameResult result)
        {
            var activity = CareerConfig.GetActivity(result.ActivityId);
            var accent = AccentForActivity(result.ActivityId);
            var earnedDegree = result.Tier == CompletionTier.Degree;
            var tierLabel = earnedDegree ? "Degree" : "Practice";
            var title = earnedDegree
                ? $"{activity.DisplayName} complete!"
                : $"{activity.DisplayName} practice run";

            var message = earnedDegree
                ? $"You earned the {activity.BadgeName} badge. Keep exploring the campus!"
                : "Nice try! Practice again or head to the gallery when you are ready.";

            return new CeremonyPresentation(
                title,
                message,
                $"{activity.BadgeName} · {tierLabel}",
                accent,
                $"ceremony_{result.ActivityId}_{(earnedDegree ? "success" : "practice")}");
        }

        private static Color AccentForActivity(string activityId)
        {
            return activityId switch
            {
                CareerConfig.DesignBuildId => DesignBuildAccent,
                CareerConfig.HealthHeroId => HealthHeroAccent,
                CareerConfig.LogicCourtId => LogicCourtAccent,
                _ => DefaultAccent
            };
        }
    }
}
