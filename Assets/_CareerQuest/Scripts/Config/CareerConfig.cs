using System;
using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    /// <summary>
    /// Career family tags (design doc: Career Expansion Target). Families
    /// power the ceremony subhead and the later Career Cluster Pack roadmap.
    /// </summary>
    public static class CareerFamilies
    {
        public const string CareAndCommunity = "Care & Community";
        public const string FutureTech = "Future Tech";
        public const string DesignAndBuild = "Design & Build";
        public const string StoryAndStage = "Story & Stage";
        public const string NatureAndSpace = "Nature & Space";
        public const string JusticeAndLeadership = "Justice & Leadership";

        public static readonly string[] All =
        {
            CareAndCommunity,
            FutureTech,
            DesignAndBuild,
            StoryAndStage,
            NatureAndSpace,
            JusticeAndLeadership
        };
    }

    /// <summary>How a reveal career path is supported by playable content.</summary>
    public enum CareerPathSupport
    {
        StationBacked,
        RevealSupported,
        FuturePackReady
    }

    public class CareerDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Tagline { get; }
        public IReadOnlyDictionary<string, int> TraitWeights { get; }
        public string PrimaryFamily { get; }
        public IReadOnlyList<string> SecondaryFamilies { get; }
        public CareerPathSupport Support { get; }

        public CareerDefinition(string id, string displayName, string tagline, IReadOnlyDictionary<string, int> traitWeights)
            : this(id, displayName, tagline, traitWeights, string.Empty, null, CareerPathSupport.RevealSupported)
        {
        }

        public CareerDefinition(
            string id,
            string displayName,
            string tagline,
            IReadOnlyDictionary<string, int> traitWeights,
            string primaryFamily,
            IReadOnlyList<string> secondaryFamilies,
            CareerPathSupport support)
        {
            Id = id;
            DisplayName = displayName;
            Tagline = tagline;
            TraitWeights = traitWeights;
            PrimaryFamily = primaryFamily;
            SecondaryFamilies = secondaryFamilies ?? new List<string>();
            Support = support;
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

        // 30 reveal career paths (R13): the original five plus the Party Pack
        // expansion. Trait weights stay small, positive, and explainable so
        // ranking remains readable from trait deltas. New careers are weighted
        // so the seeded showcase profile still co-leads with AI Engineer and
        // Architect (GameSessionTests guard).
        public static readonly CareerDefinition[] Careers =
        {
            new("doctor", "Doctor", "Helping people feel better with science and care.", new Dictionary<string, int>
            {
                ["Helping"] = 5,
                ["Science"] = 5,
                ["Focus"] = 3,
                ["Communication"] = 3,
                ["Reasoning"] = 2
            }, CareerFamilies.CareAndCommunity, null, CareerPathSupport.StationBacked),
            new("lawyer", "Lawyer", "Using evidence, words, and fairness to solve problems.", new Dictionary<string, int>
            {
                ["Reasoning"] = 5,
                ["Communication"] = 5,
                ["Leadership"] = 3,
                ["Focus"] = 3,
                ["Helping"] = 2
            }, CareerFamilies.JusticeAndLeadership, null, CareerPathSupport.StationBacked),
            new("ai_engineer", "AI Engineer", "Future Problem Solver: using logic and creativity to solve problems people care about.", new Dictionary<string, int>
            {
                ["Reasoning"] = 5,
                ["Creativity"] = 4,
                ["Building"] = 4,
                ["Spatial Thinking"] = 3,
                ["Collaboration"] = 3,
                ["Science"] = 2
            }, CareerFamilies.FutureTech, null, CareerPathSupport.StationBacked),
            new("artist", "Artist", "Making ideas, feelings, and stories visible.", new Dictionary<string, int>
            {
                ["Creativity"] = 5,
                ["Communication"] = 3,
                ["Focus"] = 2,
                ["Building"] = 2,
                ["Helping"] = 1
            }, CareerFamilies.StoryAndStage, null, CareerPathSupport.StationBacked),
            new("architect", "Architect", "Designing spaces where people can learn, heal, create, and work together.", new Dictionary<string, int>
            {
                ["Building"] = 5,
                ["Spatial Thinking"] = 5,
                ["Creativity"] = 4,
                ["Collaboration"] = 3,
                ["Reasoning"] = 3
            }, CareerFamilies.DesignAndBuild, null, CareerPathSupport.StationBacked),
            new("robotics_engineer", "Robotics Engineer", "Building helpful machines that work side by side with people.", new Dictionary<string, int>
            {
                ["Building"] = 5,
                ["Reasoning"] = 4,
                ["Spatial Thinking"] = 3,
                ["Collaboration"] = 2
            }, CareerFamilies.FutureTech, new[] { CareerFamilies.DesignAndBuild }, CareerPathSupport.StationBacked),
            new("chef", "Chef", "Turning fresh ideas into food that brings people together.", new Dictionary<string, int>
            {
                ["Helping"] = 4,
                ["Creativity"] = 4,
                ["Collaboration"] = 3,
                ["Focus"] = 2
            }, CareerFamilies.CareAndCommunity, null, CareerPathSupport.StationBacked),
            new("musician", "Musician", "Shaping sounds and rhythms that make people feel something.", new Dictionary<string, int>
            {
                ["Creativity"] = 5,
                ["Communication"] = 4,
                ["Focus"] = 3
            }, CareerFamilies.StoryAndStage, null, CareerPathSupport.StationBacked),
            new("veterinarian", "Veterinarian", "Caring for animals with gentle hands and sharp observation.", new Dictionary<string, int>
            {
                ["Helping"] = 5,
                ["Science"] = 4,
                ["Focus"] = 2,
                ["Communication"] = 2
            }, CareerFamilies.CareAndCommunity, null, CareerPathSupport.StationBacked),
            new("game_designer", "Game Designer", "Inventing playful rules and worlds that are fun and fair.", new Dictionary<string, int>
            {
                ["Creativity"] = 5,
                ["Reasoning"] = 4,
                ["Communication"] = 3,
                ["Building"] = 2
            }, CareerFamilies.StoryAndStage, new[] { CareerFamilies.FutureTech }, CareerPathSupport.StationBacked),
            new("teacher", "Teacher", "Helping ideas click by explaining, listening, and encouraging.", new Dictionary<string, int>
            {
                ["Communication"] = 5,
                ["Helping"] = 4,
                ["Leadership"] = 3,
                ["Creativity"] = 2
            }, CareerFamilies.CareAndCommunity, null, CareerPathSupport.StationBacked),
            new("entrepreneur", "Entrepreneur", "Spotting a need and building a bold idea around it.", new Dictionary<string, int>
            {
                ["Leadership"] = 5,
                ["Creativity"] = 4,
                ["Communication"] = 3,
                ["Reasoning"] = 2
            }, CareerFamilies.JusticeAndLeadership, new[] { CareerFamilies.DesignAndBuild }, CareerPathSupport.StationBacked),
            new("nurse", "Nurse", "Noticing what people need and caring for them with steady focus.", new Dictionary<string, int>
            {
                ["Helping"] = 5,
                ["Focus"] = 4,
                ["Science"] = 3,
                ["Communication"] = 2
            }, CareerFamilies.CareAndCommunity, null, CareerPathSupport.RevealSupported),
            new("counselor", "Counselor", "Listening closely and helping people find their next step.", new Dictionary<string, int>
            {
                ["Helping"] = 5,
                ["Communication"] = 4,
                ["Reasoning"] = 2
            }, CareerFamilies.CareAndCommunity, null, CareerPathSupport.StationBacked),
            new("community_organizer", "Community Organizer", "Bringing neighbors together to make good things happen.", new Dictionary<string, int>
            {
                ["Collaboration"] = 5,
                ["Helping"] = 4,
                ["Leadership"] = 3,
                ["Communication"] = 3
            }, CareerFamilies.CareAndCommunity, new[] { CareerFamilies.JusticeAndLeadership }, CareerPathSupport.StationBacked),
            new("data_scientist", "Data Scientist", "Finding the story hidden inside patterns and numbers.", new Dictionary<string, int>
            {
                ["Reasoning"] = 5,
                ["Science"] = 4,
                ["Focus"] = 3,
                ["Communication"] = 2
            }, CareerFamilies.FutureTech, null, CareerPathSupport.StationBacked),
            new("cybersecurity_analyst", "Cybersecurity Analyst", "Keeping people's digital worlds safe by thinking ahead.", new Dictionary<string, int>
            {
                ["Reasoning"] = 5,
                ["Focus"] = 4,
                ["Science"] = 2,
                ["Helping"] = 2
            }, CareerFamilies.FutureTech, new[] { CareerFamilies.JusticeAndLeadership }, CareerPathSupport.RevealSupported),
            new("scientist", "Scientist", "Asking big questions and testing ideas to learn how things work.", new Dictionary<string, int>
            {
                ["Science"] = 5,
                ["Reasoning"] = 4,
                ["Focus"] = 3,
                ["Creativity"] = 2
            }, CareerFamilies.FutureTech, new[] { CareerFamilies.NatureAndSpace }, CareerPathSupport.StationBacked),
            new("city_planner", "City Planner", "Designing neighborhoods where people can move, meet, and grow.", new Dictionary<string, int>
            {
                ["Building"] = 4,
                ["Spatial Thinking"] = 4,
                ["Collaboration"] = 4,
                ["Reasoning"] = 3
            }, CareerFamilies.DesignAndBuild, new[] { CareerFamilies.CareAndCommunity }, CareerPathSupport.StationBacked),
            new("inventor", "Inventor", "Sketching wild ideas and building the ones that help.", new Dictionary<string, int>
            {
                ["Creativity"] = 5,
                ["Building"] = 4,
                ["Reasoning"] = 4
            }, CareerFamilies.DesignAndBuild, new[] { CareerFamilies.FutureTech }, CareerPathSupport.StationBacked),
            new("mechanic", "Mechanic", "Listening to machines and fixing them with skill and patience.", new Dictionary<string, int>
            {
                ["Building"] = 5,
                ["Focus"] = 4,
                ["Reasoning"] = 3,
                ["Spatial Thinking"] = 2
            }, CareerFamilies.DesignAndBuild, null, CareerPathSupport.StationBacked),
            new("animator", "Animator", "Bringing drawings to life one playful frame at a time.", new Dictionary<string, int>
            {
                ["Creativity"] = 5,
                ["Focus"] = 4,
                ["Communication"] = 2,
                ["Building"] = 2
            }, CareerFamilies.StoryAndStage, null, CareerPathSupport.StationBacked),
            new("journalist", "Journalist", "Checking facts and telling true stories people need to hear.", new Dictionary<string, int>
            {
                ["Communication"] = 5,
                ["Reasoning"] = 4,
                ["Creativity"] = 2,
                ["Helping"] = 2
            }, CareerFamilies.StoryAndStage, new[] { CareerFamilies.JusticeAndLeadership }, CareerPathSupport.StationBacked),
            new("environmental_scientist", "Environmental Scientist", "Studying nature closely to help people and the planet thrive.", new Dictionary<string, int>
            {
                ["Science"] = 5,
                ["Reasoning"] = 3,
                ["Helping"] = 3,
                ["Focus"] = 2
            }, CareerFamilies.NatureAndSpace, null, CareerPathSupport.StationBacked),
            new("marine_biologist", "Marine Biologist", "Exploring ocean life and caring about every creature in it.", new Dictionary<string, int>
            {
                ["Science"] = 5,
                ["Helping"] = 3,
                ["Focus"] = 3,
                ["Creativity"] = 2
            }, CareerFamilies.NatureAndSpace, new[] { CareerFamilies.CareAndCommunity }, CareerPathSupport.StationBacked),
            new("renewable_energy_engineer", "Renewable Energy Engineer", "Building clean power that keeps communities glowing.", new Dictionary<string, int>
            {
                ["Science"] = 5,
                ["Building"] = 4,
                ["Collaboration"] = 3,
                ["Reasoning"] = 2
            }, CareerFamilies.NatureAndSpace, new[] { CareerFamilies.DesignAndBuild }, CareerPathSupport.StationBacked),
            new("pilot", "Pilot", "Staying calm and focused while guiding big journeys safely.", new Dictionary<string, int>
            {
                ["Focus"] = 5,
                ["Spatial Thinking"] = 5,
                ["Leadership"] = 3,
                ["Science"] = 2
            }, CareerFamilies.NatureAndSpace, null, CareerPathSupport.StationBacked),
            new("meteorologist", "Meteorologist", "Reading the sky's patterns so people can plan and stay ready.", new Dictionary<string, int>
            {
                ["Science"] = 5,
                ["Reasoning"] = 4,
                ["Communication"] = 3,
                ["Focus"] = 2
            }, CareerFamilies.NatureAndSpace, null, CareerPathSupport.StationBacked),
            new("emergency_planner", "Emergency Planner", "Making calm, caring plans that keep communities ready.", new Dictionary<string, int>
            {
                ["Helping"] = 4,
                ["Reasoning"] = 4,
                ["Leadership"] = 4,
                ["Focus"] = 2
            }, CareerFamilies.CareAndCommunity, new[] { CareerFamilies.JusticeAndLeadership }, CareerPathSupport.StationBacked),
            new("mission_planner", "Mission Planner", "Charting every step so a big team reaches its goal together.", new Dictionary<string, int>
            {
                ["Focus"] = 4,
                ["Reasoning"] = 4,
                ["Leadership"] = 4,
                ["Spatial Thinking"] = 3,
                ["Collaboration"] = 2
            }, CareerFamilies.NatureAndSpace, new[] { CareerFamilies.JusticeAndLeadership }, CareerPathSupport.StationBacked)
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

        public static bool TryGetCareer(string id, out CareerDefinition career)
        {
            career = Careers.FirstOrDefault(candidate => candidate.Id == id);
            return career != null;
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
