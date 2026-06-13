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

        // U7: the reveal-start sync moment. A monotonic counter the host bumps
        // when it begins an unlocked reveal; clients already on the reveal
        // route use it as one input of their start latch — never as a forced
        // navigation signal (per-client route divergence is real, and
        // _currentRoute stays a read model, not a navigation lock).
        private readonly NetworkVariable<int> _revealStartCount = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public SessionPhase CurrentPhase => (SessionPhase)_sessionPhase.Value;
        public ActivityRoute CurrentRoute => (ActivityRoute)_currentRoute.Value;
        public int PlayerCount => _playerCount.Value;
        public int UniqueCompletedGames => _uniqueCompletedGames.Value;

        /// <summary>How many times the host has announced a reveal start this session.</summary>
        public int RevealStartCount => _revealStartCount.Value;

        public event Action Changed;

        /// <summary>Raised (on every peer) when the host announces a reveal start.</summary>
        public event Action RevealStartAnnounced;

        private GameSession _boundHostSession;
        private StationProgressNetworkState _stationProgress;

        /// <summary>
        /// U3 seam: the Party Pack station-progress shared state. It rides this
        /// always-spawned NetworkObject as a second behaviour (EmoteRelay
        /// precedent), so the generic station layer needs no new scene object.
        /// </summary>
        public StationProgressNetworkState StationProgress
        {
            get
            {
                if (_stationProgress == null)
                {
                    _stationProgress = GetComponent<StationProgressNetworkState>();
                }

                return _stationProgress;
            }
        }

        private void Awake()
        {
            // U3: attach the station-progress state at scene-load Awake on EVERY
            // peer, before any network spawn, so the NetworkBehaviour order on
            // this object stays deterministic across host and clients.
            if (GetComponent<StationProgressNetworkState>() == null)
            {
                gameObject.AddComponent<StationProgressNetworkState>();
            }
        }

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
            _revealStartCount.OnValueChanged += HandleRevealStartChanged;
        }

        public override void OnNetworkDespawn()
        {
            UnbindHostSession();
            _sessionPhase.OnValueChanged -= HandleValueChanged;
            _currentRoute.OnValueChanged -= HandleValueChanged;
            _playerCount.OnValueChanged -= HandleValueChanged;
            _uniqueCompletedGames.OnValueChanged -= HandleValueChanged;
            _revealStartCount.OnValueChanged -= HandleRevealStartChanged;

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

        /// <summary>
        /// Host-only: announces the reveal-start sync moment. Only meaningful
        /// to clients already on the reveal route — nobody is force-navigated.
        /// </summary>
        public void ServerAnnounceRevealStart()
        {
            if (!IsServer)
            {
                return;
            }

            _revealStartCount.Value++;
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

        private void HandleRevealStartChanged(int previous, int current)
        {
            RevealStartAnnounced?.Invoke();
            Changed?.Invoke();
        }
    }
}
