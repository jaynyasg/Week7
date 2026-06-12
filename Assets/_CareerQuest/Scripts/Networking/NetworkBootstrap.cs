using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CareerQuest
{
    public class NetworkBootstrap : MonoBehaviour
    {
        public const ushort DefaultPort = 7777;

        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport unityTransport;
        [SerializeField] private ushort port = DefaultPort;

        public ConnectionMode LastMode { get; private set; } = ConnectionMode.None;
        public string Status { get; private set; } = "Not connected";
        public string LastAddress { get; private set; } = "127.0.0.1";
        public string LastConnectionError { get; private set; } = string.Empty;
        public bool LastConnectionSucceeded { get; private set; }
        public ushort Port => port;

        public event Action ClientConnectionLost;

        public NetworkManager Manager => networkManager != null ? networkManager : NetworkManager.Singleton;

        private bool _handlersRegistered;
        private NetworkManager _registeredManager;
        private bool _localDisconnectNotified;

        public void Bind(NetworkManager manager, UnityTransport transport)
        {
            networkManager = manager;
            unityTransport = transport;
            EnsureConnectionHandlers(manager);
        }

        public bool StartHostP1()
        {
            LastMode = ConnectionMode.HostP1;
            LastConnectionError = string.Empty;
            LastConnectionSucceeded = false;
            _localDisconnectNotified = false;
            ConfigureTransport("127.0.0.1", port, "0.0.0.0");
            return StartNetwork("Host P1", manager =>
            {
                EnsureNetworkConfig(manager);
                return manager.StartHost();
            });
        }

        public bool JoinLocalhostP2()
        {
            LastMode = ConnectionMode.JoinLocalhostP2;
            LastConnectionError = string.Empty;
            LastConnectionSucceeded = false;
            _localDisconnectNotified = false;
            ConfigureTransport("127.0.0.1", port);
            return StartNetwork("Join Localhost as P2", manager =>
            {
                EnsureNetworkConfig(manager);
                return manager.StartClient();
            });
        }

        public bool JoinLanByIp(string hostAddress)
        {
            LastMode = ConnectionMode.JoinLanByIp;
            LastConnectionError = string.Empty;
            LastConnectionSucceeded = false;
            _localDisconnectNotified = false;
            var address = string.IsNullOrWhiteSpace(hostAddress) ? "127.0.0.1" : hostAddress.Trim();
            ConfigureTransport(address, port);
            return StartNetwork($"Join LAN by IP ({address})", manager =>
            {
                EnsureNetworkConfig(manager);
                return manager.StartClient();
            });
        }

        public void StartSoloFallback()
        {
            LastMode = ConnectionMode.SoloFallback;
            Status = "Solo Fallback active";
        }

        public IEnumerator WaitForConnection(float timeoutSeconds = 12f)
        {
            LastConnectionSucceeded = false;
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;

            while (Time.realtimeSinceStartup < deadline)
            {
                var manager = Manager;
                if (manager == null)
                {
                    yield return null;
                    continue;
                }

                if (LastMode == ConnectionMode.HostP1 && manager.IsHost && manager.IsListening)
                {
                    LastConnectionSucceeded = true;
                    LastConnectionError = string.Empty;
                    yield break;
                }

                if ((LastMode == ConnectionMode.JoinLocalhostP2 || LastMode == ConnectionMode.JoinLanByIp)
                    && manager.IsConnectedClient)
                {
                    LastConnectionSucceeded = true;
                    LastConnectionError = string.Empty;
                    yield break;
                }

                if (!string.IsNullOrWhiteSpace(LastConnectionError))
                {
                    yield break;
                }

                yield return null;
            }

            if (string.IsNullOrWhiteSpace(LastConnectionError))
            {
                LastConnectionError = "Connection timed out. Check the host is running, then try again.";
            }
        }

        public void ConfigureTransport(string address, ushort targetPort, string listenAddress = null)
        {
            LastAddress = address;
            port = targetPort;

            var transport = ResolveTransport();
            if (transport == null)
            {
                Status = "Unity Transport missing";
                return;
            }

            transport.SetConnectionData(address, targetPort, listenAddress);
            Status = listenAddress == null
                ? $"Transport {address}:{targetPort}"
                : $"Transport {address}:{targetPort} listen {listenAddress}";
        }

        private void EnsureNetworkConfig(NetworkManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (manager.NetworkConfig == null)
            {
                manager.NetworkConfig = new NetworkConfig();
            }

            manager.NetworkConfig.ConnectionApproval = true;
            EnsureConnectionHandlers(manager);
        }

        private void EnsureConnectionHandlers(NetworkManager manager)
        {
            if (manager == null)
            {
                return;
            }

            if (_handlersRegistered && _registeredManager == manager)
            {
                return;
            }

            manager.ConnectionApprovalCallback = ApprovalCheck;
            manager.OnClientConnectedCallback += HandleClientConnected;
            manager.OnClientDisconnectCallback += HandleClientDisconnect;
            _handlersRegistered = true;
            _registeredManager = manager;
        }

        public bool TryGetJoinRejectionReason(out string reason)
        {
            var manager = Manager;
            var connectedCount = manager != null && manager.IsListening ? manager.ConnectedClientsIds.Count : 0;
            var hostPhase = CampusSessionState.Instance != null
                ? CampusSessionState.Instance.CurrentPhase
                : SessionPhase.Hub;
            return TryGetJoinRejectionForPhase(hostPhase, connectedCount, out reason);
        }

        public static bool TryGetJoinRejectionForPhase(SessionPhase hostPhase, int connectedClientCount, out string reason)
        {
            if (CampusJoinPolicy.CanJoin(hostPhase, connectedClientCount))
            {
                reason = string.Empty;
                return false;
            }

            reason = CampusJoinPolicy.GetRejectionMessage(hostPhase, connectedClientCount);
            return true;
        }

        private void ApprovalCheck(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            var manager = Manager;
            var connectedCount = manager != null ? manager.ConnectedClientsIds.Count : 0;
            var hostPhase = CampusSessionState.Instance != null
                ? CampusSessionState.Instance.CurrentPhase
                : SessionPhase.Hub;

            if (TryGetJoinRejectionForPhase(hostPhase, connectedCount, out var rejectionReason))
            {
                response.Approved = false;
                response.Reason = rejectionReason;
                response.CreatePlayerObject = false;
                response.Pending = false;
                return;
            }

            response.Approved = true;
            response.Reason = string.Empty;
            response.CreatePlayerObject = true;
            response.Pending = false;
        }

        private void HandleClientConnected(ulong clientId)
        {
            var manager = Manager;
            if (manager == null || !manager.IsServer)
            {
                return;
            }

            SyncPlayerCountFromServer(manager);
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            var manager = Manager;
            if (manager == null)
            {
                return;
            }

            if (manager.IsServer)
            {
                SyncPlayerCountFromServer(manager);

                // U12 P17 lifecycle: a client that disconnects mid-drag must not
                // leave a stale held-piece flag (partner highlight) behind.
                ClearHeldPiecesFor(clientId);
            }

            if (!manager.IsServer && manager.LocalClientId == clientId)
            {
                var reason = manager.DisconnectReason;
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    LastConnectionError = reason;
                }

                if (!_localDisconnectNotified && manager.IsConnectedClient == false)
                {
                    _localDisconnectNotified = true;
                    ClientConnectionLost?.Invoke();
                }
            }
        }

        /// <summary>
        /// Server-side held-piece sweep for a departed client across all three
        /// room states (U12 P17 — also a host-only test seam). Each call is a
        /// safe no-op when the state is absent or the client held nothing.
        /// </summary>
        public static void ClearHeldPiecesFor(ulong clientId)
        {
            FindFirstObjectByType<DesignBuildNetworkState>()?.ApplyHeldPiece(-1, clientId);
            FindFirstObjectByType<HealthHeroNetworkState>()?.ApplyHeldPiece(-1, clientId);
            FindFirstObjectByType<LogicCourtNetworkState>()?.ApplyHeldPiece(-1, clientId);
        }

        private static void SyncPlayerCountFromServer(NetworkManager manager)
        {
            var state = CampusSessionState.Instance;
            if (state == null)
            {
                return;
            }

            state.ServerSyncPlayerCount(manager.ConnectedClientsIds.Count);
        }

        private bool StartNetwork(string label, Func<NetworkManager, bool> start)
        {
            var manager = Manager;
            if (manager == null)
            {
                Status = $"{label} failed: NetworkManager missing";
                LastConnectionError = Status;
                return false;
            }

            var ok = start(manager);
            Status = ok ? $"{label} started" : $"{label} failed";
            if (!ok)
            {
                LastConnectionError = Status;
            }

            return ok;
        }

        private UnityTransport ResolveTransport()
        {
            if (unityTransport != null)
            {
                return unityTransport;
            }

            var manager = Manager;
            if (manager != null && manager.NetworkConfig != null && manager.NetworkConfig.NetworkTransport is UnityTransport transport)
            {
                unityTransport = transport;
                return unityTransport;
            }

            unityTransport = FindFirstObjectByType<UnityTransport>();
            return unityTransport;
        }
    }
}
