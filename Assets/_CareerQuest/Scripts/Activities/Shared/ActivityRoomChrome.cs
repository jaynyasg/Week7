using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    public static class ActivityRoomChrome
    {
        public static readonly Color InkDefault = new(0.15f, 0.18f, 0.24f);
        public static readonly Color Paper = new(0.96f, 0.97f, 0.99f);
        public static readonly Color ButtonPrimary = new(0.22f, 0.56f, 0.92f);
        public static readonly Color ButtonReady = new(0.34f, 0.76f, 0.45f);

        public static readonly Color DesignInk = new(0.12f, 0.15f, 0.2f);
        public static readonly Color DesignPaper = new(0.97f, 0.98f, 1f);
        public static readonly Color DesignTeal = new(0.18f, 0.62f, 0.58f);

        public readonly struct QuestHudRefs
        {
            public QuestHudRefs(RectTransform panel, Text title, Text prompt, Text status)
            {
                Panel = panel;
                Title = title;
                Prompt = prompt;
                Status = status;
            }

            public RectTransform Panel { get; }
            public Text Title { get; }
            public Text Prompt { get; }
            public Text Status { get; }
        }

        public static void StyleButton(Button button, Color color, int fontSize)
        {
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = Color.white;
                label.fontSize = fontSize;
                label.fontStyle = FontStyle.Bold;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 10;
                label.resizeTextMaxSize = fontSize;
            }
        }

        public static QuestHudRefs MountQuestHud(
            Transform parent,
            string prefix,
            Color paper,
            Color stripe,
            string title,
            string prompt,
            string status,
            float stripeX = -318f)
        {
            var questHud = UiBuilder.Panel(parent, $"{prefix}QuestHud", paper);
            UiBuilder.Place(questHud, -286f, 282f, 664f, 96f);
            UiBuilder.Shape(questHud, $"{prefix}HudStripe", stripe, stripeX, 0f, 14f, 96f);

            var titleText = UiBuilder.Text(questHud, $"{prefix}Title", title, 22, TextAnchor.MiddleLeft, InkDefault);
            UiBuilder.Place(titleText.rectTransform, 4f, 27f, 560f, 26f);

            var promptText = UiBuilder.Text(questHud, $"{prefix}Prompt", prompt, 15, TextAnchor.MiddleLeft, InkDefault);
            UiBuilder.Place(promptText.rectTransform, 4f, 0f, 560f, 24f);

            var statusText = UiBuilder.Text(questHud, $"{prefix}Status", status, 13, TextAnchor.MiddleLeft, InkDefault);
            UiBuilder.Place(statusText.rectTransform, 4f, -27f, 560f, 22f);

            return new QuestHudRefs(questHud, titleText, promptText, statusText);
        }
    }
}
