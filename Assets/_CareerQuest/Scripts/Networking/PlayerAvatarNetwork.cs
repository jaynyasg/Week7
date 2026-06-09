using Unity.Netcode;
using UnityEngine;

namespace CareerQuest
{
    [RequireComponent(typeof(PlayerInputRouter))]
    public class PlayerAvatarNetwork : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private PlayerInputRouter inputRouter;
        [SerializeField] private Color localColor = new(0.2f, 0.65f, 1f);
        [SerializeField] private Color remoteColor = new(1f, 0.75f, 0.2f);

        private static Sprite _avatarSprite;
        private readonly NetworkVariable<Vector3> _networkPosition = new(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            inputRouter = inputRouter != null ? inputRouter : GetComponent<PlayerInputRouter>();
            _spriteRenderer = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            EnsureVisibleAvatar();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _networkPosition.Value = transform.position;
            }

            ApplyColor();
        }

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsOwner)
            {
                var move = inputRouter != null ? inputRouter.ReadMove() : Vector2.zero;
                if (move.sqrMagnitude > 0f)
                {
                    SubmitMoveRpc(move, Time.deltaTime);
                }
            }

            if (!IsServer)
            {
                transform.position = Vector3.Lerp(transform.position, _networkPosition.Value, 20f * Time.deltaTime);
            }
        }

        public void ApplyLocalMove(Vector2 move, float deltaTime)
        {
            var next = transform.position + new Vector3(move.x, move.y, 0f) * moveSpeed * deltaTime;
            transform.position = ClampCampus(next);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitMoveRpc(Vector2 move, float deltaTime)
        {
            var next = transform.position + new Vector3(move.x, move.y, 0f) * moveSpeed * deltaTime;
            _networkPosition.Value = ClampCampus(next);
            transform.position = _networkPosition.Value;
        }

        private Vector3 ClampCampus(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, -7.5f, 7.5f);
            position.y = Mathf.Clamp(position.y, -4.2f, 4.2f);
            position.z = 0f;
            return position;
        }

        private void ApplyColor()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            _spriteRenderer.color = IsOwner ? localColor : remoteColor;
        }

        private void EnsureVisibleAvatar()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            if (_spriteRenderer.sprite == null)
            {
                _spriteRenderer.sprite = AvatarSprite;
            }

            _spriteRenderer.sortingOrder = 10;

            if (transform.localScale == Vector3.one)
            {
                transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            }
        }

        private static Sprite AvatarSprite
        {
            get
            {
                if (_avatarSprite != null)
                {
                    return _avatarSprite;
                }

                const int width = 64;
                const int height = 88;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }

                FillCircle(texture, 32, 65, 15, Color.white);
                FillRect(texture, 20, 26, 24, 36, Color.white);
                FillRect(texture, 15, 30, 8, 24, Color.white);
                FillRect(texture, 41, 30, 8, 24, Color.white);
                FillRect(texture, 21, 6, 8, 24, Color.white);
                FillRect(texture, 35, 6, 8, 24, Color.white);
                FillCircle(texture, 32, 17, 24, new Color(1f, 1f, 1f, 0.22f));

                texture.Apply();
                _avatarSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.18f), 64f);
                return _avatarSprite;
            }
        }

        private static void FillRect(Texture2D texture, int left, int bottom, int width, int height, Color color)
        {
            for (var y = bottom; y < bottom + height; y++)
            {
                for (var x = left; x < left + width; x++)
                {
                    if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void FillCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            var radiusSquared = radius * radius;
            for (var y = centerY - radius; y <= centerY + radius; y++)
            {
                for (var x = centerX - radius; x <= centerX + radius; x++)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    if (dx * dx + dy * dy <= radiusSquared && x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }
    }
}
