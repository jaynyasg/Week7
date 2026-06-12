using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    /// <summary>
    /// One handcrafted hybrid identity unlocked by completing two specific
    /// stations (design doc: Starter Combo Cards). Combos are session-only
    /// ceremony flavor, never scoring input (KTD8); the U7 CareerComboResolver
    /// picks the primary combo by strongest traits, most recent station, then
    /// AuthoredPriority.
    /// </summary>
    public sealed class CareerComboDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<string> RequiredStationIds { get; }
        public IReadOnlyList<string> FamilyBlend { get; }
        public string RevealCopy { get; }
        public int AuthoredPriority { get; }

        public CareerComboDefinition(
            string id,
            string displayName,
            IEnumerable<string> requiredStationIds,
            IEnumerable<string> familyBlend,
            string revealCopy,
            int authoredPriority)
        {
            Id = id;
            DisplayName = displayName;
            RequiredStationIds = requiredStationIds?.ToList() ?? new List<string>();
            FamilyBlend = familyBlend?.ToList() ?? new List<string>();
            RevealCopy = revealCopy;
            AuthoredPriority = authoredPriority;
        }
    }

    /// <summary>
    /// Static starter combo table (13 cards, design doc verbatim copy). Station
    /// pairs reference catalog ids, including the three core rooms.
    /// </summary>
    public static class CareerComboConfig
    {
        private static readonly CareerComboDefinition[] Definitions =
        {
            Combo(
                "combo.robot_chef",
                "Robot Chef",
                CareerQuestCatalog.RoboticsGarageId,
                CareerQuestCatalog.CommunityKitchenId,
                CareerFamilies.FutureTech,
                CareerFamilies.CareAndCommunity,
                "You mixed build-it thinking with helping people through food.",
                1),
            Combo(
                "combo.music_doctor",
                "Music Doctor",
                CareerQuestCatalog.MusicStudioId,
                CareerConfig.HealthHeroId,
                CareerFamilies.StoryAndStage,
                CareerFamilies.CareAndCommunity,
                "You used rhythm and care to make people feel supported.",
                2),
            Combo(
                "combo.space_architect",
                "Space Architect",
                CareerQuestCatalog.SpaceportId,
                CareerConfig.DesignBuildId,
                CareerFamilies.NatureAndSpace,
                CareerFamilies.DesignAndBuild,
                "You planned big journeys and built places for them to land.",
                3),
            Combo(
                "combo.courtroom_reporter",
                "Courtroom Reporter",
                CareerQuestCatalog.NewsroomId,
                CareerConfig.LogicCourtId,
                CareerFamilies.StoryAndStage,
                CareerFamilies.JusticeAndLeadership,
                "You checked facts, found patterns, and explained the case clearly.",
                4),
            Combo(
                "combo.courtroom_inventor",
                "Courtroom Inventor",
                CareerConfig.LogicCourtId,
                CareerConfig.DesignBuildId,
                CareerFamilies.JusticeAndLeadership,
                CareerFamilies.DesignAndBuild,
                "You tested ideas, weighed evidence, and built a fair solution.",
                5),
            Combo(
                "combo.climate_builder",
                "Climate Builder",
                CareerQuestCatalog.GreenCityId,
                CareerQuestCatalog.WeatherLabId,
                CareerFamilies.NatureAndSpace,
                CareerFamilies.DesignAndBuild,
                "You protected a community by building with the weather in mind.",
                6),
            Combo(
                "combo.ai_music_producer",
                "AI Music Producer",
                CareerQuestCatalog.AiLabId,
                CareerQuestCatalog.MusicStudioId,
                CareerFamilies.FutureTech,
                CareerFamilies.StoryAndStage,
                "You found patterns in data and turned them into a creative mix.",
                7),
            Combo(
                "combo.robot_care_engineer",
                "Robot Care Engineer",
                CareerQuestCatalog.RoboticsGarageId,
                CareerQuestCatalog.VetClinicId,
                CareerFamilies.FutureTech,
                CareerFamilies.CareAndCommunity,
                "You repaired helpful tools and used them with kindness.",
                8),
            Combo(
                "combo.game_studio_doctor",
                "Game Studio Doctor",
                CareerQuestCatalog.GameStudioId,
                CareerConfig.HealthHeroId,
                CareerFamilies.StoryAndStage,
                CareerFamilies.CareAndCommunity,
                "You designed playful systems that help people practice care.",
                9),
            Combo(
                "combo.data_detective",
                "Data Detective",
                CareerQuestCatalog.AiLabId,
                CareerConfig.LogicCourtId,
                CareerFamilies.FutureTech,
                CareerFamilies.JusticeAndLeadership,
                "You sorted clues, tested evidence, and followed the logic.",
                10),
            Combo(
                "combo.community_inventor",
                "Community Inventor",
                CareerQuestCatalog.CommunityKitchenId,
                CareerConfig.DesignBuildId,
                CareerFamilies.CareAndCommunity,
                CareerFamilies.DesignAndBuild,
                "You built practical ideas around what people need.",
                11),
            Combo(
                "combo.mission_medic",
                "Mission Medic",
                CareerQuestCatalog.SpaceportId,
                CareerQuestCatalog.VetClinicId,
                CareerFamilies.NatureAndSpace,
                CareerFamilies.CareAndCommunity,
                "You stayed focused under pressure and made a careful rescue plan.",
                12),
            Combo(
                "combo.sound_architect",
                "Sound Architect",
                CareerQuestCatalog.MusicStudioId,
                CareerConfig.DesignBuildId,
                CareerFamilies.StoryAndStage,
                CareerFamilies.DesignAndBuild,
                "You shaped sound like a builder, with structure and imagination.",
                13)
        };

        public static IReadOnlyList<CareerComboDefinition> All => Definitions;

        public static bool TryGetById(string id, out CareerComboDefinition definition)
        {
            definition = Definitions.FirstOrDefault(candidate => candidate.Id == id);
            return definition != null;
        }

        private static CareerComboDefinition Combo(
            string id,
            string displayName,
            string firstStationId,
            string secondStationId,
            string firstFamily,
            string secondFamily,
            string revealCopy,
            int authoredPriority)
        {
            return new CareerComboDefinition(
                id,
                displayName,
                new[] { firstStationId, secondStationId },
                new[] { firstFamily, secondFamily },
                revealCopy,
                authoredPriority);
        }
    }
}
