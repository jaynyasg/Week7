using TMPro;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// DESIGN.md Door Sign pattern as world-space TMP: a small paper plate with
    /// a short Fredoka label near a building entrance. Replaces the legacy
    /// TextMesh door labels (hub TextMesh dies in U4 per plan). Sign text is
    /// live TMP, never baked into building PNGs (art rules: no baked text).
    /// Works both ways: configured at runtime (PlayableHubController) or
    /// serialized on the authored prefab (editor builder sets the fields and
    /// the label builds itself on Start).
    /// </summary>
    public class DoorSign : MonoBehaviour
    {
        private static readonly Color PaperColor = new(1f, 0.97f, 0.88f, 0.92f);
        private static readonly Color InkColor = new(0.098f, 0.196f, 0.235f); // DESIGN.md Ink #19323C

        [SerializeField] private string label;
        [SerializeField] private Color accentColor = new(0.96f, 0.77f, 0.36f);
        [SerializeField] private float yOffset = -0.62f;
        [SerializeField] private int sortingOrder = 330;
        [SerializeField] private float fontSize = 2.1f;
        [SerializeField] private float plateWidth = 1.55f;
        [SerializeField] private bool showPlate = true;

        // DESIGN.md door focus pulse: soft loop within the 600-900 ms band.
        private const float PulsePeriodSeconds = 0.75f;
        private const float PulseAmplitude = 0.07f;

        private TextMeshPro _text;
        private float _pulseClock;
        private Vector3 _pulseBaseScale = Vector3.one;
        private bool _pulseBaseCaptured;

        public string Label => label;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public bool IsPulsing { get; private set; }

        /// <summary>
        /// First-run pointer / focus pulse: the sign and door pulse together
        /// (the whole entrance object scales softly on a 600-900 ms loop).
        /// </summary>
        public void SetPulsing(bool active)
        {
            if (IsPulsing == active)
            {
                return;
            }

            if (active && !_pulseBaseCaptured)
            {
                _pulseBaseScale = transform.localScale;
                _pulseBaseCaptured = true;
            }

            IsPulsing = active;
            _pulseClock = 0f;

            if (!active && _pulseBaseCaptured)
            {
                transform.localScale = _pulseBaseScale;
            }
        }

        public void Tick(float deltaSeconds)
        {
            if (!IsPulsing || deltaSeconds <= 0f)
            {
                return;
            }

            _pulseClock += deltaSeconds;
            var wave = 0.5f + 0.5f * Mathf.Sin(_pulseClock / PulsePeriodSeconds * 2f * Mathf.PI);
            transform.localScale = _pulseBaseScale * (1f + PulseAmplitude * wave);
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        /// <summary>Editor-builder seam: sets serialized data without building (the label builds on Start).</summary>
        public void SetData(string signLabel, Color accent, float offsetY, int order, float size, float width, bool plate = true)
        {
            label = signLabel;
            accentColor = accent;
            yOffset = offsetY;
            sortingOrder = order;
            fontSize = size;
            plateWidth = width;
            showPlate = plate;
        }

        /// <summary>Runtime seam: sets data and builds the sign immediately.</summary>
        public void Configure(string signLabel, Color accent, float offsetY, int order)
        {
            SetData(signLabel, accent, offsetY, order, fontSize, plateWidth, showPlate);
            Rebuild();
        }

        private void Start()
        {
            if (_text == null && !string.IsNullOrWhiteSpace(label))
            {
                Rebuild();
            }
        }

        private void Rebuild()
        {
            if (_text == null)
            {
                if (showPlate)
                {
                    var plate = new GameObject("DoorSignPlate", typeof(SpriteRenderer));
                    plate.transform.SetParent(transform, false);
                    plate.transform.localPosition = new Vector3(0f, yOffset, 0f);
                    plate.transform.localScale = new Vector3(plateWidth, 0.36f, 1f);
                    var plateRenderer = plate.GetComponent<SpriteRenderer>();
                    plateRenderer.sprite = CampusWorldSprites.Square;
                    plateRenderer.color = PaperColor;
                    plateRenderer.sortingOrder = sortingOrder - 2;

                    var stripe = new GameObject("DoorSignStripe", typeof(SpriteRenderer));
                    stripe.transform.SetParent(transform, false);
                    stripe.transform.localPosition = new Vector3(0f, yOffset - 0.155f, 0f);
                    stripe.transform.localScale = new Vector3(plateWidth, 0.05f, 1f);
                    var stripeRenderer = stripe.GetComponent<SpriteRenderer>();
                    stripeRenderer.sprite = CampusWorldSprites.Square;
                    stripeRenderer.color = accentColor;
                    stripeRenderer.sortingOrder = sortingOrder - 1;
                }

                var labelObject = new GameObject("DoorSignLabel", typeof(TextMeshPro));
                labelObject.transform.SetParent(transform, false);
                labelObject.transform.localPosition = new Vector3(0f, yOffset, 0f);
                _text = labelObject.GetComponent<TextMeshPro>();
                _text.rectTransform.sizeDelta = new Vector2(Mathf.Max(plateWidth, 1.6f), 0.5f);
            }

            _text.font = TypeStyles.Resolve(TypeRole.Display, TypeWeight.SemiBold);
            _text.fontSize = fontSize;
            _text.color = InkColor;
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            _text.text = label;

            var renderer = _text.GetComponent<MeshRenderer>();
            renderer.sortingOrder = sortingOrder;
        }
    }
}
