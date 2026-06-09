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
            Assert.That(session.ConfidencePhrase(), Is.EqualTo("Keep exploring"));
        }

        [Test]
        public void OneResultUnlocksRevealWithGoodMatch()
        {
            var session = new GameSession();

            session.RecordResult(Result(CompletionTier.Degree, 40f, 0.8f));

            Assert.That(session.RevealReady, Is.True);
            Assert.That(session.ConfidencePhrase(), Is.EqualTo("Good match"));
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
        public void AdditionalUniqueResultsImproveConfidence()
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

            Assert.That(session.ConfidencePhrase(), Is.EqualTo("Strong match"));
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
