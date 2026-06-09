using System;
using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    public class CareerDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Tagline { get; }
        public IReadOnlyDictionary<string, int> TraitWeights { get; }

        public CareerDefinition(string id, string displayName, string tagline, IReadOnlyDictionary<string, int> traitWeights)
        {
            Id = id;
            DisplayName = displayName;
            Tagline = tagline;
            TraitWeights = traitWeights;
        }
    }

    public class ActivityDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string BadgeName { get; }
        public string BuildingName { get; }

        public ActivityDefinition(string id, string displayName, string badgeName, string buildingName)
        {
            Id = id;
            DisplayName = displayName;
            BadgeName = badgeName;
            BuildingName = buildingName;
        }
    }

    public class CareerMatch
    {
        public CareerDefinition Career { get; }
        public int Score { get; }

        public CareerMatch(CareerDefinition career, int score)
        {
            Career = career;
            Score = score;
        }
    }

    public static class CareerConfig
    {
        public const string DesignBuildId = "design_build";
        public const string HealthHeroId = "health_hero";
        public const string LogicCourtId = "logic_court";

        public static readonly string[] AllTraits =
        {
            "Helping",
            "Science",
            "Focus",
            "Reasoning",
            "Communication",
            "Leadership",
            "Creativity",
            "Building",
            "Spatial Thinking",
            "Collaboration"
        };

        public static readonly ActivityDefinition[] Activities =
        {
            new(DesignBuildId, "Future City Design Build", "Future City Builder", "Design Build Studio"),
            new(HealthHeroId, "Health Hero Clinic", "Health Hero", "Health Hero Clinic"),
            new(LogicCourtId, "Logic Court", "Logic Detective", "Logic Court")
        };

        public static readonly CareerDefinition[] Careers =
        {
            new("doctor", "Doctor", "Helping people feel better with science and care.", new Dictionary<string, int>
            {
                ["Helping"] = 5,
                ["Science"] = 5,
                ["Focus"] = 3,
                ["Communication"] = 3,
                ["Reasoning"] = 2
            }),
            new("lawyer", "Lawyer", "Using evidence, words, and fairness to solve problems.", new Dictionary<string, int>
            {
                ["Reasoning"] = 5,
                ["Communication"] = 5,
                ["Leadership"] = 3,
                ["Focus"] = 3,
                ["Helping"] = 2
            }),
            new("ai_engineer", "AI Engineer", "Future Problem Solver: using logic and creativity to solve problems people care about.", new Dictionary<string, int>
            {
                ["Reasoning"] = 5,
                ["Creativity"] = 4,
                ["Building"] = 4,
                ["Spatial Thinking"] = 3,
                ["Collaboration"] = 3,
                ["Science"] = 2
            }),
            new("artist", "Artist", "Making ideas, feelings, and stories visible.", new Dictionary<string, int>
            {
                ["Creativity"] = 5,
                ["Communication"] = 3,
                ["Focus"] = 2,
                ["Building"] = 2,
                ["Helping"] = 1
            }),
            new("architect", "Architect", "Designing spaces where people can learn, heal, create, and work together.", new Dictionary<string, int>
            {
                ["Building"] = 5,
                ["Spatial Thinking"] = 5,
                ["Creativity"] = 4,
                ["Collaboration"] = 3,
                ["Reasoning"] = 3
            })
        };

        public static readonly string[] FutureBuildingLabels =
        {
            "Space Lab",
            "Music Studio",
            "Green Energy Center",
            "Robotics Garage",
            "Community Kitchen"
        };

        public static ActivityDefinition GetActivity(string id)
        {
            return Activities.First(activity => activity.Id == id);
        }

        public static IReadOnlyList<CareerMatch> RankCareers(CareerDnaProfile profile)
        {
            return Careers
                .Select(career => new CareerMatch(career, ScoreCareer(career, profile)))
                .OrderByDescending(match => match.Score)
                .ThenBy(match => match.Career.DisplayName, StringComparer.Ordinal)
                .ToList();
        }

        private static int ScoreCareer(CareerDefinition career, CareerDnaProfile profile)
        {
            var score = 0;

            foreach (var weight in career.TraitWeights)
            {
                score += profile.GetTraitTotal(weight.Key) * weight.Value;
            }

            return score;
        }
    }
}
