using System;
using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    public sealed class CatalogEntry
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string BadgeName { get; }
        public string BuildingName { get; }
        public ActivityRoute Route { get; }
        public string BadgeArtKey { get; }
        public string CareerTag { get; }
        public string CampusAssetId { get; }
        public bool IsCore { get; }

        public CatalogEntry(
            string id,
            string displayName,
            string badgeName,
            string buildingName,
            ActivityRoute route,
            string badgeArtKey,
            string careerTag,
            string campusAssetId,
            bool isCore)
        {
            Id = id;
            DisplayName = displayName;
            BadgeName = badgeName;
            BuildingName = buildingName;
            Route = route;
            BadgeArtKey = badgeArtKey;
            CareerTag = careerTag;
            CampusAssetId = campusAssetId;
            IsCore = isCore;
        }

        public ActivityDefinition ToActivityDefinition()
        {
            return new ActivityDefinition(Id, DisplayName, BadgeName, BuildingName);
        }
    }

    public static class CareerQuestCatalog
    {
        public const string AiLabId = "ai_lab";
        public const string MusicStudioId = "music_studio";
        public const string RoboticsGarageId = "robotics_garage";
        public const string CommunityKitchenId = "community_kitchen";

        private static readonly CatalogEntry[] Entries =
        {
            new(
                CareerConfig.DesignBuildId,
                "Future City Design Build",
                "Future City Builder",
                "Design Build Studio",
                ActivityRoute.DesignBuild,
                "badge.design_build",
                "architect",
                "campus.design_build_studio",
                true),
            new(
                CareerConfig.HealthHeroId,
                "Health Hero Clinic",
                "Health Hero",
                "Health Hero Clinic",
                ActivityRoute.HealthHero,
                "badge.health_hero",
                "doctor",
                "campus.health_hero_clinic",
                true),
            new(
                CareerConfig.LogicCourtId,
                "Logic Court",
                "Logic Detective",
                "Logic Court",
                ActivityRoute.LogicCourt,
                "badge.logic_court",
                "lawyer",
                "campus.logic_court",
                true),
            new(
                AiLabId,
                "AI Space Lab",
                "Future Problem Solver",
                "AI Lab",
                ActivityRoute.AiLab,
                "badge.ai_lab",
                "ai_engineer",
                "campus.space_lab",
                false),
            new(
                MusicStudioId,
                "Music Studio",
                "Sound Creator",
                "Music Studio",
                ActivityRoute.MusicStudio,
                "badge.music_studio",
                "artist",
                "campus.music_studio",
                false),
            new(
                RoboticsGarageId,
                "Robotics Garage",
                "Robot Builder",
                "Robotics Garage",
                ActivityRoute.RoboticsGarage,
                "badge.robotics_garage",
                "ai_engineer",
                "campus.robotics_garage",
                false),
            new(
                CommunityKitchenId,
                "Community Kitchen",
                "Community Chef",
                "Community Kitchen",
                ActivityRoute.CommunityKitchen,
                "badge.community_kitchen",
                "doctor",
                "campus.community_kitchen",
                false)
        };

        public static IReadOnlyList<CatalogEntry> All => Entries;

        public static IEnumerable<CatalogEntry> OptionalEntries => Entries.Where(entry => !entry.IsCore);

        public static CatalogEntry GetById(string id)
        {
            return Entries.First(entry => entry.Id == id);
        }

        public static CatalogEntry GetByRoute(ActivityRoute route)
        {
            return Entries.First(entry => entry.Route == route);
        }

        public static bool TryGetByRoute(ActivityRoute route, out CatalogEntry entry)
        {
            entry = Entries.FirstOrDefault(candidate => candidate.Route == route);
            return entry != null;
        }

        public static bool IsPlayableRoute(ActivityRoute route)
        {
            return TryGetByRoute(route, out _);
        }

        public static ActivityDefinition GetActivity(string id)
        {
            return GetById(id).ToActivityDefinition();
        }
    }
}
