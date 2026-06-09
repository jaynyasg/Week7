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
    }
}
