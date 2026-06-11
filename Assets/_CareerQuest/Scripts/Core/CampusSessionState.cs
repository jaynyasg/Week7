using System;
using Unity.Netcode;
using UnityEngine;

namespace CareerQuest
{
    public class CampusSessionState : NetworkBehaviour
    {
        public static CampusSessionState Instance { get; private set; }

        private readonly NetworkVariable<int> _sessionPhase = new(
            (int)SessionPhase.Hub,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> _currentRoute = new(
            (int)ActivityRoute.Campus,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> _playerCount = new(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<int> _uniqueCompletedGames = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public SessionPhase CurrentPhase => (SessionPhase)_sessionPhase.Value;
        public ActivityRoute CurrentRoute => (ActivityRoute)_currentRoute.Value;
        public int PlayerCount => _playerCount.Value;
        public int UniqueCompletedGames => _uniqueCompletedGames.Value;

        public event Action Changed;

        private GameSession _boundHostSession;

        public override void OnNetworkSpawn()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Multiple CampusSessionState instances detected.");
            }

            Instance = this;
            _sessionPhase.OnValueChanged += HandleValueChanged;
            _currentRoute.OnValueChanged += HandleValueChanged;
            _playerCount.OnValueChanged += HandleValueChanged;
            _uniqueCompletedGames.OnValueChanged += HandleValueChanged;
        }

        public override void OnNetworkDespawn()
        {
            UnbindHostSession();
            _sessionPhase.OnValueChanged -= HandleValueChanged;
            _currentRoute.OnValueChanged -= HandleValueChanged;
            _playerCount.OnValueChanged -= HandleValueChanged;
            _uniqueCompletedGames.OnValueChanged -= HandleValueChanged;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void BindHostSession(GameSession session)
        {
            if (session == null)
            {
                return;
            }

            UnbindHostSession();
            _boundHostSession = session;
            _boundHostSession.Changed += HandleHostSessionChanged;

            if (IsServer)
            {
                ServerSyncFrom(_boundHostSession);
            }
        }

        public void UnbindHostSession()
        {
            if (_boundHostSession == null)
            {
                return;
            }

            _boundHostSession.Changed -= HandleHostSessionChanged;
            _boundHostSession = null;
        }

        public void ApplyToGameSession(GameSession session)
        {
            if (session == null)
            {
                return;
            }

            session.ApplyNetworkSnapshot(CurrentPhase, CurrentRoute, PlayerCount, UniqueCompletedGames);
        }

        public void ServerSyncPlayerCount(int count)
        {
            if (!IsServer)
            {
                return;
            }

            _playerCount.Value = count;
            if (_boundHostSession != null)
            {
                _boundHostSession.PlayerCount = count;
            }
        }

        private void HandleHostSessionChanged()
        {
            if (!IsServer || _boundHostSession == null)
            {
                return;
            }

            ServerSyncFrom(_boundHostSession);
        }

        private void ServerSyncFrom(GameSession session)
        {
            _sessionPhase.Value = (int)session.CurrentPhase;
            _currentRoute.Value = (int)session.CurrentRoute;
            _playerCount.Value = session.PlayerCount;
            _uniqueCompletedGames.Value = session.UniqueCompletedGames;
        }

        private void HandleValueChanged(int previous, int current)
        {
            Changed?.Invoke();
        }
    }
}
