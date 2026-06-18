using System.Collections.Generic;

namespace CareerQuest
{
    /// <summary>
    /// A single Showcase station-montage card: a station display title, its
    /// distinct interaction verb, and a one-line kid-facing blurb. Presentation
    /// data only (no scoring) — consumed by ShowcaseStationsMontage during the
    /// Showcase "stations" beat to sell the breadth of the ten career stations.
    /// </summary>
    public readonly struct ShowcaseMontageEntry
    {
        public string Title { get; }
        public string Verb { get; }
        public string Blurb { get; }

        public ShowcaseMontageEntry(string title, string verb, string blurb)
        {
            Title = title;
            Verb = verb;
            Blurb = blurb;
        }
    }

    public static class ShowcaseSeedConfig
    {
        /// <summary>
        /// Seeded Creative Technical Builder profile. The first three results are
        /// the core rooms; the fourth (Robotics Rescue) adds one station badge to
        /// the Gallery and broadens Career DNA toward Building/Spatial/Reasoning.
        ///
        /// The result COUNT is deliberately held at 4: RevealSynthesis buckets
        /// 3-4 unique completions as RevealStyle.Simple, and that bucket is an
        /// asserted invariant in RevealCinematicPlayModeTests + ShowcaseRevealFlow
        /// Tests (beat sequence + confidence). A fifth result would flip the
        /// reveal to RevealStyle.Rich and rewrite the cinematic — do not add more
        /// seeded results without retuning those tests under a Unity run.
        ///
        /// Trait deltas keep AI Engineer + Architect as the reveal co-leads
        /// (Architect 137 / AI Engineer 134, next career 111) — guarded by
        /// ShowcaseSeedConfigTests, GameSessionTests, and CareerConfigTests.
        /// </summary>
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
                    true),
                new MiniGameResult(
                    CareerQuestCatalog.RoboticsGarageId,
                    "Robotics Rescue",
                    CompletionTier.Degree,
                    ResultSource.ShowcaseSeed,
                    new[]
                    {
                        new TraitDelta("Building", 4),
                        new TraitDelta("Spatial Thinking", 3),
                        new TraitDelta("Reasoning", 2)
                    },
                    30f,
                    0.92f,
                    "Launched rescue robot parts onto the pad with a steady aim.",
                    true)
            };
        }

        /// <summary>
        /// Representative station cards for the Showcase "stations" montage beat.
        /// Four distinct verbs (Launch / Trace / Deduce / Balance) sell the breadth
        /// of the ten career stations without mounting real play surfaces. This is
        /// presentation breadth only — it does not seed results, so it never
        /// affects the reveal style bucket above.
        /// </summary>
        public static IReadOnlyList<ShowcaseMontageEntry> MontageStations()
        {
            return new[]
            {
                new ShowcaseMontageEntry("Robotics Rescue", "Launch", "Pull back and fire a rescue robot onto the pad."),
                new ShowcaseMontageEntry("Weather Lab", "Trace", "Trace a flight path to steer the storm to the rain zone."),
                new ShowcaseMontageEntry("AI Lab", "Deduce", "Cross out the wrong rules until the right one is left."),
                new ShowcaseMontageEntry("Green City", "Balance", "Tune power, water, and parks into the green band.")
            };
        }
    }
}
