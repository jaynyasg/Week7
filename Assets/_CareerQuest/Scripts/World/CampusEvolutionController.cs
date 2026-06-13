using System;
using System.Collections.Generic;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// R18 campus evolution + P19 arrival fanfare. Every campus entry mounts a
    /// city piece on the skyline for each earned badge (one fixed slot per
    /// activity, so the skyline grows deterministically and persists for the
    /// session). The FIRST time a piece appears in a session it celebrates:
    /// scale pop + particle sparkle + cue + a small CameraDirector nudge toward
    /// the piece and back. The fanfare memory lives on the app (session scope),
    /// so re-entering the hub re-mounts pieces silently — exactly one fanfare
    /// per piece per session, even if a fanfare is interrupted by a route
    /// change (membership is recorded when the fanfare is queued).
    ///
    /// Deterministic clock: Tick(deltaSeconds) advances the beat sequence;
    /// Update only forwards Time.deltaTime when AutoTick is on (house idiom,
    /// mirrors CameraDirector / DragFeel). Camera motion itself is requested
    /// from CameraDirector, which owns restoration (P23): the nudge returns to
    /// the route shot, then re-engages hub follow when a player is present.
    /// </summary>
    public class CampusEvolutionController : MonoBehaviour
    {
        public const string RootName = "CampusEvolution";

        public const float StartDelaySeconds = 0.7f;
        public const float NudgeSeconds = 0.5f;
        public const float HoldSeconds = 0.55f;
        public const float PopSeconds = 0.35f;
        public const float NudgeOrthographicSize = 3.3f;

        private sealed class PieceSlot
        {
            public string ActivityId;
            public string PropAssetId;
            public Vector2 Position;

            public PieceSlot(string activityId, string propAssetId, Vector2 position)
            {
                ActivityId = activityId;
                PropAssetId = propAssetId;
                Position = position;
            }
        }

        /// <summary>One fixed skyline slot per activity (far-hills band coordinates).</summary>
        private static readonly PieceSlot[] Layout =
        {
            new(CareerConfig.DesignBuildId, "prop.city_piece_studio", new Vector2(-2.3f, 1.12f)),
            new(CareerConfig.HealthHeroId, "prop.city_piece_clinic", new Vector2(-0.75f, 1.2f)),
            new(CareerConfig.LogicCourtId, "prop.city_piece_court", new Vector2(0.8f, 1.12f)),
            new(CareerQuestCatalog.AiLabId, "prop.city_piece_lab", new Vector2(-3.85f, 1.05f)),
            new(CareerQuestCatalog.MusicStudioId, "prop.city_piece_art_tower", new Vector2(2.35f, 1.18f)),
            new(CareerQuestCatalog.RoboticsGarageId, "prop.city_piece_garage", new Vector2(3.9f, 1.05f)),
            new(CareerQuestCatalog.CommunityKitchenId, "prop.city_piece_kitchen", new Vector2(5.0f, 1.12f)),
            // U1 Party Pack stations: pieces appear only once a best result
            // exists for the station id, so these slots are dormant until the
            // stations become playable (U4/U5/U10).
            new(CareerQuestCatalog.VetClinicId, "prop.city_piece_vet_clinic", new Vector2(-5.0f, 1.1f)),
            new(CareerQuestCatalog.GameStudioId, "prop.city_piece_game_studio", new Vector2(6.1f, 1.18f)),
            new(CareerQuestCatalog.WeatherLabId, "prop.city_piece_weather_lab", new Vector2(-6.15f, 1.05f)),
            new(CareerQuestCatalog.SpaceportId, "prop.city_piece_spaceport", new Vector2(7.2f, 1.2f)),
            new(CareerQuestCatalog.NewsroomId, "prop.city_piece_newsroom", new Vector2(-7.3f, 1.12f)),
            new(CareerQuestCatalog.GreenCityId, "prop.city_piece_green_city", new Vector2(8.35f, 1.08f))
        };

        /// <summary>Activity ids with a skyline evolution slot (U1 validation seam).</summary>
        public static IReadOnlyList<string> EvolutionActivityIds
        {
            get
            {
                var ids = new string[Layout.Length];
                for (var index = 0; index < Layout.Length; index++)
                {
                    ids[index] = Layout[index].ActivityId;
                }

                return ids;
            }
        }

        /// <summary>Resolves the skyline prop asset id for an activity/station id.</summary>
        public static bool TryGetEvolutionPropAssetId(string activityId, out string propAssetId)
        {
            propAssetId = null;
            if (string.IsNullOrEmpty(activityId))
            {
                return false;
            }

            foreach (var slot in Layout)
            {
                if (slot.ActivityId == activityId)
                {
                    propAssetId = slot.PropAssetId;
                    return true;
                }
            }

            return false;
        }

        private static readonly Vector2 PieceWorldSize = new(0.52f, 0.56f);

        private sealed class FanfareItem
        {
            public string ActivityId;
            public Transform Piece;
            public Vector3 TargetScale;
            public Color Accent;
        }

        private enum Phase
        {
            Idle,
            Delay,
            NudgeIn,
            Hold,
            NudgeOut
        }

        private readonly Dictionary<string, Transform> _pieces = new();
        private readonly Queue<FanfareItem> _pending = new();
        private FanfareItem _current;
        private Phase _phase = Phase.Idle;
        private float _phaseElapsed;

        private CameraDirector _camera;
        private Func<Transform> _followTargetProvider;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        /// <summary>True while the arrival beat sequence is running.</summary>
        public bool IsFanfareActive => _phase != Phase.Idle;

        /// <summary>Number of fanfares queued by this mount (new pieces this session).</summary>
        public int FanfaresQueuedThisMount { get; private set; }

        public int SpawnedPieceCount => _pieces.Count;

        public bool HasPiece(string activityId)
        {
            return !string.IsNullOrEmpty(activityId) && _pieces.ContainsKey(activityId);
        }

        public Transform PieceFor(string activityId)
        {
            return activityId != null && _pieces.TryGetValue(activityId, out var piece) ? piece : null;
        }

        /// <summary>
        /// Mounts the evolution layer into the freshly built hub world.
        /// <paramref name="fanfareMemory"/> is the session-scoped "already
        /// celebrated" set owned by the caller (CareerQuestApp).
        /// </summary>
        public static CampusEvolutionController Mount(
            Transform worldRoot,
            GameSession session,
            ISet<string> fanfareMemory,
            CameraDirector camera,
            Func<Transform> followTargetProvider)
        {
            if (worldRoot == null || session == null || fanfareMemory == null)
            {
                return null;
            }

            var rootObject = new GameObject(RootName);
            rootObject.transform.SetParent(ResolveSkylineParent(worldRoot), false);
            var controller = rootObject.AddComponent<CampusEvolutionController>();
            controller.Configure(session, fanfareMemory, camera, followTargetProvider);
            return controller;
        }

        /// <summary>
        /// Pieces ride the far-hills parallax band when the authored hub is
        /// mounted (a literal growing skyline); otherwise they sit at the world
        /// root so the fallback ground still shows session progress.
        /// </summary>
        private static Transform ResolveSkylineParent(Transform worldRoot)
        {
            var hub = worldRoot.Find("CampusHub");
            if (hub != null)
            {
                var band = hub.Find("Band_FarHills");
                if (band != null)
                {
                    return band;
                }
            }

            return worldRoot;
        }

        private void Configure(GameSession session, ISet<string> fanfareMemory, CameraDirector camera, Func<Transform> followTargetProvider)
        {
            _camera = camera;
            _followTargetProvider = followTargetProvider;

            var order = 48; // above the far-hills sprites (40-46), below the mid band (120+)
            foreach (var slot in Layout)
            {
                if (session.GetBestResult(slot.ActivityId) == null)
                {
                    continue;
                }

                var piece = SpawnPiece(slot, order++);
                _pieces[slot.ActivityId] = piece.transform;

                if (fanfareMemory.Contains(slot.ActivityId))
                {
                    continue;
                }

                // Membership is recorded at queue time so an interrupted
                // fanfare can never replay on the next hub entry.
                fanfareMemory.Add(slot.ActivityId);
                FanfaresQueuedThisMount++;

                var targetScale = piece.transform.localScale;
                piece.transform.localScale = Vector3.zero;
                _pending.Enqueue(new FanfareItem
                {
                    ActivityId = slot.ActivityId,
                    Piece = piece.transform,
                    TargetScale = targetScale,
                    Accent = AccentFor(slot.ActivityId)
                });
            }

            if (_pending.Count > 0)
            {
                _phase = Phase.Delay;
                _phaseElapsed = 0f;
            }
        }

        private GameObject SpawnPiece(PieceSlot slot, int sortingOrder)
        {
            var pieceObject = new GameObject($"CityPiece_{slot.ActivityId}", typeof(SpriteRenderer));
            pieceObject.transform.SetParent(transform, false);
            pieceObject.transform.localPosition = new Vector3(slot.Position.x, slot.Position.y, 0f);

            var renderer = pieceObject.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetCatalog.SpriteFor(slot.PropAssetId);
            renderer.sortingOrder = sortingOrder;

            var bounds = renderer.sprite != null ? renderer.sprite.bounds.size : Vector3.one;
            var width = Mathf.Approximately(bounds.x, 0f) ? 1f : bounds.x;
            var height = Mathf.Approximately(bounds.y, 0f) ? 1f : bounds.y;
            pieceObject.transform.localScale = new Vector3(PieceWorldSize.x / width, PieceWorldSize.y / height, 1f);
            return pieceObject;
        }

        private static Color AccentFor(string activityId)
        {
            var entry = CareerQuestCatalog.GetById(activityId);
            return AssetCatalog.TryGetDefinition(entry.BadgeArtKey, out var definition)
                ? definition.PrimaryColor
                : new Color(0.953f, 0.769f, 0.357f); // Path Gold
        }

        /// <summary>Deterministic clock seam — tests fast-forward through here.</summary>
        public void Tick(float deltaSeconds)
        {
            if (_phase == Phase.Idle || deltaSeconds <= 0f)
            {
                return;
            }

            _phaseElapsed += deltaSeconds;

            switch (_phase)
            {
                case Phase.Delay:
                    if (_phaseElapsed >= StartDelaySeconds)
                    {
                        BeginNextFanfare();
                    }

                    break;

                case Phase.NudgeIn:
                    UpdatePopScale();
                    if (_phaseElapsed >= NudgeSeconds)
                    {
                        _phase = Phase.Hold;
                        _phaseElapsed = 0f;
                    }

                    break;

                case Phase.Hold:
                    UpdatePopScale();
                    if (_phaseElapsed >= HoldSeconds)
                    {
                        FinishPop();
                        _camera?.TweenToShot(_camera.RouteShot, NudgeSeconds);
                        _phase = Phase.NudgeOut;
                        _phaseElapsed = 0f;
                    }

                    break;

                case Phase.NudgeOut:
                    if (_phaseElapsed >= NudgeSeconds)
                    {
                        _current = null;
                        if (_pending.Count > 0)
                        {
                            BeginNextFanfare();
                        }
                        else
                        {
                            CompleteSequence();
                        }
                    }

                    break;
            }
        }

        private void BeginNextFanfare()
        {
            _current = _pending.Dequeue();
            _phase = Phase.NudgeIn;
            _phaseElapsed = 0f;

            if (_current.Piece != null)
            {
                var position = _current.Piece.position;
                ParticlePoof.Burst(position, _current.Accent, 18);
                AudioCueCatalog.TryPlay(AudioCueIds.CityPiecePop);
                _camera?.TweenToShot(new CameraShot(new Vector3(position.x, position.y, -10f), NudgeOrthographicSize), NudgeSeconds);
            }
        }

        private void UpdatePopScale()
        {
            if (_current?.Piece == null)
            {
                return;
            }

            // Pop runs inside the nudge-in window (180-350ms per DESIGN motion).
            var popElapsed = _phase == Phase.NudgeIn ? _phaseElapsed : NudgeSeconds + _phaseElapsed;
            var t = Mathf.Clamp01(popElapsed / PopSeconds);
            _current.Piece.localScale = _current.TargetScale * EaseOutBack(t);
        }

        private void FinishPop()
        {
            if (_current?.Piece != null)
            {
                _current.Piece.localScale = _current.TargetScale;
            }
        }

        private void CompleteSequence()
        {
            _phase = Phase.Idle;
            _phaseElapsed = 0f;

            // Route restoration: the camera is back on the route shot; if the
            // hub player exists, hand the camera back to hub follow framing.
            var followTarget = _followTargetProvider?.Invoke();
            if (followTarget != null && _camera != null)
            {
                _camera.BeginFollow(followTarget, CameraFollowSettings.HubDefault);
            }
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            var x = t - 1f;
            return Mathf.Max(0f, 1f + c3 * x * x * x + c1 * x * x);
        }
    }
}
