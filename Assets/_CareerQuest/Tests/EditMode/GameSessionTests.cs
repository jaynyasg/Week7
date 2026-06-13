using System.Collections.Generic;
using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class GameSessionTests
    {
        [Test]
        public void NewSessionStartsWithoutReveal()
        {
            var session = new GameSession();

            Assert.That(session.Mode, Is.EqualTo(AppMode.Entry));
            Assert.That(session.RevealReady, Is.False);
            Assert.That(session.ConfidencePhrase(), Is.EqualTo("3 games to go"));
        }

        [Test]
        public void OneResultKeepsRevealLocked()
        {
            var session = new GameSession();

            session.RecordResult(Result(CompletionTier.Degree, 40f, 0.8f));

            Assert.That(session.RevealReady, Is.False);
            Assert.That(session.UniqueCompletedGames, Is.EqualTo(1));
            Assert.That(session.ConfidencePhrase(), Is.EqualTo("2 games to go"));
        }

        [Test]
        public void BetterResultReplacesWeakerPriorAttempt()
        {
            var session = new GameSession();

            session.RecordResult(Result(CompletionTier.Practice, 50f, 0.95f));
            session.RecordResult(Result(CompletionTier.Degree, 10f, 0.5f));

            Assert.That(session.BestResults.Single().Tier, Is.EqualTo(CompletionTier.Degree));
        }

        [Test]
        public void ThreeUniqueResultsUnlockReveal()
        {
            var session = new GameSession();

            session.RecordResult(Result(CompletionTier.Degree, 40f, 0.8f));
            session.RecordResult(new MiniGameResult(
                CareerConfig.LogicCourtId,
                "Logic Court",
                CompletionTier.Degree,
                ResultSource.Solo,
                new[] { new TraitDelta("Reasoning", 4) },
                30f,
                0.9f,
                "Sorted evidence."));
            session.RecordResult(new MiniGameResult(
                CareerConfig.HealthHeroId,
                "Health Hero Clinic",
                CompletionTier.Degree,
                ResultSource.Solo,
                new[] { new TraitDelta("Helping", 4) },
                32f,
                0.88f,
                "Helped a patient."));

            Assert.That(session.RevealReady, Is.True);
            Assert.That(session.ConfidencePhrase(), Is.EqualTo("Very strong match"));
        }

        [Test]
        public void SeededShowcaseProducesArchitectAndAiEngineerCoLeads()
        {
            var session = new GameSession();

            session.SeedShowcase();
            var names = session.CoLeadMatches().Select(match => match.Career.DisplayName).ToArray();

            Assert.That(names, Does.Contain("AI Engineer"));
            Assert.That(names, Does.Contain("Architect"));
            Assert.That(session.HasSeededResults, Is.True);
        }

        // ------------------------------------------------------------------
        // U6: completion order, reward-event log, and the 2P read model.
        // PRESERVES the best-result/Career-DNA/reveal semantics above (KTD8).
        // ------------------------------------------------------------------

        [Test]
        public void CompletedActivityIdsTrackFirstCompletionOrderOnce()
        {
            var session = new GameSession();

            session.RecordResult(StationResult(CareerConfig.DesignBuildId, CompletionTier.Practice, 10f, 0.5f));
            session.RecordResult(StationResult(CareerConfig.LogicCourtId, CompletionTier.Degree, 30f, 0.9f));

            // A better result for an already-completed id replaces the best
            // result but must NOT re-append to the completion order.
            session.RecordResult(StationResult(CareerConfig.DesignBuildId, CompletionTier.Degree, 40f, 0.95f));

            Assert.That(session.CompletedActivityIds,
                Is.EqualTo(new[] { CareerConfig.DesignBuildId, CareerConfig.LogicCourtId }),
                "Completion order is first-completion order, each id exactly once.");
            Assert.That(session.UniqueCompletedGames, Is.EqualTo(2));
            Assert.That(session.GetBestResult(CareerConfig.DesignBuildId).Tier, Is.EqualTo(CompletionTier.Degree),
                "Best-result replacement semantics are preserved.");
        }

        [Test]
        public void AppendStationRewardEventLogsWithoutTouchingScoring()
        {
            var session = new GameSession();
            session.RecordResult(StationResult(CareerConfig.DesignBuildId, CompletionTier.Degree, 40f, 0.9f));
            var dnaBefore = new Dictionary<string, int>(session.CareerDna.TraitTotals);

            var rewardEvent = session.AppendStationRewardEvent(new StationRewardEvent(
                CareerConfig.DesignBuildId,
                "seed.demo",
                CompletionTier.Degree,
                ResultSource.Solo,
                "You practiced Building.",
                "accessory.tool_belt",
                new[] { new TraitDelta("Building", 5) }));

            Assert.That(rewardEvent, Is.Not.Null);
            Assert.That(rewardEvent.SeedId, Is.EqualTo("seed.demo"), "Reward event carries the selected seed id.");
            Assert.That(rewardEvent.AccessoryRewardId, Is.EqualTo("accessory.tool_belt"), "And the unlocked accessory id.");
            Assert.That(session.RewardLog.Recent(1).Single(), Is.SameAs(rewardEvent), "It is appended to the session log.");

            // KTD8: the reward event is presentation only — Career DNA, unique
            // count, and reveal readiness are untouched by appending it.
            Assert.That(session.CareerDna.TraitTotals, Is.EqualTo(dnaBefore));
            Assert.That(session.UniqueCompletedGames, Is.EqualTo(1));
        }

        [Test]
        public void ReplayAppendsRewardEventWithoutInflatingUniqueCount()
        {
            var session = new GameSession();
            session.RecordResult(StationResult(CareerConfig.DesignBuildId, CompletionTier.Practice, 10f, 0.5f));

            session.AppendStationRewardEvent(StationEvent(CareerConfig.DesignBuildId, "seed.a"));
            session.AppendStationRewardEvent(StationEvent(CareerConfig.DesignBuildId, "seed.b")); // replay

            Assert.That(session.RewardLog.Events.Count, Is.EqualTo(2), "Each completion appends an event, replays included.");
            Assert.That(session.UniqueCompletedGames, Is.EqualTo(1), "Replays never inflate the unique count.");
            Assert.That(session.RewardLog.Recent(1).Single().SeedId, Is.EqualTo("seed.b"),
                "The Results page shows the most recent seed-aware micro-result.");
        }

        [Test]
        public void NetworkReadModelCarriesCompletedActivityOrderAndTier()
        {
            var session = new GameSession();
            var snapshots = new[]
            {
                new CompletedActivitySnapshot(CareerQuestCatalog.RoboticsGarageId, CompletionTier.Degree),
                new CompletedActivitySnapshot(CareerQuestCatalog.AiLabId, CompletionTier.Practice)
            };

            session.ApplyNetworkSnapshot(SessionPhase.Hub, ActivityRoute.Campus, 2, 2, snapshots);

            Assert.That(session.CompletedActivityIds,
                Is.EqualTo(new[] { CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.AiLabId }),
                "Client completion order mirrors the replicated snapshot order.");
            Assert.That(session.UniqueCompletedGames, Is.EqualTo(2));

            Assert.That(session.CompletedTier(CareerQuestCatalog.RoboticsGarageId, out var roboticsTier), Is.True);
            Assert.That(roboticsTier, Is.EqualTo(CompletionTier.Degree));
            Assert.That(session.CompletedTier(CareerQuestCatalog.AiLabId, out var aiTier), Is.True);
            Assert.That(aiTier, Is.EqualTo(CompletionTier.Practice));

            // Clients derive the same accessories the host would from this order.
            var earned = AccessoryResolver.ResolveEarned(session).Select(accessory => accessory.Id).ToList();
            Assert.That(earned, Does.Contain("accessory.tool_belt"));
            Assert.That(earned, Does.Contain("accessory.lab_goggles"));
        }

        [Test]
        public void ClearingNetworkReadModelRestoresHostCompletionOrder()
        {
            var session = new GameSession();
            session.RecordResult(StationResult(CareerConfig.DesignBuildId, CompletionTier.Degree, 40f, 0.9f));

            session.ApplyNetworkSnapshot(SessionPhase.Hub, ActivityRoute.Campus, 2, 5,
                new[] { new CompletedActivitySnapshot(CareerQuestCatalog.SpaceportId, CompletionTier.Degree) });
            Assert.That(session.CompletedActivityIds, Is.EqualTo(new[] { CareerQuestCatalog.SpaceportId }));
            Assert.That(session.UniqueCompletedGames, Is.EqualTo(5));

            session.ClearNetworkReadModel();

            Assert.That(session.CompletedActivityIds, Is.EqualTo(new[] { CareerConfig.DesignBuildId }),
                "Back on the host read model, the live completion order returns.");
            Assert.That(session.UniqueCompletedGames, Is.EqualTo(1));
        }

        [Test]
        public void ResetClearsCompletionOrderAndRewardLog()
        {
            var session = new GameSession();
            session.RecordResult(StationResult(CareerConfig.DesignBuildId, CompletionTier.Degree, 40f, 0.9f));
            session.AppendStationRewardEvent(StationEvent(CareerConfig.DesignBuildId, "seed.a"));

            session.ResetResults();

            Assert.That(session.CompletedActivityIds, Is.Empty);
            Assert.That(session.RewardLog.Events, Is.Empty);
            Assert.That(session.UniqueCompletedGames, Is.EqualTo(0));
        }

        private static MiniGameResult Result(CompletionTier tier, float timeRemaining, float accuracy)
        {
            return new MiniGameResult(
                CareerConfig.DesignBuildId,
                "Future City Design Build",
                tier,
                ResultSource.Solo,
                new[] { new TraitDelta("Building", 3), new TraitDelta("Creativity", 2) },
                timeRemaining,
                accuracy,
                "Built a city.");
        }

        private static MiniGameResult StationResult(string activityId, CompletionTier tier, float timeRemaining, float accuracy)
        {
            return new MiniGameResult(
                activityId,
                activityId,
                tier,
                ResultSource.Solo,
                new[] { new TraitDelta("Building", 3) },
                timeRemaining,
                accuracy,
                "You practiced something today.");
        }

        private static StationRewardEvent StationEvent(string stationId, string seedId)
        {
            return new StationRewardEvent(
                stationId,
                seedId,
                CompletionTier.Degree,
                ResultSource.Solo,
                "You practiced Building.",
                "accessory.tool_belt",
                new[] { new TraitDelta("Building", 5) });
        }
    }
}
