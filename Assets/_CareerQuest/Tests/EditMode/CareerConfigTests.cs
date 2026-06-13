using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class CareerConfigTests
    {
        [Test]
        public void CareerRankingExposesThirtyUniquePaths()
        {
            Assert.That(CareerConfig.Careers.Length, Is.EqualTo(30));
            Assert.That(CareerConfig.Careers.Select(career => career.Id).Distinct().Count(), Is.EqualTo(30));
            Assert.That(CareerConfig.Careers.Select(career => career.DisplayName).Distinct().Count(), Is.EqualTo(30));
        }

        [Test]
        public void OriginalCoreCareerIdsRemainUnchanged()
        {
            var ids = CareerConfig.Careers.Select(career => career.Id).ToArray();

            Assert.That(ids, Does.Contain("doctor"));
            Assert.That(ids, Does.Contain("lawyer"));
            Assert.That(ids, Does.Contain("ai_engineer"));
            Assert.That(ids, Does.Contain("artist"));
            Assert.That(ids, Does.Contain("architect"));
        }

        [Test]
        public void EveryCareerHasFamilyTaglineAndKnownTraitWeights()
        {
            foreach (var career in CareerConfig.Careers)
            {
                Assert.That(career.Tagline, Is.Not.Empty, career.Id);
                Assert.That(CareerFamilies.All, Does.Contain(career.PrimaryFamily), career.Id);

                foreach (var family in career.SecondaryFamilies)
                {
                    Assert.That(CareerFamilies.All, Does.Contain(family), career.Id);
                    Assert.That(family, Is.Not.EqualTo(career.PrimaryFamily), career.Id);
                }

                Assert.That(career.TraitWeights, Is.Not.Empty, career.Id);
                foreach (var weight in career.TraitWeights)
                {
                    Assert.That(CareerConfig.AllTraits, Does.Contain(weight.Key), career.Id);
                    Assert.That(weight.Value, Is.GreaterThan(0), career.Id);
                }
            }
        }

        [Test]
        public void AtLeastTwelveCareerPathsAreStationBackedByPartyStations()
        {
            var stationTags = PartyStationDefinitions.All
                .SelectMany(station => station.CareerTags)
                .Concat(CareerQuestCatalog.All.Select(entry => entry.CareerTag))
                .Distinct()
                .ToArray();

            var stationBacked = CareerConfig.Careers
                .Where(career => career.Support == CareerPathSupport.StationBacked)
                .ToArray();

            Assert.That(stationBacked.Length, Is.GreaterThanOrEqualTo(12));
            foreach (var career in stationBacked)
            {
                Assert.That(stationTags, Does.Contain(career.Id), career.Id);
            }
        }

        [Test]
        public void EveryCareerFamilyHasAtLeastOneCareer()
        {
            foreach (var family in CareerFamilies.All)
            {
                Assert.That(
                    CareerConfig.Careers.Any(career => career.PrimaryFamily == family),
                    Is.True,
                    family);
            }
        }

        [Test]
        public void RankCareersScoresAllThirtyPathsDeterministically()
        {
            var profile = new CareerDnaProfile();
            profile.Recompute(new[]
            {
                new MiniGameResult(
                    CareerConfig.DesignBuildId,
                    "Future City Design Build",
                    CompletionTier.Degree,
                    ResultSource.Solo,
                    new[] { new TraitDelta("Building", 5), new TraitDelta("Creativity", 3) },
                    30f,
                    0.9f,
                    "Built a city.")
            });

            var ranked = CareerConfig.RankCareers(profile);

            Assert.That(ranked.Count, Is.EqualTo(30));
            Assert.That(ranked[0].Score, Is.GreaterThanOrEqualTo(ranked[ranked.Count - 1].Score));
            Assert.That(
                CareerConfig.RankCareers(profile).Select(match => match.Career.Id),
                Is.EqualTo(ranked.Select(match => match.Career.Id)));
        }

        [Test]
        public void SeededShowcaseCoLeadsStayStableAfterCareerExpansion()
        {
            // Guard for the 30-path expansion: the seeded showcase profile must
            // keep co-leading with AI Engineer + Architect (GameSessionTests
            // depends on this; new careers are weighted below that pair).
            var session = new GameSession();
            session.SeedShowcase();

            var names = session.CoLeadMatches().Select(match => match.Career.DisplayName).ToArray();

            Assert.That(names, Does.Contain("AI Engineer"));
            Assert.That(names, Does.Contain("Architect"));
        }

        // ------------------------------------------------------------------
        // U7: family presentation layer aligns with the U1 family tags.
        // ------------------------------------------------------------------

        [Test]
        public void EveryCareerFamilyHasAPresentationEntry()
        {
            foreach (var family in CareerFamilies.All)
            {
                Assert.That(CareerFamilyConfig.TryGet(family, out var presentation), Is.True, family);
                Assert.That(presentation.DisplayName, Is.Not.Empty, family);
                Assert.That(presentation.Superpower, Is.Not.Empty, family);
                Assert.That(presentation.Blurb, Is.Not.Empty, family);
            }
        }

        [Test]
        public void FamilyPresentationKeysAreExactlyTheFamilyTags()
        {
            Assert.That(
                CareerFamilyConfig.All.Select(presentation => presentation.Family).OrderBy(key => key),
                Is.EqualTo(CareerFamilies.All.OrderBy(key => key)));
            Assert.That(
                CareerFamilyConfig.All.Select(presentation => presentation.Superpower).Distinct().Count(),
                Is.EqualTo(CareerFamilyConfig.All.Count),
                "Each family gets a distinct superpower headline.");
        }

        [Test]
        public void StationBackedPathSurfacesThroughSynthesisWhenItsTraitsLead()
        {
            // R13/R14: a station-backed path (Robotics Engineer) appears in the
            // synthesized top 5 when its Building+Reasoning profile is strongest.
            var profile = new CareerDnaProfile();
            profile.Recompute(new[]
            {
                new MiniGameResult(
                    CareerQuestCatalog.RoboticsGarageId,
                    "Robotics Garage",
                    CompletionTier.Degree,
                    ResultSource.Solo,
                    new[] { new TraitDelta("Building", 6), new TraitDelta("Reasoning", 5), new TraitDelta("Spatial Thinking", 3) },
                    20f,
                    0.9f,
                    "Built robots.")
            });

            var result = RevealSynthesis.Resolve(
                CareerConfig.RankCareers(profile), profile.TraitTotals, System.Array.Empty<string>(), 5);

            Assert.That(result.TopPaths.Select(match => match.Career.Id), Does.Contain("robotics_engineer"));
        }
    }
}
