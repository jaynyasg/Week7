using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class ActivityResultEmitterTests
    {
        [Test]
        public void ResultEmitterRecordsMatchingActivityOnce()
        {
            var session = new GameSession();
            var state = new ActivitySessionState(CareerConfig.DesignBuildId);
            var emitter = new ActivityResultEmitter();
            var result = Result(CareerConfig.DesignBuildId);

            Assert.That(emitter.TryRecord(session, state, result), Is.True);
            Assert.That(emitter.TryRecord(session, state, result), Is.False);
            Assert.That(session.UniqueCompletedGames, Is.EqualTo(1));
            Assert.That(state.ResultRecorded, Is.True);
            Assert.That(state.Phase, Is.EqualTo(ActivityPhase.ResultRecorded));
        }

        [Test]
        public void ResultEmitterRejectsMismatchedActivity()
        {
            var session = new GameSession();
            var state = new ActivitySessionState(CareerConfig.HealthHeroId);
            var emitter = new ActivityResultEmitter();

            Assert.That(emitter.TryRecord(session, state, Result(CareerConfig.DesignBuildId)), Is.False);
            Assert.That(session.UniqueCompletedGames, Is.EqualTo(0));
        }

        private static MiniGameResult Result(string activityId)
        {
            return new MiniGameResult(
                activityId,
                "Activity",
                CompletionTier.Degree,
                ResultSource.Solo,
                new[] { new TraitDelta("Focus", 2) },
                30f,
                1f,
                "Completed.");
        }
    }
}
