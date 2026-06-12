using System;
using System.Collections.Generic;
using UnityEngine;

namespace CareerQuest
{
    public enum RevealCinematicBeat
    {
        Idle,
        WaitingForLatch,
        CameraToStage,
        TokenTravel,
        LightSweep,
        UnlockBurst,
        Settle,
        Resolved
    }

    /// <summary>
    /// Everything the beat sequencer needs, captured at reveal render time.
    /// EarnedEntries may be shorter than EarnedCount on multiplayer clients
    /// (results live host-side; the count is the synced read model) — missing
    /// entries fall back to the generic reveal badge art.
    /// </summary>
    public sealed class RevealCinematicContext
    {
        public bool Unlocked;
        public int EarnedCount;
        public IReadOnlyList<CatalogEntry> EarnedEntries = Array.Empty<CatalogEntry>();
        public Transform WorldRoot;
        public CameraDirector Camera;
        public CameraShot StageShot = RevealStageLayout.FallbackStageShot;
        public CameraShot SettleShot = RevealStageLayout.SettleShot;

        /// <summary>Latch input A: the local stage prefab/fallback has mounted (room veil cleared).</summary>
        public Func<bool> IsStageMounted;

        /// <summary>
        /// Latch input B (multiplayer clients only): the host's reveal-start
        /// sync moment has been observed. The cinematic NEVER starts on the RPC
        /// alone — both latch inputs must be open (max of the two moments).
        /// </summary>
        public bool RequireRevealStartSync;
        public Func<bool> HasRevealStartSync;

        /// <summary>Fired exactly once when the sequence resolves (naturally or via skip).</summary>
        public Action OnResolved;
    }

    /// <summary>
    /// U7 in-world reveal beat sequencer on the house deterministic clock
    /// (Tick(deltaSeconds) + AutoTick, mirroring CeremonyController/CameraDirector).
    ///
    /// Unlocked beat timeline (from latch open; ~12s hard cap, Skip after 3s):
    ///   camera tween to stage (1.2s) → badge tokens travel ×N (0.9s each)
    ///   → light sweep (0.9s, DESIGN 700-1200ms) → unlock burst (ParticleSystem,
    ///   P1) + avatar celebrate (P15) + 0.8s hold → Resolved (result copy mounts).
    /// Locked branch: short settle shot (0.7s), earned tokens pre-placed on the
    /// slots, no Skip, no full cinematic.
    ///
    /// Skip acts per-client and fast-forwards world beats to end-state: tokens
    /// snapped to slots, tweens killed, camera at the final shot — never
    /// half-traveled visuals. The director never writes the camera directly;
    /// every move is a request to CameraDirector (P23).
    /// </summary>
    public class RevealCinematicDirector : MonoBehaviour
    {
        public const float SkipDelaySeconds = 3f;
        public const float MaxSeconds = 12f;
        public const float CameraTweenSeconds = 1.2f;
        public const float TokenTravelSeconds = 0.9f;  // DESIGN reveal motion 700-1200ms
        public const float LightSweepSeconds = 0.9f;   // DESIGN reveal motion 700-1200ms
        public const float UnlockHoldSeconds = 0.8f;
        public const float SettleSeconds = 0.7f;
        public const float CelebrateSeconds = 1.4f;

        private const int TokenSortingOrder = 345;     // characters/props band
        private const float SweepFinalRotationFactor = 0.38f;

        private static readonly Color BurstGold = new(0.953f, 0.769f, 0.357f);
        private static readonly Color BurstTeal = new(0.055f, 0.42f, 0.435f);

        private sealed class TokenBeat
        {
            public Transform Transform;
            public Vector3 From;
            public Vector3 To;
        }

        private sealed class GlowBeam
        {
            public SpriteRenderer Renderer;
            public float FromAlpha;
            public float FromRotationZ;
        }

