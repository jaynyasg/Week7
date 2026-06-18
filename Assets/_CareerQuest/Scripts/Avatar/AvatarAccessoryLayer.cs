using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Visual accessory layers on an avatar (U6, design doc Accessory Display
    /// rule): child SpriteRenderers with fixed local anchors under the avatar
    /// root, so they follow the avatar transform for free, flip with facing
    /// (mirroring the host renderer's flipX), and sort against the host
    /// renderer via each definition's SortingOffset. Placeholder accessory art
    /// (AssetCatalog prop tokens until U11) is normalized to a small token
    /// height so nothing floats off or swallows the avatar.
    ///
    /// Slot rules are enforced defensively here too: at most one visible
    /// accessory per slot, and ceremony-only items render only when the layer
    /// is in ceremony context (the reveal stage) — campus play never shows
    /// them. Accessories are presentation only (KTD8); this component never
    /// writes session state.
    /// </summary>
    public class AvatarAccessoryLayer : MonoBehaviour
    {
        public const string LayerNamePrefix = "Accessory_";

        /// <summary>Placeholder token height in avatar-local units (U11 retunes per accessory).</summary>
        public const float TokenLocalHeight = 0.55f;

        /// <summary>
        /// Celebrate jump-sync: the curated celebrate frames bake an airborne
        /// pose (the character is drawn jumped UP inside the frame, unlike the
        /// in-place walk frames), so gear anchored to the static body would stay
        /// grounded while the body leaps. While the host shows the airborne
        /// celebrate frame, lift the gear by this fraction of the body half-height
        /// so it jumps with the body. Tunable to match the art.
        /// </summary>
        public const float CelebrateLiftFraction = 0.12f;

        private readonly Dictionary<string, SpriteRenderer> _layers = new();
        private readonly Dictionary<string, AccessoryDefinition> _applied = new();

        private SpriteRenderer _hostRenderer;
        private GameSession _boundSession;
        private bool _ceremonyContext;
        private bool _renderedFlip;
        private float _renderedLift = float.MinValue;
        private SpriteFrameAnimator _animator;
        private bool _animatorChecked;

        public bool IsCeremonyContext => _ceremonyContext;
        public int VisibleCount => _layers.Count;
        public IReadOnlyCollection<string> VisibleAccessoryIds => _layers.Keys;

        /// <summary>Test/QA seam: the live layer renderer for an accessory id, or null.</summary>
        public SpriteRenderer RendererFor(string accessoryId)
        {
            return accessoryId != null && _layers.TryGetValue(accessoryId, out var layer) ? layer : null;
        }

        /// <summary>
        /// Binds the layer to a session: earned accessories derive from the
        /// session read model on every change (host best results or the 2P
        /// compact facts) — no stored wardrobe state anywhere.
        /// </summary>
        public void Bind(GameSession session, bool ceremonyContext = false)
        {
            Unbind();
            _boundSession = session;
            _ceremonyContext = ceremonyContext;
            if (_boundSession != null)
            {
                _boundSession.Changed += HandleSessionChanged;
            }

            RefreshFromSession();
        }

        public void Unbind()
        {
            if (_boundSession != null)
            {
                _boundSession.Changed -= HandleSessionChanged;
                _boundSession = null;
            }
        }

        /// <summary>Reveal seam (U7): flips ceremony-only items on/off in place.</summary>
        public void SetCeremonyContext(bool ceremonyContext)
        {
            if (_ceremonyContext == ceremonyContext)
            {
                return;
            }

            _ceremonyContext = ceremonyContext;
            if (_boundSession != null)
            {
                RefreshFromSession();
            }
        }

        /// <summary>
        /// Applies an explicit accessory set (the resolver's visible list, or a
        /// direct list in tests/preview surfaces). Slot and ceremony rules are
        /// re-enforced here so no caller can render a cluttered avatar.
        /// </summary>
        public void Apply(IReadOnlyList<AccessoryDefinition> accessories)
        {
            var visible = AccessoryResolver.ResolveVisible(accessories, _ceremonyContext);
            var visibleIds = new HashSet<string>(visible.Select(accessory => accessory.Id));

            foreach (var staleId in _layers.Keys.Where(id => !visibleIds.Contains(id)).ToList())
            {
                if (_layers[staleId] != null)
                {
                    Destroy(_layers[staleId].gameObject);
                }

                _layers.Remove(staleId);
                _applied.Remove(staleId);
            }

            foreach (var accessory in visible)
            {
                if (!_layers.ContainsKey(accessory.Id))
                {
                    MountLayer(accessory);
                }
            }

            SyncFacingAndSorting(force: true);
        }

        private void RefreshFromSession()
        {
            Apply(AccessoryResolver.ResolveEarned(_boundSession));
        }

        private void HandleSessionChanged()
        {
            RefreshFromSession();
        }

        private void MountLayer(AccessoryDefinition accessory)
        {
            var layerObject = new GameObject($"{LayerNamePrefix}{accessory.Id}", typeof(SpriteRenderer));
            layerObject.transform.SetParent(transform, false);

            var renderer = layerObject.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetCatalog.SpriteFor(accessory.SpriteAssetId);

            // Normalize the placeholder token to a small fixed local height so
            // 128px prop art and future final art both sit on the avatar.
            var spriteHeight = renderer.sprite != null ? renderer.sprite.bounds.size.y : 0f;
            var normalize = spriteHeight > 0.0001f ? TokenLocalHeight / spriteHeight : 1f;
            var scale = normalize * Mathf.Max(0.01f, accessory.LocalScale);
            layerObject.transform.localScale = new Vector3(scale, scale, 1f);
            layerObject.transform.localPosition = AnchorFor(accessory);

            _layers[accessory.Id] = renderer;
            _applied[accessory.Id] = accessory;
        }

        /// <summary>
        /// Slot anchor in avatar-local units, proportional to the host sprite
        /// so accessories never float off the body regardless of art size.
        /// The definition's LocalOffset adds on top (U11 fit tuning).
        /// </summary>
        private Vector3 AnchorFor(AccessoryDefinition accessory)
        {
            var host = EnsureHostRenderer();
            var extents = host != null && host.sprite != null
                ? (Vector2)host.sprite.bounds.extents
                : new Vector2(0.9f, 1.2f);

            // Design-review (2026-06-16): the Kenney Toon avatars carry ~27% transparent
            // padding above the head (measured: head-top ~0.48*extents.y, feet ~ -0.98),
            // so the old Head=0.78 / Face=0.42 fractions floated hats/goggles in that
            // empty space. Recalibrated onto the visible body (crown ~0.40, face ~0.27,
            // chest ~ -0.03); per-accessory LocalOffset still fine-tunes on top.
            var anchor = accessory.Slot switch
            {
                AccessorySlot.Head => new Vector2(0f, extents.y * 0.40f),
                AccessorySlot.Face => new Vector2(0f, extents.y * 0.27f),
                AccessorySlot.Torso => new Vector2(0f, -extents.y * 0.03f),
                AccessorySlot.Back => new Vector2(0f, -extents.y * 0.05f),
                AccessorySlot.Hand => new Vector2(extents.x * 0.54f, -extents.y * 0.32f),
                _ => new Vector2(0f, -extents.y * 0.12f) // Sash
            };

            anchor += accessory.LocalOffset;
            return new Vector3(anchor.x, anchor.y, 0f);
        }

        /// <summary>
        /// Facing follows the host renderer's flipX (AvatarRuntimeView house
        /// rule: facing is flipX, scale stays positive): each layer mirrors
        /// the flip flag AND its anchor x so hand/back items swap sides.
        /// </summary>
        private void SyncFacingAndSorting(bool force = false)
        {
            var host = EnsureHostRenderer();
            if (host == null)
            {
                return;
            }

            var flipped = host.flipX;
            var lift = CurrentCelebrateLift();
            if (!force && flipped == _renderedFlip && Mathf.Approximately(lift, _renderedLift))
            {
                return;
            }

            _renderedFlip = flipped;
            _renderedLift = lift;
            foreach (var pair in _layers)
            {
                var renderer = pair.Value;
                if (renderer == null || !_applied.TryGetValue(pair.Key, out var accessory))
                {
                    continue;
                }

                renderer.flipX = flipped;
                renderer.sortingLayerID = host.sortingLayerID;
                renderer.sortingOrder = host.sortingOrder + accessory.SortingOffset;

                var anchor = AnchorFor(accessory);
                renderer.transform.localPosition = new Vector3(flipped ? -anchor.x : anchor.x, anchor.y + lift, 0f);
            }
        }

        private SpriteRenderer EnsureHostRenderer()
        {
            if (_hostRenderer == null)
            {
                _hostRenderer = GetComponent<SpriteRenderer>();
            }

            return _hostRenderer;
        }

        private SpriteFrameAnimator EnsureAnimator()
        {
            if (!_animatorChecked)
            {
                _animator = GetComponent<SpriteFrameAnimator>();
                _animatorChecked = true;
            }

            return _animator;
        }

        /// <summary>
        /// The vertical gear lift for the current host frame: while celebrating,
        /// the airborne celebrate frame(s) (index > 0; frame 0 is the grounded
        /// cheer) lift the gear so it jumps with the body. Zero in every other
        /// state (walk/idle frames animate in place, so gear stays anchored).
        /// </summary>
        private float CurrentCelebrateLift()
        {
            var animator = EnsureAnimator();
            if (animator == null || !animator.IsCelebrating || animator.CurrentFrameIndex <= 0)
            {
                return 0f;
            }

            var host = EnsureHostRenderer();
            var extentsY = host != null && host.sprite != null ? host.sprite.bounds.extents.y : 1.2f;
            return extentsY * CelebrateLiftFraction;
        }

        private void LateUpdate()
        {
            SyncFacingAndSorting();
        }

        private void OnDestroy()
        {
            Unbind();
        }
    }
}

