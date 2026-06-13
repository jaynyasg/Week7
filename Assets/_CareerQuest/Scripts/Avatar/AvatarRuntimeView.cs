using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Renders a player avatar or NPC with frame-animated walk/idle/celebrate
    /// (U5): the SpriteFrameAnimator cycles curated Kenney frames; facing is
    /// SpriteRenderer.flipX (scale stays positive). Falls back to static catalog
    /// sprites when frame sets are missing — never throws.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class AvatarRuntimeView : MonoBehaviour
    {
        [SerializeField] private string avatarId = AvatarConfig.DefaultAvatarId;
        // Characters band 300-399 per the U4 sorting-order banding decision
        // (authored world content occupies 200-299).
        [SerializeField] private int sortingOrder = 320;

        private SpriteRenderer _spriteRenderer;
        private SpriteFrameAnimator _animator;
        private AvatarAccessoryLayer _accessoryLayer;
        private string _spriteAssetId;
        // Default base scale (NPCs and the bare ApplySpriteAsset path); player
        // avatars override it from their U11-tuned AvatarDefinition.RenderScale.
        private Vector3 _baseScale = new(AvatarConfig.LegacyRenderScale, AvatarConfig.LegacyRenderScale, 1f);

        public string AvatarId => avatarId;
        public AvatarDefinition Definition => AvatarConfig.GetAvatar(avatarId);
        public SpriteFrameAnimator Animator => EnsureAnimator();
        public string SpriteAssetId => _spriteAssetId;

        /// <summary>U6 test/QA seam: the avatar's accessory layer once bound, or null.</summary>
        public AvatarAccessoryLayer AccessoryLayer => _accessoryLayer;

        private void Awake()
        {
            EnsureRenderer();
            ApplyAvatar(avatarId);
        }

        public void ApplyAvatar(string id)
        {
            ApplyAvatar(AvatarConfig.GetAvatar(id));
        }

        public void ApplyAvatar(AvatarDefinition avatar)
        {
            if (avatar == null)
            {
                avatar = AvatarConfig.DefaultAvatar;
            }

            avatarId = avatar.Id;
            // U11 polish pass: adopt this avatar's authored rest-pose proportion
            // before applying the sprite, so the cast reads as cleaner, more
            // distinct characters and the accessory anchors derive from the
            // correctly-scaled host sprite.
            var scale = avatar.RenderScale > 0.01f ? avatar.RenderScale : AvatarConfig.LegacyRenderScale;
            _baseScale = new Vector3(scale, scale, 1f);
            ApplySpriteAsset(avatar.SpriteAssetId);
        }

        /// <summary>
        /// NPC seam: binds the view to any catalog sprite id (npc.*) with the
        /// same frame-animation behavior the player avatars get.
        /// </summary>
        public void ApplySpriteAsset(string spriteAssetId)
        {
            EnsureRenderer();
            _spriteAssetId = spriteAssetId;

            var animator = EnsureAnimator();
            animator.Configure(_spriteRenderer, _spriteAssetId);
            animator.SetBaseScale(_baseScale);

            _spriteRenderer.color = Color.white;
            _spriteRenderer.sortingOrder = sortingOrder;
        }

        public void SetLocomotion(bool isMoving, float facingX)
        {
            EnsureAnimator().SetLocomotion(isMoving, facingX);
        }

        /// <summary>
        /// U6: mounts the accessory layer on THIS avatar (it shares this
        /// SpriteRenderer for facing/sorting) and binds it to the session, so
        /// earned accessories derive from the session read model and follow the
        /// avatar transform/flip for free. Campus context = not ceremony, so
        /// ceremony-only items (star robe, reveal flourish) stay hidden in
        /// normal play. NPCs simply never call this — no session, no accessories.
        /// </summary>
        public void BindAccessories(GameSession session, bool ceremonyContext = false)
        {
            EnsureRenderer();
            if (_accessoryLayer == null)
            {
                _accessoryLayer = GetComponent<AvatarAccessoryLayer>() ?? gameObject.AddComponent<AvatarAccessoryLayer>();
            }

            _accessoryLayer.Bind(session, ceremonyContext);
        }

        /// <summary>Drops the accessory binding (kept for symmetry; safe when unbound).</summary>
        public void UnbindAccessories()
        {
            _accessoryLayer?.Unbind();
        }

        /// <summary>P15: forwarded celebrate trigger (wired into ceremony in U7).</summary>
        public void TriggerCelebrate(float durationSeconds)
        {
            EnsureAnimator().TriggerCelebrate(durationSeconds);
        }

        private SpriteFrameAnimator EnsureAnimator()
        {
            if (_animator == null)
            {
                _animator = GetComponent<SpriteFrameAnimator>();
                if (_animator == null)
                {
                    _animator = gameObject.AddComponent<SpriteFrameAnimator>();
                }
            }

            return _animator;
        }

        private void EnsureRenderer()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }
    }
}
