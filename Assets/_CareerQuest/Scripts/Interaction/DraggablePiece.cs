using UnityEngine;
using UnityEngine.EventSystems;

namespace CareerQuest
{
    /// <summary>
    /// Room-side decisions for a drag shell. Implemented by room controllers
    /// (Design Build in U6; Health Hero / Logic Court replicate in U10) and by
    /// the Party Pack station surface over <see cref="ToyInteractionKit"/> (U3),
    /// so the pointer shell stays generic and ALL game logic flows through the
    /// host's programmatic seams.
    /// </summary>
    public interface IDragDropHost
    {
        bool CanBeginDrag(string pieceId);

        /// <summary>P17 groundwork: pickup raises the held-piece flag.</summary>
        void NotifyPickUp(string pieceId);

        /// <summary>Drop/cancel clears the held-piece flag.</summary>
        void NotifyRelease(string pieceId);

        /// <summary>Drop resolution. zone is null when released over no zone.</summary>
        void HandleDrop(DraggablePiece piece, DropZone zone);

        /// <summary>Ghost-preview validity (P12) — paint-time only, never authoritative.</summary>
        bool WouldAcceptDrop(string pieceId, string zoneId);
    }

    /// <summary>
    /// Generic 2D drag shell: Collider2D + IBeginDrag/IDrag/IEndDrag handlers
    /// raycast by the single Physics2DRaycaster on CameraDirector's CameraHost.
    /// The pointer handlers are thin wrappers over the programmatic seam
    /// (BeginDragProgrammatic / DragTo / EndDragAt) so tests drive the exact
    /// same code path without synthetic pointer events.
    ///
    /// Safety: the dragged piece's collider disables during the drag (so drop
    /// raycasts hit zones, not itself); a single static ActiveDrag is cancelled
    /// by world teardown (<see cref="CancelActiveDrag"/>) and by OnDisable /
    /// OnDestroy — disconnect mid-drag never throws or leaves an orphan sprite.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DraggablePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static DraggablePiece ActiveDrag { get; private set; }

        public string PieceId { get; private set; }
        public Vector3 HomePosition { get; private set; }
        public bool IsDragging { get; private set; }

        /// <summary>True while a multiplayer submission is pending host response.</summary>
        public bool IsAwaitingResult { get; set; }

        private IDragDropHost _host;
        private Collider2D _collider;
        private DragFeel _feel;
        private DropZone _hoverZone;

        /// <summary>
        /// Framework-level input shell: ensures the EventSystem (via UiBuilder's
        /// canvas bootstrap) and attaches the single Physics2DRaycaster to
        /// CameraDirector's CameraHost — the documented attach point.
        /// </summary>
        public static void EnsureInputShell()
        {
            UiBuilder.EnsureCanvas();
            var host = CameraDirector.Ensure().CameraHost;
            if (host.GetComponent<Physics2DRaycaster>() == null)
            {
                host.AddComponent<Physics2DRaycaster>();
            }
        }

        /// <summary>Cancels whichever drag is active (world-clear teardown hook).</summary>
        public static void CancelActiveDrag()
        {
            var active = ActiveDrag;
            ActiveDrag = null;
            if (active != null)
            {
                active.CancelDrag();
            }
        }

        public void Configure(string pieceId, IDragDropHost host, Vector3 homePosition)
        {
            PieceId = pieceId;
            _host = host;
            HomePosition = homePosition;
            _collider = GetComponent<Collider2D>();
            _feel = GetComponent<DragFeel>();
        }

        public bool CanDrag => _host != null && !IsAwaitingResult && _host.CanBeginDrag(PieceId);

        // ---- Pointer shell (thin wrappers over the programmatic seam) ----

        public void OnBeginDrag(PointerEventData eventData)
        {
            BeginDragProgrammatic();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging)
            {
                return;
            }

