using TMPro;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// U4 station guide voice (design doc Station guide rule): one tiny guide
    /// identity per station, rendered first-pass as a paper chip — portrait
    /// disc, guide name, voice tag, and ONE live line that moves through
    /// intro → hint → escalation → success. Every line comes from the station
    /// seed data (PartyStationValidator-checked); this view never authors copy.
    /// Plain UI composition over UiBuilder — no coroutines, no self-ticking —
    /// so route teardown is just the canvas clear.
    /// </summary>
    public sealed class StationGuideView
    {
        public const string PanelName = "StationGuidePanel";
        public const string NameTextName = "StationGuideName";
        public const string VoiceTextName = "StationGuideVoice";
        public const string LineTextName = "StationGuideLine";
        public const string ReactionTextName = "StationGuideReaction";

        private static readonly Color PaperColor = new(1f, 0.97f, 0.86f, 0.92f);
        private static readonly Color InkColor = new(0.098f, 0.196f, 0.235f);
        private static readonly Color SoftInkColor = new(0.27f, 0.36f, 0.4f);

        private readonly PartyStationSeedDefinition _seed;
        private readonly TextMeshProUGUI _line;
        private readonly TextMeshProUGUI _reaction;

        private StationGuideView(PartyStationSeedDefinition seed, TextMeshProUGUI line, TextMeshProUGUI reaction)
        {
            _seed = seed;
            _line = line;
            _reaction = reaction;
        }

        /// <summary>The guide UI survives until the route clears the canvas.</summary>
        public bool IsAlive => _line != null;

        /// <summary>Test/QA seam: the line currently spoken by the guide.</summary>
        public string CurrentLine => _line != null ? _line.text : null;

        public static StationGuideView Mount(
            Transform parent,
            PartyStationDefinition definition,
            PartyStationSeedDefinition seed,
            Color accent)
        {
            if (parent == null || definition == null || seed == null)
            {
                return null;
            }

            // Compact bottom-left card: portrait/name/voice on top, the live
            // line + reaction stacked left-aligned beneath. The old wide single
            // row pushed the line/reaction text into screen-center, where it
            // overlapped the world-space tray-object labels; keeping the whole
            // card narrow and left of the centered tray row removes that overlap.
            var panel = UiBuilder.Panel(parent, PanelName, PaperColor);
            UiBuilder.Place(panel, -462f, -202f, 332f, 124f);
            UiBuilder.Shape(panel, "StationGuideStripe", accent, -161f, 0f, 10f, 124f);
            UiBuilder.Circle(panel, "StationGuidePortrait", accent, -128f, 38f, 38f, 38f);

            var name = UiBuilder.Text(panel, NameTextName, definition.GuideName, 15, TextAnchor.MiddleLeft, InkColor, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(name.rectTransform, 14f, 46f, 210f, 22f);

            var voice = UiBuilder.Text(panel, VoiceTextName, definition.GuideVoice, 11, TextAnchor.MiddleLeft, SoftInkColor);
            UiBuilder.Place(voice.rectTransform, 14f, 26f, 210f, 16f);

            var line = UiBuilder.Text(panel, LineTextName, seed.IntroLine, 13, TextAnchor.UpperLeft, InkColor);
            UiBuilder.Place(line.rectTransform, 0f, -10f, 300f, 46f);
            line.textWrappingMode = TextWrappingModes.Normal;
            line.enableAutoSizing = true;
            line.fontSizeMin = 10;
            line.fontSizeMax = 13;

            var reaction = UiBuilder.Text(panel, ReactionTextName, seed.NpcReaction, 11, TextAnchor.UpperLeft, SoftInkColor);
            UiBuilder.Place(reaction.rectTransform, 0f, -46f, 300f, 26f);
            reaction.textWrappingMode = TextWrappingModes.Normal;
            reaction.gameObject.SetActive(false);

            return new StationGuideView(seed, line, reaction);
        }

        /// <summary>Intro beat (and hint-ladder recovery): the seed's premise line.</summary>
        public void ShowIntro()
        {
            SetLine(_seed.IntroLine);
            SetReactionVisible(false);
        }

        /// <summary>Hint ladder presentation — the caller passes seed hint/escalation copy.</summary>
        public void ShowHint(string hintLine)
        {
            if (!string.IsNullOrWhiteSpace(hintLine))
            {
                SetLine(hintLine);
            }
        }

        /// <summary>Completion beat: success line plus the NPC/room reaction.</summary>
        public void ShowSuccess()
        {
            SetLine(_seed.SuccessLine);
            SetReactionVisible(true);
        }

        private void SetLine(string value)
        {
            if (_line != null)
            {
                _line.text = value;
            }
        }

        private void SetReactionVisible(bool visible)
        {
            if (_reaction != null)
            {
                _reaction.gameObject.SetActive(visible);
            }
        }
    }
}
