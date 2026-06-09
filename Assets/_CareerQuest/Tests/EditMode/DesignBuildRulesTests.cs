using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    public class DesignBuildRulesTests
    {
        [Test]
        public void CorrectPiecesFillSlotsAndCompleteBlueprint()
        {
            var blueprint = FutureCityBlueprint.CreateDefault();

            foreach (var piece in blueprint.Pieces)
            {
                Assert.That(blueprint.TryPlace(piece.Id), Is.True);
            }

            Assert.That(blueprint.Complete, Is.True);
        }

        [Test]
        public void DuplicatePlacementIsRejected()
        {
            var blueprint = FutureCityBlueprint.CreateDefault();

            Assert.That(blueprint.TryPlace("clinic"), Is.True);
            Assert.That(blueprint.TryPlace("clinic"), Is.False);
        }

        [Test]
        public void CompletionEmitsDesignBuildResult()
        {
            var gameObject = new GameObject("design-build-test");
            var controller = gameObject.AddComponent<DesignBuildController>();

            controller.TryPlacePiece("clinic");
            controller.TryPlacePiece("court");
            controller.TryPlacePiece("studio");
            controller.TryPlacePiece("lab");
            controller.TryPlacePiece("art_tower");
            var result = controller.CreateResult(ResultSource.Multiplayer);

            Assert.That(result.ActivityId, Is.EqualTo(CareerConfig.DesignBuildId));
            Assert.That(result.Tier, Is.EqualTo(CompletionTier.Degree));
            Assert.That(result.TraitValue("Building"), Is.GreaterThan(0));

            Object.DestroyImmediate(gameObject);
        }
    }
}
