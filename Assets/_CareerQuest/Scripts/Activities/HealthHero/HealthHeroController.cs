using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    public class HealthHeroController : MonoBehaviour
    {
        private static readonly Color Ink = new(0.08f, 0.18f, 0.14f);
        private static readonly Color Paper = new(1f, 0.97f, 0.86f, 0.9f);
        private static readonly Color ButtonPrimary = new(0.08f, 0.34f, 0.42f);
        private static readonly Color ButtonReady = new(0.05f, 0.48f, 0.4f);

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
            var panel = UiBuilder.FullPanel(parent, "HealthHeroPanel", new Color(0.92f, 1f, 0.92f, 0.04f));
            var symptomChecked = false;
            var toolSelected = false;
            var careSelected = false;
            var mistakes = 0;

            var questHud = UiBuilder.Panel(panel, "HealthHeroQuestHud", Paper);
            UiBuilder.Place(questHud, -286f, 282f, 664f, 96f);

            UiBuilder.Shape(questHud, "HealthHeroHudStripe", new Color(0.36f, 0.78f, 0.6f, 0.95f), -318f, 0f, 14f, 96f);

            var title = UiBuilder.Text(questHud, "HealthHeroTitle", "Health Hero Clinic", 22, TextAnchor.MiddleLeft, Ink);
            UiBuilder.Place(title.rectTransform, 4f, 27f, 560f, 26f);

            var prompt = UiBuilder.Text(questHud, "HealthHeroPrompt", "Case: sore throat. Start by checking symptoms.", 15, TextAnchor.MiddleLeft, Ink);
            UiBuilder.Place(prompt.rectTransform, 4f, 0f, 560f, 24f);

            var status = UiBuilder.Text(questHud, "HealthHeroStatus", "Progress: symptoms unchecked / no tool / no care plan", 13, TextAnchor.MiddleLeft, new Color(0.08f, 0.16f, 0.12f));
            UiBuilder.Place(status.rectTransform, 4f, -27f, 560f, 22f);

            void Refresh()
            {
                status.text = $"Progress: {(symptomChecked ? "symptoms checked" : "symptoms unchecked")} / {(toolSelected ? "thermometer ready" : "no tool")} / {(careSelected ? "care plan ready" : "no care plan")}";
            }

            var toolTray = UiBuilder.Panel(panel, "HealthHeroToolTray", new Color(0.95f, 0.99f, 1f, 0.78f));
            UiBuilder.Place(toolTray, -254f, -320f, 790f, 56f);

            var trayLabel = UiBuilder.Text(toolTray, "HealthHeroTrayLabel", "Clinic tools", 12, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(trayLabel.rectTransform, -344f, 0f, 82f, 22f);

            var check = UiBuilder.Button(toolTray, "HealthHeroCheckButton", "Check Symptoms", () =>
            {
                symptomChecked = true;
                prompt.text = "You found a sore throat and warm forehead. Choose a useful tool.";
                Refresh();
            });
            UiBuilder.Place(check.GetComponent<RectTransform>(), -222f, 0f, 164f, 34f);
            StyleButton(check, ButtonPrimary, 14);

            var tool = UiBuilder.Button(toolTray, "HealthHeroToolButton", "Thermometer", () =>
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
            UiBuilder.Place(tool.GetComponent<RectTransform>(), -52f, 0f, 150f, 34f);
            StyleButton(tool, ButtonPrimary, 14);

            var wrongTool = UiBuilder.Button(toolTray, "HealthHeroWrongToolButton", "Bandage", () =>
            {
                mistakes++;
                prompt.text = "A bandage will not help this sore throat. Try the tool that measures temperature.";
            });
            UiBuilder.Place(wrongTool.GetComponent<RectTransform>(), 110f, 0f, 138f, 34f);
            StyleButton(wrongTool, ButtonPrimary, 14);

            var care = UiBuilder.Button(toolTray, "HealthHeroCareButton", "Warm Tea + Rest", () =>
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
            UiBuilder.Place(care.GetComponent<RectTransform>(), 286f, 0f, 174f, 34f);
            StyleButton(care, ButtonPrimary, 14);

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
            UiBuilder.Place(complete.GetComponent<RectTransform>(), 430f, -320f, 156f, 38f);
            StyleButton(complete, ButtonReady, 14);

            var campus = UiBuilder.Button(panel, "HealthHeroCampusButton", "Campus", app.ShowCampus);
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
