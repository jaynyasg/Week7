using System.Collections.Generic;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Code-driven Sprite[] frame cycling for avatars and NPCs — no Animator
    /// assets, no NetworkAnimator. Frames come from AssetCatalog.FrameSetFor
    /// (curated Kenney Toon Character poses); facing is SpriteRenderer.flipX.
    ///
    /// States:
    /// - Idle: cycles idle frames when 2+ exist, otherwise holds the static
    ///   sprite with a gentle breathing bob (DESIGN.md: slow, 1-2 px feel —
    ///   implemented as a tiny scale pulse so it never fights position writes
    ///   from movement/clamp/network-lerp code).
    /// - Walk: cycles walk frames at 8-12 fps; falls back to the legacy single
    ///   ".walk" pose sprite when no frame set is curated (always safe).
    /// - Celebrate (P15): cycles the cheer poses with a small bounce for a
    ///   requested duration via TriggerCelebrate, then returns to idle/walk.
    ///
    /// Deterministic clock: Tick(deltaSeconds) advances everything; Update only
    /// forwards Time.deltaTime when AutoTick is on (house idiom, mirrors
    /// CameraDirector/AmbientMotion).
    /// </summary>
    public class SpriteFrameAnimator : MonoBehaviour
    {
        public enum AnimState
        {
            Idle,
            Walk,
            Celebrate
        }

        [SerializeField] private float framesPerSecond = 10f; // 8-12 fps per plan
        [SerializeField] private float idleBobAmplitude = 0.018f;
        [SerializeField] private float idleBobFrequency = 1.1f; // slow and gentle
        [SerializeField] private float celebrateBounceAmplitude = 0.06f;

        private SpriteRenderer _renderer;
        private Sprite _staticSprite;
        private Sprite _walkFallbackSprite;
        private IReadOnlyList<Sprite> _walkFrames = System.Array.Empty<Sprite>();
        private IReadOnlyList<Sprite> _idleFrames = System.Array.Empty<Sprite>();
        private IReadOnlyList<Sprite> _celebrateFrames = System.Array.Empty<Sprite>();

        private bool _isMoving;
        private float _celebrateRemaining;
        private float _frameClock;
        private float _bobClock;
        private Vector3 _baseScale = Vector3.one;
        private bool _baseScaleCaptured;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public AnimState CurrentState { get; private set; } = AnimState.Idle;
        public float FacingX { get; private set; } = 1f;
        public int CurrentFrameIndex { get; private set; }
        public bool HasWalkFrames => _walkFrames.Count > 1;
        public bool HasIdleFrames => _idleFrames.Count > 1;
        public bool HasCelebrateFrames => _celebrateFrames.Count > 0;
        public bool IsCelebrating => CurrentState == AnimState.Celebrate;
        public Sprite CurrentSprite => _renderer != null ? _renderer.sprite : null;

        /// <summary>
        /// Binds the renderer and loads the frame sets for a catalog sprite id
        /// (avatar.* or npc.*). Missing frames fall back to static sprites —
        /// never throws.
        /// </summary>
        public void Configure(SpriteRenderer renderer, string baseSpriteId)
        {
            _renderer = renderer;
            _staticSprite = AssetCatalog.SpriteFor(baseSpriteId);
            _walkFallbackSprite = AssetCatalog.SpriteFor(AssetCatalog.SpriteIdForLocomotion(baseSpriteId, true));
            _walkFrames = AssetCatalog.FrameSetFor(baseSpriteId, AssetCatalog.FrameStateWalk);
            _idleFrames = AssetCatalog.FrameSetFor(baseSpriteId, AssetCatalog.FrameStateIdle);
            _celebrateFrames = AssetCatalog.FrameSetFor(baseSpriteId, AssetCatalog.FrameStateCelebrate);

            _isMoving = false;
            _celebrateRemaining = 0f;
            _frameClock = 0f;
            _bobClock = 0f;
            CurrentFrameIndex = 0;
            CurrentState = AnimState.Idle;
            CaptureBaseScale();
            ApplyVisual();
        }

        /// <summary>Records the scale the host view authored as the rest pose.</summary>
        public void SetBaseScale(Vector3 baseScale)
        {
            _baseScale = baseScale;
            _baseScaleCaptured = true;
            ApplyVisual();
        }

        public void SetLocomotion(bool isMoving, float facingX)
        {
            if (Mathf.Abs(facingX) > 0.01f)
            {
                FacingX = Mathf.Sign(facingX);
            }

            if (_isMoving != isMoving)
            {
                _isMoving = isMoving;
                if (CurrentState != AnimState.Celebrate)
                {
                    EnterState(isMoving ? AnimState.Walk : AnimState.Idle);
                }
            }

            ApplyVisual();
        }

        /// <summary>P15: short arms-up/bounce loop, then back to idle/walk.</summary>
        public void TriggerCelebrate(float durationSeconds)
        {
            _celebrateRemaining = Mathf.Max(0.1f, durationSeconds);
            EnterState(AnimState.Celebrate);
            ApplyVisual();
        }

        /// <summary>Deterministic clock seam — tests fast-forward through here.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            _bobClock += deltaSeconds;

            if (CurrentState == AnimState.Celebrate)
            {
                _celebrateRemaining -= deltaSeconds;
                if (_celebrateRemaining <= 0f)
                {
                    _celebrateRemaining = 0f;
                    EnterState(_isMoving ? AnimState.Walk : AnimState.Idle);
                }
            }

            var frames = ActiveFrames();
            if (frames.Count > 1)
            {
                _frameClock += deltaSeconds * Mathf.Max(1f, framesPerSecond);
                if (_frameClock >= 1f)
                {
                    CurrentFrameIndex = (CurrentFrameIndex + (int)_frameClock) % frames.Count;
                    _frameClock -= Mathf.Floor(_frameClock);
                }
            }

            ApplyVisual();
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void EnterState(AnimState state)
        {
            CurrentState = state;
            CurrentFrameIndex = 0;
            _frameClock = 0f;
        }

        private IReadOnlyList<Sprite> ActiveFrames()
        {
            switch (CurrentState)
            {
                case AnimState.Walk:
                    return _walkFrames;
                case AnimState.Celebrate:
                    return _celebrateFrames;
                default:
                    return _idleFrames;
            }
        }

        private void ApplyVisual()
        {
            if (_renderer == null)
            {
                return;
            }

            CaptureBaseScale();

            var frames = ActiveFrames();
            // A single idle frame is the same pose as the static sprite —
            // idle only cycles when 2+ frames are curated.
            var useFrames = frames.Count > (CurrentState == AnimState.Idle ? 1 : 0);
            Sprite sprite;
            if (useFrames)
            {
                CurrentFrameIndex = Mathf.Clamp(CurrentFrameIndex, 0, frames.Count - 1);
                sprite = frames[CurrentFrameIndex];
            }
            else if (CurrentState == AnimState.Walk && _walkFallbackSprite != null)
            {
                sprite = _walkFallbackSprite;
            }
            else
            {
                sprite = _staticSprite;
            }

            if (sprite != null)
            {
                _renderer.sprite = sprite;
            }

            _renderer.flipX = FacingX < 0f;

            // Idle breathing bob / celebrate bounce as a scale pulse — position
            // is owned by movement code, so the animator never writes it.
            var scale = _baseScale;
            if (CurrentState == AnimState.Idle && frames.Count <= 1)
            {
                scale.y = _baseScale.y * (1f + idleBobAmplitude * Mathf.Sin(_bobClock * idleBobFrequency * 2f * Mathf.PI));
            }
            else if (CurrentState == AnimState.Celebrate)
            {
                var bounce = Mathf.Abs(Mathf.Sin(_bobClock * 2f * 2f * Mathf.PI));
                scale.y = _baseScale.y * (1f + celebrateBounceAmplitude * bounce);
            }

            _renderer.transform.localScale = scale;
        }

        private void CaptureBaseScale()
        {
            if (_baseScaleCaptured || _renderer == null)
            {
                return;
            }

            _baseScale = _renderer.transform.localScale;
            _baseScaleCaptured = true;
        }
    }
}
