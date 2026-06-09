using UnityEngine;

namespace CareerQuest
{
    public class HealthHeroController : MonoBehaviour
    {
        public HealthHeroCase CurrentCase { get; private set; } = new("sore throat", "thermometer", "warm tea and rest");

        public bool CheckMatch(string symptom, string tool, string treatment)
        {
            return symptom == CurrentCase.Symptom && tool == CurrentCase.Tool && treatment == CurrentCase.Treatment;
        }

        public MiniGameResult CreateResult(bool success, ResultSource source)
        {
            return new MiniGameResult(
                CareerConfig.HealthHeroId,
                "Health Hero Clinic",
                success ? CompletionTier.Degree : CompletionTier.Practice,
                source,
                new[]
                {
                    new TraitDelta("Helping", success ? 5 : 3),
                    new TraitDelta("Science", success ? 4 : 2),
                    new TraitDelta("Focus", 3),
                    new TraitDelta("Communication", 3)
                },
                success ? 42f : 15f,
                success ? 0.92f : 0.65f,
                success
                    ? "Matched symptoms to the right tool and care plan."
                    : "Practiced reading symptoms and choosing helpful care.");
        }

        public void Render(Transform parent, GameSession session, CareerQuestApp app, ResultSource source)
        {
            var panel = UiBuilder.FullPanel(parent, "HealthHeroPanel", new Color(0.92f, 1f, 0.92f));
            var title = UiBuilder.Text(panel, "HealthHeroTitle", "Health Hero Clinic", 38, TextAnchor.MiddleCenter, new Color(0.08f, 0.22f, 0.12f));
            UiBuilder.Place(title.rectTransform, 0f, 230f, 900f, 60f);

            var prompt = UiBuilder.Text(panel, "HealthHeroPrompt", "Case: sore throat. Pick the tool and care plan.", 24, TextAnchor.MiddleCenter, new Color(0.1f, 0.18f, 0.12f));
            UiBuilder.Place(prompt.rectTransform, 0f, 140f, 900f, 70f);

            var correct = UiBuilder.Button(panel, "HealthHeroCorrectButton", "Thermometer + warm tea", () =>
            {
                session.RecordResult(CreateResult(true, source));
                app.ShowGallery();
            });
            UiBuilder.Place(correct.GetComponent<RectTransform>(), -160f, 20f, 330f, 64f);

            var practice = UiBuilder.Button(panel, "HealthHeroPracticeButton", "Bandage + sprint", () =>
            {
                session.RecordResult(CreateResult(false, source));
                app.ShowGallery();
            });
            UiBuilder.Place(practice.GetComponent<RectTransform>(), 180f, 20f, 280f, 64f);

            var campus = UiBuilder.Button(panel, "HealthHeroCampusButton", "Campus", app.ShowCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 0f, -150f, 210f, 64f);
        }
    }
}
