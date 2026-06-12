using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// P17 partner drag indicator: a soft Path Gold glow pulsing gently behind
    /// the tray piece the PARTNER currently holds — co-presence at a glance,
    /// explicitly NOT continuous drag-position mirroring (the piece itself
    /// never moves on the observer's screen).
    ///
    /// Pulse sits in the DESIGN.md door-pulse band (600–900 ms loop), rendered
    /// one sorting step behind the piece so it reads as an aura, never a state
    /// change. Shared by all three converted rooms via the static seams
    /// (<see cref="Show"/> / <see cref="Clear"/> / <see cref="IsShownOn"/>).
    ///
    /// Clearing hides the glow IMMEDIATELY (same-frame readable for tests and
    /// teardown) and then destroys the component; world clears destroy the
    /// piece hierarchy and the indicator with it.
    ///
    /// Deterministic clock: Tick(deltaSeconds) drives the pulse; Update only
    /// forwards Time.deltaTime when AutoTick is on (house idiom).
    /// </summary>
    public class PartnerHoldIndicator : MonoBehaviour
    {
        public const float PulseSeconds = 0.75f; // DESIGN door-pulse 600–900ms
        private const float MinAlpha = 0.18f;
        private const float MaxAlpha = 0.4f;

        private static readonly Color GlowGold = new(0.953f, 0.769f, 0.357f); // Path Gold

        private SpriteRenderer _glow;
        private float _elapsed;
        private bool _cleared;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public bool IsActive => !_cleared && _glow != null && _glow.gameObject.activeSelf;

        /// <summary>Attaches (or refreshes) the indicator on a held piece.</summary>
        public static PartnerHoldIndicator Show(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            PartnerHoldIndicator indicator = null;
            foreach (var candidate in target.GetComponents<PartnerHoldIndicator>())
            {
                if (!candidate._cleared)
                {
                    indicator = candidate;
                    break;
                }
            }

            if (indicator == null)
            {
                indicator = target.AddComponent<PartnerHoldIndicator>();
            }

            indicator.EnsureGlow();
            return indicator;
        }

        /// <summary>Removes the indicator (drop/reject/accept/disconnect paths).</summary>
        public static void Clear(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            foreach (var indicator in target.GetComponents<PartnerHoldIndicator>())
            {
                indicator.ClearSelf();
            }
        }

        /// <summary>Test/render seam: is the soft highlight visible on this piece?</summary>
        public static bool IsShownOn(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            foreach (var indicator in target.GetComponents<PartnerHoldIndicator>())
            {
                if (indicator.IsActive)
                {
                    return true;
                }
            }

            return false;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || !IsActive)
            {
                return;
            }

            _elapsed += deltaSeconds;
            var wave = 0.5f + 0.5f * Mathf.Sin(_elapsed * (2f * Mathf.PI / PulseSeconds));
            var color = GlowGold;
            color.a = Mathf.Lerp(MinAlpha, MaxAlpha, wave);
            _glow.color = color;
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void ClearSelf()
        {
            if (_cleared)
            {
                return;
            }

            _cleared = true;
            if (_glow != null)
            {
                _glow.gameObject.SetActive(false); // same-frame invisible
                Destroy(_glow.gameObject);
            }

            Destroy(this);
        }

        private void EnsureGlow()
        {
            if (_glow != null)
            {
                _glow.gameObject.SetActive(true);
                return;
            }

            var pieceRenderer = GetComponent<SpriteRenderer>();
            var glowObject = new GameObject("PartnerHoldGlow", typeof(SpriteRenderer));
            glowObject.transform.SetParent(transform, false);
            glowObject.transform.localPosition = Vector3.zero;

            _glow = glowObject.GetComponent<SpriteRenderer>();
            _glow.sprite = CampusWorldSprites.Circle;
            var startColor = GlowGold;
            startColor.a = MaxAlpha;
            _glow.color = startColor;
            _glow.sortingOrder = (pieceRenderer != null ? pieceRenderer.sortingOrder : 330) - 1;

            // Aura slightly larger than the piece sprite, in the piece's local
            // space (the glow inherits the piece scale, so size off the sprite
            // bounds the piece renderer actually draws).
            var bounds = pieceRenderer != null && pieceRenderer.sprite != null
                ? pieceRenderer.sprite.bounds.size
                : Vector3.one;
            var size = Mathf.Max(Mathf.Max(bounds.x, bounds.y), 0.2f) * 1.45f;
            glowObject.transform.localScale = new Vector3(size, size, 1f);
        }
    }
}
