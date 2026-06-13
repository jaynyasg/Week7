using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    /// <summary>
    /// Family PRESENTATION layer for the reveal ceremony (U7, R14). U1's
    /// <see cref="CareerFamilies"/> already owns the family KEY constants and
    /// tags each career; this is the parallel data RevealSynthesis needs that
    /// the tags do not carry — the family display name, a superpower phrase, and
    /// a short strength-based blurb for the ceremony subhead.
    ///
    /// Kept as a parallel layer (not an extraction of CareerFamilies) on
    /// purpose: CareerConfig.Careers and the locked CareerConfigTests reference
    /// the CareerFamilies constants directly, so re-homing them would risk that
    /// gate for no gain. Keys here align 1:1 with CareerFamilies.All — a test
    /// asserts every family has a presentation entry.
    ///
    /// Copy is strength-based and PartyStationValidator-safe (no deterministic
    /// phrasing, jargon, or unsafe words); StationCopySafetyTests scans it.
    /// </summary>
    public sealed class CareerFamilyPresentation
    {
        /// <summary>Family key — one of <see cref="CareerFamilies"/>.</summary>
        public string Family { get; }

        /// <summary>Ceremony subhead label, e.g. "Future Tech".</summary>
        public string DisplayName { get; }

        /// <summary>
        /// Superpower phrase the ceremony can lead with when this family tops
        /// the player's blend. Short, celebratory, possibility-based.
        /// </summary>
        public string Superpower { get; }

        /// <summary>One-line family blurb for the subhead support copy.</summary>
        public string Blurb { get; }

        public CareerFamilyPresentation(string family, string displayName, string superpower, string blurb)
        {
            Family = family;
            DisplayName = displayName;
            Superpower = superpower;
            Blurb = blurb;
        }
    }

    public static class CareerFamilyConfig
    {
        // DisplayName trims the "& "/"and" connective from the key so subheads
        // read cleanly ("Future Tech Careers", "Care + Community"); the key
        // itself stays the CareerFamilies string for tag alignment.
        private static readonly CareerFamilyPresentation[] Presentations =
        {
            new(
                CareerFamilies.CareAndCommunity,
                "Care + Community",
                "Care Captain",
                "You look out for people and help your community feel supported."),
            new(
                CareerFamilies.FutureTech,
                "Future Tech",
                "Future Maker",
                "You use logic and curiosity to build helpful new ideas."),
            new(
                CareerFamilies.DesignAndBuild,
                "Design + Build",
                "Creative Builder",
                "You picture what could exist and build it step by step."),
            new(
                CareerFamilies.StoryAndStage,
                "Story + Stage",
                "Story Inventor",
                "You turn ideas and feelings into things people can enjoy."),
            new(
                CareerFamilies.NatureAndSpace,
                "Nature + Space",
                "Explorer Scout",
                "You explore the world and care about how it all works."),
            new(
                CareerFamilies.JusticeAndLeadership,
                "Justice + Leadership",
                "Community Spark",
                "You speak up for fairness and help a group reach its goal.")
        };

        public static IReadOnlyList<CareerFamilyPresentation> All => Presentations;

        public static bool TryGet(string family, out CareerFamilyPresentation presentation)
        {
            presentation = Presentations.FirstOrDefault(candidate => candidate.Family == family);
            return presentation != null;
        }

        /// <summary>Display label for a family key, or the raw key if unknown.</summary>
        public static string DisplayNameFor(string family)
        {
            return TryGet(family, out var presentation) ? presentation.DisplayName : family;
        }
    }
}
