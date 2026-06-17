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

        public static string ResolveMessage(GameSession session, string stationId = null)
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
                    return "Drag the symptom clipboard, the right tool, then the care plan to the patient.";
                case ActivityRoute.LogicCourt:
                    return "Drag the case file to the podium, then sort each evidence card.";
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
                case ActivityRoute.PartyStation:
                    // U2 generic station branch: one copy seam keyed by station
                    // id — never one switch case per station (KTD3). Design-review
                    // (2026-06-15): name the station's actual verb so each station
                    // teaches a distinct action (trace / launch / cross out) instead
                    // of a generic "play here" — stays station-aware and short.
                    if (CareerQuestCatalog.IsPartyStationId(stationId) && CareerQuestCatalog.TryGetById(stationId, out var stationEntry))
                    {
                        return PartyStationDefinitions.TryGetById(stationId, out var stationDef)
                            ? $"{VerbCue(stationDef.Pattern)} in the {stationEntry.BuildingName}!"
                            : $"Play in the {stationEntry.BuildingName} to finish your quest.";
                    }

                    return "Follow the quest steps to earn your badge.";
                case ActivityRoute.Campus:
                default:
                    if (session.CurrentPhase == SessionPhase.Hub)
                    {
                        // U2 walk-into-door entry: no key press is required.
                        return "Walk into a career door to start a quest. It opens on its own!";
                    }

                    return "Follow the quest steps to earn your badge.";
            }
        }

        /// <summary>
        /// Design-review (2026-06-15): a short, kid-facing cue for each station's
        /// verb so the instruction strip teaches the distinct action rather than a
        /// generic "play here". Combined with " in the {building}!" it stays well
        /// under PartyStationValidator.MaxGuideLineLength (80) and copy-safe.
        /// </summary>
        private static string VerbCue(ToyPatternId pattern)
        {
            return pattern switch
            {
                ToyPatternId.TracePath => "Trace the route",
                ToyPatternId.ShootTarget => "Pull back and launch",
                ToyPatternId.DeduceAnswer => "Cross out the wrong ones",
                ToyPatternId.BalanceMeters => "Tune both meters",
                ToyPatternId.PickMatchingTrio => "Match the trio",
                ToyPatternId.ComposeSet => "Build your set",
                ToyPatternId.MatchAndCare => "Match the care clues",
                ToyPatternId.SortToBin => "Sort each piece",
                ToyPatternId.SequenceCards => "Put the steps in order",
                ToyPatternId.DragToSlot => "Drag each piece into place",
                ToyPatternId.RhythmTap => "Tap on the beat",
                ToyPatternId.PourToLine => "Fill to the line",
                ToyPatternId.WireUp => "Connect the pairs",
                ToyPatternId.ScanReveal => "Scan, then tap what you find",
                _ => "Play"
            };
        }

        public static TextMeshProUGUI Build(Transform parent, GameSession session, string stationId = null)
        {
            var panel = UiBuilder.InstructionStripPanel(parent, PanelName, Paper, TealAccent);
            UiBuilder.Place(panel, 0f, -305f, 1120f, 64f);

            var label = UiBuilder.Text(panel, LabelName, ResolveMessage(session, stationId), 20, TextAnchor.MiddleCenter, Ink, TypeRole.Body, TypeWeight.Medium);
            UiBuilder.Place(label.rectTransform, 0f, 0f, 1040f, 48f);

            // Long kid-facing strings must wrap or shrink instead of overflowing the strip.
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14;
            label.fontSizeMax = 20;
            return label;
        }

        public static void Refresh(TextMeshProUGUI label, GameSession session, string stationId = null)
        {
            if (label == null)
            {
                return;
            }

            label.text = ResolveMessage(session, stationId);
        }
    }
}
