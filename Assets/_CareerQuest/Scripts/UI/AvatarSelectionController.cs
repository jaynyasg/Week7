using System;
using System.Collections.Generic;
using TMPro;
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
            Render(parent, app, "Enter Campus", avatarId => app.ChooseAvatar(avatarId), app.ShowEntry);
        }

        public void Render(
            Transform parent,
            CareerQuestApp app,
            string confirmLabel,
            Action<string> onConfirm,
            Action onBack)
        {
            var panel = UiBuilder.FullPanel(parent, "AvatarSelectionPanel", new Color(0.62f, 0.88f, 1f));
            panel.GetComponent<Image>().color = new Color(0.69f, 0.9f, 1f, 1f);
            var selectedAvatar = app.Session.SelectedAvatar ?? AvatarConfig.DefaultAvatar;

            UiBuilder.Shape(panel, "AvatarSelectionSkyBand", new Color(0.84f, 0.96f, 1f, 0.7f), 0f, 172f, 1280f, 376f);
            UiBuilder.Shape(panel, "AvatarSelectionGroundBand", new Color(0.55f, 0.82f, 0.5f, 0.9f), 0f, -242f, 1280f, 174f);
            UiBuilder.Shape(panel, "AvatarSelectionPathBand", new Color(0.95f, 0.77f, 0.36f, 0.78f), 326f, -280f, 470f, 28f);
            UiBuilder.Circle(panel, "AvatarSelectionSun", new Color(1f, 0.84f, 0.27f), 520f, 234f, 92f, 92f);
            UiBuilder.Circle(panel, "AvatarSelectionCloudA", new Color(1f, 1f, 1f, 0.82f), -468f, 250f, 112f, 44f);
            UiBuilder.Circle(panel, "AvatarSelectionCloudB", new Color(1f, 1f, 1f, 0.74f), -404f, 262f, 118f, 54f);
            UiBuilder.Circle(panel, "AvatarSelectionCloudC", new Color(1f, 1f, 1f, 0.82f), -336f, 250f, 104f, 42f);

            var title = UiBuilder.Text(panel, "AvatarSelectionTitle", "Choose Your Quest Hero", 32, TextAnchor.MiddleLeft, Ink, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(title.rectTransform, -366f, 286f, 470f, 44f);

            var subtitle = UiBuilder.Text(panel, "AvatarSelectionSubtitle", "Pick the character you want to bring into Career Quest Campus.", 16, TextAnchor.MiddleLeft, Ink);
            UiBuilder.Place(subtitle.rectTransform, -344f, 250f, 520f, 28f);

            var previewPanel = UiBuilder.Panel(panel, "SelectedAvatarPanel", Paper);
            UiBuilder.Place(previewPanel, -392f, 16f, 328f, 450f);

            var passportBand = UiBuilder.Shape(previewPanel, "SelectedAvatarPassportBand", selectedAvatar.AccentColor, 0f, 197f, 328f, 56f);
            var passportTitle = UiBuilder.Text(previewPanel, "SelectedAvatarPassportTitle", "Quest Passport", 18, TextAnchor.MiddleCenter, Color.white);
            UiBuilder.Place(passportTitle.rectTransform, 0f, 202f, 286f, 30f);

            UiBuilder.Shape(previewPanel, "SelectedAvatarPlatformShadow", new Color(0.54f, 0.39f, 0.18f, 0.28f), 0f, -52f, 214f, 24f);
            var platformTop = UiBuilder.Shape(previewPanel, "SelectedAvatarPlatform", selectedAvatar.AccentColor, 0f, -42f, 198f, 22f);

            var previewImage = AvatarPreviewController.CreatePreview(previewPanel, "SelectedAvatarPreview", selectedAvatar, new Vector2(178f, 224f));
            UiBuilder.Place(previewImage.rectTransform, 0f, 62f, 178f, 224f);

            var previewName = UiBuilder.Text(previewPanel, "SelectedAvatarName", selectedAvatar.DisplayName, 24, TextAnchor.MiddleCenter, Ink, TypeRole.Display, TypeWeight.Medium);
            UiBuilder.Place(previewName.rectTransform, 0f, -96f, 282f, 30f);

            var previewRole = UiBuilder.Text(previewPanel, "SelectedAvatarRole", selectedAvatar.Role, 16, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(previewRole.rectTransform, 0f, -126f, 282f, 24f);

            var previewPersonality = UiBuilder.Text(previewPanel, "SelectedAvatarPersonality", selectedAvatar.PersonalityLabel, 13, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(previewPersonality.rectTransform, 0f, -166f, 282f, 48f);

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

            for (var i = 0; i < AvatarConfig.Avatars.Length; i++)
            {
                var avatar = AvatarConfig.Avatars[i];
                var x = i % 2 == 0 ? 52f : 360f;
                var y = i < 2 ? 102f : -126f;
                cardViews.Add(RenderAvatarCard(panel, avatar, x, y, () => SelectAvatar(avatar)));
            }

            RefreshCardStates(cardViews, selectedAvatar);

            var confirm = UiBuilder.Button(panel, "AvatarConfirmButton", confirmLabel, () => onConfirm?.Invoke(selectedAvatar.Id));
            UiBuilder.Place(confirm.GetComponent<RectTransform>(), 364f, -310f, 220f, 48f);
            StyleButton(confirm, new Color(0.05f, 0.49f, 0.43f), 20);

            var back = UiBuilder.Button(panel, "AvatarBackButton", "Back", onBack ?? app.ShowEntry);
            UiBuilder.Place(back.GetComponent<RectTransform>(), 126f, -310f, 190f, 48f);
            StyleButton(back, ButtonTeal, 20);
        }

        private static AvatarCardView RenderAvatarCard(RectTransform parent, AvatarDefinition avatar, float x, float y, UnityEngine.Events.UnityAction onSelect)
        {
            var card = UiBuilder.Panel(parent, $"{avatar.Id}Card", PaperSoft);
            UiBuilder.Place(card, x, y, 270f, 196f);

            var accentBand = UiBuilder.Shape(card, $"{avatar.Id}AccentBand", avatar.AccentColor, 0f, 88f, 270f, 10f);
            var accentImage = accentBand.GetComponent<Image>();
            accentImage.raycastTarget = false;

            var preview = AvatarPreviewController.CreatePreview(card, $"{avatar.Id}Preview", avatar, new Vector2(82f, 98f));
            UiBuilder.Place(preview.rectTransform, -72f, 32f, 82f, 98f);
            preview.raycastTarget = false;

            var name = UiBuilder.Text(card, $"{avatar.Id}Name", avatar.DisplayName, 19, TextAnchor.MiddleCenter, Ink, TypeRole.Display, TypeWeight.Medium);
            UiBuilder.Place(name.rectTransform, 46f, 32f, 166f, 28f);
            name.raycastTarget = false;

            var role = UiBuilder.Text(card, $"{avatar.Id}Role", avatar.Role, 14, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(role.rectTransform, 46f, 4f, 170f, 24f);
            role.raycastTarget = false;

            var selectedBadge = UiBuilder.Shape(card, $"{avatar.Id}SelectedBadge", new Color(1f, 1f, 1f, 0f), 50f, -30f, 136f, 22f);
            var selectedBadgeImage = selectedBadge.GetComponent<Image>();
            selectedBadgeImage.raycastTarget = false;

            var selectedText = UiBuilder.Text(card, $"{avatar.Id}SelectedState", string.Empty, 13, TextAnchor.MiddleCenter, Ink);
            UiBuilder.Place(selectedText.rectTransform, 50f, -30f, 136f, 18f);
            selectedText.raycastTarget = false;

            var choose = UiBuilder.Button(card, $"{avatar.Id}ChooseButton", "Choose", () => onSelect.Invoke());
            UiBuilder.Place(choose.GetComponent<RectTransform>(), 50f, -70f, 136f, 30f);
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
            var labelText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText == null)
            {
                return;
            }

            if (label != null)
            {
                labelText.text = label;
            }

            labelText.fontSize = fontSize;
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 12;
            labelText.fontSizeMax = fontSize;
        }

        private sealed class AvatarCardView
        {
            public AvatarCardView(AvatarDefinition avatar, Image cardImage, Image accentImage, Image selectedBadge, TextMeshProUGUI selectedText, Button chooseButton)
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
            public TextMeshProUGUI SelectedText { get; }
            public Button ChooseButton { get; }
        }
    }
}
