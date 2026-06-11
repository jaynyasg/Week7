using System.Collections;
using CareerQuest;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    public class NetcodePlayModeHarnessTests
    {
        [UnityTest]
        public IEnumerator LobbyJoin_HostHubPhase_AllowsSecondPlayerSlot()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            Assert.That(CampusSessionState.Instance.CurrentPhase, Is.EqualTo(SessionPhase.Hub));
            Assert.That(
                NetworkBootstrap.TryGetJoinRejectionForPhase(SessionPhase.Hub, 1, out _),
                Is.False,
                "Hub with one connected player should still accept a second join.");
            Assert.That(bootstrap.TryGetJoinRejectionReason(out _), Is.False);

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator CoEnterBuilding_HostRouteMirrorsInRoomPhase()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            var session = NetcodePlayModeHarness.BindFreshHostSession();
            NetcodePlayModeHarness.NavigateHostToRoom(session, ActivityRoute.DesignBuild);
            yield return null;

            Assert.That(CampusSessionState.Instance.CurrentPhase, Is.EqualTo(SessionPhase.InRoom));
            Assert.That(CampusSessionState.Instance.CurrentRoute, Is.EqualTo(ActivityRoute.DesignBuild));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator ResultMirror_ClientReadModelMatchesHostProgress()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            var hostSession = NetcodePlayModeHarness.BindFreshHostSession();
            hostSession.RecordResult(
                NetcodePlayModeHarness.SampleDegreeResult(CareerConfig.DesignBuildId, "Design Build"));
            yield return null;

            Assert.That(CampusSessionState.Instance.UniqueCompletedGames, Is.EqualTo(1));

            var clientSession = NetcodePlayModeHarness.CreateClientReadModelFromNetwork();
            Assert.That(clientSession.UniqueCompletedGames, Is.EqualTo(1));
            Assert.That(clientSession.GamesNeededForReveal, Is.EqualTo(2));
            Assert.That(clientSession.RevealReady, Is.False);
            Assert.That(clientSession.CurrentPhase, Is.EqualTo(SessionPhase.Hub));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HostDisconnectMidRoom_ClearsCampusSessionState()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            var session = NetcodePlayModeHarness.BindFreshHostSession();
            NetcodePlayModeHarness.NavigateHostToRoom(session, ActivityRoute.HealthHero);
            yield return null;

            Assert.That(CampusSessionState.Instance.CurrentPhase, Is.EqualTo(SessionPhase.InRoom));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
            Assert.That(CampusSessionState.Instance, Is.Null);
        }

        [UnityTest]
        public IEnumerator HostDisconnectMidCeremony_ClearsCampusSessionState()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            var session = NetcodePlayModeHarness.BindFreshHostSession();
            NetcodePlayModeHarness.NavigateHostToRoom(session, ActivityRoute.LogicCourt);
            NetcodePlayModeHarness.EnterHostCeremony(session);
            yield return null;

            Assert.That(CampusSessionState.Instance.CurrentPhase, Is.EqualTo(SessionPhase.Ceremony));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
            Assert.That(CampusSessionState.Instance, Is.Null);
        }

        [UnityTest]
        public IEnumerator JoinRejected_WhenHostAlreadyInRoom()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            var session = NetcodePlayModeHarness.BindFreshHostSession();
            NetcodePlayModeHarness.NavigateHostToRoom(session, ActivityRoute.HealthHero);
            yield return null;

            Assert.That(bootstrap.TryGetJoinRejectionReason(out var reason), Is.True);
            Assert.That(reason, Does.Contain("activity room"));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator JoinRejected_WhenHostInCeremony()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            var session = NetcodePlayModeHarness.BindFreshHostSession();
            NetcodePlayModeHarness.EnterHostCeremony(session);
            yield return null;

            Assert.That(bootstrap.TryGetJoinRejectionReason(out var reason), Is.True);
            Assert.That(reason, Does.Contain("finishing an activity"));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator JoinRejected_WhenLobbyFull()
        {
            Assert.That(
                NetworkBootstrap.TryGetJoinRejectionForPhase(SessionPhase.Hub, 2, out var reason),
                Is.True);
            Assert.That(reason, Does.Contain("two players"));
            yield return null;
        }
    }
}
