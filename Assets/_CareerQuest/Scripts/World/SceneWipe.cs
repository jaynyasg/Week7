using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// P6 paper-wipe transition replacing the plain translucent veil sprite.
    /// World-space (composites with the world content the veil covers; no canvas
    /// reordering concerns). Two states:
    /// - Cover: an opaque warm-paper curtain over the whole shot. The covering
    ///   object keeps the legacy "RoomVeil" name so the veil contract
    ///   (IsRoomVeilActive + named object during the covered frame) is preserved.
    /// - Open: the curtain lifts (top edge pinned, eased) and destroys itself.
    ///   The opening tail is named "SceneWipeOpen", lives past the veil-clear
    ///   frame as a non-blocking visual, and is parented under the world root so
    ///   any ClearWorld removes it.
    /// Deterministic-friendly: Tick(deltaSeconds) seam with AutoTick.
    ///
    /// U9 (R19) reduced motion: the cover still mounts (the transition still
    /// reads — the room is briefly veiled, completion clarity preserved), but
    /// <see cref="BeginOpen"/> collapses the lift to a single tick so the
    /// swooshing curtain animation is suppressed in quiet-classroom mode. The
    /// gate is the static ClassroomAccessSettings.ReducedMotionActive (this
    /// object is built by a static factory with no session reference).
    /// </summary>
    public class SceneWipe : MonoBehaviour
    {
        public const string CoverName = "RoomVeil";
        public const string OpenName = "SceneWipeOpen";
        public const int WipeSortingOrder = 600; // foreground band (400+)

        private static readonly Color PaperColor = new(1f, 0.97f, 0.88f, 1f);
        private static readonly Color PaperEdgeColor = new(0.85f, 0.71f, 0.43f, 1f);

        private const float CoverWidth = 16f;
        private const float CoverHeight = 11f;

        private SpriteRenderer _renderer;
        private float _duration;
        private float _elapsed;
        private bool _opening;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public bool IsOpening => _opening;

        /// <summary>Creates a full-shot paper cover parented under the world root.</summary>
        public static SceneWipe CreateCover(Transform parent)
        {
            var cover = new GameObject(CoverName, typeof(SpriteRenderer), typeof(SceneWipe));
            cover.transform.SetParent(parent, false);
            cover.transform.localPosition = Vector3.zero;
            cover.transform.localScale = new Vector3(CoverWidth, CoverHeight, 1f);

            var renderer = cover.GetComponent<SpriteRenderer>();
            renderer.sprite = CampusWorldSprites.Square;
            renderer.color = PaperColor;
            renderer.sortingOrder = WipeSortingOrder;

            // Paper-shadow lip along the bottom edge so the curtain reads as a sheet.
            var lip = new GameObject("WipeEdge", typeof(SpriteRenderer));
            lip.transform.SetParent(cover.transform, false);
            lip.transform.localPosition = new Vector3(0f, -0.5f + 0.011f, 0f);
            lip.transform.localScale = new Vector3(1f, 0.022f, 1f);
            var lipRenderer = lip.GetComponent<SpriteRenderer>();
            lipRenderer.sprite = CampusWorldSprites.Square;
            lipRenderer.color = PaperEdgeColor;
            lipRenderer.sortingOrder = WipeSortingOrder + 1;

            return cover.GetComponent<SceneWipe>();
        }

        /// <summary>Starts the lift animation; the object destroys itself when done.</summary>
        public void BeginOpen(float durationSeconds)
        {
            gameObject.name = OpenName;
            // U9 reduced motion: collapse the lift to one tick — the cover frame
            // already happened (transition reads), the swoosh is what we drop.
            _duration = ClassroomAccessSettings.ReducedMotionActive
                ? 0.01f
                : Mathf.Max(0.01f, durationSeconds);
            _elapsed = 0f;
            _opening = true;
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
            if (!_opening)
            {
                return;
            }

            _elapsed += deltaSeconds;
            var t = Mathf.Clamp01(_elapsed / _duration);
            var eased = 1f - (1f - t) * (1f - t);

            // Curtain lift: shrink toward the top edge (top stays pinned).
            transform.localScale = new Vector3(CoverWidth, CoverHeight * (1f - eased), 1f);
            transform.localPosition = new Vector3(0f, CoverHeight * 0.5f * eased, 0f);

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
