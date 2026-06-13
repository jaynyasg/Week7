using System;
using System.Collections.Generic;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// U3 facade over the proven drag shell (DraggablePiece / DropZone /
    /// DragFeel): wires the pieces and zones a station seed needs from its
    /// <see cref="ToyPatternController"/>, and owns the teardown discipline —
    /// cancel the active drag, clear highlight pulses, drop event subscribers,
    /// and remove every transient toy object on route change.
    ///
    /// Also hosts the interaction helpers the three converted drag rooms each
    /// duplicated (drop-outcome handling, partner-hold rendering, anchor and
    /// world-size resolution) so room controllers and the U4 station surface
    /// share one implementation.
    /// </summary>
    public sealed class ToyInteractionKit
    {
        public const string DefaultPlayfieldName = "ToyStationPlayfield";
        public const int PieceSortingOrder = 330; // characters/props band
        public const int ZoneSortingOrder = 320;

        private static readonly Vector2 DefaultPieceWorldSize = new(0.9f, 0.9f);

        private readonly Dictionary<string, DraggablePiece> _pieces = new();
        private readonly Dictionary<string, DropZone> _zones = new();

        private Transform _root;
        private ToyPatternController _controller;
        private string _highlightedObjectId;

        public bool IsMounted => _root != null;
        public Transform Root => _root;
        public ToyPatternController Controller => _controller;

        /// <summary>The toy currently pulsing with the level-2 hint, or null.</summary>
        public string HighlightedObjectId => _highlightedObjectId;

        public IReadOnlyDictionary<string, DraggablePiece> Pieces => _pieces;
        public IReadOnlyDictionary<string, DropZone> Zones => _zones;

        /// <summary>
        /// Builds the station playfield: one DropZone per rule target, one
        /// draggable piece per interactable toy (chain toys plus reaction-role
        /// toys — meters render as zones only). Remount-safe: any previous
        /// mount tears down first. Position/sprite providers are optional;
        /// defaults lay the tray along the bottom and targets across the middle.
        /// </summary>
        public Transform Mount(
            Transform parent,
            ToyPatternController controller,
            IDragDropHost host,
            Func<string, Sprite> spriteFor = null,
            Func<int, Vector3> trayPositionFor = null,
            Func<int, Vector3> targetPositionFor = null)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            Teardown();
            _controller = controller;

            if (Application.isPlaying)
            {
                // EditMode rule/wiring tests run without the pointer shell —
                // the input raycaster and canvas only matter in play.
                DraggablePiece.EnsureInputShell();
            }

            var rootObject = new GameObject(DefaultPlayfieldName);
            if (parent != null)
            {
                rootObject.transform.SetParent(parent, false);
            }

            _root = rootObject.transform;

            var rules = controller.Rules;
            for (var i = 0; i < rules.TargetIds.Count; i++)
            {
                var targetId = rules.TargetIds[i];
                var zoneObject = new GameObject($"DropZone_{targetId}", typeof(BoxCollider2D), typeof(DropZone));
                zoneObject.transform.SetParent(_root, false);
                zoneObject.transform.localPosition = targetPositionFor != null
                    ? targetPositionFor(i)
                    : DefaultTargetPosition(i);
                zoneObject.GetComponent<BoxCollider2D>().size = new Vector2(1.2f, 1f);
                var zone = zoneObject.GetComponent<DropZone>();
                zone.Configure(targetId, ZoneSortingOrder);
                _zones[targetId] = zone;
            }

            var trayIndex = 0;
            foreach (var definition in rules.Objects)
            {
                if (definition.Role == PartyStationObjectRole.Meter)
                {
                    continue; // meters are adjusted through their zone, not dragged
                }

                var pieceObject = new GameObject($"Piece_{definition.ObjectId}", typeof(SpriteRenderer));
                pieceObject.transform.SetParent(_root, false);
                pieceObject.transform.localPosition = trayPositionFor != null
                    ? trayPositionFor(trayIndex)
                    : DefaultTrayPosition(trayIndex);
                trayIndex++;

                var renderer = pieceObject.GetComponent<SpriteRenderer>();
                renderer.sprite = spriteFor != null
                    ? spriteFor(definition.SpriteKey)
                    : AssetCatalog.SpriteFor(definition.SpriteKey);
                renderer.sortingOrder = PieceSortingOrder;
                ApplyWorldSize(pieceObject.transform, renderer.sprite, DefaultPieceWorldSize);

                pieceObject.AddComponent<BoxCollider2D>();
                pieceObject.AddComponent<DragFeel>();
                var draggable = pieceObject.AddComponent<DraggablePiece>();
                draggable.Configure(definition.ObjectId, host, pieceObject.transform.position);
                _pieces[definition.ObjectId] = draggable;
            }

            return _root;
        }

        /// <summary>Test/QA seam: the live piece object for a toy id (post-mount).</summary>
        public DraggablePiece PieceFor(string objectId)
        {
            return objectId != null && _pieces.TryGetValue(objectId, out var piece) ? piece : null;
        }

        /// <summary>Test/QA seam: the live drop zone for a target id (post-mount).</summary>
        public DropZone ZoneFor(string targetId)
        {
            return targetId != null && _zones.TryGetValue(targetId, out var zone) ? zone : null;
        }

        /// <summary>
        /// Accepted-toy lockdown: parks the piece on its expected target zone,
        /// optionally with the shared accept punch (play mode only — the punch
        /// spawns particles).
        /// </summary>
        public void LockAcceptedPiece(string objectId, bool celebrate, Color accentColor)
        {
            var piece = PieceFor(objectId);
            if (piece == null)
            {
                return;
            }

            if (string.Equals(_highlightedObjectId, objectId, StringComparison.Ordinal))
            {
                ClearHintHighlight();
            }

            var zone = _controller != null ? ZoneFor(_controller.Rules.ExpectedTargetFor(objectId)) : null;
            piece.LockAtPosition(zone != null ? zone.transform.position : piece.HomePosition);

            if (celebrate && Application.isPlaying)
            {
                var feel = piece.GetComponent<DragFeel>();
                if (feel != null)
                {
                    feel.PlayAcceptPunch(accentColor);
                }
            }
        }

        /// <summary>Fresh-attempt unlock: the toy returns home and is draggable again.</summary>
        public void UnlockPiece(string objectId)
        {
            var piece = PieceFor(objectId);
            if (piece != null)
            {
                piece.UnlockAtHome();
            }
        }

        /// <summary>Level-2 hint: a soft pulse on the toy the player should try next.</summary>
        public void SetHintHighlight(string objectId)
        {
            if (string.Equals(_highlightedObjectId, objectId, StringComparison.Ordinal))
            {
                return;
            }

            ClearHintHighlight();
            var piece = PieceFor(objectId);
            if (piece == null)
            {
                return;
            }

            ToyHintPulse.Show(piece.gameObject);
            _highlightedObjectId = objectId;
        }

        public void ClearHintHighlight()
        {
            if (_highlightedObjectId == null)
            {
                return;
            }

            var piece = PieceFor(_highlightedObjectId);
            if (piece != null)
            {
                ToyHintPulse.Clear(piece.gameObject);
            }

            _highlightedObjectId = null;
        }

        /// <summary>
        /// Route-change discipline: cancels the active drag, clears highlight
        /// pulses and ghost previews, drops controller event subscribers, and
        /// removes every transient toy object. Safe to call repeatedly.
        /// </summary>
        public void Teardown()
        {
            DraggablePiece.CancelActiveDrag();
            ClearHintHighlight();

            foreach (var zone in _zones.Values)
            {
                if (zone != null)
                {
                    zone.HideGhost();
                }
            }

            if (_controller != null)
            {
                _controller.Teardown();
                _controller = null;
            }

            if (_root != null)
            {
                DestroySafe(_root.gameObject);
                _root = null;
            }

            _pieces.Clear();
            _zones.Clear();
        }

        // ------------------------------------------------------------------
        // Shared interaction helpers (promoted from the three drag rooms)
        // ------------------------------------------------------------------

        /// <summary>
        /// The shared drop-outcome handling every drag surface used inline:
        /// accepted drops were already rendered by the accept path, pending
        /// multiplayer drops await the host response, and every reject snaps
        /// the piece home.
        /// </summary>
        public static void ApplyDropOutcome(DraggablePiece piece, DropSubmitResult result)
        {
            if (piece == null)
            {
                return;
            }

            switch (result)
            {
                case DropSubmitResult.Accepted:
                    // Visuals were applied by the accept path (local or network).
                    break;
                case DropSubmitResult.Pending:
                    piece.IsAwaitingResult = true;
                    break;
                default:
                    piece.SnapToHome();
                    break;
            }
        }

        /// <summary>
        /// P17 partner-hold rendering shared by every drag surface: clears the
        /// previous held piece's indicator when the held piece changed, shows
        /// the indicator on the new one, and returns the new held id (null
        /// clears). Never drag-position mirroring.
        /// </summary>
        public static string ApplyPartnerHold(
            IReadOnlyDictionary<string, DraggablePiece> pieces,
            string previousPieceId,
            string pieceId)
        {
            if (pieces == null)
            {
                return pieceId;
            }

            if (previousPieceId != null
                && !string.Equals(previousPieceId, pieceId, StringComparison.Ordinal)
                && pieces.TryGetValue(previousPieceId, out var previous)
                && previous != null)
            {
                PartnerHoldIndicator.Clear(previous.gameObject);
            }

            if (pieceId != null && pieces.TryGetValue(pieceId, out var piece) && piece != null)
            {
                PartnerHoldIndicator.Show(piece.gameObject);
            }

            return pieceId;
        }

        /// <summary>Named-anchor position lookup with a layout fallback (room pattern).</summary>
        public static Vector3 AnchorPosition(Transform worldRoot, string anchorName, Vector2 fallback)
        {
            if (worldRoot != null)
            {
                foreach (var child in worldRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == anchorName)
                    {
                        return child.position;
                    }
                }
            }

            return new Vector3(fallback.x, fallback.y, 0f);
        }

        /// <summary>Scales a sprite transform to a target world size (room pattern).</summary>
        public static void ApplyWorldSize(Transform target, Sprite sprite, Vector2 worldSize)
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

        private static Vector3 DefaultTrayPosition(int trayIndex)
        {
            return new Vector3(-3.2f + trayIndex * 1.4f, -2.4f, 0f);
        }

        private static Vector3 DefaultTargetPosition(int targetIndex)
        {
            return new Vector3(-3.2f + targetIndex * 1.7f, 0.6f, 0f);
        }

        /// <summary>EditMode-safe destroy (EditMode tests mount real hierarchies).</summary>
        internal static void DestroySafe(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }

    /// <summary>
    /// Hint-ladder highlight pulse: a soft Sky-blue glow behind the toy the
    /// level-2 hint points at. Same gentle door-pulse band (600–900 ms) and
    /// same show/clear discipline as <see cref="PartnerHoldIndicator"/>, but a
    /// distinct color so a hint never reads as a partner hold. Clearing hides
    /// the glow immediately (same-frame readable) and is EditMode-safe.
    ///
    /// Deterministic clock: Tick(deltaSeconds) drives the pulse; Update only
    /// forwards Time.deltaTime when AutoTick is on (house idiom).
    /// </summary>
    public class ToyHintPulse : MonoBehaviour
    {
        public const float PulseSeconds = 0.75f; // DESIGN door-pulse 600–900ms
        private const float MinAlpha = 0.2f;
        private const float MaxAlpha = 0.45f;

        private static readonly Color GlowSky = new(0.42f, 0.71f, 0.95f); // hint sky-blue

        private SpriteRenderer _glow;
        private float _elapsed;
        private bool _cleared;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public bool IsActive => !_cleared && _glow != null && _glow.gameObject.activeSelf;

        /// <summary>Attaches (or refreshes) the hint pulse on a toy piece.</summary>
        public static ToyHintPulse Show(GameObject target)
        {
            if (target == null)
            {
                return null;
            }

            ToyHintPulse pulse = null;
            foreach (var candidate in target.GetComponents<ToyHintPulse>())
            {
                if (!candidate._cleared)
                {
                    pulse = candidate;
                    break;
                }
            }

            if (pulse == null)
            {
                pulse = target.AddComponent<ToyHintPulse>();
            }

            pulse.EnsureGlow();
            return pulse;
        }

        /// <summary>Removes the hint pulse (accept/teardown/hint-recovery paths).</summary>
        public static void Clear(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            foreach (var pulse in target.GetComponents<ToyHintPulse>())
            {
                pulse.ClearSelf();
            }
        }

        /// <summary>Test/render seam: is the hint pulse visible on this toy?</summary>
        public static bool IsShownOn(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            foreach (var pulse in target.GetComponents<ToyHintPulse>())
            {
                if (pulse.IsActive)
                {
                    return true;
                }
            }

            return false;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || !IsActive)
            {
                return;
            }

            _elapsed += deltaSeconds;
            var wave = 0.5f + 0.5f * Mathf.Sin(_elapsed * (2f * Mathf.PI / PulseSeconds));
            var color = GlowSky;
            color.a = Mathf.Lerp(MinAlpha, MaxAlpha, wave);
            _glow.color = color;
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void ClearSelf()
        {
            if (_cleared)
            {
                return;
            }

            _cleared = true;
            if (_glow != null)
            {
                _glow.gameObject.SetActive(false); // same-frame invisible
                ToyInteractionKit.DestroySafe(_glow.gameObject);
            }

            if (Application.isPlaying)
            {
                Destroy(this);
            }
            else
            {
                DestroyImmediate(this);
            }
        }

        private void EnsureGlow()
        {
            if (_glow != null)
            {
                _glow.gameObject.SetActive(true);
                return;
            }

            var pieceRenderer = GetComponent<SpriteRenderer>();
            var glowObject = new GameObject("ToyHintGlow", typeof(SpriteRenderer));
            glowObject.transform.SetParent(transform, false);
            glowObject.transform.localPosition = Vector3.zero;

            _glow = glowObject.GetComponent<SpriteRenderer>();
            _glow.sprite = CampusWorldSprites.Circle;
            var startColor = GlowSky;
            startColor.a = MaxAlpha;
            _glow.color = startColor;
            _glow.sortingOrder = (pieceRenderer != null ? pieceRenderer.sortingOrder : ToyInteractionKit.PieceSortingOrder) - 1;

            var bounds = pieceRenderer != null && pieceRenderer.sprite != null
                ? pieceRenderer.sprite.bounds.size
                : Vector3.one;
            var size = Mathf.Max(Mathf.Max(bounds.x, bounds.y), 0.2f) * 1.45f;
            glowObject.transform.localScale = new Vector3(size, size, 1f);
        }
    }
}