            DragTo(ScreenToWorld(eventData));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDragging)
            {
                return;
            }

            EndDragAt(transform.position);
        }

        // ---- Programmatic seam (tests and pointer shell share this path) ----

        public bool BeginDragProgrammatic()
        {
            if (IsDragging || !CanDrag)
            {
                return false;
            }

            CancelActiveDrag();
            ActiveDrag = this;
            IsDragging = true;

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            if (_feel != null)
            {
                _feel.BeginLift();
            }

            _host?.NotifyPickUp(PieceId);
            return true;
        }

        public void DragTo(Vector3 worldPosition)
        {
            if (!IsDragging)
            {
                return;
            }

            transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
            UpdateGhostPreview();
        }

        public void EndDragAt(Vector3 worldPosition)
        {
            if (!IsDragging)
            {
                return;
            }

            IsDragging = false;
            if (ActiveDrag == this)
            {
                ActiveDrag = null;
            }

            ClearGhost();

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            if (_feel != null)
            {
                _feel.EndLift();
            }

            var host = _host;
            if (host == null)
            {
                SnapToHome();
                return;
            }

            var zone = DropZone.FindAt(worldPosition);
            host.HandleDrop(this, zone);
            host.NotifyRelease(PieceId);
        }

        /// <summary>
        /// Teardown-safe cancel: restores state without drop resolution. Safe to
        /// call on a piece that is being destroyed.
        /// </summary>
        public void CancelDrag()
        {
            if (!IsDragging)
            {
                return;
            }

            IsDragging = false;
            if (ActiveDrag == this)
            {
                ActiveDrag = null;
            }

            ClearGhost();

            // Unity fake-null: the GameObject may be mid-destroy (disconnect or
            // world clear during a drag) — only touch it while it is alive.
            if (this != null && gameObject != null)
            {
                if (_collider != null)
                {
                    _collider.enabled = true;
                }

                if (_feel != null)
                {
                    _feel.CancelImmediate();
                }

                transform.position = HomePosition;
            }

            _host?.NotifyRelease(PieceId);
        }

        public void SnapToHome()
        {
            if (_feel != null)
            {
                _feel.SnapBack(HomePosition, null);
                return;
            }

            transform.position = HomePosition;
        }

        /// <summary>Accepted-piece lockdown: parked on the slot, no longer interactive.</summary>
        public void LockAtPosition(Vector3 worldPosition)
        {
            IsAwaitingResult = false;
            if (IsDragging)
            {
                IsDragging = false;
                if (ActiveDrag == this)
                {
                    ActiveDrag = null;
                }

                ClearGhost();
                if (_feel != null)
                {
                    _feel.EndLift();
                }
            }

            transform.position = worldPosition;
            if (_collider != null)
            {
                _collider.enabled = false;
            }
        }

        public void UnlockAtHome()
        {
            IsAwaitingResult = false;
            transform.position = HomePosition;
            if (_collider != null)
            {
                _collider.enabled = true;
            }
        }

        private void UpdateGhostPreview()
        {
            var zone = DropZone.FindAt(transform.position);
            if (zone == _hoverZone)
            {
                return;
            }

            ClearGhost();
            if (zone == null || zone.IsOccupied || _host == null || !_host.WouldAcceptDrop(PieceId, zone.ZoneId))
            {
                return;
            }

            var renderer = GetComponent<SpriteRenderer>();
            zone.ShowGhost(renderer != null ? renderer.sprite : null);
            _hoverZone = zone;
        }

        private void ClearGhost()
        {
            if (_hoverZone != null)
            {
                _hoverZone.HideGhost();
                _hoverZone = null;
            }
        }

        private static Vector3 ScreenToWorld(PointerEventData eventData)
        {
            var camera = eventData.pressEventCamera != null
                ? eventData.pressEventCamera
                : CameraDirector.Ensure().Camera;
            if (camera == null)
            {
                return Vector3.zero;
            }

            var world = camera.ScreenToWorldPoint(eventData.position);
            return new Vector3(world.x, world.y, 0f);
        }

        private void OnDisable()
        {
            if (ActiveDrag == this)
            {
                CancelDrag();
            }
        }

        private void OnDestroy()
        {
            if (ActiveDrag == this)
            {
                ActiveDrag = null;
                IsDragging = false;
            }
        }
    }
}
