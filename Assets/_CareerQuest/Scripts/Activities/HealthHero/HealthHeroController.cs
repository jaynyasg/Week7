using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    public class HealthHeroController : ActivityRoomController
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
            BeginRoom(CareerConfig.HealthHeroId);
            var networkState = FindAnyObjectByType<HealthHeroNetworkState>();

            var panel = UiBuilder.FullPanel(parent, "HealthHeroPanel", new Color(0.92f, 1f, 0.92f, 0.04f));
            var symptomChecked = false;
            var toolSelected = false;
            var careSelected = false;
            var mistakes = 0;

            var hud = ActivityRoomChrome.MountQuestHud(
                panel,
                "HealthHero",
                new Color(1f, 0.97f, 0.86f, 0.9f),
                new Color(0.36f, 0.78f, 0.6f, 0.95f),
                "Health Hero Clinic",
                "Case: sore throat. Start by checking symptoms.",
                "Progress: symptoms unchecked / no tool / no care plan");

            void Refresh()
            {
                hud.Status.text = $"Progress: {(symptomChecked ? "symptoms checked" : "symptoms unchecked")} / {(toolSelected ? "thermometer ready" : "no tool")} / {(careSelected ? "care plan ready" : "no care plan")}";
            }

            var toolTray = UiBuilder.Panel(panel, "HealthHeroToolTray", new Color(0.95f, 0.99f, 1f, 0.78f));
            UiBuilder.Place(toolTray, -254f, -320f, 790f, 56f);

            var trayLabel = UiBuilder.Text(toolTray, "HealthHeroTrayLabel", "Clinic tools", 12, TextAnchor.MiddleCenter, ActivityRoomChrome.InkDefault);
            UiBuilder.Place(trayLabel.rectTransform, -344f, 0f, 82f, 22f);

            var check = UiBuilder.Button(toolTray, "HealthHeroCheckButton", "Check Symptoms", () =>
            {
                symptomChecked = true;
                hud.Prompt.text = "You found a sore throat and warm forehead. Choose a useful tool.";
                if (source == ResultSource.Multiplayer && networkState != null && networkState.IsSpawned)
                {
                    networkState.SubmitStep(0);
                }

                Refresh();
            });
            UiBuilder.Place(check.GetComponent<RectTransform>(), -222f, 0f, 164f, 34f);
            ActivityRoomChrome.StyleButton(check, ActivityRoomChrome.ButtonPrimary, 14);

            var tool = UiBuilder.Button(toolTray, "HealthHeroToolButton", "Thermometer", () =>
            {
                if (!symptomChecked)
                {
                    mistakes++;
                    hud.Prompt.text = "Check symptoms before choosing tools.";
                    return;
                }

                toolSelected = true;
                hud.Prompt.text = "Good tool choice. Now choose a kind care plan.";
                if (source == ResultSource.Multiplayer && networkState != null && networkState.IsSpawned)
                {
                    networkState.SubmitStep(1);
                }

                Refresh();
            });
            UiBuilder.Place(tool.GetComponent<RectTransform>(), -52f, 0f, 150f, 34f);
            ActivityRoomChrome.StyleButton(tool, ActivityRoomChrome.ButtonPrimary, 14);

            var wrongTool = UiBuilder.Button(toolTray, "HealthHeroWrongToolButton", "Bandage", () =>
            {
                mistakes++;
                hud.Prompt.text = "A bandage will not help this sore throat. Try the tool that measures temperature.";
            });
            UiBuilder.Place(wrongTool.GetComponent<RectTransform>(), 110f, 0f, 138f, 34f);
            ActivityRoomChrome.StyleButton(wrongTool, ActivityRoomChrome.ButtonPrimary, 14);

            var care = UiBuilder.Button(toolTray, "HealthHeroCareButton", "Warm Tea + Rest", () =>
            {
                if (!toolSelected)
                {
                    mistakes++;
                    hud.Prompt.text = "Pick the right tool before choosing the care plan.";
                    return;
                }

                careSelected = true;
                hud.Prompt.text = "Care plan ready. Complete the case to earn your badge.";
                if (source == ResultSource.Multiplayer && networkState != null && networkState.IsSpawned)
                {
                    networkState.SubmitStep(2);
                }

                Refresh();
            });
            UiBuilder.Place(care.GetComponent<RectTransform>(), 286f, 0f, 174f, 34f);
            ActivityRoomChrome.StyleButton(care, ActivityRoomChrome.ButtonPrimary, 14);

            var complete = UiBuilder.Button(panel, "HealthHeroCompleteButton", "Complete Case", () =>
            {
                if (!symptomChecked || !toolSelected || !careSelected)
                {
                    mistakes++;
                    hud.Prompt.text = "Finish all three clinic steps before completing the case.";
                    return;
                }

                if (source == ResultSource.Multiplayer && networkState != null && networkState.IsSpawned && !networkState.Complete)
                {
                    hud.Prompt.text = "Wait for both players to finish all clinic steps.";
                    return;
                }

                TryCompleteRoom(session, app, CreateResult(mistakes <= 1, source));
            });
            UiBuilder.Place(complete.GetComponent<RectTransform>(), 430f, -320f, 156f, 38f);
            ActivityRoomChrome.StyleButton(complete, ActivityRoomChrome.ButtonReady, 14);

            var campus = UiBuilder.Button(panel, "HealthHeroCampusButton", "Campus", () => ExitToCampus(app));
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 570f, -320f, 112f, 38f);
            ActivityRoomChrome.StyleButton(campus, ActivityRoomChrome.ButtonPrimary, 14);
        }
    }
}
