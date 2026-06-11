using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    public class CampusSessionStatePlayModeTests
    {
        [UnityTest]
        public IEnumerator SceneEnablesConnectionApproval()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(bootstrap.Manager.NetworkConfig.ConnectionApproval, Is.True);
            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HostStartsWithHubPhaseOnCampusSessionState()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            Assert.That(CampusSessionState.Instance, Is.Not.Null);
            Assert.That(CampusSessionState.Instance.CurrentPhase, Is.EqualTo(SessionPhase.Hub));
            Assert.That(CampusSessionState.Instance.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HostMirrorUpdatesWhenGameSessionRouteChanges()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            var session = NetcodePlayModeHarness.BindFreshHostSession();
            session.SetRoute(ActivityRoute.DesignBuild);
            yield return null;

            Assert.That(CampusSessionState.Instance.CurrentPhase, Is.EqualTo(SessionPhase.InRoom));
            Assert.That(CampusSessionState.Instance.CurrentRoute, Is.EqualTo(ActivityRoute.DesignBuild));

            session.SetSessionPhase(SessionPhase.Ceremony);
            yield return null;

            Assert.That(CampusSessionState.Instance.CurrentPhase, Is.EqualTo(SessionPhase.Ceremony));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }
    }
}
