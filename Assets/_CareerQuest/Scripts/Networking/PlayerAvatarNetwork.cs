using System;
using System.Collections.Generic;
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
        private AvatarNameTag _nameTag;
        private IReadOnlyList<BuildingEntrance> _campusEntrances = Array.Empty<BuildingEntrance>();
        private Action<BuildingEntrance> _onCampusDestination;
        private BuildingEntrance _pendingEntrance;
        private float _dwellElapsed;
        private float _graceRemaining;
        private bool _entryLatched;
        // Remote avatars derive walk/idle from observed position deltas with a
        // deadzone + hysteresis (the network lerp produces residual motion that
        // would flicker the walk state — U5 plan callout).
        private readonly RemoteLocomotionFilter _remoteLocomotion = new();

        public string AvatarId => avatarId;
        public AvatarNameTag NameTag => _nameTag;

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
            _remoteLocomotion.Reset(transform.position);
            RefreshNameTag();
        }

        public override void OnNetworkDespawn()
        {
            _networkAvatarIndex.OnValueChanged -= HandleAvatarChanged;
            ClearCampusEntrances();
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

                // Local player animates from its own input — no network round-trip.
                _avatarView?.SetLocomotion(move.sqrMagnitude > 0f, move.x);
                TickCampusDoorEntry(Time.deltaTime);
                if (inputRouter != null && inputRouter.ReadConfirmDown())
                {
                    TryEnterNearestCampusEntrance();
                }
            }

            if (!IsServer)
            {
                transform.position = Vector3.Lerp(transform.position, _networkPosition.Value, 20f * Time.deltaTime);
            }

            if (!IsOwner)
            {
                // Remote avatar: derive moving/idle/facing from position deltas
                // through the deadzone filter so lerp residue never flickers it.
                _remoteLocomotion.Step(transform.position, Time.deltaTime);
                _avatarView?.SetLocomotion(
                    _remoteLocomotion.IsMoving,
                    _remoteLocomotion.IsMoving ? _remoteLocomotion.FacingX : 0f);
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

        public void ConfigureLocalPlayer(string selectedAvatarId, PlayerControlScheme controlScheme)
        {
            inputRouter = inputRouter != null ? inputRouter : GetComponent<PlayerInputRouter>();
            if (inputRouter != null)
            {
                inputRouter.ControlScheme = controlScheme;
            }

            SetAvatar(selectedAvatarId);
        }

        public void ConfigureCampusEntrances(IReadOnlyList<BuildingEntrance> entrances, Action<BuildingEntrance> onDestination)
        {
            ClearCampusEntrances();
            _campusEntrances = entrances ?? Array.Empty<BuildingEntrance>();
            _onCampusDestination = onDestination;
            _graceRemaining = PlayerAvatarController.ReturnToCampusGraceSeconds;
        }

        public void ClearCampusEntrances()
        {
            SetPendingEntrance(null);
            _campusEntrances = Array.Empty<BuildingEntrance>();
            _onCampusDestination = null;
            _dwellElapsed = 0f;
            _graceRemaining = 0f;
            _entryLatched = false;
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
            RefreshNameTag();
        }

        /// <summary>
        /// P16: world-space identity tag above the avatar (fixed identity data,
        /// never free text). Built on spawn, refreshed on avatar change.
        /// </summary>
        private void RefreshNameTag()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (_nameTag == null)
            {
                _nameTag = GetComponent<AvatarNameTag>() ?? gameObject.AddComponent<AvatarNameTag>();
            }

            _nameTag.Configure(AvatarNameTag.IdentityTextFor(OwnerClientId, AvatarConfig.GetAvatar(avatarId)));
        }

        private void TickCampusDoorEntry(float deltaSeconds)
        {
            if (_entryLatched || deltaSeconds <= 0f || _campusEntrances.Count == 0)
            {
                return;
            }

            var inside = EntranceContaining(transform.position);
            if (inside != _pendingEntrance)
            {
                SetPendingEntrance(inside);
            }

            if (_graceRemaining > 0f)
            {
                var consumed = Mathf.Min(_graceRemaining, deltaSeconds);
                _graceRemaining -= consumed;
                deltaSeconds -= consumed;
            }

            if (_pendingEntrance == null || deltaSeconds <= 0f)
            {
                return;
            }

            _dwellElapsed += deltaSeconds;
            if (_dwellElapsed < PlayerAvatarController.AutoEntryDwellSeconds)
            {
                return;
            }

            var entrance = _pendingEntrance;
            SetPendingEntrance(null);
            EnterCampusEntrance(entrance);
        }

        private bool TryEnterNearestCampusEntrance()
        {
            var entrance = NearestEntrance(transform.position);
            return entrance != null && entrance.Contains(transform.position) && EnterCampusEntrance(entrance);
        }

        private bool EnterCampusEntrance(BuildingEntrance entrance)
        {
            if (_entryLatched || entrance == null)
            {
                return false;
            }

            _entryLatched = true;
            SetPendingEntrance(null);
            AudioCueCatalog.TryPlay(AudioCueIds.DoorEnter);
            _onCampusDestination?.Invoke(entrance);
            return true;
        }

        private BuildingEntrance EntranceContaining(Vector2 worldPosition)
        {
            foreach (var entrance in _campusEntrances)
            {
                if (entrance != null && entrance.Contains(worldPosition))
                {
                    return entrance;
                }
            }

            return null;
        }

        private BuildingEntrance NearestEntrance(Vector2 worldPosition)
        {
            BuildingEntrance nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var entrance in _campusEntrances)
            {
                if (entrance == null)
                {
                    continue;
                }

                var distance = Vector2.Distance(entrance.transform.position, worldPosition);
                if (distance < nearestDistance)
                {
                    nearest = entrance;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void SetPendingEntrance(BuildingEntrance entrance)
        {
            if (_pendingEntrance == entrance)
            {
                return;
            }

            SetEntranceHighlight(_pendingEntrance, false);
            _pendingEntrance = entrance;
            _dwellElapsed = 0f;
            SetEntranceHighlight(_pendingEntrance, true);
        }

        private static void SetEntranceHighlight(BuildingEntrance entrance, bool active)
        {
            if (entrance == null)
            {
                return;
            }

            var sign = entrance.GetComponent<DoorSign>();
            if (sign != null)
            {
                sign.SetPulsing(active);
            }
        }
    }
}
