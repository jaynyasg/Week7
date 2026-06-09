using System.Collections.Generic;

namespace CareerQuest
{
    public static class ShowcaseSeedConfig
    {
        public static IReadOnlyList<MiniGameResult> CreativeTechnicalBuilderResults()
        {
            return new[]
            {
                new MiniGameResult(
                    CareerConfig.DesignBuildId,
                    "Future City Design Build",
                    CompletionTier.Degree,
                    ResultSource.ShowcaseSeed,
                    new[]
                    {
                        new TraitDelta("Building", 5),
                        new TraitDelta("Spatial Thinking", 5),
                        new TraitDelta("Creativity", 4),
                        new TraitDelta("Reasoning", 3),
                        new TraitDelta("Collaboration", 3)
                    },
                    52f,
                    0.95f,
                    "Designed a future city where clinics, courts, studios, labs, and art towers work together.",
                    true),
                new MiniGameResult(
                    CareerConfig.LogicCourtId,
                    "Logic Court",
                    CompletionTier.Degree,
                    ResultSource.ShowcaseSeed,
                    new[]
                    {
                        new TraitDelta("Reasoning", 4),
                        new TraitDelta("Communication", 3),
                        new TraitDelta("Focus", 2),
                        new TraitDelta("Leadership", 2)
                    },
                    38f,
                    0.9f,
                    "Sorted evidence and chose the strongest fair argument.",
                    true),
                new MiniGameResult(
                    CareerConfig.HealthHeroId,
                    "Health Hero Clinic",
                    CompletionTier.Practice,
                    ResultSource.ShowcaseSeed,
                    new[]
                    {
                        new TraitDelta("Helping", 3),
                        new TraitDelta("Science", 2),
                        new TraitDelta("Focus", 2),
                        new TraitDelta("Communication", 1)
                    },
                    20f,
                    0.78f,
                    "Matched symptoms to helpful tools with care.",
                    true)
            };
        }
    }
}
