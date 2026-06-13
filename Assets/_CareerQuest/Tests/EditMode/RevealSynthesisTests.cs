using System.Collections.Generic;
using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U7 reveal synthesis (R14, KTD9). The ONE resolver picks top traits, top 5
    /// paths, career family, superpower, the completion-count style bucket, and
    /// the combo spotlight from shared session inputs. Representative trait
    /// fixtures prove the family + an expected path; the same resolver yields
    /// different style tokens at each completion bucket; the empty-combo and
    /// pre-reveal paths still render.
    /// </summary>
    public class RevealSynthesisTests
    {
        // ------------------------------------------------------------------
        // Representative profiles: family + an expected top-5 path
        // ------------------------------------------------------------------

        [Test]
        public void CareHeavyProfileSurfacesCareFamilyAndACarePath()
        {
            var result = Synthesize(Profile(("Helping", 18), ("Communication", 10), ("Science", 6)));

            Assert.That(result.PrimaryFamily, Is.EqualTo(CareerFamilies.CareAndCommunity));
            AssertHasPathFamily(result, CareerFamilies.CareAndCommunity);
            Assert.That(result.Superpower, Is.EqualTo("Care Captain"));
        }

        [Test]
        public void TechHeavyProfileSurfacesFutureTechAndATechPath()
        {
            var result = Synthesize(Profile(("Reasoning", 18), ("Science", 10), ("Focus", 8)));

            Assert.That(result.PrimaryFamily, Is.EqualTo(CareerFamilies.FutureTech));
            AssertHasPathId(result, "data_scientist");
        }

        [Test]
        public void CreativeHeavyProfileSurfacesStoryAndStageAndACreativePath()
        {
            var result = Synthesize(Profile(("Creativity", 18), ("Communication", 9), ("Focus", 5)));

            Assert.That(result.PrimaryFamily, Is.EqualTo(CareerFamilies.StoryAndStage));
            AssertHasPathFamily(result, CareerFamilies.StoryAndStage);
        }

        [Test]
        public void BuildHeavyProfileSurfacesDesignAndBuildAndABuildPath()
        {
            var result = Synthesize(Profile(("Building", 18), ("Spatial Thinking", 12), ("Reasoning", 6)));

            Assert.That(result.PrimaryFamily, Is.EqualTo(CareerFamilies.DesignAndBuild));
            AssertHasPathFamily(result, CareerFamilies.DesignAndBuild);
        }

        [Test]
        public void NatureHeavyProfileSurfacesNatureAndSpaceAndANaturePath()
        {
            // Focus + Spatial + Science leans into the space/nature paths
            // (pilot, mission planner, marine biologist) — a Care-leaning
            // Science-only profile would instead surface care careers, so the
            // Nature fixture is spatial/focus heavy on purpose.
            var result = Synthesize(Profile(("Focus", 18), ("Spatial Thinking", 16), ("Science", 8)));

            Assert.That(result.PrimaryFamily, Is.EqualTo(CareerFamilies.NatureAndSpace));
            AssertHasPathFamily(result, CareerFamilies.NatureAndSpace);
        }

        [Test]
        public void StoryHeavyProfileSurfacesStoryAndStageAndAStoryPath()
        {
            var result = Synthesize(Profile(("Communication", 16), ("Creativity", 12), ("Reasoning", 7)));

            Assert.That(result.PrimaryFamily, Is.EqualTo(CareerFamilies.StoryAndStage));
            AssertHasPathFamily(result, CareerFamilies.StoryAndStage);
        }

        [Test]
        public void JusticeLeadershipHeavyProfileSurfacesJusticeFamilyAndALeadershipPath()
        {
            var result = Synthesize(Profile(("Leadership", 18), ("Communication", 10), ("Reasoning", 8)));

            Assert.That(result.PrimaryFamily, Is.EqualTo(CareerFamilies.JusticeAndLeadership));
            AssertHasPathFamily(result, CareerFamilies.JusticeAndLeadership);
        }

        [Test]
        public void BalancedExplorerStillProducesAFamilyAndFivePaths()
        {
            // Even spread across every trait — no single family dominates, but
            // the reveal must still resolve a family, a superpower, and 5 paths.
            var balanced = CareerConfig.AllTraits.Select(trait => (trait, 6)).ToArray();
            var result = Synthesize(Profile(balanced));

            Assert.That(CareerFamilies.All, Does.Contain(result.PrimaryFamily));
            Assert.That(result.Superpower, Is.Not.Empty);
            Assert.That(result.TopPaths.Count, Is.EqualTo(RevealSynthesis.TopPathCount));
            Assert.That(result.TopTraits.Count, Is.GreaterThan(0));
        }

        [Test]
        public void EveryReturnedTopPathIsScoredAndOrderedHighestFirst()
        {
            var result = Synthesize(Profile(("Building", 14), ("Creativity", 10), ("Reasoning", 9)));

            Assert.That(result.TopPaths.Count, Is.EqualTo(RevealSynthesis.TopPathCount));
            for (var i = 1; i < result.TopPaths.Count; i++)
            {
                Assert.That(result.TopPaths[i].Score, Is.LessThanOrEqualTo(result.TopPaths[i - 1].Score));
            }
        }

        // ------------------------------------------------------------------
        // First-wave station-backed paths appear; reveal-supported need no building
        // ------------------------------------------------------------------

        [Test]
        public void StationBackedPathAppearsWhenItsTraitProfileIsStrongest()
        {
            // Robotics-style profile (Building + Reasoning) surfaces the
            // station-backed Robotics Engineer in the top 5.
            var result = Synthesize(Profile(("Building", 16), ("Reasoning", 12), ("Spatial Thinking", 8)));

            AssertHasPathId(result, "robotics_engineer");
            Assert.That(
                CareerConfig.Careers.First(c => c.Id == "robotics_engineer").Support,
                Is.EqualTo(CareerPathSupport.StationBacked));
        }

        [Test]
        public void RevealSupportedPathCanAppearWithoutItsOwnStation()
        {
            // Nurse is RevealSupported (no dedicated station) yet a strong
            // Helping + Focus profile can still surface it via trait scoring.
            var result = Synthesize(Profile(("Helping", 18), ("Focus", 14), ("Science", 6)));

            Assert.That(
                CareerConfig.Careers.First(c => c.Id == "nurse").Support,
                Is.EqualTo(CareerPathSupport.RevealSupported));
            AssertHasPathId(result, "nurse");
        }

        // ------------------------------------------------------------------
        // Completion-count style buckets through ONE resolver
        // ------------------------------------------------------------------

        [Test]
        public void StyleBucketsChangeAtEachThresholdThroughOneResolver()
        {
            Assert.That(RevealSynthesis.StyleFor(0), Is.EqualTo(RevealStyle.PreReveal));
            Assert.That(RevealSynthesis.StyleFor(2), Is.EqualTo(RevealStyle.PreReveal));
            Assert.That(RevealSynthesis.StyleFor(3), Is.EqualTo(RevealStyle.Simple));
            Assert.That(RevealSynthesis.StyleFor(4), Is.EqualTo(RevealStyle.Simple));
            Assert.That(RevealSynthesis.StyleFor(5), Is.EqualTo(RevealStyle.Rich));
            Assert.That(RevealSynthesis.StyleFor(7), Is.EqualTo(RevealStyle.Rich));
            Assert.That(RevealSynthesis.StyleFor(8), Is.EqualTo(RevealStyle.BigExplorer));
            Assert.That(RevealSynthesis.StyleFor(9), Is.EqualTo(RevealStyle.BigExplorer));
            Assert.That(RevealSynthesis.StyleFor(10), Is.EqualTo(RevealStyle.Completionist));
        }

        [Test]
        public void SameResolverYieldsDifferentStyleTokensAtEachBucketFromSession()
        {
            var stationIds = CareerQuestCatalog.PartyStationIds;

            var styleAt = new Dictionary<int, RevealStyle>();
            var session = new GameSession();
            for (var i = 0; i < stationIds.Length; i++)
            {
                CompleteStation(session, stationIds[i]);
                styleAt[i + 1] = RevealSynthesis.Resolve(session).Style;
            }

            Assert.That(styleAt[3], Is.EqualTo(RevealStyle.Simple));
            Assert.That(styleAt[5], Is.EqualTo(RevealStyle.Rich));
            Assert.That(styleAt[8], Is.EqualTo(RevealStyle.BigExplorer));
            Assert.That(styleAt[10], Is.EqualTo(RevealStyle.Completionist));
            // Distinct tokens across the four buckets through the one path.
            Assert.That(new[] { styleAt[3], styleAt[5], styleAt[8], styleAt[10] }.Distinct().Count(), Is.EqualTo(4));
        }

        // ------------------------------------------------------------------
        // Pre-reveal teaser (0/1/2) + ready gate
        // ------------------------------------------------------------------

        [Test]
        public void BelowThreeCompletionsIsPreRevealTeaserAndNotReady()
        {
            var session = new GameSession();
            Assert.That(RevealSynthesis.Resolve(session).Style, Is.EqualTo(RevealStyle.PreReveal));
            Assert.That(RevealSynthesis.Resolve(session).IsRevealReady, Is.False);

            CompleteStation(session, CareerQuestCatalog.RoboticsGarageId);
            Assert.That(RevealSynthesis.Resolve(session).Style, Is.EqualTo(RevealStyle.PreReveal));

            CompleteStation(session, CareerQuestCatalog.AiLabId);
            var twoDone = RevealSynthesis.Resolve(session);
            Assert.That(twoDone.Style, Is.EqualTo(RevealStyle.PreReveal));
            Assert.That(twoDone.IsRevealReady, Is.False);

            CompleteStation(session, CareerQuestCatalog.MusicStudioId);
            var threeDone = RevealSynthesis.Resolve(session);
            Assert.That(threeDone.Style, Is.EqualTo(RevealStyle.Simple));
            Assert.That(threeDone.IsRevealReady, Is.True);
        }

        [Test]
        public void PreRevealTeaserStillCarriesTraitsAndPathsForTheTeaser()
        {
            var session = new GameSession();
            CompleteStation(session, CareerQuestCatalog.RoboticsGarageId);

            var result = RevealSynthesis.Resolve(session);
            Assert.That(result.IsRevealReady, Is.False);
            Assert.That(result.TopPaths.Count, Is.EqualTo(RevealSynthesis.TopPathCount), "Teaser can still preview paths.");
            Assert.That(result.TopTraits.Count, Is.GreaterThan(0));
        }

        // ------------------------------------------------------------------
        // Combo spotlight layers on any style; empty-combo still renders
        // ------------------------------------------------------------------

        [Test]
        public void EligibleComboPairProducesAPrimaryComboSpotlight()
        {
            // Robotics + Community Kitchen = Robot Chef (combo.robot_chef).
            var session = new GameSession();
            CompleteStation(session, CareerQuestCatalog.RoboticsGarageId);
            CompleteStation(session, CareerQuestCatalog.CommunityKitchenId);
            CompleteStation(session, CareerQuestCatalog.MusicStudioId);

            var result = RevealSynthesis.Resolve(session);

            Assert.That(result.HasComboSpotlight, Is.True);
            Assert.That(result.PrimaryCombo.Id, Is.EqualTo("combo.robot_chef"));
        }

        [Test]
        public void NoEligibleComboPairStillRendersRevealWithNullCombo()
        {
            // Three completions with no combo pair among them.
            var session = new GameSession();
            CompleteStation(session, CareerQuestCatalog.RoboticsGarageId);
            CompleteStation(session, CareerQuestCatalog.MusicStudioId);
            CompleteStation(session, CareerQuestCatalog.WeatherLabId);

            var result = RevealSynthesis.Resolve(session);

            Assert.That(result.HasComboSpotlight, Is.False);
            Assert.That(result.PrimaryCombo, Is.Null);
            Assert.That(result.AdditionalCombos, Is.Empty);
            // The rest of the reveal is intact.
            Assert.That(result.TopPaths.Count, Is.EqualTo(RevealSynthesis.TopPathCount));
            Assert.That(result.Superpower, Is.Not.Empty);
        }

        [Test]
        public void ComboSpotlightLayersOnTopOfAnyCompletionCountStyle()
        {
            // Build to a Rich-style (5+) session that also has an eligible combo.
            var session = new GameSession();
            CompleteStation(session, CareerQuestCatalog.RoboticsGarageId);
            CompleteStation(session, CareerQuestCatalog.CommunityKitchenId); // Robot Chef pair
            CompleteStation(session, CareerQuestCatalog.MusicStudioId);
            CompleteStation(session, CareerQuestCatalog.AiLabId);
            CompleteStation(session, CareerQuestCatalog.VetClinicId);

            var result = RevealSynthesis.Resolve(session);

            Assert.That(result.Style, Is.EqualTo(RevealStyle.Rich));
            Assert.That(result.HasComboSpotlight, Is.True, "Hybrid spotlight layers on the Rich style, not only Simple.");
        }

        [Test]
        public void NullSessionResolvesToASafePreRevealResult()
        {
            var result = RevealSynthesis.Resolve(null);

            Assert.That(result.Style, Is.EqualTo(RevealStyle.PreReveal));
            Assert.That(result.IsRevealReady, Is.False);
            Assert.That(result.HasComboSpotlight, Is.False);
            Assert.That(result.TopPaths, Is.Empty);
        }

        // ------------------------------------------------------------------
        // Showcase profile still resolves sensibly (locked co-lead expectation)
        // ------------------------------------------------------------------

        [Test]
        public void SeededShowcaseResolvesArchitectOrAiEngineerNearTheTop()
        {
            var session = new GameSession();
            session.SeedShowcase();

            var result = RevealSynthesis.Resolve(session);

            var topIds = result.TopPaths.Select(match => match.Career.Id).ToList();
            Assert.That(topIds, Does.Contain("ai_engineer").Or.Contain("architect"),
                "Seeded showcase must keep co-leading sensibly through synthesis.");
            Assert.That(result.IsRevealReady, Is.True);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static RevealSynthesisResult Synthesize(CareerDnaProfile profile)
        {
            // Pure overload: 5 unique completions so style is well past the gate
            // and the family/path selection is the thing under test.
            var ranked = CareerConfig.RankCareers(profile);
            return RevealSynthesis.Resolve(ranked, profile.TraitTotals, new List<string>(), 5);
        }

        private static CareerDnaProfile Profile(params (string trait, int delta)[] traits)
        {
            var profile = new CareerDnaProfile();
            profile.Recompute(new[]
            {
                new MiniGameResult(
                    "fixture",
                    "Fixture",
                    CompletionTier.Degree,
                    ResultSource.Solo,
                    traits.Select(pair => new TraitDelta(pair.trait, pair.delta)),
                    20f,
                    0.9f,
                    "Fixture profile.")
            });
            return profile;
        }

        private static void CompleteStation(GameSession session, string stationId)
        {
            var definition = PartyStationDefinitions.GetById(stationId);
            session.RecordResult(PartyStationController.BuildResult(
                definition, definition.DefaultSeed, ResultSource.Solo, complete: true, wrongAttempts: 0, playElapsedSeconds: 12f));
        }

        private static void AssertHasPathFamily(RevealSynthesisResult result, string family)
        {
            Assert.That(
                result.TopPaths.Any(match =>
                    match.Career.PrimaryFamily == family
                    || match.Career.SecondaryFamilies.Contains(family)),
                Is.True,
                $"Expected a top-5 path in family '{family}'. Got: {string.Join(", ", result.TopPaths.Select(m => m.Career.Id))}");
        }

        private static void AssertHasPathId(RevealSynthesisResult result, string careerId)
        {
            Assert.That(
                result.TopPaths.Select(match => match.Career.Id),
                Does.Contain(careerId),
                $"Expected '{careerId}' in the top 5. Got: {string.Join(", ", result.TopPaths.Select(m => m.Career.Id))}");
        }
    }
}
