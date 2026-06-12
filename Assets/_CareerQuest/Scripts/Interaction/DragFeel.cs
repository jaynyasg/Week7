using System;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Drag game-feel per DESIGN.md motion rules, framework-level so every drag
    /// room shares it:
    /// - Pickup: pop to ~1.08 scale over 180-250ms, +100 sortingOrder lift into
    ///   the foreground band, soft drop shadow.
    /// - Snap-back (invalid / no zone): 0.15-0.25s ease-out tween home.
    /// - Accept: scale punch (250-400ms) plus a Unity ParticleSystem poof (P1 —
    ///   no hand-rolled confetti).
    ///
    /// Deterministic clock: Tick(deltaSeconds) advances all tweens; Update only
    /// forwards Time.deltaTime when AutoTick is on (house idiom, mirrors
    /// CameraDirector / SpriteFrameAnimator).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DragFeel : MonoBehaviour
    {
        public const float LiftScale = 1.08f;
        public const int LiftSortingBoost = 100;
        public const float LiftPopSeconds = 0.2f;     // tool pickup 180-250ms
        public const float SnapBackSeconds = 0.2f;    // invalid 150-250ms ease-out
        public const float AcceptPunchSeconds = 0.3f; // correct action 250-400ms

        private enum FeelPhase
        {
            None,
            LiftPop,
            SnapBack,
            AcceptPunch
        }

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public bool IsAnimating => _phase != FeelPhase.None;
        public bool IsLifted { get; private set; }

        private SpriteRenderer _renderer;
        private SpriteRenderer _shadow;
        private Vector3 _baseScale = Vector3.one;
        private int _baseOrder;
        private bool _baseCaptured;

        private FeelPhase _phase = FeelPhase.None;
        private float _phaseElapsed;
        private Vector3 _snapFrom;
        private Vector3 _snapTo;
        private Action _onSnapComplete;

        public void BeginLift()
        {
            EnsureBase();
            if (!IsLifted)
            {
                _baseOrder = _renderer.sortingOrder;
                _renderer.sortingOrder = _baseOrder + LiftSortingBoost;
                IsLifted = true;
            }

            EnsureShadow();
            _shadow.gameObject.SetActive(true);
            _phase = FeelPhase.LiftPop;
            _phaseElapsed = 0f;
        }

        public void EndLift()
        {
            EnsureBase();
            if (IsLifted)
            {
                _renderer.sortingOrder = _baseOrder;
                IsLifted = false;
            }

            if (_shadow != null)
            {
                _shadow.gameObject.SetActive(false);
            }

            transform.localScale = _baseScale;
            if (_phase == FeelPhase.LiftPop)
            {
                _phase = FeelPhase.None;
            }
        }

        /// <summary>0.15-0.25s ease-out tween back to the tray.</summary>
        public void SnapBack(Vector3 home, Action onComplete)
        {
            EnsureBase();
            EndLift();
            _snapFrom = transform.position;
            _snapTo = home;
            _onSnapComplete = onComplete;
            _phase = FeelPhase.SnapBack;
            _phaseElapsed = 0f;
        }

        /// <summary>Accept punch + particle poof (P1: real ParticleSystem).</summary>
        public void PlayAcceptPunch(Color accentColor)
        {
            EnsureBase();
            EndLift();
            _phase = FeelPhase.AcceptPunch;
            _phaseElapsed = 0f;
            ParticlePoof.Burst(transform.position, accentColor);
        }

        /// <summary>Teardown-safe: stops all tweens and restores the rest pose.</summary>
        public void CancelImmediate()
        {
            _phase = FeelPhase.None;
            _onSnapComplete = null;
            EndLift();
        }

        /// <summary>Deterministic clock seam — tests fast-forward through here.</summary>
        public void Tick(float deltaSeconds)
        {
            if (_phase == FeelPhase.None || deltaSeconds <= 0f)
            {
                return;
            }

            _phaseElapsed += deltaSeconds;

            switch (_phase)
            {
                case FeelPhase.LiftPop:
                {
                    var t = Mathf.Clamp01(_phaseElapsed / LiftPopSeconds);
                    var eased = EaseOutQuad(t);
                    transform.localScale = _baseScale * Mathf.Lerp(1f, LiftScale, eased);
                    if (t >= 1f)
                    {
                        _phase = FeelPhase.None;
                    }

                    break;
                }
                case FeelPhase.SnapBack:
                {
                    var t = Mathf.Clamp01(_phaseElapsed / SnapBackSeconds);
                    var eased = EaseOutQuad(t);
                    transform.position = Vector3.Lerp(_snapFrom, _snapTo, eased);
                    if (t >= 1f)
                    {
                        transform.position = _snapTo;
                        _phase = FeelPhase.None;
                        var done = _onSnapComplete;
                        _onSnapComplete = null;
                        done?.Invoke();
                    }

                    break;
                }
                case FeelPhase.AcceptPunch:
                {
                    var t = Mathf.Clamp01(_phaseElapsed / AcceptPunchSeconds);
                    var punch = Mathf.Sin(t * Mathf.PI) * 0.15f;
                    transform.localScale = _baseScale * (1f + punch);
                    if (t >= 1f)
                    {
                        transform.localScale = _baseScale;
                        _phase = FeelPhase.None;
                    }

                    break;
                }
            }
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void EnsureBase()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }

            if (!_baseCaptured)
            {
                _baseScale = transform.localScale;
                _baseOrder = _renderer != null ? _renderer.sortingOrder : 0;
                _baseCaptured = true;
            }
        }

        private void EnsureShadow()
        {
            if (_shadow != null)
            {
                return;
            }

            var shadowObject = new GameObject("DragShadow", typeof(SpriteRenderer));
            shadowObject.transform.SetParent(transform, false);
            shadowObject.transform.localPosition = new Vector3(0.05f, -0.22f, 0f);
            shadowObject.transform.localScale = new Vector3(0.9f, 0.35f, 1f);
            _shadow = shadowObject.GetComponent<SpriteRenderer>();
            _shadow.sprite = CampusWorldSprites.Circle;
            _shadow.color = new Color(0.05f, 0.07f, 0.09f, 0.22f);
            _shadow.sortingOrder = (_renderer != null ? _renderer.sortingOrder : 0) - 1;
            shadowObject.SetActive(false);
        }

        private static float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }

    /// <summary>
    /// P1: celebration poofs come from Unity's ParticleSystem — never hand-rolled
    /// confetti sprites. Bursts self-destroy.
    /// </summary>
    public static class ParticlePoof
    {
        public static void Burst(Vector3 position, Color color, int count = 14)
        {
            var poofObject = new GameObject("AcceptPoof", typeof(ParticleSystem));
            poofObject.transform.position = position;

            var system = poofObject.GetComponent<ParticleSystem>();
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 2.1f;
            main.startSize = 0.14f;
            main.startColor = color;
            main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;

            var renderer = poofObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = PoofMaterial();
            renderer.sortingOrder = 460; // foreground band 400+

            system.Play();
            UnityEngine.Object.Destroy(poofObject, 1.4f);
        }

        /// <summary>
        /// P1 ceremony confetti: a celebratory two-color ParticleSystem shower
        /// (upward cone, gravity pull-down). World-space, self-destroying;
        /// returns the object so the ceremony teardown can drop it early.
        /// </summary>
        public static GameObject ConfettiBurst(Vector3 position, Color colorA, Color colorB, int count = 48)
        {
            var confettiObject = new GameObject("CeremonyConfetti", typeof(ParticleSystem));
            confettiObject.transform.position = position;

            var system = confettiObject.GetComponent<ParticleSystem>();
            var main = system.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.6f, 4.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = 0.65f;
            main.maxParticles = 96;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 38f;
            shape.radius = 0.25f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // cone fires upward

            var renderer = confettiObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = PoofMaterial();
            // Above every world band including the scene wipe (600). The ceremony
            // overlay UI sits above world rendering but its full-screen backdrop is
            // alpha-clamped translucent (UiBuilder.FullPanel), so the burst reads
            // through it around the ceremony card.
            renderer.sortingOrder = 650;

            system.Play();
            UnityEngine.Object.Destroy(confettiObject, 3f);
            return confettiObject;
        }

        private static Material _poofMaterial;

        private static Material PoofMaterial()
        {
            if (_poofMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                _poofMaterial = new Material(shader) { mainTexture = Texture2D.whiteTexture };
            }

            return _poofMaterial;
        }
    }
}
