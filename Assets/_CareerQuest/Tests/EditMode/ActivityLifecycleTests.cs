using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class ActivityLifecycleTests
    {
        [Test]
        public void LifecycleTracksCanonicalPhasesAndFeedback()
        {
            var lifecycle = new ActivityLifecycle(CareerConfig.DesignBuildId);
            var changeCount = 0;
            lifecycle.Changed += _ => changeCount++;

            lifecycle.BeginExplore();
            lifecycle.BeginInteract();
            var feedback = lifecycle.ApplyAction(
                new ActivityAction("place", "p1", "clinic"),
                action => ActivityFeedback.Accept($"Accepted {action.Payload}."));
            lifecycle.BeginReview();
            lifecycle.MarkComplete();

            Assert.That(feedback.Accepted, Is.True);
            Assert.That(lifecycle.State.Phase, Is.EqualTo(ActivityPhase.Complete));
            Assert.That(lifecycle.State.AcceptedActions, Is.EqualTo(1));
            Assert.That(lifecycle.State.LastFeedback, Is.EqualTo("Accepted clinic."));
            Assert.That(changeCount, Is.GreaterThanOrEqualTo(4));
        }

        [Test]
        public void MissingReducerRejectsActionWithoutThrowing()
        {
            var lifecycle = new ActivityLifecycle(CareerConfig.HealthHeroId);

            var feedback = lifecycle.ApplyAction(new ActivityAction("unknown"), null);

            Assert.That(feedback.Accepted, Is.False);
            Assert.That(lifecycle.State.RejectedActions, Is.EqualTo(1));
        }
    }
}
