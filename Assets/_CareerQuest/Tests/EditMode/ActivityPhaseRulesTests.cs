using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class ActivityPhaseRulesTests
    {
        [Test]
        public void CanCompleteFrom_InteractAndReviewOnly()
        {
            Assert.That(ActivityPhaseRules.CanCompleteFrom(ActivityPhase.Interact), Is.True);
            Assert.That(ActivityPhaseRules.CanCompleteFrom(ActivityPhase.Review), Is.True);
            Assert.That(ActivityPhaseRules.CanCompleteFrom(ActivityPhase.Intro), Is.False);
            Assert.That(ActivityPhaseRules.CanCompleteFrom(ActivityPhase.Ceremony), Is.False);
        }

        [Test]
        public void CanExitFrom_ActivePhasesOnly()
        {
            Assert.That(ActivityPhaseRules.CanExitFrom(ActivityPhase.Explore), Is.True);
            Assert.That(ActivityPhaseRules.CanExitFrom(ActivityPhase.Interact), Is.True);
            Assert.That(ActivityPhaseRules.CanExitFrom(ActivityPhase.Complete), Is.False);
            Assert.That(ActivityPhaseRules.CanExitFrom(ActivityPhase.Ceremony), Is.False);
            Assert.That(ActivityPhaseRules.CanExitFrom(ActivityPhase.ResultRecorded), Is.False);
        }

        [Test]
        public void CanTransition_RejectsInvalidLifecycleJump()
        {
            Assert.That(ActivityPhaseRules.CanTransition(ActivityPhase.Complete, ActivityPhase.Explore), Is.False);
            Assert.That(ActivityPhaseRules.CanTransition(ActivityPhase.Ceremony, ActivityPhase.Interact), Is.False);
            Assert.That(ActivityPhaseRules.CanTransition(ActivityPhase.Interact, ActivityPhase.Review), Is.True);
            Assert.That(ActivityPhaseRules.CanTransition(ActivityPhase.Interact, ActivityPhase.Exit), Is.True);
        }
    }
}
