using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Derives walk/idle state and facing for a remote avatar from observed
    /// position deltas. The network position lerp produces residual motion at
    /// rest that would flicker the walk state, so state changes pass through a
    /// speed hysteresis band (start threshold above stop threshold) and facing
    /// only flips after a meaningful horizontal step (deadzone) — never on
    /// lerp jitter. Plain class so tests drive it deterministically.
    /// </summary>
    public sealed class RemoteLocomotionFilter
    {
        private readonly float _startSpeed;
        private readonly float _stopSpeed;
        private readonly float _facingDeadzonePerSecond;
        private readonly float _stopHoldSeconds;

        private Vector3 _lastPosition;
        private bool _hasSample;
        private float _belowStopElapsed;

        public RemoteLocomotionFilter(
            float startSpeed = 0.6f,
            float stopSpeed = 0.25f,
            float facingDeadzonePerSecond = 0.45f,
            float stopHoldSeconds = 0.12f)
        {
            _startSpeed = Mathf.Max(0.01f, startSpeed);
            _stopSpeed = Mathf.Clamp(stopSpeed, 0.001f, _startSpeed);
            _facingDeadzonePerSecond = Mathf.Max(0.01f, facingDeadzonePerSecond);
            _stopHoldSeconds = Mathf.Max(0f, stopHoldSeconds);
        }

        public bool IsMoving { get; private set; }
        public float FacingX { get; private set; } = 1f;

        /// <summary>Seeds the filter at a position without deriving motion (spawn/teleport).</summary>
        public void Reset(Vector3 position)
        {
            _lastPosition = position;
            _hasSample = true;
            IsMoving = false;
            _belowStopElapsed = 0f;
        }

        /// <summary>Feeds one observed position sample; updates IsMoving/FacingX.</summary>
        public void Step(Vector3 position, float deltaSeconds)
        {
            if (!_hasSample || deltaSeconds <= 0f)
            {
                Reset(position);
                return;
            }

            var delta = position - _lastPosition;
            _lastPosition = position;

            var speed = delta.magnitude / deltaSeconds;
            if (IsMoving)
            {
                // Stop only after the speed stays under the stop threshold for
                // a hold window — bursty RPC arrival (a frame with no message)
                // must not blink the walk state off.
                if (speed <= _stopSpeed)
                {
                    _belowStopElapsed += deltaSeconds;
                    if (_belowStopElapsed >= _stopHoldSeconds)
                    {
                        IsMoving = false;
                    }
                }
                else
                {
                    _belowStopElapsed = 0f;
                }
            }
            else if (speed >= _startSpeed)
            {
                IsMoving = true;
                _belowStopElapsed = 0f;
            }

            // Facing hysteresis: only honor horizontal motion fast enough to be
            // intentional — sign(lerp residue) never flips the character.
            var horizontalSpeed = delta.x / deltaSeconds;
            if (Mathf.Abs(horizontalSpeed) >= _facingDeadzonePerSecond)
            {
                FacingX = Mathf.Sign(horizontalSpeed);
            }
        }
    }
}
