using System.Collections;
using System.Linq;
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

        /// <summary>
        /// U6 compact 2P read model: the host records completed-station reward
        /// facts (station + best tier, first-completion order); a client read
        /// model built from the replicated state derives the SAME completed
        /// stations, accessories, and combo eligibility the host would — the
        /// host-authority single-process seam (true two-client delivery is
        /// manual 2P evidence; this proves the derivation contract host-side).
        /// </summary>
        [UnityTest]
        public IEnumerator ClientReadModelDerivesCompletedStationsAccessoriesAndCombosFromHostFacts()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            var hostSession = NetcodePlayModeHarness.BindFreshHostSession();

            // Host completes a combo-eligible pair (Robotics + Kitchen = Robot
            // Chef). Real play records BOTH the best result and the compact fact;
            // mirror that here so unique count and facts agree.
            CompleteStation(hostSession, CareerQuestCatalog.RoboticsGarageId, CompletionTier.Degree);
            CompleteStation(hostSession, CareerQuestCatalog.CommunityKitchenId, CompletionTier.Practice);
            yield return null;

            Assert.That(CampusSessionState.Instance.UniqueCompletedGames, Is.EqualTo(2));
            Assert.That(CampusSessionState.Instance.StationProgress.RewardFactCount, Is.EqualTo(2));

            // Build the client read model from the replicated state.
            var clientSession = NetcodePlayModeHarness.CreateClientReadModelFromNetwork();

            // Completed stations: order + tier mirror the host facts.
            Assert.That(clientSession.CompletedActivityIds,
                Is.EqualTo(new[] { CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.CommunityKitchenId }),
                "Client renders the same completed stations in first-completion order.");
            Assert.That(clientSession.UniqueCompletedGames, Is.EqualTo(2));
            Assert.That(clientSession.CompletedTier(CareerQuestCatalog.RoboticsGarageId, out var roboticsTier), Is.True);
            Assert.That(roboticsTier, Is.EqualTo(CompletionTier.Degree));

            // Accessories: the client derives the same gear the host would.
            var hostAccessories = AccessoryResolver.ResolveEarned(hostSession).Select(accessory => accessory.Id).ToArray();
            var clientAccessories = AccessoryResolver.ResolveEarned(clientSession).Select(accessory => accessory.Id).ToArray();
            Assert.That(clientAccessories, Is.EqualTo(hostAccessories), "Client accessory derivation matches the host.");
            Assert.That(clientAccessories, Does.Contain("accessory.tool_belt"));
            Assert.That(clientAccessories, Does.Contain("accessory.chef_hat"));

            // Combo eligibility: the same pure pair check on both sides.
            var hostCombos = RewardEventLog.EligibleComboIds(hostSession.CompletedActivityIds);
            var clientCombos = RewardEventLog.EligibleComboIds(clientSession.CompletedActivityIds);
            Assert.That(clientCombos, Is.EqualTo(hostCombos), "Combo eligibility is consistent host/client.");
            Assert.That(clientCombos, Does.Contain("combo.robot_chef"), "Robotics + Kitchen sparks Robot Chef.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        private static void CompleteStation(GameSession hostSession, string stationId, CompletionTier tier)
        {
            var definition = PartyStationDefinitions.GetById(stationId);
            hostSession.RecordResult(PartyStationController.BuildResult(
                definition,
                definition.DefaultSeed,
                ResultSource.Multiplayer,
                complete: tier == CompletionTier.Degree,
                wrongAttempts: 0,
                playElapsedSeconds: 12f));

            // The host also replicates the compact completion fact (R17) — the
            // controller does this on the host authority during real play.
            CampusSessionState.Instance.StationProgress.ServerRecordRewardFact(stationId, tier);
        }
    }
}
