using System;
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
        public ushort Port => port;

        public NetworkManager Manager => networkManager != null ? networkManager : NetworkManager.Singleton;

        public void Bind(NetworkManager manager, UnityTransport transport)
        {
            networkManager = manager;
            unityTransport = transport;
        }

        public bool StartHostP1()
        {
            LastMode = ConnectionMode.HostP1;
            ConfigureTransport("127.0.0.1", port, "0.0.0.0");
            return StartNetwork("Host P1", manager => manager.StartHost());
        }

        public bool JoinLocalhostP2()
        {
            LastMode = ConnectionMode.JoinLocalhostP2;
            ConfigureTransport("127.0.0.1", port);
            return StartNetwork("Join Localhost as P2", manager => manager.StartClient());
        }

        public bool JoinLanByIp(string hostAddress)
        {
            LastMode = ConnectionMode.JoinLanByIp;
            var address = string.IsNullOrWhiteSpace(hostAddress) ? "127.0.0.1" : hostAddress.Trim();
            ConfigureTransport(address, port);
            return StartNetwork($"Join LAN by IP ({address})", manager => manager.StartClient());
        }

        public void StartSoloFallback()
        {
            LastMode = ConnectionMode.SoloFallback;
            Status = "Solo Fallback active";
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

        private bool StartNetwork(string label, Func<NetworkManager, bool> start)
        {
            var manager = Manager;
            if (manager == null)
            {
                Status = $"{label} failed: NetworkManager missing";
                return false;
            }

            var ok = start(manager);
            Status = ok ? $"{label} started" : $"{label} failed";
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
