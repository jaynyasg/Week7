using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U10 2P scenarios, host-side — mirrors DesignBuildNetworkSeamPlayModeTests.
    /// NetcodeIntegrationTest is not compiled in this project (no "testables"
    /// entry), so these tests exercise the REAL host validation core
    /// (ApplySubmission / ApplyHeldPiece) with simulated partner client ids on a
    /// live spawned network state: accept/reject races, reject targeting with
    /// submission-id echo, the completion guard, the attempt lifecycle, and the
    /// P13 host-seeded shuffle. Wire delivery to a real second client remains a
    /// manual 2P evidence row (U14).
    /// </summary>
    public class LogicCourtNetworkSeamPlayModeTests
    {
        private const ulong SimulatedPartnerClientId = 2UL;

        [UnityTest]
        public IEnumerator DuplicateSortRejectsOnlyThePartnerAndKeepsFirstAccept()
        {
            yield return StartHost();
            var state = FindState();
            var hostClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;

            // Player A (host) lands the sort; partner B races the same card.
            Assert.That(state.ApplySubmission(0, 1, hostClientId), Is.EqualTo(PlacementSubmissionResult.Accepted));
            Assert.That(state.CompletedSteps, Is.EqualTo(1));

            Assert.That(state.ApplySubmission(0, 7, SimulatedPartnerClientId), Is.EqualTo(PlacementSubmissionResult.Rejected));

            // The reject targets the SENDER (P21) and echoes the submission id.
            Assert.That(state.LastRejectClientId, Is.EqualTo(SimulatedPartnerClientId));
            Assert.That(state.LastRejectStepIndex, Is.EqualTo(0));
            Assert.That(state.LastRejectSubmissionId, Is.EqualTo(7));
            Assert.That(state.LastRejectReason, Is.EqualTo(LogicCourtRejectReason.AlreadyDone));

            // Player A is unaffected: the completed list still holds A's sort.
            Assert.That(state.CompletedSteps, Is.EqualTo(1));
            Assert.That(state.IsStepComplete(0), Is.True);

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HostSelfRejectArrivesWithEchoedSubmissionId()
        {
            yield return StartHost();
            var state = FindState();

            var rejectedStepIndex = -1;
            var rejectedSubmissionId = -1;
            var rejectedReason = LogicCourtRejectReason.None;
            state.StepRejected += (stepIndex, submissionId, reason) =>
            {
                rejectedStepIndex = stepIndex;
                rejectedSubmissionId = submissionId;
                rejectedReason = reason;
            };

            state.SubmitStep(LogicCourtLayout.EvidenceTestPieceId, 11);
            state.SubmitStep(LogicCourtLayout.EvidenceTestPieceId, 12); // duplicate -> sender-targeted reject
            yield return null;

            Assert.That(rejectedStepIndex, Is.EqualTo(0));
            Assert.That(rejectedSubmissionId, Is.EqualTo(12), "Reject must echo the client submission id.");
            Assert.That(rejectedReason, Is.EqualTo(LogicCourtRejectReason.AlreadyDone));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HostAcceptFiresChangedExactlyOnce()
        {
            yield return StartHost();
            var state = FindState();

            var changedCount = 0;
            state.Changed += () => changedCount++;

            Assert.That(state.ApplySubmission(1, 1, 0UL), Is.EqualTo(PlacementSubmissionResult.Accepted));

            Assert.That(changedCount, Is.EqualTo(1),
                "Host accepts fire Changed via the list event only — single-fire contract.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator SubmissionAfterCompletionIsIgnoredSilently()
        {
            yield return StartHost();
            var state = FindState();

            for (var i = 0; i < LogicCourtNetworkState.RequiredSteps; i++)
            {
                Assert.That(state.ApplySubmission(i, i + 1, 0UL), Is.EqualTo(PlacementSubmissionResult.Accepted));
            }

            Assert.That(state.Complete, Is.True);

            // Completion guard: post-completion stragglers are ignored — no list
            // change AND no reject response.
            var rejectsBefore = state.LastRejectSubmissionId;
            Assert.That(state.ApplySubmission(0, 99, SimulatedPartnerClientId), Is.EqualTo(PlacementSubmissionResult.IgnoredComplete));
            Assert.That(state.CompletedSteps, Is.EqualTo(LogicCourtNetworkState.RequiredSteps));
            Assert.That(state.LastRejectSubmissionId, Is.EqualTo(rejectsBefore), "Ignored submissions send no reject.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator BeginAttemptAfterCompletionStartsFreshAttemptAndReseeds()
        {
            yield return StartHost();
            var state = FindState();
            var attemptBefore = state.AttemptNumber;
            var seedBefore = state.ShuffleSeed;

            for (var i = 0; i < LogicCourtNetworkState.RequiredSteps; i++)
            {
                state.ApplySubmission(i, i + 1, 0UL);
            }

            Assert.That(state.Complete, Is.True);

            // Re-entry after a completed attempt resets the room's network state.
            state.BeginAttempt();

            Assert.That(state.CompletedSteps, Is.EqualTo(0), "Sorts render open on the fresh attempt.");
            Assert.That(state.AttemptNumber, Is.EqualTo(attemptBefore + 1));
            Assert.That(state.ApplySubmission(0, 50, 0UL), Is.EqualTo(PlacementSubmissionResult.Accepted),
                "Drops are accepted again on the fresh attempt.");

            // P13: the reset reseeds and the new seed derives a DIFFERENT order.
            Assert.That(state.ShuffleSeed, Is.Not.Zero);
            Assert.That(state.ShuffleSeed, Is.Not.EqualTo(seedBefore));
            Assert.That(
                ContentShuffle.DeriveOrder(state.ShuffleSeed, LogicCourtLayout.EvidencePieceIds.Length),
                Is.Not.EqualTo(ContentShuffle.DeriveOrder(seedBefore, LogicCourtLayout.EvidencePieceIds.Length)),
                "Consecutive attempts must present different evidence orders.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator BeginAttemptMidAttemptJoinsInProgressStateWithoutWiping()
        {
            yield return StartHost();
            var state = FindState();
            var attemptBefore = state.AttemptNumber;
            var seedBefore = state.ShuffleSeed;

            // Partner is mid-attempt (1 of 3 sorts done)...
            state.ApplySubmission(0, 1, 0UL);
            Assert.That(state.Complete, Is.False);

            // ...a player entering the room must JOIN it, never wipe it.
            state.BeginAttempt();

            Assert.That(state.CompletedSteps, Is.EqualTo(1), "Join-in-progress preserves partner sorts.");
            Assert.That(state.AttemptNumber, Is.EqualTo(attemptBefore));
            Assert.That(state.ShuffleSeed, Is.EqualTo(seedBefore), "Join-in-progress keeps the shared order.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HostSeedsShuffleAtSpawnSoBothClientsDeriveTheSameOrder()
        {
            yield return StartHost();
            var state = FindState();

            // P13: the host seeds at spawn; zero means "not seeded yet".
            Assert.That(state.ShuffleSeed, Is.Not.Zero, "Host must seed the shuffle on spawn.");

            // Both clients derive from the same synced seed — identical order.
            Assert.That(
                ContentShuffle.DeriveOrder(state.ShuffleSeed, LogicCourtLayout.EvidencePieceIds.Length),
                Is.EqualTo(ContentShuffle.DeriveOrder(state.ShuffleSeed, LogicCourtLayout.EvidencePieceIds.Length)));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HeldPieceFlagSetsOnPickupAndClearsOnAccept()
        {
            yield return StartHost();
            var state = FindState();

            // P17 plumbing: pickup sets, accept clears. The evidence_test card is
            // piece index 1 in the held-piece domain (case file rides index 0).
            var heldIndex = LogicCourtNetworkState.PieceIndexFor(LogicCourtLayout.EvidenceTestPieceId);
            state.ApplyHeldPiece(heldIndex, SimulatedPartnerClientId);
            Assert.That(state.HeldPieceIndexFor(SimulatedPartnerClientId), Is.EqualTo(heldIndex));

            state.ApplySubmission(LogicCourtNetworkState.StepIndexFor(LogicCourtLayout.EvidenceTestPieceId), 1, SimulatedPartnerClientId);
            Assert.That(state.HeldPieceIndexFor(SimulatedPartnerClientId), Is.EqualTo(-1));

            // Explicit clear (drop with no zone / reject path).
            state.ApplyHeldPiece(2, SimulatedPartnerClientId);
            state.ApplyHeldPiece(-1, SimulatedPartnerClientId);
            Assert.That(state.HeldPieceIndexFor(SimulatedPartnerClientId), Is.EqualTo(-1));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        private static IEnumerator StartHost()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);
        }

        private static LogicCourtNetworkState FindState()
        {
            var state = Object.FindAnyObjectByType<LogicCourtNetworkState>();
            Assert.That(state, Is.Not.Null, "LogicCourtNetworkState should exist in the campus scene.");
            Assert.That(state.IsSpawned, Is.True, "LogicCourtNetworkState should be spawned after host start.");
            return state;
        }
    }
}
