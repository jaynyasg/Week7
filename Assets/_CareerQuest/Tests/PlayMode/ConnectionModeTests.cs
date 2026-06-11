using CareerQuest;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CareerQuest.Tests
{
    public class ConnectionModeTests
    {
        [Test]
        public void TransportCanConfigureLocalAndLanAddresses()
        {
            var networkObject = new GameObject("network-manager-test");
            var manager = networkObject.AddComponent<NetworkManager>();
            var transport = networkObject.AddComponent<UnityTransport>();
            manager.NetworkConfig = new NetworkConfig();
            manager.NetworkConfig.NetworkTransport = transport;

            var bootstrapObject = new GameObject("bootstrap-test");
            var bootstrap = bootstrapObject.AddComponent<NetworkBootstrap>();
            bootstrap.Bind(manager, transport);

            bootstrap.ConfigureTransport("127.0.0.1", NetworkBootstrap.DefaultPort, "0.0.0.0");
            Assert.That(bootstrap.Status, Does.Contain("listen 0.0.0.0"));

            bootstrap.ConfigureTransport("192.168.1.20", NetworkBootstrap.DefaultPort);
            Assert.That(bootstrap.LastAddress, Is.EqualTo("192.168.1.20"));

            Object.DestroyImmediate(bootstrapObject);
            Object.DestroyImmediate(networkObject);
        }

        [Test]
        public void JoinRejectionMatchesCampusJoinPolicyForHubAndInRoom()
        {
            Assert.That(
                NetworkBootstrap.TryGetJoinRejectionForPhase(SessionPhase.Hub, 1, out var reason),
                Is.False);
            Assert.That(reason, Is.Empty);

            Assert.That(
                NetworkBootstrap.TryGetJoinRejectionForPhase(SessionPhase.InRoom, 1, out reason),
                Is.True);
            Assert.That(reason, Does.Contain("activity room"));

            Assert.That(
                NetworkBootstrap.TryGetJoinRejectionForPhase(SessionPhase.Ceremony, 1, out reason),
                Is.True);
            Assert.That(reason, Does.Contain("finishing an activity"));

            Assert.That(
                NetworkBootstrap.TryGetJoinRejectionForPhase(SessionPhase.Hub, 2, out reason),
                Is.True);
            Assert.That(reason, Does.Contain("two players"));
        }
    }
}