        private RevealCinematicContext _context;
        private readonly List<TokenBeat> _tokens = new();
        private readonly List<GlowBeam> _beams = new();
        private SpriteRenderer _glowSpot;
        private Vector3[] _slotPositions = Array.Empty<Vector3>();
        private Transform _tokenLayer;
        private float _beatElapsed;
        private int _travelingTokenIndex;
        private bool _resolvedRaised;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public RevealCinematicBeat CurrentBeat { get; private set; } = RevealCinematicBeat.Idle;

        public bool IsRunning =>
            CurrentBeat != RevealCinematicBeat.Idle && CurrentBeat != RevealCinematicBeat.Resolved;

        public bool IsResolved => CurrentBeat == RevealCinematicBeat.Resolved;

        /// <summary>True once both latch inputs opened and beats began.</summary>
        public bool LatchOpened { get; private set; }

        /// <summary>Seconds since the latch opened (the cinematic clock; 0 while waiting).</summary>
        public float ElapsedSeconds { get; private set; }

        /// <summary>Skip is per-client, unlocked-branch only, available after 3s.</summary>
        public bool CanSkip =>
            _context != null
            && _context.Unlocked
            && LatchOpened
            && !IsResolved
            && ElapsedSeconds >= SkipDelaySeconds;

        public int SpawnedTokenCount => _tokens.Count;

        /// <summary>Test/QA seam: the live token transform for a slot index.</summary>
        public Transform TokenAt(int index)
        {
            return index >= 0 && index < _tokens.Count ? _tokens[index].Transform : null;
        }

        /// <summary>Test/QA seam: the resolved world slot position for an index.</summary>
        public Vector3 SlotWorldPosition(int index)
        {
            if (index >= 0 && index < _slotPositions.Length)
            {
                return _slotPositions[index];
            }

            var fallback = RevealStageLayout.SlotPosition(index);
            return new Vector3(fallback.x, fallback.y, 0f);
        }

        public void Begin(RevealCinematicContext context)
        {
            StopImmediate();
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _resolvedRaised = false;
            CurrentBeat = RevealCinematicBeat.WaitingForLatch;
        }

        /// <summary>Guarded skip — the UI button and the test seam share this path.</summary>
        public bool TrySkip()
        {
            if (!CanSkip)
            {
                return false;
            }

            FastForwardToEnd();
            return true;
        }

        /// <summary>
        /// Fast-forwards every world beat to its end-state: all tokens snapped
        /// to their slots, glow sweep at final values, camera snapped to the
        /// final shot, celebrate fired — then resolves. Also the deterministic
        /// end-state seam tests drive at arbitrary beat boundaries.
        /// </summary>
        public void FastForwardToEnd()
        {
            if (_context == null || IsResolved)
            {
                return;
            }

            if (!LatchOpened)
            {
                OpenLatch();
            }

            _context.Camera?.TweenToShot(FinalShot, 0f); // duration 0 snaps — no stranded tween

            if (_context.Unlocked)
            {
                EnsureAllTokensAtSlots();
                ApplyFinalGlowState();
                TriggerHeroCelebrate();
            }

            Resolve();
        }

        /// <summary>
        /// Teardown half of the single-exit contract: stops beats and drops the
        /// token layer without resolving. Camera restoration is owned by the
        /// caller (CareerRevealController.CancelCinematic → CameraDirector).
        /// </summary>
        public void StopImmediate()
        {
            if (_tokenLayer != null)
            {
                Destroy(_tokenLayer.gameObject);
            }

            _tokenLayer = null;
            _tokens.Clear();
            _beams.Clear();
            _glowSpot = null;
            _slotPositions = Array.Empty<Vector3>();
            _context = null;
            _beatElapsed = 0f;
            _travelingTokenIndex = 0;
            ElapsedSeconds = 0f;
            LatchOpened = false;
            _resolvedRaised = false;
            CurrentBeat = RevealCinematicBeat.Idle;
        }

