using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CareerQuest
{
    public static class UiBuilder
    {
        public static TMP_FontAsset FontFor(TypeRole role, TypeWeight weight)
        {
            return TypeStyles.Resolve(role, weight);
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
            var image = panel.GetComponent<Image>();
            image.color = new Color(color.r, color.g, color.b, Mathf.Min(color.a, 0.22f));
            // U6 raycast policy: full-screen washes never block world drags — the
            // known "drag doesn't work at all" failure. Modal overlays (ceremony)
            // explicitly opt back in at their call site.
            image.raycastTarget = false;
            return rect;
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var image = panel.GetComponent<Image>();
            image.color = color;
            // U6 raycast policy: decorative panels/shapes default non-blocking;
            // Buttons (their own Image) stay blocking.
            image.raycastTarget = false;
            return panel.GetComponent<RectTransform>();
        }

        public static RectTransform InstructionStripPanel(Transform parent, string name, Color fill, Color accent)
        {
            var panel = Panel(parent, name, fill);
            var band = Shape(panel, $"{name}Accent", accent, 0f, 30f, 1120f, 4f);
            band.GetComponent<Image>().raycastTarget = false;
            return panel;
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

        public static TextMeshProUGUI Text(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor anchor,
            Color color,
            TypeRole role = TypeRole.Body,
            TypeWeight weight = TypeWeight.Regular)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TypeStyles.Resolve(role, weight);
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = ToAlignment(anchor);
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            // U6 raycast policy: text never blocks pointer raycasts (button
            // labels pass through to the Button image beneath).
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(Transform parent, string name, string label, Action onClick)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.09f, 0.31f, 0.42f);

            var button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                // U8: every factory button presses with the same UI-tier cue
                // (silent no-op when the clip is absent — never blocks clicks).
                AudioDirector.Ensure().PlayUi(AudioCueIds.UiPress);
                onClick?.Invoke();
            });

            var labelText = Text(buttonObject.transform, $"{name}Label", label, TypeStyles.ButtonLabel, TextAnchor.MiddleCenter, Color.white, TypeRole.Body, TypeWeight.SemiBold);
            Stretch(labelText.rectTransform);
            return button;
        }

        public static Button SmallButton(Transform parent, string name, string label, Action onClick)
        {
            var button = Button(parent, name, label, onClick);
            var labelText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null)
            {
                labelText.fontSize = 16;
            }

            return button;
        }

        /// <summary>
        /// U13 paper-styled horizontal slider (0..1). Follows the factory
        /// conventions: TMP-free composition (label is the caller's job),
        /// kid-large hit area (size the root ≥ 44px tall via Place), and the
        /// raycast policy — track + handle stay raycast targets (they ARE the
        /// control), the fill never blocks. Track is paper-shadow, fill is
        /// Path Gold, handle is a Workshop Teal circle.
        /// </summary>
        public static Slider Slider(Transform parent, string name, float initialValue, Action<float> onValueChanged)
        {
            var sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(parent, false);

            // Track (background): full-width band, vertically centered.
            var background = new GameObject($"{name}Track", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sliderObject.transform, false);
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = new Color(0.851f, 0.714f, 0.435f, 0.85f); // DESIGN Paper Shadow
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.offsetMin = new Vector2(0f, -7f);
            backgroundRect.offsetMax = new Vector2(0f, 7f);

            // Fill area + fill: progress reads as Path Gold.
            var fillArea = new GameObject($"{name}FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            var fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.offsetMin = new Vector2(0f, -7f);
            fillAreaRect.offsetMax = new Vector2(0f, 7f);

            var fill = new GameObject($"{name}Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.GetComponent<Image>();
            fillImage.color = new Color(0.953f, 0.769f, 0.357f); // DESIGN Path Gold
            fillImage.raycastTarget = false;
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            // Handle slide area + kid-large circular handle (≥ 36px visual,
            // the whole root rect is the drag surface).
            var handleArea = new GameObject($"{name}HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObject.transform, false);
            var handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(18f, 0f);
            handleAreaRect.offsetMax = new Vector2(-18f, 0f);

            var handle = new GameObject($"{name}Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleImage = handle.GetComponent<Image>();
            handleImage.sprite = CircleSprite;
            handleImage.color = new Color(0.055f, 0.42f, 0.435f); // DESIGN Workshop Teal
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(36f, 36f);

            var slider = sliderObject.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(Mathf.Clamp01(initialValue));
            if (onValueChanged != null)
            {
                slider.onValueChanged.AddListener(value => onValueChanged(value));
            }

            return slider;
        }

        public static TMP_InputField Input(Transform parent, string name, string value)
        {
            var inputObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputObject.transform.SetParent(parent, false);
            inputObject.GetComponent<Image>().color = Color.white;

            var viewportObject = new GameObject($"{name}Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportObject.transform.SetParent(inputObject.transform, false);
            var viewport = viewportObject.GetComponent<RectTransform>();
            Stretch(viewport, 12f, 4f);

            var text = Text(viewportObject.transform, $"{name}Text", value, 20, TextAnchor.MiddleLeft, Color.black);
            Stretch(text.rectTransform);

            var input = inputObject.GetComponent<TMP_InputField>();
            input.textViewport = viewport;
            input.textComponent = text;
            input.text = value;
            return input;
        }

        public static TextAlignmentOptions ToAlignment(TextAnchor anchor)
        {
            return anchor switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
                TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
                TextAnchor.MiddleRight => TextAlignmentOptions.Right,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center
            };
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
