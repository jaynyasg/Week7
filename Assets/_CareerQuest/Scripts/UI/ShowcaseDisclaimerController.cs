using UnityEngine;

namespace CareerQuest
{
    public class ShowcaseDisclaimerController : MonoBehaviour
    {
        public const string DisclaimerText = "We'll give you a quick tour of the campus, badges, and reveal. Choose Play to explore on your own.";

        public void Render(Transform parent, CareerQuestApp app)
        {
            var panel = UiBuilder.FullPanel(parent, "ShowcaseDisclaimerPanel", new Color(0.95f, 0.95f, 0.88f));

            var title = UiBuilder.Text(panel, "DisclaimerTitle", "Guided Showcase", 42, TextAnchor.MiddleCenter, new Color(0.1f, 0.15f, 0.18f), TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(title.rectTransform, 0f, 155f, 820f, 70f);

            var body = UiBuilder.Text(panel, "DisclaimerBody", DisclaimerText, 26, TextAnchor.MiddleCenter, new Color(0.12f, 0.16f, 0.2f));
            UiBuilder.Place(body.rectTransform, 0f, 45f, 820f, 120f);

            var start = UiBuilder.Button(panel, "StartShowcaseButton", "Start Tour", app.ShowAvatarSelectionForShowcase);
            UiBuilder.Place(start.GetComponent<RectTransform>(), -140f, -100f, 230f, 66f);

            var back = UiBuilder.Button(panel, "BackToEntryButton", "Back", app.ShowEntry);
            UiBuilder.Place(back.GetComponent<RectTransform>(), 140f, -100f, 230f, 66f);
        }
    }
}
