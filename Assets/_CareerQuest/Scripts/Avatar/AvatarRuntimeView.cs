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
        private string _spriteAssetId;
        private Vector3 _baseScale = new(0.75f, 0.75f, 1f);

        public string AvatarId => avatarId;
        public AvatarDefinition Definition => AvatarConfig.GetAvatar(avatarId);
        public SpriteFrameAnimator Animator => EnsureAnimator();
        public string SpriteAssetId => _spriteAssetId;

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
