using UnityEngine;

namespace CareerQuest
{
    public enum AmbientMotionKind
    {
        /// <summary>Horizontal drift that loops across a range (clouds).</summary>
        Drift,

        /// <summary>Gentle figure-of-motion bob along a small curve (butterflies, birds).</summary>
        Bob,

        /// <summary>Small rotation oscillation around the rest pose (flags, foliage).</summary>
        Sway
    }

    /// <summary>
    /// P9 living-campus beat: deterministic-friendly ambient motion. Time is
    /// accumulated through Tick(deltaSeconds) — Update only forwards
    /// Time.deltaTime when AutoTick is on, mirroring the CameraDirector clock
    /// seam so tests can fast-forward without real-time waits.
    /// </summary>
    public class AmbientMotion : MonoBehaviour
    {
        [SerializeField] private AmbientMotionKind kind = AmbientMotionKind.Bob;
        [SerializeField] private float speed = 0.35f;
        [SerializeField] private float amplitude = 0.06f;
        [SerializeField] private float frequency = 1.4f;
        [SerializeField] private float phase;
        [SerializeField] private float driftRange = 13f;

        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private float _elapsed;
        private bool _baseCaptured;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public AmbientMotionKind Kind => kind;

        /// <summary>Editor-builder/runtime seam: sets the motion parameters.</summary>
        public void Configure(AmbientMotionKind motionKind, float motionSpeed, float motionAmplitude, float motionFrequency, float motionPhase, float motionDriftRange = 13f)
        {
            kind = motionKind;
            speed = motionSpeed;
            amplitude = motionAmplitude;
            frequency = motionFrequency;
            phase = motionPhase;
            driftRange = motionDriftRange;
        }

        private void OnEnable()
        {
            CaptureBase();
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        public void Tick(float deltaSeconds)
        {
            CaptureBase();
            _elapsed += deltaSeconds;

            switch (kind)
            {
                case AmbientMotionKind.Drift:
                {
                    var half = driftRange * 0.5f;
                    var x = Mathf.Repeat(_basePosition.x + half + _elapsed * speed, driftRange) - half;
                    var bob = Mathf.Sin(_elapsed * frequency + phase) * amplitude * 0.4f;
                    transform.localPosition = new Vector3(x, _basePosition.y + bob, _basePosition.z);
                    break;
                }
                case AmbientMotionKind.Bob:
                {
                    var x = Mathf.Sin(_elapsed * frequency * 0.63f + phase) * amplitude * 1.7f;
                    var y = Mathf.Sin(_elapsed * frequency + phase) * amplitude;
                    transform.localPosition = _basePosition + new Vector3(x, y, 0f);
                    break;
                }
                case AmbientMotionKind.Sway:
                {
                    // amplitude is interpreted as degrees for sway.
                    var angle = Mathf.Sin(_elapsed * frequency + phase) * amplitude;
                    transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
                    break;
                }
            }
        }

        private void CaptureBase()
        {
            if (_baseCaptured)
            {
                return;
            }

            _basePosition = transform.localPosition;
            _baseRotation = transform.localRotation;
            _baseCaptured = true;
        }
    }
}
