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

        /// <summary>
        /// U2 walk-into-door entry: the avatar must stay inside an entrance
        /// circle this long before the door opens — a one-frame edge brush
        /// never fires. Tunable; the locked design specifies roughly
        /// 0.15-0.25s so entry feels effortless without firing on a brush.
        /// </summary>
        public const float AutoEntryDwellSeconds = 0.35f;

        /// <summary>
        /// U2 return-to-campus grace: applied every time the hub avatar is
        /// (re)configured, so coming back from a room cannot instantly re-enter
        /// a door the player happens to spawn near.
        /// </summary>
        public const float ReturnToCampusGraceSeconds = 0.5f;

        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private Vector2 minBounds = new(-5.25f, -2.45f);
        [SerializeField] private Vector2 maxBounds = new(5.25f, 0.55f);

        private IReadOnlyList<BuildingEntrance> _entrances = Array.Empty<BuildingEntrance>();
        private Action<BuildingEntrance> _onDestination;
        private AvatarRuntimeView _avatarView;
        private float _footstepCountdown;
        private BuildingEntrance _pendingEntrance;
        private float _dwellElapsed;
        private float _graceRemaining;
        private bool _entryLatched;
        // Design-review (2026-06-16): true on frames the player presses a move
        // direction. Dwell only accrues when this is false, so walking through a
        // door circle never auto-enters it — you stop on the mat to go in.
        private bool _movingThisFrame;

        /// <summary>Real-time auto-entry clock toggle. Tests set false and drive TickAutoEntry directly.</summary>
        public bool AutoEntryAutoTick { get; set; } = true;

        /// <summary>The entrance the avatar is standing in (highlighted; opens after dwell).</summary>
        public BuildingEntrance PendingEntrance => _pendingEntrance;

        /// <summary>Seconds spent inside the pending entrance so far.</summary>
        public float DwellElapsedSeconds => _dwellElapsed;

        /// <summary>
        /// U2 route cooldown: once any entry fires, every further entry on this
        /// avatar is ignored until the hub remounts (a fresh avatar + the
        /// return grace) — double-entry while the new route mounts is impossible.
        /// </summary>
        public bool IsEntryLatched => _entryLatched;

        private void Awake()
        {
            _avatarView = GetComponent<AvatarRuntimeView>();
        }

        public void Configure(GameSession session, IReadOnlyList<BuildingEntrance> entrances, Action<BuildingEntrance> onDestination)
        {
            _avatarView ??= GetComponent<AvatarRuntimeView>();
            _avatarView.ApplyAvatar(session?.SelectedAvatar ?? AvatarConfig.DefaultAvatar);

            // U6: the local hub avatar wears its earned accessories in campus
            // play (campus context = not ceremony). Derived from the session
            // read model; follows this avatar's transform/flip for free.
            _avatarView.BindAccessories(session, ceremonyContext: false);

            _entrances = entrances ?? Array.Empty<BuildingEntrance>();
            _onDestination = onDestination;
            _entryLatched = false;
            _dwellElapsed = 0f;
            _graceRemaining = ReturnToCampusGraceSeconds;

            // Walk clamp from the single anchor truth (WorldAnchors); the
            // serialized defaults stay only as an editor-visible mirror.
            var bounds = WorldAnchors.ActiveWalkBounds;
            minBounds = bounds.min;
            maxBounds = bounds.max;
        }

        /// <summary>
        /// Test seam: re-arm the auto-entry clock to a fresh state (full return
        /// grace, no dwell, unlatched, no pending door). Deterministic-clock tests
        /// call this right after toggling <see cref="AutoEntryAutoTick"/> off so
        /// they don't inherit real-time grace/dwell accrual from the mount frames
        /// (which scales with scene-load cost and would otherwise make the dwell
        /// assertions timing-dependent).
        /// </summary>
        public void ResetAutoEntryClock()
        {
            _entryLatched = false;
            _dwellElapsed = 0f;
            _graceRemaining = ReturnToCampusGraceSeconds;
            SetPendingEntrance(null);
        }

        private void Update()
        {
            var move = ReadMove();
            _movingThisFrame = move.sqrMagnitude > 0f;
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

            if (AutoEntryAutoTick)
            {
                TickAutoEntry(Time.deltaTime);
            }

            // Legacy keys stay as a convenience — auto-entry means they are
            // never required (campus copy no longer mentions them).
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

        /// <summary>
        /// U2 walk-into-door clock. Highlights the entrance the avatar stands
        /// in immediately (so kids see which station will open), accrues dwell
        /// only after the return grace, resets the dwell on exit, and fires the
        /// entry exactly once per hub mount (latch).
        /// </summary>
        public void TickAutoEntry(float deltaSeconds)
        {
            if (_entryLatched || deltaSeconds <= 0f)
            {
                return;
            }

            // Highlight tracks the standing-in entrance even during grace.
            var inside = EntranceContaining(transform.position);
            if (inside != _pendingEntrance)
            {
                SetPendingEntrance(inside);
            }

            // Grace consumes its share of the tick first — a tick never counts
            // toward grace AND dwell at the same time.
            if (_graceRemaining > 0f)
            {
                var consumed = Mathf.Min(_graceRemaining, deltaSeconds);
                _graceRemaining -= consumed;
                deltaSeconds -= consumed;
            }

            if (_pendingEntrance == null || deltaSeconds <= 0f)
            {
                return;
            }

            // Design-review (2026-06-16): only dwell while standing still. The
            // ~1.0-wide trigger circle takes longer to walk across than the dwell,
            // so a straight pass-by used to open whichever door you brushed - often
            // not the one you aimed at. Stop on the glowing mat to enter.
            if (_movingThisFrame)
            {
                _dwellElapsed = 0f;
                return;
            }

            _dwellElapsed += deltaSeconds;
            if (_dwellElapsed < AutoEntryDwellSeconds)
            {
                return;
            }

            var entrance = _pendingEntrance;
            SetPendingEntrance(null);
            EnterEntrance(entrance);
        }

        public bool TryEnterNearest()
        {
            var entrance = NearestEntrance(transform.position);
            if (entrance == null || !entrance.Contains(transform.position))
            {
                return false;
            }

            return EnterEntrance(entrance);
        }

        public bool TryEnterAt(Vector2 worldPosition)
        {
            var entrance = _entrances.FirstOrDefault(candidate => candidate.Contains(worldPosition));
            if (entrance == null)
            {
                return false;
            }

            return EnterEntrance(entrance);
        }

        /// <summary>
        /// U8/U2: every door entry (dwell, click, or convenience key) shares
        /// the door cue and the entry latch (route cooldown while mounting).
        /// </summary>
        private bool EnterEntrance(BuildingEntrance entrance)
        {
            if (_entryLatched || entrance == null)
            {
                return false;
            }

            _entryLatched = true;
            SetPendingEntrance(null);
            AudioCueCatalog.TryPlay(AudioCueIds.DoorEnter);
            _onDestination?.Invoke(entrance);
            return true;
        }

        /// <summary>Swaps the highlighted entrance and resets the dwell clock.</summary>
        private void SetPendingEntrance(BuildingEntrance entrance)
        {
            if (_pendingEntrance == entrance)
            {
                return;
            }

            SetEntranceHighlight(_pendingEntrance, false);
            _pendingEntrance = entrance;
            _dwellElapsed = 0f;
            SetEntranceHighlight(_pendingEntrance, true);
        }

        /// <summary>Nearby highlight: the entrance's DoorSign pulses while the avatar stands inside.</summary>
        private static void SetEntranceHighlight(BuildingEntrance entrance, bool active)
        {
            if (entrance == null)
            {
                return;
            }

            var sign = entrance.GetComponent<DoorSign>();
            if (sign != null)
            {
                sign.SetPulsing(active);
            }
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

        private BuildingEntrance EntranceContaining(Vector2 worldPosition)
        {
            // Entrance circles are non-overlap validated (WorldAnchors), so the
            // first containing entrance is the only containing entrance.
            return _entrances.FirstOrDefault(entrance => entrance != null && entrance.Contains(worldPosition));
        }

        private BuildingEntrance NearestEntrance(Vector2 worldPosition)
        {
            return _entrances
                .OrderBy(entrance => Vector2.Distance(entrance.transform.position, worldPosition))
                .FirstOrDefault();
        }

        private void OnDestroy()
        {
            // Hub teardown mid-dwell: release the door highlight cleanly.
            SetEntranceHighlight(_pendingEntrance, false);
            _pendingEntrance = null;
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


