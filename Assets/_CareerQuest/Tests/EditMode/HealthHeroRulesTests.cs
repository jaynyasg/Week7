using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    public class HealthHeroRulesTests
    {
        [Test]
        public void CorrectMatchCompletesHealthHero()
        {
            var gameObject = new GameObject("health-hero-test");
            var controller = gameObject.AddComponent<HealthHeroController>();

            Assert.That(controller.CheckMatch("sore throat", "thermometer", "warm tea and rest"), Is.True);
            var result = controller.CreateResult(true, ResultSource.SoloFallback);

            Assert.That(result.ActivityId, Is.EqualTo(CareerConfig.HealthHeroId));
            Assert.That(result.Tier, Is.EqualTo(CompletionTier.Degree));
            Assert.That(result.TraitValue("Helping"), Is.GreaterThan(0));

            Object.DestroyImmediate(gameObject);
        }
    }
}
