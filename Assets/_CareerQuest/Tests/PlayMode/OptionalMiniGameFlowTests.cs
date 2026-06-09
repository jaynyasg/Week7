using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    public class OptionalMiniGameFlowTests
    {
        [Test]
        public void OptionalMiniGamesFeedSameBestResultContract()
        {
            var session = new GameSession();
            var healthObject = new GameObject("health-flow-test");
            var courtObject = new GameObject("court-flow-test");
            var health = healthObject.AddComponent<HealthHeroController>();
            var court = courtObject.AddComponent<LogicCourtController>();

            session.RecordResult(health.CreateResult(true, ResultSource.SoloFallback));
            session.RecordResult(court.CreateResult(true, ResultSource.SoloFallback));

            Assert.That(session.GetBestResult(CareerConfig.HealthHeroId), Is.Not.Null);
            Assert.That(session.GetBestResult(CareerConfig.LogicCourtId), Is.Not.Null);
            Assert.That(session.ConfidencePhrase(), Is.EqualTo("Strong match"));

            Object.DestroyImmediate(healthObject);
            Object.DestroyImmediate(courtObject);
        }
    }
}
