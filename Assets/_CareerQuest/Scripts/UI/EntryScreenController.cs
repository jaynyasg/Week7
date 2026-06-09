using UnityEngine;

namespace CareerQuest
{
    public class EntryScreenController : MonoBehaviour
    {
        public void Render(Transform parent, CareerQuestApp app)
        {
            var panel = UiBuilder.FullPanel(parent, "EntryPanel", new Color(0.9f, 0.98f, 1f));

            var title = UiBuilder.Text(panel, "Title", "Career Quest Campus", 48, TextAnchor.MiddleCenter, new Color(0.08f, 0.18f, 0.24f));
            UiBuilder.Place(title.rectTransform, 0f, 210f, 900f, 70f);

            var subtitle = UiBuilder.Text(panel, "Subtitle", "Build, explore, earn badges, and reveal future paths.", 24, TextAnchor.MiddleCenter, new Color(0.08f, 0.18f, 0.24f));
            UiBuilder.Place(subtitle.rectTransform, 0f, 150f, 900f, 50f);

            var play = UiBuilder.Button(panel, "PlayButton", "Play", app.BeginPlay);
            UiBuilder.Place(play.GetComponent<RectTransform>(), -150f, 45f, 240f, 72f);

            var showcase = UiBuilder.Button(panel, "ShowcaseButton", "Showcase", app.ShowShowcaseDisclaimer);
            UiBuilder.Place(showcase.GetComponent<RectTransform>(), 150f, 45f, 240f, 72f);

            var note = UiBuilder.Text(panel, "EntryNote", "Showcase gives a quick guided tour. Play lets you explore freely.", 20, TextAnchor.MiddleCenter, new Color(0.2f, 0.25f, 0.28f));
            UiBuilder.Place(note.rectTransform, 0f, -50f, 820f, 50f);
        }
    }
}
