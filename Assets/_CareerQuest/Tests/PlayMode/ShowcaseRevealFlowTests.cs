using System.Linq;
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

        [Test]
        public void SeededShowcaseSynthesisResolvesSensiblyForTheReveal()
        {
            // U7: the seeded showcase must still resolve a coherent reveal —
            // ready, a named family + superpower, and the locked co-leads near
            // the top of the synthesized paths.
            var session = new GameSession();
            session.SeedShowcase();

            var synthesis = RevealSynthesis.Resolve(session);

            Assert.That(synthesis.IsRevealReady, Is.True);
            Assert.That(synthesis.Style, Is.EqualTo(RevealStyle.Simple), "3 seeded completions = Simple style bucket.");
            Assert.That(synthesis.Superpower, Is.Not.Empty);
            Assert.That(CareerFamilies.All, Does.Contain(synthesis.PrimaryFamily));
            Assert.That(synthesis.TopPaths.Count, Is.EqualTo(RevealSynthesis.TopPathCount));

            var topIds = synthesis.TopPaths.Select(match => match.Career.Id).ToList();
            Assert.That(topIds, Does.Contain("ai_engineer").Or.Contain("architect"));
        }
    }
}
