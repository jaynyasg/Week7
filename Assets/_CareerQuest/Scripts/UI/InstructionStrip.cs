using TMPro;
using UnityEngine;

namespace CareerQuest
{
    public static class InstructionStrip
    {
        public const string PanelName = "InstructionStrip";
        public const string LabelName = "InstructionStripLabel";

        private static readonly Color Paper = new(1f, 0.969f, 0.878f, 0.94f);
        private static readonly Color Ink = new(0.098f, 0.196f, 0.235f);
        private static readonly Color TealAccent = new(0.055f, 0.42f, 0.435f, 0.35f);

        public static bool ShouldShowForMode(AppMode mode)
        {
            return mode == AppMode.Play || mode == AppMode.SoloFallback;
        }

        public static string ResolveMessage(GameSession session)
        {
            if (session == null)
            {
                return string.Empty;
            }

            if (session.CurrentPhase == SessionPhase.Ceremony)
            {
                return string.Empty;
            }

            switch (session.CurrentRoute)
            {
                case ActivityRoute.DesignBuild:
                    return "Drag each city piece onto its matching lot to finish your blueprint.";
                case ActivityRoute.HealthHero:
                    return "Read the symptom, choose the right tool, then pick the care plan.";
                case ActivityRoute.LogicCourt:
                    return "Match each clue to the fair rule, then decide the case.";
                case ActivityRoute.AiLab:
                    return "Train the model, then launch the probe to finish the lab quest.";
                case ActivityRoute.MusicStudio:
                    return "Record a beat, then mix your chorus to finish the studio quest.";
                case ActivityRoute.RoboticsGarage:
                    return "Build the robot, then power it on to finish the garage quest.";
                case ActivityRoute.CommunityKitchen:
                    return "Prep the ingredients, then serve the meal to finish the kitchen quest.";
                case ActivityRoute.Gallery:
                    return "Look at your badges. Tap a room when you are ready for another quest.";
                case ActivityRoute.Campus:
                default:
                    if (session.CurrentPhase == SessionPhase.Hub)
                    {
                        return "Walk to a career door and press Enter to start a quest.";
                    }

                    return "Follow the quest steps to earn your badge.";
            }
        }

        public static TextMeshProUGUI Build(Transform parent, GameSession session)
        {
            var panel = UiBuilder.InstructionStripPanel(parent, PanelName, Paper, TealAccent);
            UiBuilder.Place(panel, 0f, -305f, 1120f, 64f);

            var label = UiBuilder.Text(panel, LabelName, ResolveMessage(session), 20, TextAnchor.MiddleCenter, Ink, TypeRole.Body, TypeWeight.Medium);
            UiBuilder.Place(label.rectTransform, 0f, 0f, 1040f, 48f);

            // Long kid-facing strings must wrap or shrink instead of overflowing the strip.
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14;
            label.fontSizeMax = 20;
            return label;
        }

        public static void Refresh(TextMeshProUGUI label, GameSession session)
        {
            if (label == null)
            {
                return;
            }

            label.text = ResolveMessage(session);
        }
    }
}