        /// <summary>Deterministic clock seam — tests fast-forward through here.</summary>
        public void Tick(float deltaSeconds)
        {
            if (_context == null || CurrentBeat == RevealCinematicBeat.Idle || IsResolved)
            {
                return;
            }

            if (CurrentBeat == RevealCinematicBeat.WaitingForLatch)
            {
                // Latch = max(sync moment observed, local stage mounted) — never
                // the RPC alone, never an unmounted stage.
                var mounted = _context.IsStageMounted == null || _context.IsStageMounted();
                var synced = !_context.RequireRevealStartSync
                    || _context.HasRevealStartSync == null
                    || _context.HasRevealStartSync();
                if (mounted && synced)
                {
                    OpenLatch();
                }

                return;
            }

            if (deltaSeconds <= 0f)
            {
                return;
            }

            ElapsedSeconds += deltaSeconds;
            _beatElapsed += deltaSeconds;

            if (_context.Unlocked && ElapsedSeconds >= MaxSeconds)
            {
                FastForwardToEnd(); // hard pacing cap (~12s)
                return;
            }

            switch (CurrentBeat)
            {
                case RevealCinematicBeat.CameraToStage:
                    if (_beatElapsed >= CameraTweenSeconds)
                    {
                        BeginTokenTravel();
                    }

                    break;
                case RevealCinematicBeat.TokenTravel:
                    TickTokenTravel();
                    break;
                case RevealCinematicBeat.LightSweep:
                    TickLightSweep();
                    break;
                case RevealCinematicBeat.UnlockBurst:
                    if (_beatElapsed >= UnlockHoldSeconds)
                    {
                        Resolve();
                    }

                    break;
                case RevealCinematicBeat.Settle:
                    if (_beatElapsed >= SettleSeconds)
                    {
                        Resolve();
                    }

                    break;
            }
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            StopImmediate();
        }

        // ------------------------------------------------------------------
        // Beats
        // ------------------------------------------------------------------

        private CameraShot FinalShot =>
            _context != null && _context.Unlocked ? _context.StageShot : _context?.SettleShot ?? RevealStageLayout.SettleShot;

        private void OpenLatch()
        {
            LatchOpened = true;
            ElapsedSeconds = 0f;
            _beatElapsed = 0f;
            ResolveStageReferences();

            if (_context.Unlocked)
            {
                CurrentBeat = RevealCinematicBeat.CameraToStage;
                _context.Camera?.TweenToShot(_context.StageShot, CameraTweenSeconds);
            }
            else
            {
                // Locked: earned tokens sit on the slots immediately (earned/3
                // progress in-world), short settle shot, no full cinematic.
                PlaceTokensImmediate(Mathf.Clamp(_context.EarnedCount, 0, RevealStageLayout.SlotCount));
                CurrentBeat = RevealCinematicBeat.Settle;
                _context.Camera?.TweenToShot(_context.SettleShot, SettleSeconds);
            }
        }

        private void BeginTokenTravel()
        {
            _travelingTokenIndex = 0;
            _beatElapsed = 0f;

            var tokenTotal = Mathf.Clamp(_context.EarnedCount, 0, RevealStageLayout.SlotCount);
            if (tokenTotal <= 0)
            {
                BeginLightSweep();
                return;
            }

            CurrentBeat = RevealCinematicBeat.TokenTravel;
            SpawnToken(_travelingTokenIndex);
        }

        private void TickTokenTravel()
        {
            var token = _travelingTokenIndex < _tokens.Count ? _tokens[_travelingTokenIndex] : null;
            if (token == null || token.Transform == null)
            {
                AdvanceTokenOrSweep();
                return;
            }

            var t = Mathf.Clamp01(_beatElapsed / TokenTravelSeconds);
            var eased = 1f - (1f - t) * (1f - t) * (1f - t); // ease-out cubic
            token.Transform.position = Vector3.Lerp(token.From, token.To, eased);

            if (t >= 1f)
            {
                token.Transform.position = token.To;
                ParticlePoof.Burst(token.To, BurstGold, 8);
                AdvanceTokenOrSweep();
            }
        }

