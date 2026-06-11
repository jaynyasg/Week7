using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CareerQuest
{
    /// <summary>
    /// Shared PlayMode helpers for host session, join policy, and teardown.
    /// True simultaneous LAN client requires Unity Multiplayer Play Mode; matrix rows
    /// use host authority plus <see cref="CampusSessionState.ApplyToGameSession"/> for client read models.
    /// </summary>
    public static class NetcodePlayModeHarness
    {
        public const string CampusSceneName = "CareerQuestCampus";

        public static IEnumerator LoadCampusScene()
        {
            yield return ShutdownNetwork();
            yield return SceneManager.LoadSceneAsync(CampusSceneName, LoadSceneMode.Single);
        }

        public static IEnumerator ShutdownNetwork()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            yield return null;
        }

        public static NetworkBootstrap FindBootstrap()
        {
            return UnityEngine.Object.FindFirstObjectByType<NetworkBootstrap>();
        }

        public static IEnumerator WaitForCampusSessionState(float timeoutSeconds = 8f)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline && CampusSessionState.Instance == null)
            {
                yield return null;
            }

        }

        public static IEnumerator StartHostAndWait(NetworkBootstrap bootstrap, float timeoutSeconds = 12f)
        {
            if (bootstrap == null)
            {
                throw new InvalidOperationException("NetworkBootstrap is required.");
            }

            if (!bootstrap.StartHostP1())
            {
                throw new InvalidOperationException("StartHostP1 returned false.");
            }

            yield return bootstrap.WaitForConnection(timeoutSeconds);
            if (!bootstrap.LastConnectionSucceeded)
            {
                throw new InvalidOperationException(bootstrap.LastConnectionError);
            }

            yield return WaitForCampusSessionState();
        }

        public static GameSession BindFreshHostSession()
        {
            var session = new GameSession();
            CampusSessionState.Instance.BindHostSession(session);
            return session;
        }

        public static GameSession CreateClientReadModelFromNetwork()
        {
            var clientSession = new GameSession();
            CampusSessionState.Instance.ApplyToGameSession(clientSession);
            return clientSession;
        }

        public static void NavigateHostToRoom(GameSession session, ActivityRoute roomRoute)
        {
            session.SetRoute(roomRoute);
        }

        public static void EnterHostCeremony(GameSession session)
        {
            session.SetSessionPhase(SessionPhase.Ceremony);
        }

        public static MiniGameResult SampleDegreeResult(string activityId, string displayName)
        {
            return new MiniGameResult(
                activityId,
                displayName,
                CompletionTier.Degree,
                ResultSource.Multiplayer,
                null,
                12f,
                0.92f,
                "Harness sample result");
        }
    }
}
