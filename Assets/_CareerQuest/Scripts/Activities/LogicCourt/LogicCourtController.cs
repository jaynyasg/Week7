using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CareerQuest
{
    public class LogicCourtController : MonoBehaviour
    {
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
            var panel = UiBuilder.FullPanel(parent, "LogicCourtPanel", new Color(0.97f, 0.93f, 1f));
            var caseReviewed = false;
            var testMarked = false;
            var paintRejected = false;
            var blueprintMarked = false;
            var mistakes = 0;

            var title = UiBuilder.Text(panel, "LogicCourtTitle", "Logic Court", 38, TextAnchor.MiddleCenter, new Color(0.18f, 0.1f, 0.24f));
            UiBuilder.Place(title.rectTransform, 0f, 230f, 900f, 60f);

            var prompt = UiBuilder.Text(panel, "LogicCourtPrompt", "Review the case, sort evidence, then choose a closing argument.", 24, TextAnchor.MiddleCenter, new Color(0.18f, 0.1f, 0.24f));
            UiBuilder.Place(prompt.rectTransform, 0f, 150f, 960f, 60f);

            var status = UiBuilder.Text(panel, "LogicCourtStatus", "Evidence sorted: 0/3", 20, TextAnchor.MiddleCenter, new Color(0.12f, 0.08f, 0.18f));
            UiBuilder.Place(status.rectTransform, 0f, 100f, 900f, 36f);

            void Refresh()
            {
                var count = (testMarked ? 1 : 0) + (paintRejected ? 1 : 0) + (blueprintMarked ? 1 : 0);
                status.text = $"Evidence sorted: {count}/3";
            }

            var review = UiBuilder.Button(panel, "LogicCourtReviewButton", "Review Case", () =>
            {
                caseReviewed = true;
                prompt.text = "Case reviewed: only evidence that proves safety and fit should support the argument.";
            });
            UiBuilder.Place(review.GetComponent<RectTransform>(), -390f, 32f, 220f, 54f);

            var test = UiBuilder.Button(panel, "LogicCourtTestButton", "Mark Test Results Helpful", () =>
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
            UiBuilder.Place(test.GetComponent<RectTransform>(), -130f, 32f, 260f, 54f);

            var paint = UiBuilder.Button(panel, "LogicCourtPaintButton", "Reject Paint Preference", () =>
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
            UiBuilder.Place(paint.GetComponent<RectTransform>(), 150f, 32f, 260f, 54f);

            var blueprint = UiBuilder.Button(panel, "LogicCourtBlueprintButton", "Mark Blueprint Helpful", () =>
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
            UiBuilder.Place(blueprint.GetComponent<RectTransform>(), 430f, 32f, 260f, 54f);

            var closing = UiBuilder.Button(panel, "LogicCourtClosingButton", "Make Closing Argument", () =>
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
            UiBuilder.Place(closing.GetComponent<RectTransform>(), -140f, -118f, 280f, 64f);

            var campus = UiBuilder.Button(panel, "LogicCourtCampusButton", "Campus", app.ShowCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 160f, -118f, 210f, 64f);
        }
    }
}
