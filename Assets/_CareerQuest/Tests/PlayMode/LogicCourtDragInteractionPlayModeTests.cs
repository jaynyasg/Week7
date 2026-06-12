using System.Collections;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U10 Logic Court drag-room scenarios — mirrors DragInteractionPlayModeTests.
    /// All gameplay assertions drive the programmatic seams (TrySubmitDrop /
    /// IsPieceAccepted / drag-lock / reject handler) — never synthetic pointer
    /// events. Game rules are unchanged from the button room: review the case
    /// first, sort test/blueprint as helpful and paint as not helpful, and at
    /// most one exploration mistake still earns the Degree tier. The judge stamps
    /// the conclusion on completion (P14).
    /// </summary>
    public class LogicCourtDragInteractionPlayModeTests
    {
        [UnityTest]
        public IEnumerator CaseFileOnPodiumOpensSorting()
        {
            var appObject = NewApp(out var app);
            yield return ShowLogicCourt(app);
            var controller = appObject.GetComponent<LogicCourtController>();

            Assert.That(
                controller.TrySubmitDrop(LogicCourtLayout.CaseFilePieceId, LogicCourtLayout.PodiumZoneId),
                Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(controller.State.CaseReviewed, Is.True);
            Assert.That(controller.IsPieceAccepted(LogicCourtLayout.CaseFilePieceId), Is.True);
            Assert.That(controller.State.Feedback, Is.EqualTo(LogicCourtController.CaseReviewedFeedback));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator EvidenceBeforeReviewTeachesTheReviewGate()
        {
            var appObject = NewApp(out var app);
            yield return ShowLogicCourt(app);
            var controller = appObject.GetComponent<LogicCourtController>();

            var rejectedPieceId = (string)null;
            controller.DropRejected += pieceId => rejectedPieceId = pieceId;

            // Same gate as the old "Review the case before sorting evidence."
            Assert.That(
                controller.TrySubmitDrop(LogicCourtLayout.EvidenceTestPieceId, LogicCourtLayout.HelpfulZoneId),
                Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(controller.State.Feedback, Is.EqualTo(LogicCourtController.NeedReviewFeedback));
            Assert.That(controller.State.Mistakes, Is.EqualTo(1));
            Assert.That(rejectedPieceId, Is.EqualTo(LogicCourtLayout.EvidenceTestPieceId));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator CaseFileAndEvidenceBounceFromTheWrongSurfaces()
        {
            var appObject = NewApp(out var app);
            yield return ShowLogicCourt(app);
            var controller = appObject.GetComponent<LogicCourtController>();

            // The case file belongs on the podium, not in a sorting zone.
            Assert.That(
                controller.TrySubmitDrop(LogicCourtLayout.CaseFilePieceId, LogicCourtLayout.HelpfulZoneId),
                Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(controller.State.Feedback, Is.EqualTo(LogicCourtController.CaseFileZoneFeedback));

            // Evidence belongs in the sorting zones, not on the podium.
            Assert.That(
                controller.TrySubmitDrop(LogicCourtLayout.EvidenceTestPieceId, LogicCourtLayout.PodiumZoneId),
                Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(controller.State.Feedback, Is.EqualTo(LogicCourtController.EvidenceOnPodiumFeedback));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator WrongZoneSortBouncesWithGentleTeachingCopy()
        {
            var appObject = NewApp(out var app);
            yield return ShowLogicCourt(app);
            var controller = appObject.GetComponent<LogicCourtController>();

            controller.TrySubmitDrop(LogicCourtLayout.CaseFilePieceId, LogicCourtLayout.PodiumZoneId);

            // The paint opinion dropped on Helpful teaches WHY it is not proof.
            Assert.That(
                controller.TrySubmitDrop(LogicCourtLayout.EvidencePaintPieceId, LogicCourtLayout.HelpfulZoneId),
                Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(controller.State.Feedback, Does.Contain("opinion"));
            Assert.That(controller.State.Mistakes, Is.EqualTo(1), "Exploration mistakes count exactly as the button room did.");
            Assert.That(controller.IsPieceAccepted(LogicCourtLayout.EvidencePaintPieceId), Is.False);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator SortedCardRejectsGently()
        {
            var appObject = NewApp(out var app);
            yield return ShowLogicCourt(app);
            var controller = appObject.GetComponent<LogicCourtController>();

            controller.TrySubmitDrop(LogicCourtLayout.CaseFilePieceId, LogicCourtLayout.PodiumZoneId);

            Assert.That(
                controller.TrySubmitDrop(LogicCourtLayout.EvidenceTestPieceId, LogicCourtLayout.HelpfulZoneId),
                Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(
                controller.TrySubmitDrop(LogicCourtLayout.EvidenceTestPieceId, LogicCourtLayout.HelpfulZoneId),
                Is.EqualTo(DropSubmitResult.RejectedOccupied));
            Assert.That(controller.State.Feedback, Is.EqualTo(LogicCourtController.GentleOccupiedFeedback));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator CompletionStampsTheJudgeAndRoutesCeremonyOnce()
        {
            var appObject = NewApp(out var app);
            yield return ShowLogicCourt(app);
            var controller = appObject.GetComponent<LogicCourtController>();

            var completions = 0;
            controller.Completed += _ => completions++;

            Assert.That(controller.HasStamped, Is.False);
            CompleteCourtCleanly(controller);
            yield return null;

            // Emitter -> ceremony -> router, exactly as the button flow did.
            Assert.That(completions, Is.EqualTo(1), "Exactly one result per attempt.");
            Assert.That(controller.HasStamped, Is.True, "The judge stamps the conclusion on completion (P14).");
            Assert.That(GameObject.Find("CeremonyOverlay"), Is.Not.Null);
            var best = app.Session.GetBestResult(CareerConfig.LogicCourtId);
            Assert.That(best, Is.Not.Null);
            Assert.That(best.Tier, Is.EqualTo(CompletionTier.Degree), "A clean run still earns the Degree tier.");
            Assert.That(GameObject.Find("AchievementGalleryPanel"), Is.Null, "Gallery waits for the ceremony.");

            // Interaction lock: completion/ceremony raises the drag lock.
            Assert.That(controller.IsDragLocked, Is.True);
            Assert.That(controller.CanBeginDrag(LogicCourtLayout.EvidenceTestPieceId), Is.False);
            Assert.That(
                controller.TrySubmitDrop(LogicCourtLayout.EvidenceTestPieceId, LogicCourtLayout.HelpfulZoneId),
                Is.EqualTo(DropSubmitResult.RejectedLocked));
            Assert.That(completions, Is.EqualTo(1), "Locked drops never emit a second result.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator TwoExplorationMistakesStillCompleteWithPracticeTier()
        {
            var appObject = NewApp(out var app);
            yield return ShowLogicCourt(app);
            var controller = appObject.GetComponent<LogicCourtController>();

            // Unchanged success rule: more than one mistake = Practice, never failure.
            controller.TrySubmitDrop(LogicCourtLayout.EvidenceTestPieceId, LogicCourtLayout.HelpfulZoneId); // before review
            controller.TrySubmitDrop(LogicCourtLayout.CaseFilePieceId, LogicCourtLayout.PodiumZoneId);
            controller.TrySubmitDrop(LogicCourtLayout.EvidencePaintPieceId, LogicCourtLayout.HelpfulZoneId); // wrong zone
            Assert.That(controller.State.Mistakes, Is.EqualTo(2));

            foreach (var pieceId in LogicCourtLayout.EvidencePieceIds)
            {
                controller.TrySubmitDrop(pieceId, LogicCourtLayout.CorrectZoneFor(pieceId));
            }

            yield return null;

            var best = app.Session.GetBestResult(CareerConfig.LogicCourtId);
            Assert.That(best, Is.Not.Null);
            Assert.That(best.Tier, Is.EqualTo(CompletionTier.Practice));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator CardSnapsBackToTrayFromNoZoneDrop()
        {
            var appObject = NewApp(out var app);
            yield return ShowLogicCourt(app);
            var controller = appObject.GetComponent<LogicCourtController>();
            yield return WaitForPlayfield(controller);

            var piece = controller.PieceFor(LogicCourtLayout.EvidenceTestPieceId);
            Assert.That(piece.BeginDragProgrammatic(), Is.True);
            piece.DragTo(new Vector3(0f, 3.4f, 0f)); // empty sky — no zone
            piece.EndDragAt(new Vector3(0f, 3.4f, 0f));

            // Snap-back tween is 0.15-0.25s ease-out; allow it to finish.
            yield return new WaitForSecondsRealtime(0.5f);

            Assert.That(Vector3.Distance(piece.transform.position, piece.HomePosition), Is.LessThan(0.05f));
            Assert.That(piece.GetComponent<Collider2D>().enabled, Is.True);
            Assert.That(controller.IsPieceAccepted(LogicCourtLayout.EvidenceTestPieceId), Is.False);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator WorldClearMidDragCancelsCleanlyWithoutOrphans()
        {
            var appObject = NewApp(out var app);
            yield return ShowLogicCourt(app);
            var controller = appObject.GetComponent<LogicCourtController>();
            yield return WaitForPlayfield(controller);

            var piece = controller.PieceFor(LogicCourtLayout.EvidenceTestPieceId);
            Assert.That(piece.BeginDragProgrammatic(), Is.True);
            Assert.That(DraggablePiece.ActiveDrag, Is.EqualTo(piece));

            // Route change clears the world mid-drag (disconnect-equivalent teardown).
            app.ShowCampus();

            Assert.That(DraggablePiece.ActiveDrag, Is.Null, "World clear must cancel the active drag.");
            yield return null;
            Assert.That(piece == null, Is.True, "No orphaned drag piece survives the world clear.");
            Assert.That(GameObject.Find(LogicCourtLayout.PlayfieldName), Is.Null);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator StaleRejectDoesNotBounceNewerDragOfSamePiece()
        {
            var appObject = NewApp(out var app);
            yield return ShowLogicCourt(app);
            var controller = appObject.GetComponent<LogicCourtController>();

            // An in-flight submission goes stale the moment a newer drag of the
            // same piece begins (pickup invalidates it).
            var pieceId = LogicCourtLayout.EvidenceTestPieceId;
            var staleSubmissionId = controller.State.BeginSubmission(pieceId);
            controller.NotifyPickUp(pieceId);

            var feedbackBefore = controller.State.Feedback;
            controller.ProcessRejectedStep(pieceId, staleSubmissionId, LogicCourtRejectReason.AlreadyDone);
            Assert.That(controller.State.Feedback, Is.EqualTo(feedbackBefore),
                "A stale reject must not surface feedback or bounce the newer drag.");

            // Positive control: a CURRENT submission's reject does land.
            var currentSubmissionId = controller.State.BeginSubmission(pieceId);
            controller.ProcessRejectedStep(pieceId, currentSubmissionId, LogicCourtRejectReason.AlreadyDone);
            Assert.That(controller.State.Feedback, Is.EqualTo(LogicCourtController.GentleOccupiedFeedback));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator CaseFileRidesTraySlotZeroWhileEvidenceOrderShuffles()
        {
            var appObject = NewApp(out var app);
            yield return ShowLogicCourt(app);
            var controller = appObject.GetComponent<LogicCourtController>();
            yield return WaitForPlayfield(controller);

            // "Review first" stays readable: the case file always leads the tray.
            var casePiece = controller.PieceFor(LogicCourtLayout.CaseFilePieceId);
            var slotZero = LogicCourtLayout.TrayPosition(0);
            Assert.That(
                Vector3.Distance(casePiece.transform.position, new Vector3(slotZero.x, slotZero.y, 0f)),
                Is.LessThan(0.05f));

            // P13: within an attempt the derived evidence order is deterministic...
            var firstOrder = controller.EvidenceTrayOrder.ToArray();
            Assert.That(controller.EvidenceTrayOrder.ToArray(), Is.EqualTo(firstOrder),
                "Same seed must always derive the same evidence order.");
            Assert.That(firstOrder.OrderBy(id => id), Is.EqualTo(LogicCourtLayout.EvidencePieceIds.OrderBy(id => id)),
                "The tray order is a permutation of every evidence card.");

            // ...and a fresh attempt reseeds into a different order.
            controller.ResetActivity();
            Assert.That(controller.EvidenceTrayOrder.ToArray(), Is.Not.EqualTo(firstOrder),
                "Consecutive attempts must present different evidence orders.");

            Object.DestroyImmediate(appObject);
        }

        private static void CompleteCourtCleanly(LogicCourtController controller)
        {
            Assert.That(
                controller.TrySubmitDrop(LogicCourtLayout.CaseFilePieceId, LogicCourtLayout.PodiumZoneId),
                Is.EqualTo(DropSubmitResult.Accepted));

            foreach (var pieceId in LogicCourtLayout.EvidencePieceIds)
            {
                Assert.That(
                    controller.TrySubmitDrop(pieceId, LogicCourtLayout.CorrectZoneFor(pieceId)),
                    Is.EqualTo(DropSubmitResult.Accepted));
            }
        }

        private static GameObject NewApp(out CareerQuestApp app)
        {
            var appObject = new GameObject("logic-court-drag-test");
            app = appObject.AddComponent<CareerQuestApp>();
            return appObject;
        }

        private static IEnumerator ShowLogicCourt(CareerQuestApp app)
        {
            yield return null; // Start() renders the entry screen first.
            app.ShowLogicCourt();
            yield return null;
        }

        private static IEnumerator WaitForPlayfield(LogicCourtController controller)
        {
            for (var i = 0; i < 240; i++)
            {
                if (controller.PieceFor(LogicCourtLayout.CaseFilePieceId) != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Logic Court drag playfield should mount after the room veil reveals.");
        }
    }
}