        private void AdvanceTokenOrSweep()
        {
            _travelingTokenIndex++;
            _beatElapsed = 0f;

            var tokenTotal = Mathf.Clamp(_context.EarnedCount, 0, RevealStageLayout.SlotCount);
            if (_travelingTokenIndex < tokenTotal)
            {
                SpawnToken(_travelingTokenIndex);
                return;
            }

            BeginLightSweep();
        }

        private void BeginLightSweep()
        {
            CurrentBeat = RevealCinematicBeat.LightSweep;
            _beatElapsed = 0f;
        }

        private void TickLightSweep()
        {
            var t = Mathf.Clamp01(_beatElapsed / LightSweepSeconds);
            var eased = 1f - (1f - t) * (1f - t);
            ApplyGlowSweep(eased);

            if (t >= 1f)
            {
                BeginUnlockBurst();
            }
        }

        private void BeginUnlockBurst()
        {
            CurrentBeat = RevealCinematicBeat.UnlockBurst;
            _beatElapsed = 0f;

            var center = new Vector3(RevealStageLayout.StageCenter.x, RevealStageLayout.StageCenter.y, 0f);
            ParticlePoof.Burst(center, BurstGold, 26);
            ParticlePoof.Burst(center + new Vector3(0f, 0.35f, 0f), BurstTeal, 16);
            TriggerHeroCelebrate();
        }

        private void Resolve()
        {
            CurrentBeat = RevealCinematicBeat.Resolved;
            if (_resolvedRaised)
            {
                return;
            }

            _resolvedRaised = true;
            _context?.OnResolved?.Invoke();
        }

        // ------------------------------------------------------------------
        // Stage pieces (all name-resolved and null-safe: a missing prefab or
        // anchor degrades to layout fallbacks, never throws)
        // ------------------------------------------------------------------

        private void ResolveStageReferences()
        {
            _slotPositions = new Vector3[RevealStageLayout.SlotCount];
            for (var i = 0; i < RevealStageLayout.SlotCount; i++)
            {
                var fallback = RevealStageLayout.SlotPosition(i);
                _slotPositions[i] = FindWorldPosition(RevealStageLayout.SlotAnchorPrefix + i, new Vector3(fallback.x, fallback.y, 0f));
            }

            _beams.Clear();
            CacheBeam(RevealStageLayout.GlowBeamLeftName);
            CacheBeam(RevealStageLayout.GlowBeamRightName);
            _glowSpot = FindRenderer(RevealStageLayout.GlowSpotName);
        }

        private void CacheBeam(string beamName)
        {
            var renderer = FindRenderer(beamName);
            if (renderer == null)
            {
                return;
            }

            _beams.Add(new GlowBeam
            {
                Renderer = renderer,
                FromAlpha = renderer.color.a,
                FromRotationZ = NormalizedZ(renderer.transform.localEulerAngles.z)
            });
        }

        private void ApplyGlowSweep(float t)
        {
            foreach (var beam in _beams)
            {
                if (beam.Renderer == null)
                {
                    continue;
                }

                var color = beam.Renderer.color;
                color.a = Mathf.Lerp(beam.FromAlpha, 1f, t);
                beam.Renderer.color = color;
                var z = Mathf.Lerp(beam.FromRotationZ, beam.FromRotationZ * SweepFinalRotationFactor, t);
                beam.Renderer.transform.localRotation = Quaternion.Euler(0f, 0f, z);
            }

            if (_glowSpot != null)
            {
                var spotColor = _glowSpot.color;
                spotColor.a = Mathf.Lerp(spotColor.a, 0.85f, t);
                _glowSpot.color = spotColor;
            }
        }

        private void ApplyFinalGlowState()
        {
            ApplyGlowSweep(1f);
        }

        private void TriggerHeroCelebrate()
        {
            var hero = FindHeroAvatar();
            if (hero != null)
            {
                hero.TriggerCelebrate(CelebrateSeconds); // P15
            }
        }

