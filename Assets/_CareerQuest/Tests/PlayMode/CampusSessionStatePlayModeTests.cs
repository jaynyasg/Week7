using System.Collections;
using CareerQuest;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    public class CampusSessionStatePlayModeTests
    {
        private const string CampusSceneName = "CareerQuestCampus";

        [UnityTest]
        public IEnumerator SceneEnablesConnectionApproval()
        {
            yield return LoadCampusScene();
            var bootstrap = Object.FindFirstObjectByType<NetworkBootstrap>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Manager.NetworkConfig.ConnectionApproval, Is.True);
            yield return ShutdownNetworkIfRunning();
        }

        [UnityTest]
        public IEnumerator HostStartsWithHubPhaseOnCampusSessionState()
        {
            yield return LoadCampusScene();
            var bootstrap = Object.FindFirstObjectByType<NetworkBootstrap>();
            Assert.That(bootstrap.StartHostP1(), Is.True);

            yield return bootstrap.WaitForConnection(12f);
            Assert.That(bootstrap.LastConnectionSucceeded, Is.True, bootstrap.LastConnectionError);

            yield return WaitForCampusSessionState();
            Assert.That(CampusSessionState.Instance.CurrentPhase, Is.EqualTo(SessionPhase.Hub));
            Assert.That(CampusSessionState.Instance.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));

            yield return ShutdownNetworkIfRunning();
        }

        [UnityTest]
        public IEnumerator HostMirrorUpdatesWhenGameSessionRouteChanges()
        {
            yield return LoadCampusScene();
            var bootstrap = Object.FindFirstObjectByType<NetworkBootstrap>();
            Assert.That(bootstrap.StartHostP1(), Is.True);
            yield return bootstrap.WaitForConnection(12f);
            yield return WaitForCampusSessionState();

            var session = new GameSession();
            CampusSessionState.Instance.BindHostSession(session);
            session.SetRoute(ActivityRoute.DesignBuild);
            yield return null;

            Assert.That(CampusSessionState.Instance.CurrentPhase, Is.EqualTo(SessionPhase.InRoom));
            Assert.That(CampusSessionState.Instance.CurrentRoute, Is.EqualTo(ActivityRoute.DesignBuild));

            session.SetSessionPhase(SessionPhase.Ceremony);
            yield return null;

            Assert.That(CampusSessionState.Instance.CurrentPhase, Is.EqualTo(SessionPhase.Ceremony));

            yield return ShutdownNetworkIfRunning();
        }

        private static IEnumerator LoadCampusScene()
        {
            yield return ShutdownNetworkIfRunning();
            yield return SceneManager.LoadSceneAsync(CampusSceneName, LoadSceneMode.Single);
        }

        private static IEnumerator WaitForCampusSessionState(float timeoutSeconds = 8f)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline && CampusSessionState.Instance == null)
            {
                yield return null;
            }

            Assert.That(CampusSessionState.Instance, Is.Not.Null, "CampusSessionState should spawn with the host.");
        }

        private static IEnumerator ShutdownNetworkIfRunning()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            yield return null;
        }
    }
}
