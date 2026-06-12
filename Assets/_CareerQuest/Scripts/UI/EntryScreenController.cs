using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// P8 title moment: the entry screen is a Fredoka wordmark over the live
    /// campus diorama (CampusWorldController.ShowEntry mounts the authored hub
    /// prefab with ambient motion and parallax behind this menu). The panel is
    /// clear so the world carries the screen; the menu sits on one paper card.
    /// Play → avatar select routing is unchanged.
    /// </summary>
    public class EntryScreenController : MonoBehaviour
    {
        private static readonly Color Ink = new(0.098f, 0.196f, 0.235f);       // DESIGN.md Ink
        private static readonly Color Paper = new(1f, 0.968f, 0.878f, 0.92f);  // DESIGN.md Paper
        private static readonly Color PaperShadow = new(0.851f, 0.714f, 0.435f, 0.85f);

        public void Render(Transform parent, CareerQuestApp app)
        {
            // Clear full panel: the live campus diorama is the entry background.
            var panel = UiBuilder.FullPanel(parent, "EntryPanel", Color.clear);

            // Single paper card carries the wordmark and actions (no nested cards).
            var cardShadow = UiBuilder.Shape(panel, "EntryCardShadow", PaperShadow, 6f, 84f, 1000f, 332f);
            cardShadow.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            var card = UiBuilder.Shape(panel, "EntryCard", Paper, 0f, 92f, 1000f, 332f);
            card.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;

            var title = UiBuilder.Text(panel, "Title", "Career Quest Campus", TypeStyles.HeroTitle, TextAnchor.MiddleCenter, Ink, TypeRole.Display, TypeWeight.Bold);
            UiBuilder.Place(title.rectTransform, 0f, 192f, 940f, 70f);

            var subtitle = UiBuilder.Text(panel, "Subtitle", "Build, explore, earn badges, and reveal future paths.", TypeStyles.RoomPrompt, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(subtitle.rectTransform, 0f, 134f, 940f, 50f);

            var play = UiBuilder.Button(panel, "PlayButton", "Play", app.ShowAvatarSelectionForPlay);
            UiBuilder.Place(play.GetComponent<RectTransform>(), -260f, 45f, 220f, 72f);

            var showcase = UiBuilder.Button(panel, "ShowcaseButton", "Showcase", app.ShowShowcaseDisclaimer);
            UiBuilder.Place(showcase.GetComponent<RectTransform>(), 0f, 45f, 220f, 72f);

            var multiplayer = UiBuilder.Button(panel, "MultiplayerTestingButton", "Multiplayer", app.ShowConnection);
            UiBuilder.Place(multiplayer.GetComponent<RectTransform>(), 260f, 45f, 220f, 72f);

            var note = UiBuilder.Text(panel, "EntryNote", "Play starts in the campus. Multiplayer is for local testing.", TypeStyles.Body, TextAnchor.MiddleCenter, new Color(0.2f, 0.25f, 0.28f));
            UiBuilder.Place(note.rectTransform, 0f, -28f, 880f, 46f);
        }
    }
}
