using CareerQuest;
using NUnit.Framework;
using TMPro;
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

        /// <summary>
        /// U6 migration: the button rows are gone — the room renders a quest HUD
        /// (top band), a Campus exit (bottom edge), and everything plays through
        /// the TrySubmitDrop seam. Legacy piece/review/helper buttons must NOT
        /// come back.
        /// </summary>
        [Test]
        public void DesignBuildRenderShowsDragRoomChrome()
        {
            var appObject = new GameObject("design-build-ui-test");
            var app = appObject.AddComponent<CareerQuestApp>();

            app.ShowDesignBuild(false);

            var questHud = FindRectTransform("DesignBuildQuestHud");
            var campusButton = GameObject.Find("DesignBuildCampusButton").GetComponent<RectTransform>();
            var title = FindTmp("DesignBuildTitle");
            var feedback = FindTmp("DesignBuildPrompt");
            var status = FindTmp("DesignBuildStatus");

            // HUD stays off the skyline: quest band at the top, exit at the bottom edge.
            Assert.That(questHud.anchoredPosition.y, Is.GreaterThan(220f));
            Assert.That(campusButton.anchoredPosition.y, Is.InRange(-272f, -200f), "Bottom band but ABOVE the instruction strip (strip top edge is y -273).");
            Assert.That(campusButton.sizeDelta.y, Is.LessThanOrEqualTo(44f));
            Assert.That(title.fontSize, Is.LessThanOrEqualTo(28));
            Assert.That(feedback.text, Does.Contain("Drag"));

            // The legacy button interaction is retired (R10 — drag-and-drop room).
            Assert.That(GameObject.Find("ReviewBlueprintButton"), Is.Null);
            Assert.That(GameObject.Find("PatternHelperButton"), Is.Null);
            Assert.That(GameObject.Find("clinicButton"), Is.Null);
            Assert.That(GameObject.Find("DesignBuildCompleteButton"), Is.Null);

            // The seam is live immediately after Render and drives the HUD text.
            var controller = appObject.GetComponent<DesignBuildController>();
            Assert.That(controller.TrySubmitDrop("clinic", "clinic"), Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(controller.IsPieceAccepted("clinic"), Is.True);
            Assert.That(feedback.text, Does.Contain("Accepted"));
            Assert.That(status.text, Does.Contain("1/5"));

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

        private static TextMeshProUGUI FindTmp(string name)
        {
            foreach (var text in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                if (text.name == name)
                {
                    return text;
                }
            }

            Assert.Fail($"{name} should exist.");
            return null;
        }
    }
}
