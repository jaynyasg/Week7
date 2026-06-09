using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    public class LogicCourtRulesTests
    {
        [Test]
        public void CorrectEvidenceSortCompletesLogicCourt()
        {
            var gameObject = new GameObject("logic-court-test");
            var controller = gameObject.AddComponent<LogicCourtController>();

            Assert.That(controller.SortEvidence(new[] { true, false, true }), Is.True);
            var result = controller.CreateResult(true, ResultSource.SoloFallback);

            Assert.That(result.ActivityId, Is.EqualTo(CareerConfig.LogicCourtId));
            Assert.That(result.Tier, Is.EqualTo(CompletionTier.Degree));
            Assert.That(result.TraitValue("Reasoning"), Is.GreaterThan(0));

            Object.DestroyImmediate(gameObject);
        }
    }
}
