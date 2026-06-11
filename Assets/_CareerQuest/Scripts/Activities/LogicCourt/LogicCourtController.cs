using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    public class LogicCourtController : ActivityRoomController
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
            BeginRoom(CareerConfig.LogicCourtId);
            var networkState = FindAnyObjectByType<LogicCourtNetworkState>();

            var panel = UiBuilder.FullPanel(parent, "LogicCourtPanel", new Color(0.97f, 0.93f, 1f, 0.04f));
            var caseReviewed = false;
            var testMarked = false;
            var paintRejected = false;
            var blueprintMarked = false;
            var mistakes = 0;

            var hud = ActivityRoomChrome.MountQuestHud(
                panel,
                "LogicCourt",
                new Color(1f, 0.97f, 0.86f, 0.9f),
                new Color(0.96f, 0.62f, 0.18f, 0.95f),
                "Logic Court",
                "Review the case, sort evidence, then choose a closing argument.",
                "Evidence sorted: 0/3");

            void Refresh()
            {
                var count = (testMarked ? 1 : 0) + (paintRejected ? 1 : 0) + (blueprintMarked ? 1 : 0);
                hud.Status.text = $"Evidence sorted: {count}/3";
            }

            var evidenceTray = UiBuilder.Panel(panel, "LogicCourtEvidenceTray", new Color(0.95f, 0.99f, 1f, 0.78f));
            UiBuilder.Place(evidenceTray, -254f, -320f, 790f, 56f);

            var trayLabel = UiBuilder.Text(evidenceTray, "LogicCourtTrayLabel", "Evidence", 12, TextAnchor.MiddleCenter, ActivityRoomChrome.InkDefault);
            UiBuilder.Place(trayLabel.rectTransform, -344f, 0f, 82f, 22f);

            var review = UiBuilder.Button(evidenceTray, "LogicCourtReviewButton", "Review Case", () =>
            {
                caseReviewed = true;
                hud.Prompt.text = "Case reviewed: only evidence that proves safety and fit should support the argument.";
            });
            UiBuilder.Place(review.GetComponent<RectTransform>(), -236f, 0f, 136f, 34f);
            ActivityRoomChrome.StyleButton(review, ActivityRoomChrome.ButtonPrimary, 14);

            var test = UiBuilder.Button(evidenceTray, "LogicCourtTestButton", "Test Helpful", () =>
            {
                if (!caseReviewed)
                {
                    mistakes++;
                    hud.Prompt.text = "Review the case before sorting evidence.";
                    return;
                }

                testMarked = true;
                hud.Prompt.text = "Correct: bridge test results are useful evidence.";
                if (source == ResultSource.Multiplayer && networkState != null && networkState.IsSpawned)
                {
                    networkState.SubmitStep(0);
                }

                Refresh();
            });
            UiBuilder.Place(test.GetComponent<RectTransform>(), -78f, 0f, 146f, 34f);
            ActivityRoomChrome.StyleButton(test, ActivityRoomChrome.ButtonPrimary, 14);

            var paint = UiBuilder.Button(evidenceTray, "LogicCourtPaintButton", "Reject Paint", () =>
            {
                if (!caseReviewed)
                {
                    mistakes++;
                    hud.Prompt.text = "Review the case before sorting evidence.";
                    return;
                }

                paintRejected = true;
                hud.Prompt.text = "Correct: liking blue paint is not proof the design works.";
                if (source == ResultSource.Multiplayer && networkState != null && networkState.IsSpawned)
                {
                    networkState.SubmitStep(1);
                }

                Refresh();
            });
            UiBuilder.Place(paint.GetComponent<RectTransform>(), 86f, 0f, 146f, 34f);
            ActivityRoomChrome.StyleButton(paint, ActivityRoomChrome.ButtonPrimary, 14);

            var blueprint = UiBuilder.Button(evidenceTray, "LogicCourtBlueprintButton", "Blueprint Helpful", () =>
            {
                if (!caseReviewed)
                {
                    mistakes++;
                    hud.Prompt.text = "Review the case before sorting evidence.";
                    return;
                }

                blueprintMarked = true;
                hud.Prompt.text = "Correct: matching the safety slots supports the design.";
                if (source == ResultSource.Multiplayer && networkState != null && networkState.IsSpawned)
                {
                    networkState.SubmitStep(2);
                }

                Refresh();
            });
            UiBuilder.Place(blueprint.GetComponent<RectTransform>(), 266f, 0f, 178f, 34f);
            ActivityRoomChrome.StyleButton(blueprint, ActivityRoomChrome.ButtonPrimary, 14);

            var closing = UiBuilder.Button(panel, "LogicCourtClosingButton", "Make Argument", () =>
            {
                if (!testMarked || !paintRejected || !blueprintMarked)
                {
                    mistakes++;
                    hud.Prompt.text = "Sort all three evidence cards before your closing argument.";
                    return;
                }

                if (source == ResultSource.Multiplayer && networkState != null && networkState.IsSpawned && !networkState.Complete)
                {
                    hud.Prompt.text = "Wait for both players to sort all evidence.";
                    return;
                }

                TryCompleteRoom(session, app, CreateResult(mistakes <= 1, source));
            });
            UiBuilder.Place(closing.GetComponent<RectTransform>(), 430f, -320f, 156f, 38f);
            ActivityRoomChrome.StyleButton(closing, ActivityRoomChrome.ButtonReady, 14);

            var campus = UiBuilder.Button(panel, "LogicCourtCampusButton", "Campus", () => ExitToCampus(app));
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 570f, -320f, 112f, 38f);
            ActivityRoomChrome.StyleButton(campus, ActivityRoomChrome.ButtonPrimary, 14);
        }
    }
}
