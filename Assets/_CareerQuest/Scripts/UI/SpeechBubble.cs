using TMPro;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// DESIGN.md speech-bubble component: world-space TMP on a paper rounded
    /// plate (12px-radius look) with a tail toward the speaking character.
    /// One or two lines maximum — longer text wraps to two lines and then
    /// truncates with an ellipsis, never overflows. Reusable: campus guide
    /// (U5), NPC reactions (U10), emotes (U12).
    ///
    /// Deterministic clock: Tick(deltaSeconds) drives the pop-in and the timed
    /// hide; Update forwards Time.deltaTime only when AutoTick is on.
    /// </summary>
    public class SpeechBubble : MonoBehaviour
    {
        private static readonly Color PaperColor = new(1f, 0.969f, 0.878f, 0.97f); // DESIGN.md Paper #FFF7E0
        private static readonly Color InkColor = new(0.098f, 0.196f, 0.235f);      // DESIGN.md Ink #19323C

        private const float PopInSeconds = 0.16f;
        private const int MaxLines = 2;

        private static Sprite _roundedSprite;

        [SerializeField] private float width = 2.7f;
        [SerializeField] private float height = 0.78f;
        [SerializeField] private int sortingOrder = 352;
        [SerializeField] private float fontSize = 1.6f;

        private SpriteRenderer _plate;
        private SpriteRenderer _tail;
        private TextMeshPro _text;
        private float _hideRemaining;
        private float _popElapsed = PopInSeconds;
        private bool _sticky;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public bool IsVisible { get; private set; }
        public string DisplayedText => _text != null ? _text.text : string.Empty;

        /// <summary>
        /// Layout line count after TMP geometry update — the ≤2-line contract
        /// surface for tests.
        /// </summary>
        public int RenderedLineCount
        {
            get
            {
                if (_text == null || !IsVisible)
                {
                    return 0;
                }

                _text.ForceMeshUpdate();
                return Mathf.Max(1, _text.textInfo.lineCount);
            }
        }

        /// <summary>Creates a bubble parented above the speaking character.</summary>
        public static SpeechBubble Attach(Transform anchor, Vector3 localOffset, float bubbleWidth = 2.7f, int order = 352)
        {
            var bubbleObject = new GameObject("SpeechBubble", typeof(SpeechBubble));
            bubbleObject.transform.SetParent(anchor, false);
            bubbleObject.transform.localPosition = localOffset;

            var bubble = bubbleObject.GetComponent<SpeechBubble>();
            bubble.width = bubbleWidth;
            bubble.sortingOrder = order;
            bubble.EnsureBuilt();
            bubble.HideImmediate();
            return bubble;
        }

        /// <summary>Shows a line. duration ≤ 0 keeps it visible until Hide().</summary>
        public void Show(string line, float durationSeconds = 0f)
        {
            EnsureBuilt();
            _text.text = line ?? string.Empty;
            _sticky = durationSeconds <= 0f;
            _hideRemaining = _sticky ? 0f : durationSeconds;
            _popElapsed = 0f;
            IsVisible = true;
            SetChildrenActive(true);
            ApplyPopScale();
        }

        public void Hide()
        {
            HideImmediate();
        }

        public void Tick(float deltaSeconds)
        {
            if (!IsVisible || deltaSeconds <= 0f)
            {
                return;
            }

            if (_popElapsed < PopInSeconds)
            {
                _popElapsed += deltaSeconds;
                ApplyPopScale();
            }

            if (_sticky)
            {
                return;
            }

            _hideRemaining -= deltaSeconds;
            if (_hideRemaining <= 0f)
            {
                HideImmediate();
            }
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void HideImmediate()
        {
            IsVisible = false;
            _sticky = false;
            _hideRemaining = 0f;
            SetChildrenActive(false);
        }

        private void SetChildrenActive(bool active)
        {
            if (_plate != null)
            {
                _plate.gameObject.SetActive(active);
            }

            if (_tail != null)
            {
                _tail.gameObject.SetActive(active);
            }

            if (_text != null)
            {
                _text.gameObject.SetActive(active);
            }
        }

        private void ApplyPopScale()
        {
            // Quick ease-out pop per DESIGN motion rules (fast, readable).
            var t = Mathf.Clamp01(_popElapsed / PopInSeconds);
            var eased = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, eased);
        }

        private void EnsureBuilt()
        {
            if (_text != null)
            {
                return;
            }

            var plateObject = new GameObject("BubblePlate", typeof(SpriteRenderer));
            plateObject.transform.SetParent(transform, false);
            _plate = plateObject.GetComponent<SpriteRenderer>();
            _plate.sprite = RoundedSprite();
            _plate.drawMode = SpriteDrawMode.Sliced;
            _plate.size = new Vector2(width, height);
            _plate.color = PaperColor;
            _plate.sortingOrder = sortingOrder - 2;

            // Tail: small paper diamond pointing down toward the speaker.
            var tailObject = new GameObject("BubbleTail", typeof(SpriteRenderer));
            tailObject.transform.SetParent(transform, false);
            tailObject.transform.localPosition = new Vector3(-width * 0.18f, -height * 0.5f, 0f);
            tailObject.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tailObject.transform.localScale = new Vector3(0.16f, 0.16f, 1f);
            _tail = tailObject.GetComponent<SpriteRenderer>();
            _tail.sprite = CampusWorldSprites.Square;
            _tail.color = PaperColor;
            _tail.sortingOrder = sortingOrder - 1;

            var textObject = new GameObject("BubbleText", typeof(TextMeshPro));
            textObject.transform.SetParent(transform, false);
            _text = textObject.GetComponent<TextMeshPro>();
            _text.font = TypeStyles.Resolve(TypeRole.Body, TypeWeight.Medium);
            _text.fontSize = fontSize;
            _text.color = InkColor;
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.Normal;
            _text.overflowMode = TextOverflowModes.Ellipsis;
            _text.maxVisibleLines = MaxLines;
            _text.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;

            // Size the text rect to exactly MaxLines: this TMP version's
            // maxVisibleLines only hides lines (lineCount still reports the
            // full layout), so the ≤2-line contract is enforced by making
            // Ellipsis truncate at the rect. Measure a literal two-line sample
            // via TMP's own layout instead of deriving from face metrics —
            // world-space TMP applies an extra point-to-unit scale.
            _text.rectTransform.sizeDelta = new Vector2(width - 0.24f, 50f);
            _text.text = "Ag\nAg";
            _text.ForceMeshUpdate();
            var textHeight = _text.preferredHeight * 1.02f;
            _text.text = string.Empty;
            _text.rectTransform.sizeDelta = new Vector2(width - 0.24f, textHeight);
            height = textHeight + 0.14f;
            _plate.size = new Vector2(width, height);
            _tail.transform.localPosition = new Vector3(-width * 0.18f, -height * 0.5f, 0f);
        }

        /// <summary>
        /// 9-sliced rounded-rect sprite (12px corner radius at 100 PPU) shared
        /// by all bubbles — the DESIGN.md 12px speech-bubble radius look.
        /// </summary>
        private static Sprite RoundedSprite()
        {
            if (_roundedSprite != null)
            {
                return _roundedSprite;
            }

            const int size = 32;
            const float radius = 12f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
                name = "speech.bubble.rounded"
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    var dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(radius - distance + 1f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            _roundedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(14f, 14f, 14f, 14f));
            _roundedSprite.name = "speech.bubble.rounded";
            _roundedSprite.hideFlags = HideFlags.HideAndDontSave;
            return _roundedSprite;
        }
    }
}
