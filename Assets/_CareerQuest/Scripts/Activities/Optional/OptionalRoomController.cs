using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    public class OptionalRoomController : ActivityRoomController
    {
        public MiniGameResult CreateResult(CatalogEntry entry, bool success, ResultSource source)
        {
            var traits = TraitsFor(entry.Id, success);
            return new MiniGameResult(
                entry.Id,
                entry.DisplayName,
                success ? CompletionTier.Degree : CompletionTier.Practice,
                source,
                traits,
                success ? 36f : 12f,
                success ? 0.9f : 0.62f,
                success
                    ? $"Completed the {entry.BuildingName} quest and earned your badge."
                    : $"Practiced the {entry.BuildingName} quest. Try again for a full badge.");
        }

        public void Render(Transform parent, GameSession session, CareerQuestApp app, ResultSource source, string activityId)
        {
            var entry = CareerQuestCatalog.GetById(activityId);
            var play = PlayConfigFor(entry.Id);
            BeginRoom(entry.Id);

            var panel = UiBuilder.FullPanel(parent, $"{play.PanelPrefix}Panel", play.PanelColor);
            var stepComplete = false;
            var readyToFinish = false;

            var hud = ActivityRoomChrome.MountQuestHud(
                panel,
                play.PanelPrefix,
                new Color(1f, 0.97f, 0.86f, 0.9f),
                play.AccentColor,
                entry.DisplayName,
                play.OpenPrompt,
                play.ProgressLabel);

            // U11 at-bar pass: the action tray reads as a paper HUD band (DESIGN
            // warm-paper UI foundation) and the step button carries the room's
            // career identity color. Interactions stay button-driven by design.
            // Sits ABOVE the instruction strip band (strip top edge is y -273;
            // anything at y -320 renders underneath its translucent paper wash).
            var tray = UiBuilder.Panel(panel, $"{play.PanelPrefix}ToolTray", new Color(1f, 0.97f, 0.86f, 0.9f));
            UiBuilder.Place(tray, -254f, -238f, 790f, 56f);
            UiBuilder.Shape(tray, $"{play.PanelPrefix}TrayStripe", play.AccentColor, -388f, 0f, 10f, 56f);

            var step = UiBuilder.Button(tray, $"{play.PanelPrefix}StepButton", play.StepLabel, () =>
            {
                if (!stepComplete)
                {
                    // Correct-action feedback per DESIGN motion rules: sparkle
                    // poof in the diorama plus the shared accept cue.
                    ParticlePoof.Burst(new Vector3(0f, -0.2f, 0f), play.AccentColor);
                    AudioCueCatalog.TryPlay(AudioCueIds.DropAccept);
                }

                stepComplete = true;
                hud.Prompt.text = play.StepPrompt;
                hud.Status.text = play.StepProgressLabel;
            });
            UiBuilder.Place(step.GetComponent<RectTransform>(), -120f, 0f, 220f, 34f);
            ActivityRoomChrome.StyleButton(step, play.AccentColor, 14);

            var complete = UiBuilder.Button(panel, $"{play.PanelPrefix}CompleteButton", "Complete Quest", () =>
            {
                if (!stepComplete)
                {
                    hud.Prompt.text = play.StepRequiredPrompt;
                    return;
                }

                readyToFinish = true;
                TryCompleteRoom(session, app, CreateResult(entry, readyToFinish, source));
            });
            UiBuilder.Place(complete.GetComponent<RectTransform>(), 430f, -238f, 156f, 38f);
            ActivityRoomChrome.StyleButton(complete, ActivityRoomChrome.ButtonReady, 14);

            var campus = UiBuilder.Button(panel, $"{play.PanelPrefix}CampusButton", "Campus", () => ExitToCampus(app));
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 570f, -238f, 112f, 38f);
            ActivityRoomChrome.StyleButton(campus, ActivityRoomChrome.ButtonPrimary, 14);
        }

        private static TraitDelta[] TraitsFor(string activityId, bool success)
        {
            return activityId switch
            {
                CareerQuestCatalog.AiLabId => new[]
                {
                    new TraitDelta("Reasoning", success ? 5 : 3),
                    new TraitDelta("Science", success ? 4 : 2),
                    new TraitDelta("Building", success ? 3 : 2)
                },
                CareerQuestCatalog.MusicStudioId => new[]
                {
                    new TraitDelta("Creativity", success ? 5 : 3),
                    new TraitDelta("Communication", success ? 4 : 2),
                    new TraitDelta("Focus", success ? 3 : 2)
                },
                CareerQuestCatalog.RoboticsGarageId => new[]
                {
                    new TraitDelta("Building", success ? 5 : 3),
                    new TraitDelta("Reasoning", success ? 4 : 2),
                    new TraitDelta("Collaboration", success ? 3 : 2)
                },
                CareerQuestCatalog.CommunityKitchenId => new[]
                {
                    new TraitDelta("Helping", success ? 5 : 3),
                    new TraitDelta("Collaboration", success ? 4 : 2),
                    new TraitDelta("Creativity", success ? 3 : 2)
                },
                _ => new[]
                {
                    new TraitDelta("Focus", success ? 3 : 2)
                }
            };
        }

        private static OptionalRoomPlayConfig PlayConfigFor(string activityId)
        {
            return activityId switch
            {
                CareerQuestCatalog.AiLabId => new OptionalRoomPlayConfig(
                    "AiLab",
                    new Color(0.9f, 0.96f, 1f, 0.04f),
                    new Color(0.28f, 0.66f, 0.94f, 0.95f),
                    "Train a model to solve a space puzzle.",
                    "Progress: model not trained",
                    "Train Model",
                    "Model trained! Launch the probe to finish.",
                    "Progress: model trained / probe ready",
                    "Finish the training step before completing the quest."),
                CareerQuestCatalog.MusicStudioId => new OptionalRoomPlayConfig(
                    "MusicStudio",
                    new Color(0.96f, 0.92f, 1f, 0.04f),
                    new Color(0.62f, 0.52f, 0.86f, 0.95f),
                    "Layer a beat, then record your chorus.",
                    "Progress: beat not recorded",
                    "Record Beat",
                    "Beat recorded! Mix the chorus to finish.",
                    "Progress: beat recorded / chorus ready",
                    "Record the beat before completing the quest."),
                CareerQuestCatalog.RoboticsGarageId => new OptionalRoomPlayConfig(
                    "RoboticsGarage",
                    new Color(0.92f, 0.98f, 0.96f, 0.04f),
                    new Color(0.13f, 0.55f, 0.58f, 0.95f),
                    "Build a helper robot, then power it on.",
                    "Progress: robot not built",
                    "Build Robot",
                    "Robot built! Power it on to finish.",
                    "Progress: robot built / power ready",
                    "Build the robot before completing the quest."),
                CareerQuestCatalog.CommunityKitchenId => new OptionalRoomPlayConfig(
                    "CommunityKitchen",
                    new Color(1f, 0.96f, 0.9f, 0.04f),
                    new Color(0.55f, 0.82f, 0.5f, 0.95f),
                    "Prep fresh ingredients, then serve the meal.",
                    "Progress: ingredients not prepped",
                    "Prep Ingredients",
                    "Ingredients ready! Serve the meal to finish.",
                    "Progress: ingredients prepped / meal ready",
                    "Prep the ingredients before completing the quest."),
                _ => new OptionalRoomPlayConfig(
                    "OptionalRoom",
                    new Color(0.95f, 0.95f, 0.95f, 0.04f),
                    ActivityRoomChrome.ButtonPrimary,
                    "Complete the quest steps.",
                    "Progress: step pending",
                    "Start Step",
                    "Step complete! Finish the quest.",
                    "Progress: step complete",
                    "Finish the quest step before completing.")
            };
        }

        private readonly struct OptionalRoomPlayConfig
        {
            public OptionalRoomPlayConfig(
                string panelPrefix,
                Color panelColor,
                Color accentColor,
                string openPrompt,
                string progressLabel,
                string stepLabel,
                string stepPrompt,
                string stepProgressLabel,
                string stepRequiredPrompt)
            {
                PanelPrefix = panelPrefix;
                PanelColor = panelColor;
                AccentColor = accentColor;
                OpenPrompt = openPrompt;
                ProgressLabel = progressLabel;
                StepLabel = stepLabel;
                StepPrompt = stepPrompt;
                StepProgressLabel = stepProgressLabel;
                StepRequiredPrompt = stepRequiredPrompt;
            }

            public string PanelPrefix { get; }
            public Color PanelColor { get; }
            public Color AccentColor { get; }
            public string OpenPrompt { get; }
            public string ProgressLabel { get; }
            public string StepLabel { get; }
            public string StepPrompt { get; }
            public string StepProgressLabel { get; }
            public string StepRequiredPrompt { get; }
        }
    }
}
