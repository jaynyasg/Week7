using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U6 2P scenarios, host-side. This NGO 2.11.2 package ships
    /// NetcodeIntegrationTest only inside the Unity.Netcode.Runtime.Tests
    /// assembly, which is not compiled in this project (no "testables" entry in
    /// Packages/manifest.json) — so true two-client in-process tests are not
    /// available here. These tests exercise the REAL host validation core
    /// (ApplySubmission / ApplyHeldPiece) with simulated partner client ids on a
    /// live spawned network state, proving accept/reject races, reject targeting,
    /// the completion guard, and the attempt lifecycle at the authority. Wire
    /// delivery to a real second client remains a manual 2P evidence row (U9/U14).
    /// </summary>
    public class DesignBuildNetworkSeamPlayModeTests
    {
        private const ulong SimulatedPartnerClientId = 2UL;

        [UnityTest]
        public IEnumerator DuplicateSubmissionRejectsOnlyThePartnerAndKeepsFirstAccept()
        {
            yield return StartHost();
            var state = FindState();
            var hostClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;

            // Player A (host) lands the piece; partner B races the same piece.
            Assert.That(state.ApplySubmission(0, 1, hostClientId), Is.EqualTo(PlacementSubmissionResult.Accepted));
            Assert.That(state.AcceptedCount, Is.EqualTo(1));

            Assert.That(state.ApplySubmission(0, 7, SimulatedPartnerClientId), Is.EqualTo(PlacementSubmissionResult.Rejected));

            // The reject targets the SENDER (P21) and echoes the submission id.
            Assert.That(state.LastRejectClientId, Is.EqualTo(SimulatedPartnerClientId));
            Assert.That(state.LastRejectPieceIndex, Is.EqualTo(0));
            Assert.That(state.LastRejectSubmissionId, Is.EqualTo(7));
            Assert.That(state.LastRejectReason, Is.EqualTo(DesignBuildRejectReason.AlreadyPlaced));

            // Player A is unaffected: the accepted list still holds A's placement.
            Assert.That(state.AcceptedCount, Is.EqualTo(1));
            Assert.That(state.IsAccepted("clinic"), Is.True);

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HostSelfRejectArrivesWithEchoedSubmissionId()
        {
            yield return StartHost();
            var state = FindState();

            var rejectedPieceIndex = -1;
            var rejectedSubmissionId = -1;
            var rejectedReason = DesignBuildRejectReason.None;
            state.PlacementRejected += (pieceIndex, submissionId, reason) =>
            {
                rejectedPieceIndex = pieceIndex;
                rejectedSubmissionId = submissionId;
                rejectedReason = reason;
            };

            state.SubmitPlacement("clinic", 11);
            state.SubmitPlacement("clinic", 12); // duplicate -> sender-targeted reject
            yield return null;

            Assert.That(rejectedPieceIndex, Is.EqualTo(0));
            Assert.That(rejectedSubmissionId, Is.EqualTo(12), "Reject must echo the client submission id.");
            Assert.That(rejectedReason, Is.EqualTo(DesignBuildRejectReason.AlreadyPlaced));

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
                "The duplicate Changed fire on host accepts is removed — list event only.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator SubmissionAfterCompletionIsIgnoredSilently()
        {
            yield return StartHost();
            var state = FindState();

            for (var i = 0; i < 5; i++)
            {
                Assert.That(state.ApplySubmission(i, i + 1, 0UL), Is.EqualTo(PlacementSubmissionResult.Accepted));
            }

            Assert.That(state.Complete, Is.True);

            // Completion guard: post-completion stragglers are ignored — no list
            // change AND no reject response.
            var rejectsBefore = state.LastRejectSubmissionId;
            Assert.That(state.ApplySubmission(0, 99, SimulatedPartnerClientId), Is.EqualTo(PlacementSubmissionResult.IgnoredComplete));
            Assert.That(state.AcceptedCount, Is.EqualTo(5));
            Assert.That(state.LastRejectSubmissionId, Is.EqualTo(rejectsBefore), "Ignored submissions send no reject.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator BeginAttemptAfterCompletionStartsFreshAttempt()
        {
            yield return StartHost();
            var state = FindState();
            var attemptBefore = state.AttemptNumber;

            for (var i = 0; i < 5; i++)
            {
                state.ApplySubmission(i, i + 1, 0UL);
            }

            Assert.That(state.Complete, Is.True);

            // Re-entry after a completed attempt resets the room's network state.
            state.BeginAttempt();

            Assert.That(state.AcceptedCount, Is.EqualTo(0), "Slots render empty on the fresh attempt.");
            Assert.That(state.AttemptNumber, Is.EqualTo(attemptBefore + 1));
            Assert.That(state.ApplySubmission(0, 50, 0UL), Is.EqualTo(PlacementSubmissionResult.Accepted),
                "Drops are accepted again on the fresh attempt.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator BeginAttemptMidAttemptJoinsInProgressStateWithoutWiping()
        {
            yield return StartHost();
            var state = FindState();
            var attemptBefore = state.AttemptNumber;

            // Partner is mid-attempt (2 of 5 placed)...
            state.ApplySubmission(0, 1, 0UL);
            state.ApplySubmission(1, 2, 0UL);
            Assert.That(state.Complete, Is.False);

            // ...a player entering the room must JOIN it, never wipe it.
            state.BeginAttempt();

            Assert.That(state.AcceptedCount, Is.EqualTo(2), "Join-in-progress preserves partner placements.");
            Assert.That(state.AttemptNumber, Is.EqualTo(attemptBefore));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HeldPieceFlagSetsOnPickupAndClearsOnAccept()
        {
            yield return StartHost();
            var state = FindState();

            // P17 plumbing: pickup sets, accept clears.
            state.ApplyHeldPiece(2, SimulatedPartnerClientId);
            Assert.That(state.HeldPieceIndexFor(SimulatedPartnerClientId), Is.EqualTo(2));

            state.ApplySubmission(2, 1, SimulatedPartnerClientId);
            Assert.That(state.HeldPieceIndexFor(SimulatedPartnerClientId), Is.EqualTo(-1));

            // Explicit clear (drop with no zone / reject path).
            state.ApplyHeldPiece(3, SimulatedPartnerClientId);
            state.ApplyHeldPiece(-1, SimulatedPartnerClientId);
            Assert.That(state.HeldPieceIndexFor(SimulatedPartnerClientId), Is.EqualTo(-1));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator ResetForAttemptClearsHeldPieces()
        {
            yield return StartHost();
            var state = FindState();

            for (var i = 0; i < 5; i++)
            {
                state.ApplySubmission(i, i + 1, 0UL);
            }

            state.ApplyHeldPiece(4, SimulatedPartnerClientId);
            state.BeginAttempt();

            Assert.That(state.HeldPieceIndexFor(SimulatedPartnerClientId), Is.EqualTo(-1));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        private static IEnumerator StartHost()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);
        }

        private static DesignBuildNetworkState FindState()
        {
            var state = Object.FindAnyObjectByType<DesignBuildNetworkState>();
            Assert.That(state, Is.Not.Null, "DesignBuildNetworkState should exist in the campus scene.");
            Assert.That(state.IsSpawned, Is.True, "DesignBuildNetworkState should be spawned after host start.");
            return state;
        }
    }
}
