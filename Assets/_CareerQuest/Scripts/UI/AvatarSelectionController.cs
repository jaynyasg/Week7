using UnityEngine;

namespace CareerQuest
{
    public class AvatarSelectionController : MonoBehaviour
    {
        public void Render(Transform parent, CareerQuestApp app)
        {
            var panel = UiBuilder.FullPanel(parent, "AvatarSelectionPanel", new Color(0.9f, 0.97f, 1f));
            var selectedAvatar = app.Session.SelectedAvatar;

            var title = UiBuilder.Text(panel, "AvatarSelectionTitle", "Choose Your Avatar", 42, TextAnchor.MiddleCenter, new Color(0.06f, 0.14f, 0.18f));
            UiBuilder.Place(title.rectTransform, 0f, 266f, 900f, 60f);

            var subtitle = UiBuilder.Text(panel, "AvatarSelectionSubtitle", "Pick the character you want to bring into Career Quest Campus.", 20, TextAnchor.MiddleCenter, new Color(0.08f, 0.16f, 0.2f));
            UiBuilder.Place(subtitle.rectTransform, 0f, 222f, 940f, 38f);

            var previewPanel = UiBuilder.Panel(panel, "SelectedAvatarPanel", new Color(1f, 0.98f, 0.88f, 0.78f));
            UiBuilder.Place(previewPanel, 0f, 64f, 330f, 300f);

            var previewImage = AvatarPreviewController.CreatePreview(previewPanel, "SelectedAvatarPreview", selectedAvatar, new Vector2(160f, 205f));
            UiBuilder.Place(previewImage.rectTransform, 0f, 50f, 170f, 210f);

            var previewName = UiBuilder.Text(previewPanel, "SelectedAvatarName", selectedAvatar.DisplayName, 24, TextAnchor.MiddleCenter, new Color(0.06f, 0.12f, 0.14f));
            UiBuilder.Place(previewName.rectTransform, 0f, -78f, 290f, 34f);

            var previewRole = UiBuilder.Text(previewPanel, "SelectedAvatarRole", selectedAvatar.Role, 17, TextAnchor.MiddleCenter, new Color(0.12f, 0.18f, 0.2f));
            UiBuilder.Place(previewRole.rectTransform, 0f, -110f, 290f, 28f);

            var previewPersonality = UiBuilder.Text(previewPanel, "SelectedAvatarPersonality", selectedAvatar.PersonalityLabel, 14, TextAnchor.MiddleCenter, new Color(0.12f, 0.18f, 0.2f));
            UiBuilder.Place(previewPersonality.rectTransform, 0f, -144f, 290f, 42f);

            void SelectAvatar(AvatarDefinition avatar)
            {
                selectedAvatar = avatar;
                app.Session.SelectAvatar(avatar.Id);
                AvatarPreviewController.Apply(previewImage, avatar);
                previewName.text = avatar.DisplayName;
                previewRole.text = avatar.Role;
                previewPersonality.text = avatar.PersonalityLabel;
            }

            var x = -405f;
            foreach (var avatar in AvatarConfig.Avatars)
            {
                RenderAvatarCard(panel, avatar, x, -174f, () => SelectAvatar(avatar));
                x += 270f;
            }

            var confirm = UiBuilder.Button(panel, "AvatarConfirmButton", "Start", () => app.ChooseAvatar(selectedAvatar.Id));
            UiBuilder.Place(confirm.GetComponent<RectTransform>(), 118f, -286f, 200f, 54f);

            var back = UiBuilder.Button(panel, "AvatarBackButton", "Back", app.ShowEntry);
            UiBuilder.Place(back.GetComponent<RectTransform>(), -118f, -286f, 200f, 54f);
        }

        private static void RenderAvatarCard(RectTransform parent, AvatarDefinition avatar, float x, float y, UnityEngine.Events.UnityAction onPreview)
        {
            var card = UiBuilder.Panel(parent, $"{avatar.Id}Card", new Color(1f, 0.98f, 0.88f, 0.7f));
            UiBuilder.Place(card, x, y, 230f, 220f);

            var preview = AvatarPreviewController.CreatePreview(card, $"{avatar.Id}Preview", avatar, new Vector2(96f, 128f));
            UiBuilder.Place(preview.rectTransform, 0f, 42f, 104f, 132f);

            var name = UiBuilder.Text(card, $"{avatar.Id}Name", avatar.DisplayName, 22, TextAnchor.MiddleCenter, new Color(0.06f, 0.12f, 0.14f));
            UiBuilder.Place(name.rectTransform, 0f, -42f, 200f, 30f);

            var role = UiBuilder.Text(card, $"{avatar.Id}Role", avatar.Role, 16, TextAnchor.MiddleCenter, new Color(0.12f, 0.18f, 0.2f));
            UiBuilder.Place(role.rectTransform, 0f, -72f, 200f, 28f);

            var choose = UiBuilder.Button(card, $"{avatar.Id}PreviewButton", "Preview", () => onPreview.Invoke());
            UiBuilder.Place(choose.GetComponent<RectTransform>(), 0f, -116f, 142f, 42f);
        }
    }
}
