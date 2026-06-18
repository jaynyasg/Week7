using TMPro;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// P16: small world-space TMP identity label above a networked avatar.
    /// Text is fixed identity data (player slot + chosen avatar display name) —
    /// never free text, preserving the no-chat privacy boundary. Paper pill +
    /// ink label per DESIGN.md small-label rules; characters sorting band.
    /// </summary>
    public class AvatarNameTag : MonoBehaviour
    {
        private static readonly Color PaperColor = new(1f, 0.97f, 0.88f, 0.9f);
        private static readonly Color InkColor = new(0.098f, 0.196f, 0.235f); // DESIGN.md Ink #19323C

        [SerializeField] private float yOffset = 1.18f;
        [SerializeField] private int sortingOrder = 344;
        [SerializeField] private float fontSize = 1.45f;

        private TextMeshPro _label;
        private SpriteRenderer _plate;

        public string Text => _label != null ? _label.text : string.Empty;
        public TextMeshPro Label => _label;
        public int SortingOrder => sortingOrder;
        public float YOffset => yOffset;

        /// <summary>Builds the identity text for a player slot + avatar (no free text).</summary>
        public static string IdentityTextFor(ulong ownerClientId, AvatarDefinition avatar)
        {
            var definition = avatar ?? AvatarConfig.DefaultAvatar;
            var slot = ownerClientId == 0UL ? 1 : 2;
            return $"{definition.DisplayName} (P{slot})";
        }

        public void Configure(string identityText)
        {
            EnsureBuilt();
            _label.text = identityText ?? string.Empty;
            ResizePlate();
        }

        public void SetVisible(bool visible)
        {
            if (_label != null)
            {
                _label.gameObject.SetActive(visible);
            }

            if (_plate != null)
            {
                _plate.gameObject.SetActive(visible);
            }
        }

        private void EnsureBuilt()
        {
            if (_label != null)
            {
                return;
            }

            var plateObject = new GameObject("NameTagPlate", typeof(SpriteRenderer));
            plateObject.transform.SetParent(transform, false);
            plateObject.transform.localPosition = new Vector3(0f, yOffset, 0f);
            _plate = plateObject.GetComponent<SpriteRenderer>();
            _plate.sprite = CampusWorldSprites.Square;
            _plate.color = PaperColor;
            _plate.sortingOrder = sortingOrder - 1;

            var labelObject = new GameObject("NameTagLabel", typeof(TextMeshPro));
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, yOffset, 0f);
            _label = labelObject.GetComponent<TextMeshPro>();
            _label.rectTransform.sizeDelta = new Vector2(2.4f, 0.34f);
            _label.font = TypeStyles.Resolve(TypeRole.Body, TypeWeight.SemiBold);
            _label.fontSize = fontSize;
            _label.color = InkColor;
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
        }

        private void ResizePlate()
        {
            if (_plate == null || _label == null)
            {
                return;
            }

            _label.ForceMeshUpdate();
            var width = Mathf.Clamp(_label.preferredWidth + 0.18f, 0.6f, 2.6f);
            _plate.transform.localScale = new Vector3(width, 0.3f, 1f);
        }
    }
}
