using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class ShowcaseSeedConfigTests
    {
        [Test]
        public void SeedIncludesCoreRoomsPlusRoboticsStation()
        {
            var ids = ShowcaseSeedConfig.CreativeTechnicalBuilderResults()
                .Select(result => result.ActivityId)
                .ToArray();

            Assert.That(ids, Does.Contain(CareerConfig.DesignBuildId));
            Assert.That(ids, Does.Contain(CareerConfig.LogicCourtId));
            Assert.That(ids, Does.Contain(CareerConfig.HealthHeroId));
            Assert.That(ids, Does.Contain(CareerQuestCatalog.RoboticsGarageId));
        }

        // Guards the reveal-style bucket: 3-4 unique completions = RevealStyle.
        // Simple (asserted in RevealCinematicPlayModeTests + ShowcaseRevealFlow
        // Tests). A fifth seeded result flips the reveal to Rich and rewrites the
        // cinematic beat sequence — keep the seed at 4 unless those tests are
        // retuned under a Unity run.
        [Test]
        public void SeedStaysInTheSimpleRevealBucket()
        {
            var count = ShowcaseSeedConfig.CreativeTechnicalBuilderResults().Count;

            Assert.That(count, Is.EqualTo(4));
            Assert.That(RevealSynthesis.StyleFor(count), Is.EqualTo(RevealStyle.Simple));
        }

        // Guards R7/R10: the seed must still resolve the reveal to the Architect +
        // AI Engineer co-leads (same within-5 window as GameSession.CoLeadMatches).
        [Test]
        public void SeedStillCoLeadsArchitectAndAiEngineer()
        {
            var profile = new CareerDnaProfile();
            profile.Recompute(ShowcaseSeedConfig.CreativeTechnicalBuilderResults());

            var ranked = CareerConfig.RankCareers(profile);
            var topScore = ranked[0].Score;
            var coLeads = ranked
                .Where(match => topScore - match.Score <= 5)
                .Select(match => match.Career.DisplayName)
                .ToArray();

            Assert.That(coLeads, Does.Contain("AI Engineer"));
            Assert.That(coLeads, Does.Contain("Architect"));
        }

        // Guards R3: the montage must show distinct verbs, not the same action.
        [Test]
        public void MontageStationsCoverFourDistinctVerbs()
        {
            var verbs = ShowcaseSeedConfig.MontageStations()
                .Select(entry => entry.Verb)
                .ToArray();

            Assert.That(verbs.Length, Is.EqualTo(4));
            Assert.That(verbs.Distinct().Count(), Is.EqualTo(4),
                "Montage must show distinct verbs, not the same action repeated.");
        }
    }
}
