using UnityEngine;

namespace CareerQuest
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class AvatarRuntimeView : MonoBehaviour
    {
        [SerializeField] private string avatarId = AvatarConfig.DefaultAvatarId;
        // Characters band 300-399 per the U4 sorting-order banding decision
        // (authored world content occupies 200-299).
        [SerializeField] private int sortingOrder = 320;

        private SpriteRenderer _spriteRenderer;
        private string _spriteAssetId;
        private bool _isMoving;
        private float _facingX = 1f;
        private Vector3 _baseScale = new(0.75f, 0.75f, 1f);

        public string AvatarId => avatarId;
        public AvatarDefinition Definition => AvatarConfig.GetAvatar(avatarId);

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
            EnsureRenderer();

            if (avatar == null)
            {
                avatar = AvatarConfig.DefaultAvatar;
            }

            avatarId = avatar.Id;
            _spriteAssetId = avatar.SpriteAssetId;
            _isMoving = false;
            _facingX = 1f;
            RefreshSprite();
        }

        public void SetLocomotion(bool isMoving, float facingX)
        {
            _isMoving = isMoving;
            if (Mathf.Abs(facingX) > 0.01f)
            {
                _facingX = Mathf.Sign(facingX);
            }

            RefreshSprite();
        }

        private void RefreshSprite()
        {
            EnsureRenderer();
            var spriteId = AssetCatalog.SpriteIdForLocomotion(_spriteAssetId, _isMoving);
            _spriteRenderer.sprite = AssetCatalog.SpriteFor(spriteId);
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sortingOrder = sortingOrder;

            var scale = _baseScale;
            scale.x = Mathf.Abs(scale.x) * _facingX;
            transform.localScale = scale;
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
