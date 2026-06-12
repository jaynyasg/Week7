using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CareerQuest
{
    [RequireComponent(typeof(AvatarRuntimeView))]
    public class PlayerAvatarController : MonoBehaviour
    {
        /// <summary>
        /// P11 footstep cadence: one step cue per interval while walking (the
        /// gameplay tier adds ±8% pitch variation per step, so a single curated
        /// clip never machine-guns). First step lands immediately on move start.
        /// </summary>
        public const float FootstepIntervalSeconds = 0.34f;

        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private Vector2 minBounds = new(-5.25f, -2.45f);
        [SerializeField] private Vector2 maxBounds = new(5.25f, 0.55f);

        private IReadOnlyList<BuildingEntrance> _entrances = Array.Empty<BuildingEntrance>();
        private Action<ActivityRoute> _onDestination;
        private AvatarRuntimeView _avatarView;
        private float _footstepCountdown;

        private void Awake()
        {
            _avatarView = GetComponent<AvatarRuntimeView>();
        }

        public void Configure(GameSession session, IReadOnlyList<BuildingEntrance> entrances, Action<ActivityRoute> onDestination)
        {
            _avatarView ??= GetComponent<AvatarRuntimeView>();
            _avatarView.ApplyAvatar(session?.SelectedAvatar ?? AvatarConfig.DefaultAvatar);
            _entrances = entrances ?? Array.Empty<BuildingEntrance>();
            _onDestination = onDestination;

            // Walk clamp from the single anchor truth (WorldAnchors); the
            // serialized defaults stay only as an editor-visible mirror.
            var bounds = WorldAnchors.ActiveWalkBounds;
            minBounds = bounds.min;
            maxBounds = bounds.max;
        }

        private void Update()
        {
            var move = ReadMove();
            if (move.sqrMagnitude > 0f)
            {
                Move(move * moveSpeed * Time.deltaTime);
                _avatarView.SetLocomotion(true, move.x);
                TickFootsteps(Time.deltaTime);
            }
            else
            {
                // Facing is flipX-based now (U5); passing 0 keeps the last facing.
                _avatarView.SetLocomotion(false, 0f);
                _footstepCountdown = 0f; // next move starts on a fresh step
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
            {
                TryEnterNearest();
            }

            if (Input.GetMouseButtonDown(0) && !IsPointerOverEventSystemTarget())
            {
                var camera = CameraDirector.Ensure().Camera;
                if (camera != null)
                {
                    var world = camera.ScreenToWorldPoint(Input.mousePosition);
                    TryEnterAt(world);
                }
            }
        }

        public void Move(Vector2 delta)
        {
            var next = (Vector2)transform.position + delta;
            next.x = Mathf.Clamp(next.x, minBounds.x, maxBounds.x);
            next.y = Mathf.Clamp(next.y, minBounds.y, maxBounds.y);
            transform.position = new Vector3(next.x, next.y, 0f);
        }

        public bool TryEnterNearest()
        {
            var entrance = NearestEntrance(transform.position);
            if (entrance == null || !entrance.Contains(transform.position))
            {
                return false;
            }

            EnterRoute(entrance.Route);
            return true;
        }

        public bool TryEnterAt(Vector2 worldPosition)
        {
            var entrance = _entrances.FirstOrDefault(candidate => candidate.Contains(worldPosition));
            if (entrance == null)
            {
                return false;
            }

            EnterRoute(entrance.Route);
            return true;
        }

        /// <summary>U8: every door entry (key or click) shares the door cue.</summary>
        private void EnterRoute(ActivityRoute route)
        {
            AudioCueCatalog.TryPlay(AudioCueIds.DoorEnter);
            _onDestination?.Invoke(route);
        }

        /// <summary>P11 cadence — only ticks while the walk state is active.</summary>
        private void TickFootsteps(float deltaSeconds)
        {
            _footstepCountdown -= deltaSeconds;
            if (_footstepCountdown > 0f)
            {
                return;
            }

            _footstepCountdown = FootstepIntervalSeconds;
            AudioCueCatalog.TryPlay(AudioCueIds.Footstep);
        }

        private BuildingEntrance NearestEntrance(Vector2 worldPosition)
        {
            return _entrances
                .OrderBy(entrance => Vector2.Distance(entrance.transform.position, worldPosition))
                .FirstOrDefault();
        }

        /// <summary>
        /// Pointer-over guard (U6): once the Physics2DRaycaster joins the
        /// EventSystem, a click on a UI button, hub toy, or drag piece must not
        /// ALSO fire click-to-enter. IsPointerOverGameObject covers every
        /// registered raycaster (uGUI GraphicRaycaster and Physics2DRaycaster).
        /// </summary>
        private static bool IsPointerOverEventSystemTarget()
        {
            var eventSystem = EventSystem.current;
            return eventSystem != null && eventSystem.IsPointerOverGameObject();
        }

        private static Vector2 ReadMove()
        {
            var move = Vector2.zero;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                move.x -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                move.x += 1f;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                move.y -= 1f;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                move.y += 1f;
            }

            return move.sqrMagnitude > 1f ? move.normalized : move;
        }
    }
}
