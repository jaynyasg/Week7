using System.Collections.Generic;
using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U6 pure accessory derivation (R12/KTD8). Earned accessories come from
    /// completed activities + the unique count, never a saved inventory, and the
    /// derivation can never touch career scoring. Slot rules and ceremony-only
    /// gating are proven here on the pure resolver; the visual layer rules ride
    /// AvatarAccessoryLayerPlayModeTests.
    /// </summary>
    public class AccessoryResolverTests
    {
        // ------------------------------------------------------------------
        // Station accessory derivation + scoring invariance (KTD8)
        // ------------------------------------------------------------------

        [Test]
        public void CompletingOneStationDerivesExactlyItsCoreAccessory()
        {
            var session = new GameSession();
            RecordStation(session, CareerQuestCatalog.RoboticsGarageId);

            var earned = AccessoryResolver.ResolveEarned(session);

            Assert.That(earned.Select(accessory => accessory.Id), Is.EqualTo(new[] { "accessory.tool_belt" }),
                "Robotics Garage derives exactly the Tool Belt, nothing else.");
        }

        [Test]
        public void AccessoryDerivationNeverChangesRankingOrCareerDna()
        {
            // Same completed stations; the only difference is whether we derive
            // accessories. Career DNA and the ranking must be byte-identical.
            var withoutAccessories = new GameSession();
            RecordStation(withoutAccessories, CareerQuestCatalog.RoboticsGarageId);
            RecordStation(withoutAccessories, CareerQuestCatalog.AiLabId);
            RecordStation(withoutAccessories, CareerQuestCatalog.MusicStudioId);

            var withAccessories = new GameSession();
            RecordStation(withAccessories, CareerQuestCatalog.RoboticsGarageId);
            RecordStation(withAccessories, CareerQuestCatalog.AiLabId);
            RecordStation(withAccessories, CareerQuestCatalog.MusicStudioId);

            // Derive accessories on one session only.
            var earned = AccessoryResolver.ResolveEarned(withAccessories);
            var visible = AccessoryResolver.ResolveVisible(withAccessories, ceremonyContext: false);
            Assert.That(earned.Count, Is.GreaterThan(0));
            Assert.That(visible.Count, Is.GreaterThan(0));

            // Career DNA totals identical.
            Assert.That(withAccessories.CareerDna.TraitTotals, Is.EqualTo(withoutAccessories.CareerDna.TraitTotals));

            // Ranking identical (career order + scores).
            var rankedWith = withAccessories.CareerMatches().Select(match => (match.Career.Id, match.Score)).ToArray();
            var rankedWithout = withoutAccessories.CareerMatches().Select(match => (match.Career.Id, match.Score)).ToArray();
            Assert.That(rankedWith, Is.EqualTo(rankedWithout), "Accessories are presentation only — ranking is unchanged.");

            // Reveal readiness + unique count unaffected.
            Assert.That(withAccessories.UniqueCompletedGames, Is.EqualTo(withoutAccessories.UniqueCompletedGames));
            Assert.That(withAccessories.RevealReady, Is.EqualTo(withoutAccessories.RevealReady));
        }

        // ------------------------------------------------------------------
        // Milestone accessories at 3/5/8/10 with NO saved inventory
        // ------------------------------------------------------------------

        [Test]
        public void MilestoneAccessoriesDeriveAtThresholdsFromCountAlone()
        {
            // Distinct station ids in completion order; milestones derive purely
            // from the unique count — no stored state anywhere.
            var order = new List<string>();

            AssertMilestoneAt(order, CareerQuestCatalog.RoboticsGarageId, 1, expectedMilestone: null);
            AssertMilestoneAt(order, CareerQuestCatalog.AiLabId, 2, expectedMilestone: null);
            AssertMilestoneAt(order, CareerQuestCatalog.MusicStudioId, 3, expectedMilestone: "accessory.badge_sash");
            AssertMilestoneAt(order, CareerQuestCatalog.CommunityKitchenId, 4, expectedMilestone: null);
            AssertMilestoneAt(order, CareerQuestCatalog.VetClinicId, 5, expectedMilestone: "accessory.explorer_cape");
            AssertMilestoneAt(order, CareerQuestCatalog.GameStudioId, 6, expectedMilestone: null);
            AssertMilestoneAt(order, CareerQuestCatalog.WeatherLabId, 7, expectedMilestone: null);
            AssertMilestoneAt(order, CareerQuestCatalog.SpaceportId, 8, expectedMilestone: "accessory.star_robe");
            AssertMilestoneAt(order, CareerQuestCatalog.NewsroomId, 9, expectedMilestone: null);
            AssertMilestoneAt(order, CareerQuestCatalog.GreenCityId, 10, expectedMilestone: "accessory.reveal_flourish");
        }

        [Test]
        public void MilestonesDeriveWithoutAnyStationAccessoriesWhenCountIsPushedDirectly()
        {
            // Even with an empty completed list, a unique count alone derives all
            // milestones at/below it — never from inventory, purely from count.
            var earned = AccessoryResolver.ResolveEarned(new List<string>(), uniqueCompletedGames: 8);

            var ids = earned.Select(accessory => accessory.Id).ToList();
            Assert.That(ids, Does.Contain("accessory.badge_sash"));
            Assert.That(ids, Does.Contain("accessory.explorer_cape"));
            Assert.That(ids, Does.Contain("accessory.star_robe"));
            Assert.That(ids, Does.Not.Contain("accessory.reveal_flourish"), "10-count milestone not reached at 8.");
        }

        // ------------------------------------------------------------------
        // Slot rules: one visible per slot in campus; ceremony-only gating
        // ------------------------------------------------------------------

        [Test]
        public void CampusVisibleKeepsOneAccessoryPerSlotNewestEarnedWins()
        {
            // Two Torso-slot accessories: Tool Belt (Robotics) then Mission Patch
            // (Spaceport). The newest earned wins the Torso slot in campus play.
            var order = new List<string> { CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.SpaceportId };
            var earned = AccessoryResolver.ResolveEarned(order, uniqueCompletedGames: 2);

            var visible = AccessoryResolver.ResolveVisible(earned, ceremonyContext: false);

            // No slot appears twice.
            var slots = visible.Select(accessory => accessory.Slot).ToList();
            Assert.That(slots.Distinct().Count(), Is.EqualTo(slots.Count), "At most one visible accessory per slot.");

            // The Torso slot is the newest-earned Mission Patch, not the Tool Belt.
            var torso = visible.Single(accessory => accessory.Slot == AccessorySlot.Torso);
            Assert.That(torso.Id, Is.EqualTo("accessory.mission_patch"), "Newest earned wins the slot.");
        }

        [Test]
        public void CeremonyOnlyAccessoriesAreHiddenInCampusAndShownAtReveal()
        {
            // 8 unique completions earns the ceremony-only Star Robe (Torso).
            var earned = AccessoryResolver.ResolveEarned(new List<string>(), uniqueCompletedGames: 8);
            Assert.That(earned.Any(accessory => accessory.Id == "accessory.star_robe"), Is.True);

            var campus = AccessoryResolver.ResolveVisible(earned, ceremonyContext: false);
            Assert.That(campus.Any(accessory => accessory.Id == "accessory.star_robe"), Is.False,
                "Ceremony-only items never show in campus play.");

            var ceremony = AccessoryResolver.ResolveVisible(earned, ceremonyContext: true);
            Assert.That(ceremony.Any(accessory => accessory.Id == "accessory.star_robe"), Is.True,
                "Ceremony-only items show during the reveal ceremony.");
        }

        // ------------------------------------------------------------------
        // Idempotency + empty + replay-without-inflation
        // ------------------------------------------------------------------

        [Test]
        public void DerivationIsIdempotentForTheSameInputs()
        {
            var order = new List<string> { CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.AiLabId };

            var first = AccessoryResolver.ResolveEarned(order, 2).Select(accessory => accessory.Id).ToArray();
            var second = AccessoryResolver.ResolveEarned(order, 2).Select(accessory => accessory.Id).ToArray();
            var third = AccessoryResolver.ResolveEarned(order, 2).Select(accessory => accessory.Id).ToArray();

            Assert.That(second, Is.EqualTo(first));
            Assert.That(third, Is.EqualTo(first));
        }

        [Test]
        public void ZeroCompletionsDerivesNoAccessories()
        {
            var session = new GameSession();

            Assert.That(AccessoryResolver.ResolveEarned(session), Is.Empty);
            Assert.That(AccessoryResolver.ResolveVisible(session, ceremonyContext: false), Is.Empty);
            Assert.That(AccessoryResolver.ResolveVisible(session, ceremonyContext: true), Is.Empty);
        }

        [Test]
        public void ReplayDoesNotInflateUniqueCountAndDerivationStaysStable()
        {
            var session = new GameSession();
            RecordStation(session, CareerQuestCatalog.RoboticsGarageId);
            var before = AccessoryResolver.ResolveEarned(session).Select(accessory => accessory.Id).ToArray();

            // Replay the SAME station with a better result — best result may
            // change, but the completion order/unique count must not grow.
            session.RecordResult(BetterRoboticsResult());

            Assert.That(session.UniqueCompletedGames, Is.EqualTo(1), "Replay never inflates the unique count.");
            Assert.That(session.CompletedActivityIds, Is.EqualTo(new[] { CareerQuestCatalog.RoboticsGarageId }));
            var after = AccessoryResolver.ResolveEarned(session).Select(accessory => accessory.Id).ToArray();
            Assert.That(after, Is.EqualTo(before), "Accessory derivation is unchanged by a replay.");
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static void AssertMilestoneAt(List<string> order, string stationId, int expectedUnique, string expectedMilestone)
        {
            order.Add(stationId);
            Assert.That(order.Count, Is.EqualTo(expectedUnique));

            var earned = AccessoryResolver.ResolveEarned(order, expectedUnique);
            var ids = earned.Select(accessory => accessory.Id).ToList();

            if (expectedMilestone != null)
            {
                Assert.That(ids, Does.Contain(expectedMilestone), $"Milestone at {expectedUnique} completions.");
            }

            // No milestone for a threshold we have not reached yet.
            foreach (var threshold in AccessoryRewardConfig.MilestoneThresholds.Where(value => value > expectedUnique))
            {
                Assert.That(
                    AccessoryRewardConfig.TryGetForMilestone(threshold, out var future) && ids.Contains(future.Id),
                    Is.False,
                    $"Milestone {threshold} must not derive at only {expectedUnique} completions.");
            }
        }

        private static void RecordStation(GameSession session, string stationId)
        {
            var definition = PartyStationDefinitions.GetById(stationId);
            session.RecordResult(PartyStationController.BuildResult(
                definition, definition.DefaultSeed, ResultSource.Solo, complete: true, wrongAttempts: 0, playElapsedSeconds: 12f));
        }

        private static MiniGameResult BetterRoboticsResult()
        {
            var definition = PartyStationDefinitions.GetById(CareerQuestCatalog.RoboticsGarageId);
            return PartyStationController.BuildResult(
                definition, definition.DefaultSeed, ResultSource.Solo, complete: true, wrongAttempts: 0, playElapsedSeconds: 0f);
        }
    }
}
