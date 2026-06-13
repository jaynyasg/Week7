using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U3 2P scenarios for the generic station layer, host-side. Same testing
    /// posture as the three room network-seam suites: NetcodeIntegrationTest is
    /// not available in this project (no testables entry), so these exercise
    /// the REAL host validation core (ApplySubmission / ApplyHeldPiece /
    /// ServerEscalateHint / ServerRecordRewardFact) with simulated partner
    /// client ids on a live spawned state, proving host-validated seed
    /// selection, accept/reject races, reject targeting with submission-id
    /// echo, the completion guard, attempt lifecycle, hint/highlight sync,
    /// meter sync, and the compact reward-fact read model. Wire delivery to a
    /// real second client remains a manual 2P evidence row.
    /// </summary>
    public class PartyStationNetworkStatePlayModeTests
    {
        private const ulong SimulatedPartnerClientId = 2UL;

        [UnityTest]
        public IEnumerator StationProgressRidesTheCampusSessionObjectAndSyncsSeedSelection()
        {
            yield return StartHost();
            var state = FindState();

            // Rides the always-spawned CampusSessionState NetworkObject.
            Assert.That(state.gameObject, Is.EqualTo(CampusSessionState.Instance.gameObject));
            Assert.That(state.HasActiveStation, Is.False);

            var definition = PartyStationDefinitions.GetById(CareerQuestCatalog.RoboticsGarageId);
            state.ServerBeginStation(definition.Id, definition.DefaultSeed.SeedId);

            // Host validates the selected seed; every peer reads it from here.
            Assert.That(state.HasActiveStation, Is.True);
            Assert.That(state.StationId, Is.EqualTo(definition.Id));
            Assert.That(state.SeedId, Is.EqualTo(definition.DefaultSeed.SeedId));
            Assert.That(state.AcceptedCount, Is.EqualTo(0));
            Assert.That(state.Complete, Is.False);

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator GoldenSubmissionsCompleteAtTheHostAuthority()
        {
            yield return StartHost();
            var state = BeginRobotics();
            var hostClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;

            var changedCount = 0;
            state.Changed += () => changedCount++;

            var submissionId = 0;
            foreach (var action in GoldenActions(CareerQuestCatalog.RoboticsGarageId))
            {
                var result = state.ApplySubmission(
                    state.ObjectIndexFor(action.ObjectId),
                    state.TargetIndexFor(action.TargetId),
                    action.Value,
                    ++submissionId,
                    hostClientId);
                Assert.That(result, Is.EqualTo(ToyActionSubmissionResult.Accepted),
                    $"Golden action {action.ObjectId} should be accepted by the host.");
            }

            Assert.That(state.Complete, Is.True, "Completion derives from host-validated shared state.");
            Assert.That(state.IsObjectAccepted("battery_toast"), Is.True);
            Assert.That(changedCount, Is.GreaterThan(0), "Clients re-render from Changed notifications.");

            // Completion guard: post-completion stragglers are ignored — no
            // progress change AND no reject response.
            var acceptedBefore = state.AcceptedCount;
            var rejectsBefore = state.LastRejectSubmissionId;
            var straggler = state.ApplySubmission(
                state.ObjectIndexFor("battery_toast"),
                state.TargetIndexFor("slot.battery_toast"),
                0,
                99,
                SimulatedPartnerClientId);
            Assert.That(straggler, Is.EqualTo(ToyActionSubmissionResult.IgnoredComplete));
            Assert.That(state.AcceptedCount, Is.EqualTo(acceptedBefore));
            Assert.That(state.LastRejectSubmissionId, Is.EqualTo(rejectsBefore), "Ignored submissions send no reject.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator DuplicateSubmissionRejectsOnlyThePartnerAndKeepsFirstAccept()
        {
            yield return StartHost();
            var state = BeginRobotics();
            var hostClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            var batteryIndex = state.ObjectIndexFor("battery_toast");
            var slotIndex = state.TargetIndexFor("slot.battery_toast");

            // Player A (host) lands the toy; partner B races the same toy.
            Assert.That(state.ApplySubmission(batteryIndex, slotIndex, 0, 1, hostClientId),
                Is.EqualTo(ToyActionSubmissionResult.Accepted));
            Assert.That(state.AcceptedCount, Is.EqualTo(1));

            Assert.That(state.ApplySubmission(batteryIndex, slotIndex, 0, 7, SimulatedPartnerClientId),
                Is.EqualTo(ToyActionSubmissionResult.Rejected));

            // The reject targets the SENDER (P21) and echoes the submission id.
            Assert.That(state.LastRejectClientId, Is.EqualTo(SimulatedPartnerClientId));
            Assert.That(state.LastRejectObjectIndex, Is.EqualTo(batteryIndex));
            Assert.That(state.LastRejectSubmissionId, Is.EqualTo(7));
            Assert.That(state.LastRejectReason, Is.EqualTo(ToyRejectReason.AlreadyAccepted));

            // Player A is unaffected: the accepted list still holds A's progress.
            Assert.That(state.AcceptedCount, Is.EqualTo(1));
            Assert.That(state.IsObjectAccepted("battery_toast"), Is.True);

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator WrongTargetAndUnknownObjectRejectWithoutProgress()
        {
            yield return StartHost();
            var state = BeginRobotics();

            var wrongTarget = state.ApplySubmission(
                state.ObjectIndexFor("battery_toast"),
                state.TargetIndexFor("slot.wheel_sandwich"),
                0,
                3,
                SimulatedPartnerClientId);
            Assert.That(wrongTarget, Is.EqualTo(ToyActionSubmissionResult.Rejected));
            Assert.That(state.LastRejectReason, Is.EqualTo(ToyRejectReason.WrongTarget));

            var unknown = state.ApplySubmission(99, 0, 0, 4, SimulatedPartnerClientId);
            Assert.That(unknown, Is.EqualTo(ToyActionSubmissionResult.Rejected));
            Assert.That(state.LastRejectReason, Is.EqualTo(ToyRejectReason.UnknownObject));
            Assert.That(state.LastRejectSubmissionId, Is.EqualTo(4));

            Assert.That(state.AcceptedCount, Is.EqualTo(0));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HostSelfRejectArrivesWithEchoedSubmissionId()
        {
            yield return StartHost();
            var state = BeginRobotics();

            var rejectedObjectIndex = -1;
            var rejectedSubmissionId = -1;
            var rejectedReason = ToyRejectReason.None;
            state.ActionRejected += (objectIndex, submissionId, reason) =>
            {
                rejectedObjectIndex = objectIndex;
                rejectedSubmissionId = submissionId;
                rejectedReason = reason;
            };

            state.SubmitAction("battery_toast", "slot.wheel_sandwich", 0, 12);
            yield return null;

            Assert.That(rejectedObjectIndex, Is.EqualTo(state.ObjectIndexFor("battery_toast")));
            Assert.That(rejectedSubmissionId, Is.EqualTo(12), "Reject must echo the client submission id.");
            Assert.That(rejectedReason, Is.EqualTo(ToyRejectReason.WrongTarget));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator BeginAttemptResetsAfterCompletionAndJoinsMidAttempt()
        {
            yield return StartHost();
            var state = BeginRobotics();
            var hostClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;

            // Mid-attempt join must never wipe partner progress.
            var attemptBefore = state.AttemptNumber;
            state.ApplySubmission(
                state.ObjectIndexFor("battery_toast"), state.TargetIndexFor("slot.battery_toast"), 0, 1, hostClientId);
            state.BeginAttempt();
            Assert.That(state.AcceptedCount, Is.EqualTo(1), "Join-in-progress preserves partner progress.");
            Assert.That(state.AttemptNumber, Is.EqualTo(attemptBefore));

            // Complete, then re-entry starts a fresh attempt.
            var submissionId = 10;
            foreach (var action in GoldenActions(CareerQuestCatalog.RoboticsGarageId))
            {
                state.ApplySubmission(
                    state.ObjectIndexFor(action.ObjectId),
                    state.TargetIndexFor(action.TargetId),
                    action.Value,
                    ++submissionId,
                    hostClientId);
            }

            Assert.That(state.Complete, Is.True);

            state.BeginAttempt();

            Assert.That(state.AcceptedCount, Is.EqualTo(0), "Toys render fresh on the new attempt.");
            Assert.That(state.Complete, Is.False);
            Assert.That(state.AttemptNumber, Is.EqualTo(attemptBefore + 1));
            Assert.That(state.ApplySubmission(
                    state.ObjectIndexFor("battery_toast"), state.TargetIndexFor("slot.battery_toast"), 0, 50, hostClientId),
                Is.EqualTo(ToyActionSubmissionResult.Accepted),
                "Submissions are accepted again on the fresh attempt.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HintLadderSyncsLevelAndHighlightToEveryPeer()
        {
            yield return StartHost();
            var state = BeginRobotics();

            Assert.That(state.HintLevel, Is.EqualTo(0));

            state.ServerEscalateHint();
            Assert.That(state.HintLevel, Is.EqualTo(1));
            Assert.That(state.HighlightObjectIndex, Is.EqualTo(-1), "Level 1 is a text clue only.");

            state.ServerEscalateHint();
            Assert.That(state.HintLevel, Is.EqualTo(2));
            Assert.That(state.HighlightObjectId, Is.EqualTo("battery_toast"),
                "Level 2 highlights the next expected toy for BOTH players.");

            state.ResetForAttempt();
            Assert.That(state.HintLevel, Is.EqualTo(0));
            Assert.That(state.HighlightObjectIndex, Is.EqualTo(-1));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator BalanceMetersSyncMeterValuesAndGateCompletion()
        {
            yield return StartHost();
            var state = FindState();
            var hostClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;

            var definition = PartyStationDefinitions.GetById(CareerQuestCatalog.GreenCityId);
            state.ServerBeginStation(definition.Id, definition.DefaultSeed.SeedId);

            var submissionId = 0;
            foreach (var objectId in new[] { "solar_tile", "garden_block", "bike_path", "water_wheel" })
            {
                Assert.That(state.ApplySubmission(
                        state.ObjectIndexFor(objectId),
                        state.TargetIndexFor(ToyPatternRules.BuildTargetId),
                        0,
                        ++submissionId,
                        hostClientId),
                    Is.EqualTo(ToyActionSubmissionResult.Accepted));
            }

            // Placements pulled the meters down — completion stays gated and the
            // shifted values replicate for both players.
            Assert.That(state.Complete, Is.False);
            Assert.That(state.MeterValue("budget_meter"), Is.LessThan(ToyPatternRules.MeterGreenMin));

            // Boundary values: exactly green-min/green-max complete; the synced
            // read model shows the accepted values.
            state.ApplySubmission(
                state.ObjectIndexFor("budget_meter"),
                state.TargetIndexFor("meter.budget_meter"),
                ToyPatternRules.MeterGreenMin,
                ++submissionId,
                hostClientId);
            Assert.That(state.MeterValue("budget_meter"), Is.EqualTo(ToyPatternRules.MeterGreenMin));
            Assert.That(state.Complete, Is.False, "One balanced meter is not enough.");

            state.ApplySubmission(
                state.ObjectIndexFor("happy_meter"),
                state.TargetIndexFor("meter.happy_meter"),
                ToyPatternRules.MeterGreenMax,
                ++submissionId,
                hostClientId);
            Assert.That(state.MeterValue("happy_meter"), Is.EqualTo(ToyPatternRules.MeterGreenMax));
            Assert.That(state.Complete, Is.True, "Both meters in green completes Green City.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator RewardFactsReplicateCompactCompletionFactsOnly()
        {
            yield return StartHost();
            var state = BeginRobotics();

            state.ServerRecordRewardFact(CareerQuestCatalog.RoboticsGarageId, CompletionTier.Practice);
            Assert.That(state.TryGetRewardFact(CareerQuestCatalog.RoboticsGarageId, out var tier), Is.True);
            Assert.That(tier, Is.EqualTo(CompletionTier.Practice));

            // Best tier wins; replays never duplicate the fact.
            state.ServerRecordRewardFact(CareerQuestCatalog.RoboticsGarageId, CompletionTier.Degree);
            state.ServerRecordRewardFact(CareerQuestCatalog.RoboticsGarageId, CompletionTier.Practice);
            Assert.That(state.RewardFactCount, Is.EqualTo(1));
            state.TryGetRewardFact(CareerQuestCatalog.RoboticsGarageId, out tier);
            Assert.That(tier, Is.EqualTo(CompletionTier.Degree));

            state.ServerRecordRewardFact(CareerQuestCatalog.AiLabId, CompletionTier.Degree);
            Assert.That(state.RewardFactCount, Is.EqualTo(2));
            Assert.That(state.RewardFactStationIdAt(1), Is.EqualTo(CareerQuestCatalog.AiLabId));

            // Reward facts are session-scoped completion facts: they survive the
            // station closing on route change.
            state.ServerEndStation();
            Assert.That(state.HasActiveStation, Is.False);
            Assert.That(state.RewardFactCount, Is.EqualTo(2));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HeldPieceFlagSetsOnPickupAndClearsOnAccept()
        {
            yield return StartHost();
            var state = BeginRobotics();
            var batteryIndex = state.ObjectIndexFor("battery_toast");

            // Presence flag only — never per-frame drag positions.
            state.ApplyHeldPiece(batteryIndex, SimulatedPartnerClientId);
            Assert.That(state.HeldPieceIndexFor(SimulatedPartnerClientId), Is.EqualTo(batteryIndex));
            Assert.That(state.HeldPieceIndexForPartner(0UL), Is.EqualTo(batteryIndex));

            state.ApplySubmission(
                batteryIndex, state.TargetIndexFor("slot.battery_toast"), 0, 1, SimulatedPartnerClientId);
            Assert.That(state.HeldPieceIndexFor(SimulatedPartnerClientId), Is.EqualTo(-1),
                "Accepts clear the sender's held flag.");

            state.ApplyHeldPiece(state.ObjectIndexFor("wheel_sandwich"), SimulatedPartnerClientId);
            state.ApplyHeldPiece(-1, SimulatedPartnerClientId);
            Assert.That(state.HeldPieceIndexFor(SimulatedPartnerClientId), Is.EqualTo(-1));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static IEnumerator StartHost()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);
        }

        private static StationProgressNetworkState FindState()
        {
            Assert.That(CampusSessionState.Instance, Is.Not.Null,
                "CampusSessionState should exist after host start.");
            var state = CampusSessionState.Instance.StationProgress;
            Assert.That(state, Is.Not.Null,
                "StationProgressNetworkState should ride the campus session object.");
            Assert.That(state.IsSpawned, Is.True,
                "StationProgressNetworkState should be spawned after host start.");
            return state;
        }

        private static StationProgressNetworkState BeginRobotics()
        {
            var state = FindState();
            var definition = PartyStationDefinitions.GetById(CareerQuestCatalog.RoboticsGarageId);
            state.ServerBeginStation(definition.Id, definition.DefaultSeed.SeedId);
            return state;
        }

        private static System.Collections.Generic.IReadOnlyList<ToyAction> GoldenActions(string stationId)
        {
            var definition = PartyStationDefinitions.GetById(stationId);
            return ToyPatternRules.ForSeed(definition, definition.DefaultSeed).BuildGoldenActionSequence();
        }
    }
}
