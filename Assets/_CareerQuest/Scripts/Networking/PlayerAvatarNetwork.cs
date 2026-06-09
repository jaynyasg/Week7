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

        private readonly NetworkVariable<Vector3> _networkPosition = new(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            inputRouter = inputRouter != null ? inputRouter : GetComponent<PlayerInputRouter>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
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
    }
}