        private AvatarRuntimeView FindHeroAvatar()
        {
            if (_context?.WorldRoot == null)
            {
                return null;
            }

            foreach (var view in _context.WorldRoot.GetComponentsInChildren<AvatarRuntimeView>(true))
            {
                if (view.name == RevealStageLayout.HeroAvatarName)
                {
                    return view;
                }
            }

            return null;
        }

        private void PlaceTokensImmediate(int count)
        {
            for (var i = 0; i < count; i++)
            {
                SpawnToken(i);
                var token = _tokens[i];
                if (token.Transform != null)
                {
                    token.Transform.position = token.To;
                }
            }
        }

        private void EnsureAllTokensAtSlots()
        {
            var tokenTotal = Mathf.Clamp(_context.EarnedCount, 0, RevealStageLayout.SlotCount);
            for (var i = 0; i < tokenTotal; i++)
            {
                if (i >= _tokens.Count)
                {
                    SpawnToken(i);
                }

                var token = _tokens[i];
                if (token.Transform != null)
                {
                    token.Transform.position = token.To;
                }
            }
        }

        private void SpawnToken(int index)
        {
            while (_tokens.Count <= index)
            {
                _tokens.Add(new TokenBeat());
            }

            var token = _tokens[index];
            if (token.Transform != null)
            {
                return;
            }

            var layer = EnsureTokenLayer();
            if (layer == null)
            {
                return;
            }

            var entry = _context.EarnedEntries != null && index < _context.EarnedEntries.Count
                ? _context.EarnedEntries[index]
                : null;
            var sprite = AssetCatalog.SpriteFor(entry?.BadgeArtKey ?? "badge.reveal_ready");

            var tokenObject = new GameObject($"{RevealStageLayout.TokenNamePrefix}{index}", typeof(SpriteRenderer));
            tokenObject.transform.SetParent(layer, false);

            var renderer = tokenObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = TokenSortingOrder;
            ApplyWorldSize(tokenObject.transform, sprite, RevealStageLayout.TokenWorldSize);

            var spawn = RevealStageLayout.TokenSpawnPosition(index);
            token.From = new Vector3(spawn.x, spawn.y, 0f);
            token.To = SlotWorldPosition(index);
            token.Transform = tokenObject.transform;
            tokenObject.transform.position = token.From;
        }

        private Transform EnsureTokenLayer()
        {
            if (_tokenLayer != null)
            {
                return _tokenLayer;
            }

            if (_context?.WorldRoot == null)
            {
                return null;
            }

            var layer = new GameObject(RevealStageLayout.TokenLayerName).transform;
            layer.SetParent(_context.WorldRoot, false);
            _tokenLayer = layer;
            return layer;
        }

        private Vector3 FindWorldPosition(string childName, Vector3 fallback)
        {
            var found = FindTransform(childName);
            return found != null ? found.position : fallback;
        }

        private SpriteRenderer FindRenderer(string childName)
        {
            var found = FindTransform(childName);
            return found != null ? found.GetComponent<SpriteRenderer>() : null;
        }

        private Transform FindTransform(string childName)
        {
            if (_context?.WorldRoot == null)
            {
                return null;
            }

            foreach (var child in _context.WorldRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static void ApplyWorldSize(Transform target, Sprite sprite, Vector2 worldSize)
        {
            if (sprite == null)
            {
                return;
            }

            var bounds = sprite.bounds.size;
            var width = Mathf.Approximately(bounds.x, 0f) ? 1f : bounds.x;
            var height = Mathf.Approximately(bounds.y, 0f) ? 1f : bounds.y;
            target.localScale = new Vector3(worldSize.x / width, worldSize.y / height, 1f);
        }

        private static float NormalizedZ(float eulerZ)
        {
            return eulerZ > 180f ? eulerZ - 360f : eulerZ;
        }
    }
}
