using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

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

            Assert.That(session.RevealReady, Is.False);
            Assert.That(session.GetBestResult(CareerConfig.DesignBuildId), Is.Not.Null);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void DesignBuildRenderKeepsControlsOffTheSkyline()
        {
            var appObject = new GameObject("design-build-ui-test");
            var app = appObject.AddComponent<CareerQuestApp>();

            app.ShowDesignBuild(false);

            var clinicButton = GameObject.Find("clinicButton").GetComponent<RectTransform>();
            var tray = GameObject.Find("DesignBuildToolTray").GetComponent<RectTransform>();
            var briefing = GameObject.Find("DesignBuildBriefing").GetComponent<RectTransform>();
            var completeButton = GameObject.Find("DesignBuildCompleteButton").GetComponent<RectTransform>();
            var reviewButton = GameObject.Find("ReviewBlueprintButton").GetComponent<RectTransform>();
            var title = GameObject.Find("DesignBuildTitle").GetComponent<Text>();

            Assert.That(tray.anchoredPosition.y, Is.LessThan(-280f));
            Assert.That(briefing.anchoredPosition.y, Is.GreaterThan(220f));
            Assert.That(clinicButton.sizeDelta.y, Is.LessThanOrEqualTo(36f));
            Assert.That(completeButton.sizeDelta.y, Is.LessThanOrEqualTo(44f));
            Assert.That(reviewButton.sizeDelta.y, Is.LessThanOrEqualTo(36f));
            Assert.That(title.fontSize, Is.LessThanOrEqualTo(28));

            Object.DestroyImmediate(appObject);
        }
    }
}
