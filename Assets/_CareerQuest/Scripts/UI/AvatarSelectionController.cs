using UnityEngine;

namespace CareerQuest
{
    public class AvatarSelectionController : MonoBehaviour
    {
        public void Render(Transform parent, CareerQuestApp app)
        {
            var panel = UiBuilder.FullPanel(parent, "AvatarSelectionPanel", new Color(0.9f, 0.97f, 1f));

            var title = UiBuilder.Text(panel, "AvatarSelectionTitle", "Choose Your Avatar", 42, TextAnchor.MiddleCenter, new Color(0.06f, 0.14f, 0.18f));
            UiBuilder.Place(title.rectTransform, 0f, 236f, 900f, 64f);

            var subtitle = UiBuilder.Text(panel, "AvatarSelectionSubtitle", "Pick the character you want to bring into Career Quest Campus.", 22, TextAnchor.MiddleCenter, new Color(0.08f, 0.16f, 0.2f));
            UiBuilder.Place(subtitle.rectTransform, 0f, 184f, 940f, 44f);

            var x = -405f;
            foreach (var avatar in AvatarConfig.Avatars)
            {
                RenderAvatarCard(panel, avatar, x, app);
                x += 270f;
            }

            var back = UiBuilder.Button(panel, "AvatarBackButton", "Back", app.ShowEntry);
            UiBuilder.Place(back.GetComponent<RectTransform>(), 0f, -250f, 200f, 54f);
        }

        private static void RenderAvatarCard(RectTransform parent, AvatarDefinition avatar, float x, CareerQuestApp app)
        {
            var card = UiBuilder.Panel(parent, $"{avatar.Id}Card", new Color(1f, 0.98f, 0.88f, 0.76f));
            UiBuilder.Place(card, x, -28f, 230f, 330f);

            UiBuilder.Circle(card, $"{avatar.Id}Shadow", new Color(0.05f, 0.08f, 0.1f, 0.16f), 0f, -64f, 112f, 30f);
            UiBuilder.Shape(card, $"{avatar.Id}LegA", new Color(0.18f, 0.16f, 0.13f), -18f, -40f, 22f, 70f);
            UiBuilder.Shape(card, $"{avatar.Id}LegB", new Color(0.18f, 0.16f, 0.13f), 18f, -40f, 22f, 70f);
            UiBuilder.Shape(card, $"{avatar.Id}Body", avatar.ShirtColor, 0f, 30f, 82f, 94f);
            UiBuilder.Shape(card, $"{avatar.Id}Pack", new Color(0.55f, 0.12f, 0.12f), 58f, 32f, 26f, 66f);
            UiBuilder.Circle(card, $"{avatar.Id}Head", new Color(0.78f, 0.52f, 0.34f), 0f, 116f, 72f, 72f);
            UiBuilder.Circle(card, $"{avatar.Id}Hair", new Color(0.12f, 0.08f, 0.06f), -6f, 138f, 66f, 32f);

            var name = UiBuilder.Text(card, $"{avatar.Id}Name", avatar.DisplayName, 22, TextAnchor.MiddleCenter, new Color(0.06f, 0.12f, 0.14f));
            UiBuilder.Place(name.rectTransform, 0f, -118f, 200f, 34f);

            var role = UiBuilder.Text(card, $"{avatar.Id}Role", avatar.Role, 16, TextAnchor.MiddleCenter, new Color(0.12f, 0.18f, 0.2f));
            UiBuilder.Place(role.rectTransform, 0f, -150f, 200f, 32f);

            var choose = UiBuilder.Button(card, $"{avatar.Id}ChooseButton", "Choose", () => app.ChooseAvatar(avatar.Id));
            UiBuilder.Place(choose.GetComponent<RectTransform>(), 0f, -205f, 148f, 48f);
        }
    }
}
