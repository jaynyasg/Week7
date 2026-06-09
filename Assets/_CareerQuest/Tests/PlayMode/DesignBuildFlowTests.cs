using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    public class DesignBuildFlowTests
    {
        [Test]
        public void DesignBuildResultFeedsGameSession()
        {
            var session = new GameSession();
            var gameObject = new GameObject("design-build-flow-test");
            var controller = gameObject.AddComponent<DesignBuildController>();

            foreach (var piece in controller.Blueprint.Pieces)
            {
                controller.TryPlacePiece(piece.Id);
            }

            session.RecordResult(controller.CreateResult(ResultSource.SoloFallback));

            Assert.That(session.RevealReady, Is.True);
            Assert.That(session.GetBestResult(CareerConfig.DesignBuildId), Is.Not.Null);

            Object.DestroyImmediate(gameObject);
        }
    }
}
