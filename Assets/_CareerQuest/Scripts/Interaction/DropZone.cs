using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// A drop target with a stable zone id. Resolution is physics-based
    /// (<see cref="FindAt"/> uses Physics2D.OverlapPoint against trigger
    /// colliders), so it works for any room without per-room raycast wiring.
    /// Owns the P12 ghost slot preview: a faded copy of the dragged piece's
    /// sprite shown while the pointer hovers a valid empty zone.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DropZone : MonoBehaviour
    {
        private const float GhostAlpha = 0.45f;

        [SerializeField] private string zoneId;
        [SerializeField] private int ghostSortingOrder = 320;

        private SpriteRenderer _ghost;

        public string ZoneId => zoneId;
        public bool IsOccupied { get; set; }
        public bool IsGhostVisible => _ghost != null && _ghost.gameObject.activeSelf;

        public void Configure(string id, int sortingOrder = 320)
        {
            zoneId = id;
            ghostSortingOrder = sortingOrder;
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
        }

        /// <summary>Topmost DropZone under a world point, or null.</summary>
        public static DropZone FindAt(Vector2 worldPosition)
        {
            var hits = Physics2D.OverlapPointAll(worldPosition);
            foreach (var hit in hits)
            {
                var zone = hit.GetComponent<DropZone>();
                if (zone != null)
                {
                    return zone;
                }
            }

            return null;
        }

        /// <summary>P12 ghost preview: faded piece sprite snapped into the slot.</summary>
        public void ShowGhost(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            if (_ghost == null)
            {
                var ghostObject = new GameObject("SlotGhost", typeof(SpriteRenderer));
                ghostObject.transform.SetParent(transform, false);
                _ghost = ghostObject.GetComponent<SpriteRenderer>();
            }

            _ghost.sprite = sprite;
            _ghost.color = new Color(1f, 1f, 1f, GhostAlpha);
            _ghost.sortingOrder = ghostSortingOrder;

            var bounds = sprite.bounds.size;
            var width = Mathf.Approximately(bounds.x, 0f) ? 1f : bounds.x;
            var height = Mathf.Approximately(bounds.y, 0f) ? 1f : bounds.y;
            _ghost.transform.localScale = new Vector3(0.9f / width, 0.9f / height, 1f);
            _ghost.gameObject.SetActive(true);
        }

        public void HideGhost()
        {
            if (_ghost != null)
            {
                _ghost.gameObject.SetActive(false);
            }
        }
    }
}
