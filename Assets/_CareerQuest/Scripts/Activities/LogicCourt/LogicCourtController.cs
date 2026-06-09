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
            var title = UiBuilder.Text(panel, "LogicCourtTitle", "Logic Court", 38, TextAnchor.MiddleCenter, new Color(0.18f, 0.1f, 0.24f));
            UiBuilder.Place(title.rectTransform, 0f, 230f, 900f, 60f);

            var prompt = UiBuilder.Text(panel, "LogicCourtPrompt", "Which evidence actually supports the city design?", 24, TextAnchor.MiddleCenter, new Color(0.18f, 0.1f, 0.24f));
            UiBuilder.Place(prompt.rectTransform, 0f, 145f, 900f, 60f);

            var correct = UiBuilder.Button(panel, "LogicCourtCorrectButton", "Use test results + blueprint", () =>
            {
                session.RecordResult(CreateResult(true, source));
                app.ShowGallery();
            });
            UiBuilder.Place(correct.GetComponent<RectTransform>(), -170f, 25f, 330f, 64f);

            var practice = UiBuilder.Button(panel, "LogicCourtPracticeButton", "Use paint preference", () =>
            {
                session.RecordResult(CreateResult(false, source));
                app.ShowGallery();
            });
            UiBuilder.Place(practice.GetComponent<RectTransform>(), 180f, 25f, 280f, 64f);

            var campus = UiBuilder.Button(panel, "LogicCourtCampusButton", "Campus", app.ShowCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 0f, -150f, 210f, 64f);
        }
    }
}
