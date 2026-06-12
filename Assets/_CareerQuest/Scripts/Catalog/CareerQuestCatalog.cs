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

        /// <summary>
        /// True for Party Pack entries that are entered by station id through
        /// the generic station branch (U2) instead of a dedicated
        /// ActivityRoute value. Route is a Campus placeholder for these
        /// entries and is never used for route lookup.
        /// </summary>
        public bool UsesStationIdRouting { get; }

        public CatalogEntry(
            string id,
            string displayName,
            string badgeName,
            string buildingName,
            ActivityRoute route,
            string badgeArtKey,
            string careerTag,
            string campusAssetId,
            bool isCore,
            bool usesStationIdRouting = false)
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
            UsesStationIdRouting = usesStationIdRouting;
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

        // Party Pack stations without a legacy optional room (U1). They route
        // by station id (U2), so they live in PartyEntries rather than gaining
        // an ActivityRoute value each (KTD3).
        public const string VetClinicId = "vet_clinic";
        public const string GameStudioId = "game_studio";
        public const string WeatherLabId = "weather_lab";
        public const string SpaceportId = "spaceport";
        public const string NewsroomId = "newsroom";
        public const string GreenCityId = "green_city";

        /// <summary>All 10 Party Pack station ids (4 converted optional rooms + 6 new stations).</summary>
        public static readonly string[] PartyStationIds =
        {
            RoboticsGarageId,
            AiLabId,
            CommunityKitchenId,
            MusicStudioId,
            VetClinicId,
            GameStudioId,
            WeatherLabId,
            SpaceportId,
            NewsroomId,
            GreenCityId
        };

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

        // U1: catalog identity for the six station-id routed Party Pack
        // stations. Kept out of Entries so legacy surfaces (gallery grid,
        // optional-room flows, badge art gates) are untouched until U2/U5
        // promote them; identity-level art keys are intentional placeholders
        // in AssetCatalog until the station art pass.
        private static readonly CatalogEntry[] PartyEntries =
        {
            new(
                VetClinicId,
                "Vet Clinic Diagnose",
                "Gentle Vet",
                "Vet Clinic",
                ActivityRoute.Campus,
                "badge.vet_clinic",
                "veterinarian",
                "campus.vet_clinic",
                false,
                true),
            new(
                GameStudioId,
                "Game Studio Compose",
                "Game Maker",
                "Game Studio",
                ActivityRoute.Campus,
                "badge.game_studio",
                "game_designer",
                "campus.game_studio",
                false,
                true),
            new(
                WeatherLabId,
                "Weather Lab Rescue",
                "Weather Watcher",
                "Weather Lab",
                ActivityRoute.Campus,
                "badge.weather_lab",
                "meteorologist",
                "campus.weather_lab",
                false,
                true),
            new(
                SpaceportId,
                "Spaceport Pilot",
                "Mission Pilot",
                "Spaceport",
                ActivityRoute.Campus,
                "badge.spaceport",
                "pilot",
                "campus.spaceport",
                false,
                true),
            new(
                NewsroomId,
                "Newsroom Story Sprint",
                "Story Reporter",
                "Newsroom",
                ActivityRoute.Campus,
                "badge.newsroom",
                "journalist",
                "campus.newsroom",
                false,
                true),
            new(
                GreenCityId,
                "Green City Builder",
                "Green Builder",
                "Green City Workshop",
                ActivityRoute.Campus,
                "badge.green_city",
                "renewable_energy_engineer",
                "campus.green_city",
                false,
                true)
        };

        public static IReadOnlyList<CatalogEntry> All => Entries;

        public static IEnumerable<CatalogEntry> OptionalEntries => Entries.Where(entry => !entry.IsCore);

        /// <summary>Party Pack catalog entries that have no legacy ActivityRoute (station-id routed).</summary>
        public static IReadOnlyList<CatalogEntry> PartyStationEntries => PartyEntries;

        /// <summary>Every catalog entry, including the station-id routed Party Pack additions.</summary>
        public static IEnumerable<CatalogEntry> AllWithPartyStations => Entries.Concat(PartyEntries);

        public static CatalogEntry GetById(string id)
        {
            return Entries.FirstOrDefault(entry => entry.Id == id)
                ?? PartyEntries.First(entry => entry.Id == id);
        }

        public static bool TryGetById(string id, out CatalogEntry entry)
        {
            entry = Entries.FirstOrDefault(candidate => candidate.Id == id)
                ?? PartyEntries.FirstOrDefault(candidate => candidate.Id == id);
            return entry != null;
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
