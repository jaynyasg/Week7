using UnityEngine;

namespace CareerQuest
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class AvatarRuntimeView : MonoBehaviour
    {
        [SerializeField] private string avatarId = AvatarConfig.DefaultAvatarId;
        [SerializeField] private int sortingOrder = 10;

        private SpriteRenderer _spriteRenderer;

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
            _spriteRenderer.sprite = AssetCatalog.SpriteFor(avatar.SpriteAssetId);
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sortingOrder = sortingOrder;

            if (transform.localScale == Vector3.one)
            {
                transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            }
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
