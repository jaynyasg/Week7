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

        /// <summary>
        /// U7 reveal-start sync moment: the host bump is what clients already on
        /// the reveal route consume as one input of their start latch. The true
        /// two-client latch (A skips while B watches) is a manual-evidence row —
        /// the harness is host-only; the latch contract itself is covered in
        /// RevealCinematicPlayModeTests at the director seam.
        /// </summary>
        [UnityTest]
        public IEnumerator HostAnnouncesRevealStartThroughSyncedState()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            var state = CampusSessionState.Instance;
            Assert.That(state, Is.Not.Null);
            Assert.That(state.RevealStartCount, Is.EqualTo(0), "No reveal announced yet.");

            var announced = 0;
            state.RevealStartAnnounced += () => announced++;

            state.ServerAnnounceRevealStart();
            yield return null;

            Assert.That(state.RevealStartCount, Is.EqualTo(1), "Host bump is the reveal-start sync moment.");
            Assert.That(announced, Is.GreaterThanOrEqualTo(1), "Announce event reaches subscribers.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }
    }
}
