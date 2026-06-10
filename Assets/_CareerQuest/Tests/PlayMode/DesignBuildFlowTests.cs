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

            var clinicButton = FindRectTransform("clinicButton");
            var tray = FindRectTransform("DesignBuildToolTray");
            var briefing = GameObject.Find("DesignBuildBriefing").GetComponent<RectTransform>();
            var completeButton = GameObject.Find("DesignBuildCompleteButton").GetComponent<RectTransform>();
            var reviewButton = GameObject.Find("ReviewBlueprintButton").GetComponent<Button>();
            var helperButton = GameObject.Find("PatternHelperButton").GetComponent<Button>();
            var title = GameObject.Find("DesignBuildTitle").GetComponent<Text>();

            Assert.That(tray.gameObject.activeSelf, Is.False);
            Assert.That(tray.anchoredPosition.y, Is.LessThan(-280f));
            Assert.That(briefing.anchoredPosition.y, Is.GreaterThan(220f));
            Assert.That(clinicButton.sizeDelta.y, Is.LessThanOrEqualTo(36f));
            Assert.That(completeButton.sizeDelta.y, Is.LessThanOrEqualTo(44f));
            Assert.That(reviewButton.GetComponent<RectTransform>().sizeDelta.y, Is.LessThanOrEqualTo(36f));
            Assert.That(title.fontSize, Is.LessThanOrEqualTo(28));

            reviewButton.onClick.Invoke();
            helperButton.onClick.Invoke();
            Assert.That(tray.gameObject.activeSelf, Is.True);

            Object.DestroyImmediate(appObject);
        }

        private static RectTransform FindRectTransform(string name)
        {
            foreach (var rectTransform in Resources.FindObjectsOfTypeAll<RectTransform>())
            {
                if (rectTransform.name == name)
                {
                    return rectTransform;
                }
            }

            Assert.Fail($"{name} should exist.");
            return null;
        }
    }
}
