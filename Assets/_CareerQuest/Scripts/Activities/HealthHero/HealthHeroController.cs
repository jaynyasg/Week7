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
            var symptomChecked = false;
            var toolSelected = false;
            var careSelected = false;
            var mistakes = 0;

            var title = UiBuilder.Text(panel, "HealthHeroTitle", "Health Hero Clinic", 38, TextAnchor.MiddleCenter, new Color(0.08f, 0.22f, 0.12f));
            UiBuilder.Place(title.rectTransform, 0f, 230f, 900f, 60f);

            var prompt = UiBuilder.Text(panel, "HealthHeroPrompt", "Case: sore throat. Start by checking symptoms.", 24, TextAnchor.MiddleCenter, new Color(0.1f, 0.18f, 0.12f));
            UiBuilder.Place(prompt.rectTransform, 0f, 150f, 900f, 60f);

            var status = UiBuilder.Text(panel, "HealthHeroStatus", "Progress: symptoms unchecked / no tool / no care plan", 20, TextAnchor.MiddleCenter, new Color(0.08f, 0.16f, 0.12f));
            UiBuilder.Place(status.rectTransform, 0f, 100f, 980f, 40f);

            void Refresh()
            {
                status.text = $"Progress: {(symptomChecked ? "symptoms checked" : "symptoms unchecked")} / {(toolSelected ? "thermometer ready" : "no tool")} / {(careSelected ? "care plan ready" : "no care plan")}";
            }

            var check = UiBuilder.Button(panel, "HealthHeroCheckButton", "Check Symptoms", () =>
            {
                symptomChecked = true;
                prompt.text = "You found a sore throat and warm forehead. Choose a useful tool.";
                Refresh();
            });
            UiBuilder.Place(check.GetComponent<RectTransform>(), -360f, 32f, 230f, 54f);

            var tool = UiBuilder.Button(panel, "HealthHeroToolButton", "Thermometer", () =>
            {
                if (!symptomChecked)
                {
                    mistakes++;
                    prompt.text = "Check symptoms before choosing tools.";
                    return;
                }

                toolSelected = true;
                prompt.text = "Good tool choice. Now choose a kind care plan.";
                Refresh();
            });
            UiBuilder.Place(tool.GetComponent<RectTransform>(), -120f, 32f, 210f, 54f);

            var wrongTool = UiBuilder.Button(panel, "HealthHeroWrongToolButton", "Bandage", () =>
            {
                mistakes++;
                prompt.text = "A bandage will not help this sore throat. Try the tool that measures temperature.";
            });
            UiBuilder.Place(wrongTool.GetComponent<RectTransform>(), 120f, 32f, 190f, 54f);

            var care = UiBuilder.Button(panel, "HealthHeroCareButton", "Warm Tea + Rest", () =>
            {
                if (!toolSelected)
                {
                    mistakes++;
                    prompt.text = "Pick the right tool before choosing the care plan.";
                    return;
                }

                careSelected = true;
                prompt.text = "Care plan ready. Complete the case to earn your badge.";
                Refresh();
            });
            UiBuilder.Place(care.GetComponent<RectTransform>(), 360f, 32f, 240f, 54f);

            var complete = UiBuilder.Button(panel, "HealthHeroCompleteButton", "Complete Case", () =>
            {
                if (!symptomChecked || !toolSelected || !careSelected)
                {
                    mistakes++;
                    prompt.text = "Finish all three clinic steps before completing the case.";
                    return;
                }

                session.RecordResult(CreateResult(mistakes <= 1, source));
                app.ShowGallery();
            });
            UiBuilder.Place(complete.GetComponent<RectTransform>(), -130f, -118f, 240f, 64f);

            var campus = UiBuilder.Button(panel, "HealthHeroCampusButton", "Campus", app.ShowCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 130f, -118f, 210f, 64f);
        }
    }
}
