using Unity.Netcode;
using UnityEngine;

namespace CareerQuest
{
    [RequireComponent(typeof(PlayerInputRouter))]
    public class PlayerAvatarNetwork : NetworkBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private PlayerInputRouter inputRouter;
        [SerializeField] private string avatarId = AvatarConfig.DefaultAvatarId;

        private readonly NetworkVariable<Vector3> _networkPosition = new(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _networkAvatarIndex = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private AvatarRuntimeView _avatarView;
        private SpriteRenderer _spriteRenderer;
        public string AvatarId => avatarId;

        private void Awake()
        {
            inputRouter = inputRouter != null ? inputRouter : GetComponent<PlayerInputRouter>();
            _spriteRenderer = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            _avatarView = GetComponent<AvatarRuntimeView>() ?? gameObject.AddComponent<AvatarRuntimeView>();
            EnsureVisibleAvatar();
        }

        public override void OnNetworkSpawn()
        {
            _networkAvatarIndex.OnValueChanged += HandleAvatarChanged;

            if (IsServer)
            {
                _networkPosition.Value = transform.position;
                _networkAvatarIndex.Value = AvatarConfig.IndexForAvatar(avatarId);
            }

            ApplyColor();
            ApplyAvatar(AvatarConfig.GetAvatarAt(_networkAvatarIndex.Value).Id);
        }

        public override void OnNetworkDespawn()
        {
            _networkAvatarIndex.OnValueChanged -= HandleAvatarChanged;
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

        public void SetAvatar(string selectedAvatarId)
        {
            avatarId = AvatarConfig.GetAvatar(selectedAvatarId).Id;

            if (IsSpawned)
            {
                SubmitAvatarRpc(AvatarConfig.IndexForAvatar(avatarId));
                return;
            }

            ApplyAvatar(avatarId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitMoveRpc(Vector2 move, float deltaTime)
        {
            var next = transform.position + new Vector3(move.x, move.y, 0f) * moveSpeed * deltaTime;
            _networkPosition.Value = ClampCampus(next);
            transform.position = _networkPosition.Value;
        }

        /// <summary>
        /// Server-side campus clamp. Reads the walk bounds from the CampusHub
        /// prefab ASSET via WorldAnchors (never a live instance — the host can
        /// be inside a room with the hub world cleared while a client walks the
        /// campus and streams move RPCs). Hard fallback constants apply when
        /// the prefab asset is missing. Public static so the anchor-consistency
        /// test can verify entrances against the exact server clamp.
        /// </summary>
        public static Vector3 ClampCampus(Vector3 position)
        {
            var bounds = WorldAnchors.AssetWalkBounds;
            position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax);
            position.y = Mathf.Clamp(position.y, bounds.yMin, bounds.yMax);
            position.z = 0f;
            return position;
        }

        private void ApplyColor()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            _spriteRenderer.color = Color.white;
        }

        private void EnsureVisibleAvatar()
        {
            if (_spriteRenderer == null || _avatarView == null)
            {
                return;
            }

            _avatarView.ApplyAvatar(avatarId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitAvatarRpc(int selectedAvatarIndex)
        {
            _networkAvatarIndex.Value = Mathf.Clamp(selectedAvatarIndex, 0, AvatarConfig.Avatars.Length - 1);
        }

        private void HandleAvatarChanged(int previousValue, int newValue)
        {
            ApplyAvatar(AvatarConfig.GetAvatarAt(newValue).Id);
        }

        private void ApplyAvatar(string selectedAvatarId)
        {
            avatarId = AvatarConfig.GetAvatar(selectedAvatarId).Id;
            _avatarView?.ApplyAvatar(avatarId);
        }
    }
}
