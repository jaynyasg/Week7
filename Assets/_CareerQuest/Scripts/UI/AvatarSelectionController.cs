using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    public class AvatarSelectionController : MonoBehaviour
    {
        private static readonly Color Ink = new(0.1f, 0.2f, 0.24f);
        private static readonly Color Paper = new(1f, 0.97f, 0.88f, 0.94f);
        private static readonly Color PaperSoft = new(1f, 0.98f, 0.9f, 0.82f);
        private static readonly Color ButtonTeal = new(0.09f, 0.31f, 0.42f);

        public void Render(Transform parent, CareerQuestApp app)
        {
            var panel = UiBuilder.FullPanel(parent, "AvatarSelectionPanel", new Color(0.62f, 0.88f, 1f));
            var selectedAvatar = app.Session.SelectedAvatar ?? AvatarConfig.DefaultAvatar;

            UiBuilder.Shape(panel, "AvatarSelectionSkyBand", new Color(0.62f, 0.88f, 1f, 0.42f), 0f, 150f, 1280f, 390f);
            UiBuilder.Shape(panel, "AvatarSelectionPathBand", new Color(0.95f, 0.77f, 0.36f, 0.36f), 0f, -266f, 1280f, 152f);

            var title = UiBuilder.Text(panel, "AvatarSelectionTitle", "Choose Your Quest Hero", 40, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(title.rectTransform, 0f, 282f, 900f, 54f);

            var subtitle = UiBuilder.Text(panel, "AvatarSelectionSubtitle", "Pick the character you want to bring into Career Quest Campus.", 18, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(subtitle.rectTransform, 0f, 242f, 940f, 34f);

            var previewPanel = UiBuilder.Panel(panel, "SelectedAvatarPanel", Paper);
            UiBuilder.Place(previewPanel, 0f, 66f, 372f, 318f);

            var passportBand = UiBuilder.Shape(previewPanel, "SelectedAvatarPassportBand", selectedAvatar.AccentColor, 0f, 132f, 372f, 56f);
            var passportTitle = UiBuilder.Text(previewPanel, "SelectedAvatarPassportTitle", "Quest Passport", 18, TextAnchor.MiddleCenter, Color.white);
            UiBuilder.Place(passportTitle.rectTransform, 0f, 136f, 320f, 30f);

            UiBuilder.Shape(previewPanel, "SelectedAvatarPlatformShadow", new Color(0.54f, 0.39f, 0.18f, 0.28f), 0f, -52f, 232f, 24f);
            var platformTop = UiBuilder.Shape(previewPanel, "SelectedAvatarPlatform", selectedAvatar.AccentColor, 0f, -42f, 214f, 24f);

            var previewImage = AvatarPreviewController.CreatePreview(previewPanel, "SelectedAvatarPreview", selectedAvatar, new Vector2(206f, 232f));
            UiBuilder.Place(previewImage.rectTransform, 0f, 34f, 206f, 232f);

            var previewName = UiBuilder.Text(previewPanel, "SelectedAvatarName", selectedAvatar.DisplayName, 25, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(previewName.rectTransform, 0f, -78f, 318f, 32f);

            var previewRole = UiBuilder.Text(previewPanel, "SelectedAvatarRole", selectedAvatar.Role, 16, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(previewRole.rectTransform, 0f, -106f, 318f, 24f);

            var previewPersonality = UiBuilder.Text(previewPanel, "SelectedAvatarPersonality", selectedAvatar.PersonalityLabel, 13, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(previewPersonality.rectTransform, 0f, -136f, 318f, 38f);

            var cardViews = new List<AvatarCardView>();

            void SelectAvatar(AvatarDefinition avatar)
            {
                selectedAvatar = avatar ?? AvatarConfig.DefaultAvatar;
                app.Session.SelectAvatar(selectedAvatar.Id);
                AvatarPreviewController.Apply(previewImage, selectedAvatar);
                previewName.text = selectedAvatar.DisplayName;
                previewRole.text = selectedAvatar.Role;
                previewPersonality.text = selectedAvatar.PersonalityLabel;
                passportBand.GetComponent<Image>().color = selectedAvatar.AccentColor;
                platformTop.GetComponent<Image>().color = selectedAvatar.AccentColor;
                RefreshCardStates(cardViews, selectedAvatar);
            }

            var x = -390f;
            foreach (var avatar in AvatarConfig.Avatars)
            {
                cardViews.Add(RenderAvatarCard(panel, avatar, x, -178f, () => SelectAvatar(avatar)));
                x += 260f;
            }

            RefreshCardStates(cardViews, selectedAvatar);

            var confirm = UiBuilder.Button(panel, "AvatarConfirmButton", "Enter Campus", () => app.ChooseAvatar(selectedAvatar.Id));
            UiBuilder.Place(confirm.GetComponent<RectTransform>(), 132f, -318f, 236f, 52f);
            StyleButton(confirm, new Color(0.05f, 0.49f, 0.43f), 22);

            var back = UiBuilder.Button(panel, "AvatarBackButton", "Back", app.ShowEntry);
            UiBuilder.Place(back.GetComponent<RectTransform>(), -132f, -318f, 200f, 52f);
            StyleButton(back, ButtonTeal, 22);
        }

        private static AvatarCardView RenderAvatarCard(RectTransform parent, AvatarDefinition avatar, float x, float y, UnityEngine.Events.UnityAction onSelect)
        {
            var card = UiBuilder.Panel(parent, $"{avatar.Id}Card", PaperSoft);
            UiBuilder.Place(card, x, y, 220f, 210f);

            var accentBand = UiBuilder.Shape(card, $"{avatar.Id}AccentBand", avatar.AccentColor, 0f, 96f, 220f, 12f);
            var accentImage = accentBand.GetComponent<Image>();
            accentImage.raycastTarget = false;

            var preview = AvatarPreviewController.CreatePreview(card, $"{avatar.Id}Preview", avatar, new Vector2(94f, 112f));
            UiBuilder.Place(preview.rectTransform, 0f, 36f, 94f, 112f);
            preview.raycastTarget = false;

            var name = UiBuilder.Text(card, $"{avatar.Id}Name", avatar.DisplayName, 19, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(name.rectTransform, 0f, -28f, 192f, 28f);
            name.raycastTarget = false;

            var role = UiBuilder.Text(card, $"{avatar.Id}Role", avatar.Role, 14, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(role.rectTransform, 0f, -54f, 194f, 24f);
            role.raycastTarget = false;

            var selectedBadge = UiBuilder.Shape(card, $"{avatar.Id}SelectedBadge", new Color(1f, 1f, 1f, 0f), 0f, -70f, 144f, 20f);
            var selectedBadgeImage = selectedBadge.GetComponent<Image>();
            selectedBadgeImage.raycastTarget = false;

            var selectedText = UiBuilder.Text(card, $"{avatar.Id}SelectedState", string.Empty, 13, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(selectedText.rectTransform, 0f, -70f, 144f, 18f);
            selectedText.raycastTarget = false;

            var choose = UiBuilder.Button(card, $"{avatar.Id}ChooseButton", "Choose", () => onSelect.Invoke());
            UiBuilder.Place(choose.GetComponent<RectTransform>(), 0f, -92f, 142f, 30f);
            StyleButton(choose, ButtonTeal, 16);

            return new AvatarCardView(avatar, card.GetComponent<Image>(), accentImage, selectedBadgeImage, selectedText, choose);
        }

        private static void RefreshCardStates(IEnumerable<AvatarCardView> cards, AvatarDefinition selectedAvatar)
        {
            foreach (var card in cards)
            {
                var selected = card.Avatar.Id == selectedAvatar.Id;
                card.CardImage.color = selected ? new Color(1f, 0.96f, 0.78f, 0.96f) : PaperSoft;
                card.AccentImage.color = selected ? card.Avatar.ShirtColor : card.Avatar.AccentColor;
                card.SelectedBadge.color = selected ? new Color(card.Avatar.AccentColor.r, card.Avatar.AccentColor.g, card.Avatar.AccentColor.b, 0.95f) : new Color(1f, 1f, 1f, 0f);
                card.SelectedText.text = selected ? "Selected" : string.Empty;
                StyleButton(card.ChooseButton, selected ? card.Avatar.ShirtColor : ButtonTeal, 16, selected ? "Selected" : "Choose");
            }
        }

        private static void StyleButton(Button button, Color color, int fontSize, string label = null)
        {
            button.GetComponent<Image>().color = color;
            var labelText = button.GetComponentInChildren<Text>();
            if (labelText == null)
            {
                return;
            }

            if (label != null)
            {
                labelText.text = label;
            }

            labelText.fontSize = fontSize;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 12;
            labelText.resizeTextMaxSize = fontSize;
        }

        private sealed class AvatarCardView
        {
            public AvatarCardView(AvatarDefinition avatar, Image cardImage, Image accentImage, Image selectedBadge, Text selectedText, Button chooseButton)
            {
                Avatar = avatar;
                CardImage = cardImage;
                AccentImage = accentImage;
                SelectedBadge = selectedBadge;
                SelectedText = selectedText;
                ChooseButton = chooseButton;
            }

            public AvatarDefinition Avatar { get; }
            public Image CardImage { get; }
            public Image AccentImage { get; }
            public Image SelectedBadge { get; }
            public Text SelectedText { get; }
            public Button ChooseButton { get; }
        }
    }
}
