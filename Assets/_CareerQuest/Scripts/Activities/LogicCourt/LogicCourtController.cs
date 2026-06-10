using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    public class LogicCourtController : MonoBehaviour
    {
        private static readonly Color Ink = new(0.16f, 0.1f, 0.2f);
        private static readonly Color Paper = new(1f, 0.97f, 0.86f, 0.9f);
        private static readonly Color ButtonPrimary = new(0.08f, 0.34f, 0.42f);
        private static readonly Color ButtonReady = new(0.05f, 0.48f, 0.4f);

        public IReadOnlyList<EvidenceCard> Evidence { get; } = new[]
        {
            new EvidenceCard("The bridge model held 20 blocks.", true),
            new EvidenceCard("Someone liked the blue paint.", false),
            new EvidenceCard("The blueprint matched all safety slots.", true)
        };

        public bool SortEvidence(IEnumerable<bool> helpfulSelections)
        {
            return Evidence.Select(card => card.Helpful).SequenceEqual(helpfulSelections);
        }

        public MiniGameResult CreateResult(bool success, ResultSource source)
        {
            return new MiniGameResult(
                CareerConfig.LogicCourtId,
                "Logic Court",
                success ? CompletionTier.Degree : CompletionTier.Practice,
                source,
                new[]
                {
                    new TraitDelta("Reasoning", success ? 5 : 3),
                    new TraitDelta("Communication", success ? 4 : 2),
                    new TraitDelta("Focus", 3),
                    new TraitDelta("Leadership", 2)
                },
                success ? 35f : 12f,
                success ? 0.94f : 0.58f,
                success
                    ? "Sorted useful evidence and made a strong closing argument."
                    : "Practiced spotting evidence that makes an argument stronger.");
        }

        public void Render(Transform parent, GameSession session, CareerQuestApp app, ResultSource source)
        {
            var panel = UiBuilder.FullPanel(parent, "LogicCourtPanel", new Color(0.97f, 0.93f, 1f, 0.04f));
            var caseReviewed = false;
            var testMarked = false;
            var paintRejected = false;
            var blueprintMarked = false;
            var mistakes = 0;

            var questHud = UiBuilder.Panel(panel, "LogicCourtQuestHud", Paper);
            UiBuilder.Place(questHud, -286f, 282f, 664f, 96f);

            UiBuilder.Shape(questHud, "LogicCourtHudStripe", new Color(0.96f, 0.62f, 0.18f, 0.95f), -318f, 0f, 14f, 96f);

            var title = UiBuilder.Text(questHud, "LogicCourtTitle", "Logic Court", 22, TextAnchor.MiddleLeft, Ink);
            UiBuilder.Place(title.rectTransform, 4f, 27f, 560f, 26f);

            var prompt = UiBuilder.Text(questHud, "LogicCourtPrompt", "Review the case, sort evidence, then choose a closing argument.", 15, TextAnchor.MiddleLeft, Ink);
            UiBuilder.Place(prompt.rectTransform, 4f, 0f, 560f, 24f);

            var status = UiBuilder.Text(questHud, "LogicCourtStatus", "Evidence sorted: 0/3", 13, TextAnchor.MiddleLeft, new Color(0.12f, 0.08f, 0.18f));
            UiBuilder.Place(status.rectTransform, 4f, -27f, 560f, 22f);

            void Refresh()
            {
                var count = (testMarked ? 1 : 0) + (paintRejected ? 1 : 0) + (blueprintMarked ? 1 : 0);
                status.text = $"Evidence sorted: {count}/3";
            }

            var evidenceTray = UiBuilder.Panel(panel, "LogicCourtEvidenceTray", new Color(0.95f, 0.99f, 1f, 0.78f));
            UiBuilder.Place(evidenceTray, -254f, -320f, 790f, 56f);

            var trayLabel = UiBuilder.Text(evidenceTray, "LogicCourtTrayLabel", "Evidence", 12, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(trayLabel.rectTransform, -344f, 0f, 82f, 22f);

            var review = UiBuilder.Button(evidenceTray, "LogicCourtReviewButton", "Review Case", () =>
            {
                caseReviewed = true;
                prompt.text = "Case reviewed: only evidence that proves safety and fit should support the argument.";
            });
            UiBuilder.Place(review.GetComponent<RectTransform>(), -236f, 0f, 136f, 34f);
            StyleButton(review, ButtonPrimary, 14);

            var test = UiBuilder.Button(evidenceTray, "LogicCourtTestButton", "Test Helpful", () =>
            {
                if (!caseReviewed)
                {
                    mistakes++;
                    prompt.text = "Review the case before sorting evidence.";
                    return;
                }

                testMarked = true;
                prompt.text = "Correct: bridge test results are useful evidence.";
                Refresh();
            });
            UiBuilder.Place(test.GetComponent<RectTransform>(), -78f, 0f, 146f, 34f);
            StyleButton(test, ButtonPrimary, 14);

            var paint = UiBuilder.Button(evidenceTray, "LogicCourtPaintButton", "Reject Paint", () =>
            {
                if (!caseReviewed)
                {
                    mistakes++;
                    prompt.text = "Review the case before sorting evidence.";
                    return;
                }

                paintRejected = true;
                prompt.text = "Correct: liking blue paint is not proof the design works.";
                Refresh();
            });
            UiBuilder.Place(paint.GetComponent<RectTransform>(), 86f, 0f, 146f, 34f);
            StyleButton(paint, ButtonPrimary, 14);

            var blueprint = UiBuilder.Button(evidenceTray, "LogicCourtBlueprintButton", "Blueprint Helpful", () =>
            {
                if (!caseReviewed)
                {
                    mistakes++;
                    prompt.text = "Review the case before sorting evidence.";
                    return;
                }

                blueprintMarked = true;
                prompt.text = "Correct: matching the safety slots supports the design.";
                Refresh();
            });
            UiBuilder.Place(blueprint.GetComponent<RectTransform>(), 266f, 0f, 178f, 34f);
            StyleButton(blueprint, ButtonPrimary, 14);

            var closing = UiBuilder.Button(panel, "LogicCourtClosingButton", "Make Argument", () =>
            {
                if (!testMarked || !paintRejected || !blueprintMarked)
                {
                    mistakes++;
                    prompt.text = "Sort all three evidence cards before your closing argument.";
                    return;
                }

                session.RecordResult(CreateResult(mistakes <= 1, source));
                app.ShowGallery();
            });
            UiBuilder.Place(closing.GetComponent<RectTransform>(), 430f, -320f, 156f, 38f);
            StyleButton(closing, ButtonReady, 14);

            var campus = UiBuilder.Button(panel, "LogicCourtCampusButton", "Campus", app.ShowCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 570f, -320f, 112f, 38f);
            StyleButton(campus, ButtonPrimary, 14);
        }

        private static void StyleButton(Button button, Color color, int fontSize)
        {
            button.GetComponent<Image>().color = color;
            var label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            label.fontSize = fontSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = fontSize;
        }
    }
}
