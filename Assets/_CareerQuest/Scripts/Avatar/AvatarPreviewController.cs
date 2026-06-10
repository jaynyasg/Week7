using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    public static class AvatarPreviewController
    {
        public static Image CreatePreview(Transform parent, string name, AvatarDefinition avatar, Vector2 size)
        {
            var previewObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            previewObject.transform.SetParent(parent, false);

            var image = previewObject.GetComponent<Image>();
            image.preserveAspect = true;
            Apply(image, avatar);

            UiBuilder.Place(previewObject.GetComponent<RectTransform>(), 0f, 0f, size.x, size.y);
            return image;
        }

        public static void Apply(Image image, AvatarDefinition avatar)
        {
            if (image == null)
            {
                return;
            }

            avatar ??= AvatarConfig.DefaultAvatar;
            image.sprite = AssetCatalog.SpriteFor(avatar.SpriteAssetId);
            image.color = Color.white;
        }
    }
}
