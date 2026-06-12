using UnityEngine;
using UnityEngine.EventSystems;

namespace CareerQuest
{
    public enum HubToyKind
    {
        /// <summary>Plaza fountain: splash particle burst + water cue.</summary>
        Fountain,

        /// <summary>Hand bell: swing/ring animation + bell cue.</summary>
        Bell,

        /// <summary>Flag pennant: flutter burst on TOP of its ambient sway.</summary>
        Flag
    }

    /// <summary>
    /// P18 interactive hub toy: click → short animation beat + cue. Pure local
    /// delight — no state, no progress effect, nothing networked (never-punish:
    /// toys only ever reward curiosity).
    ///
    /// Input rides the SAME Physics2D raycast path as drag pieces (Collider2D +
    /// IPointerClickHandler raycast by the single Physics2DRaycaster on
    /// CameraDirector's CameraHost); PlayerAvatarController's pointer-over guard
    /// keeps toy clicks from double-firing click-to-enter.
    ///
    /// Re-trigger safety: a click RESETS the beat clock (animations restart from
    /// the captured rest pose, never stack), the particle burst has a local
    /// minimum interval, and the cue rides the AudioDirector gameplay tier whose
    /// per-cue throttle bounds click spam.
    ///
    /// Flag note: AmbientMotion sway owns localRotation; the flutter beat is
    /// scale-only (squash ripple) so the two compose without fighting.
    ///
    /// Deterministic clock: Tick(deltaSeconds) advances the beat; Update only
    /// forwards Time.deltaTime when AutoTick is on (house idiom).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HubToy : MonoBehaviour, IPointerClickHandler
    {
        public const float BeatSeconds = 0.9f;               // DESIGN completion band 500-900ms
        public const float BurstMinIntervalSeconds = 0.3f;   // particle spam guard

        private static readonly Color SplashWater = new(0.62f, 0.87f, 0.97f);
        private static readonly Color BellGold = new(0.953f, 0.769f, 0.357f);   // Path Gold
        private static readonly Color FlagCoral = new(0.969f, 0.424f, 0.369f);  // Creative Coral

        [SerializeField] private HubToyKind kind = HubToyKind.Bell;
        [SerializeField] private string cueId = AudioCueIds.ToyBell;

        private float _elapsed;
        private float _remaining;
        private float _sinceLastBurst = BurstMinIntervalSeconds;
        private Vector3 _baseScale = Vector3.one;
        private Quaternion _baseRotation = Quaternion.identity;
        private bool _baseCaptured;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public HubToyKind Kind => kind;
        public string CueId => cueId;
        public bool IsPlaying => _remaining > 0f;
        public int ActivationCount { get; private set; }

        /// <summary>Editor-builder seam: sets the toy parameters (prefab-serialized).</summary>
        public void Configure(HubToyKind toyKind, string toyCueId)
        {
            kind = toyKind;
            cueId = toyCueId;
        }

        private void Start()
        {
            // Toys share the drag framework's input shell (EventSystem + the
            // single Physics2DRaycaster on CameraDirector's CameraHost).
            DraggablePiece.EnsureInputShell();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Activate();
        }

        /// <summary>
        /// The click seam (pointer shell and tests share it). Rapid calls reset
        /// the beat instead of stacking — the rest pose is always recoverable.
        /// </summary>
        public void Activate()
        {
            CaptureBase();
            ActivationCount++;

            // Re-trigger resets: restart the beat from zero against the SAME
            // captured rest pose — transforms never compound across clicks.
            _elapsed = 0f;
            _remaining = BeatSeconds;

            if (_sinceLastBurst >= BurstMinIntervalSeconds)
            {
                _sinceLastBurst = 0f;
                SpawnBurst();
            }

            // Gameplay tier: per-cue throttle bounds spam; silent no-op when
            // the clip is missing (audio never gates the delight).
            AudioCueCatalog.TryPlay(cueId);
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            _sinceLastBurst += deltaSeconds;
            if (_remaining <= 0f)
            {
                return;
            }

            _elapsed += deltaSeconds;
            _remaining -= deltaSeconds;

            var t = Mathf.Clamp01(_elapsed / BeatSeconds);
            var decay = 1f - t;

            switch (kind)
            {
                case HubToyKind.Bell:
                {
                    // Decaying swing around the rest pose (ring readable, gentle).
                    var angle = Mathf.Sin(_elapsed * 16f) * 14f * decay;
                    transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
                    break;
                }
                case HubToyKind.Flag:
                {
                    // Scale-only flutter ripple — composes with AmbientMotion's
                    // rotation sway without either overwriting the other.
                    var ripple = 1f + Mathf.Sin(_elapsed * 22f) * 0.16f * decay;
                    transform.localScale = new Vector3(_baseScale.x * ripple, _baseScale.y, _baseScale.z);
                    break;
                }
                case HubToyKind.Fountain:
                {
                    // Soft basin pulse under the splash.
                    var pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.06f;
                    transform.localScale = _baseScale * pulse;
                    break;
                }
            }

            if (_remaining <= 0f)
            {
                RestoreRestPose();
            }
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            // World clears / route exits never strand a mid-beat pose.
            _remaining = 0f;
            RestoreRestPose();
        }

        private void SpawnBurst()
        {
            switch (kind)
            {
                case HubToyKind.Fountain:
                    // Splash above the spout (P1: real ParticleSystem).
                    ParticlePoof.Burst(transform.position + new Vector3(0f, 0.42f, 0f), SplashWater, 18);
                    break;
                case HubToyKind.Bell:
                    ParticlePoof.Burst(transform.position + new Vector3(0f, 0.22f, 0f), BellGold, 10);
                    break;
                case HubToyKind.Flag:
                    ParticlePoof.Burst(transform.position, FlagCoral, 10);
                    break;
            }
        }

        private void RestoreRestPose()
        {
            if (!_baseCaptured)
            {
                return;
            }

            transform.localScale = _baseScale;
            if (kind == HubToyKind.Bell)
            {
                transform.localRotation = _baseRotation;
            }
        }

        private void CaptureBase()
        {
            if (_baseCaptured)
            {
                return;
            }

            _baseScale = transform.localScale;
            _baseRotation = transform.localRotation;
            _baseCaptured = true;
        }
    }
}
