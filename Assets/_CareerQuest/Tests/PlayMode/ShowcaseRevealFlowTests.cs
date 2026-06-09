using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class ShowcaseRevealFlowTests
    {
        [Test]
        public void SeededShowcaseUnlocksRevealUnderOneSession()
        {
            var session = new GameSession();

            session.SeedShowcase();

            Assert.That(session.RevealReady, Is.True);
            Assert.That(session.ConfidencePhrase(), Is.EqualTo("Very strong match"));
            Assert.That(session.DebugSourceSummary, Is.EqualTo("Showcase seeded"));
        }
    }
}
