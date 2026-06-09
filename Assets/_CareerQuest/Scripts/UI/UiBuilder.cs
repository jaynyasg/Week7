using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CareerQuest
{
    public static class UiBuilder
    {
        private static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font != null)
                {
                    return _font;
                }

                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                return _font;
            }
        }

        public static Canvas EnsureCanvas()
        {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }

            var canvasObject = new GameObject("CareerQuestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            return canvas;
        }

        public static RectTransform FullPanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(color.r, color.g, color.b, Mathf.Min(color.a, 0.22f));
            return rect;
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel.GetComponent<RectTransform>();
        }

        public static RectTransform Shape(Transform parent, string name, Color color, float x, float y, float width, float height)
        {
            var shape = Panel(parent, name, color);
            Place(shape, x, y, width, height);
            return shape;
        }

        public static RectTransform Circle(Transform parent, string name, Color color, float x, float y, float width, float height)
        {
            var circle = Shape(parent, name, color, x, y, width, height);
            circle.GetComponent<Image>().sprite = CircleSprite;
            return circle;
        }

        public static Text Text(Transform parent, string name, string value, int fontSize, TextAnchor anchor, Color color)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = DefaultFont;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button Button(Transform parent, string name, string label, Action onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.09f, 0.31f, 0.42f);

            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());

            var labelText = Text(buttonObject.transform, $"{name}Label", label, 24, TextAnchor.MiddleCenter, Color.white);
            Stretch(labelText.rectTransform);
            return button;
        }

        public static Button SmallButton(Transform parent, string name, string label, Action onClick)
        {
            var button = Button(parent, name, label, onClick);
            var labelText = button.GetComponentInChildren<Text>();
            if (labelText != null)
            {
                labelText.fontSize = 16;
            }

            return button;
        }

        public static InputField Input(Transform parent, string name, string value)
        {
            var inputObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(parent, false);
            inputObject.GetComponent<Image>().color = Color.white;

            var text = Text(inputObject.transform, $"{name}Text", value, 20, TextAnchor.MiddleLeft, Color.black);
            Stretch(text.rectTransform, 12f, 4f);

            var input = inputObject.GetComponent<InputField>();
            input.textComponent = text;
            input.text = value;
            return input;
        }

        public static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        public static void Stretch(RectTransform rect, float horizontalPadding = 0f, float verticalPadding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
            rect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        }

        public static void Clear(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        private static Sprite _circleSprite;

        private static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite != null)
                {
                    return _circleSprite;
                }

                const int size = 96;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                var center = (size - 1) * 0.5f;
                var radius = center;

                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var dx = x - center;
                        var dy = y - center;
                        var alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 1f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                _circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                return _circleSprite;
            }
        }
    }
}
