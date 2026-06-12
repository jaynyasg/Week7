using System.Collections;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U10 Health Hero drag-room scenarios — mirrors DragInteractionPlayModeTests.
    /// All gameplay assertions drive the programmatic seams (TrySubmitDrop /
    /// IsPieceAccepted / drag-lock / reject handler) — never synthetic pointer
    /// events. Game rules are unchanged from the button room: ordered care steps
    /// (clipboard → thermometer → care plan), the bandage always bounces, and at
    /// most one exploration mistake still earns the Degree tier.
    /// </summary>
    public class HealthHeroDragInteractionPlayModeTests
    {
        [UnityTest]
        public IEnumerator SeamAcceptsOrderedCareStepAndUpdatesHud()
        {
            var appObject = NewApp(out var app);
            yield return ShowHealthHero(app);
            var controller = appObject.GetComponent<HealthHeroController>();

            Assert.That(
                controller.TrySubmitDrop(HealthHeroClinicLayout.SymptomClipboardPieceId, HealthHeroClinicLayout.PatientZoneId),
                Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(controller.IsPieceAccepted(HealthHeroClinicLayout.SymptomClipboardPieceId), Is.True);
            Assert.That(controller.State.Feedback, Does.Contain("sore throat"));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator BandageBouncesWithGentleTeachingCopyAndCountsTheMistake()
        {
            var appObject = NewApp(out var app);
            yield return ShowHealthHero(app);
            var controller = appObject.GetComponent<HealthHeroController>();

            var rejectedPieceId = (string)null;
            controller.DropRejected += pieceId => rejectedPieceId = pieceId;

            // The wrong tool teaches, never punishes (Practice-tone rules).
            Assert.That(
                controller.TrySubmitDrop(HealthHeroClinicLayout.BandagePieceId, HealthHeroClinicLayout.PatientZoneId),
                Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(controller.State.Feedback, Is.EqualTo(HealthHeroController.BandageFeedback));
            Assert.That(controller.State.Mistakes, Is.EqualTo(1), "Exploration mistakes count exactly as the button room did.");
            Assert.That(controller.IsPieceAccepted(HealthHeroClinicLayout.BandagePieceId), Is.False);
            Assert.That(rejectedPieceId, Is.EqualTo(HealthHeroClinicLayout.BandagePieceId));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator OutOfOrderDropTeachesTheCareOrderGate()
        {
            var appObject = NewApp(out var app);
            yield return ShowHealthHero(app);
            var controller = appObject.GetComponent<HealthHeroController>();

            // Tool before symptoms — the old "check symptoms first" gate.
            Assert.That(
                controller.TrySubmitDrop(HealthHeroClinicLayout.ThermometerPieceId, HealthHeroClinicLayout.PatientZoneId),
                Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(controller.State.Feedback, Is.EqualTo(HealthHeroController.NeedSymptomsFirstFeedback));
            Assert.That(controller.State.Mistakes, Is.EqualTo(1));

            controller.TrySubmitDrop(HealthHeroClinicLayout.SymptomClipboardPieceId, HealthHeroClinicLayout.PatientZoneId);

            // Care plan before the tool — the old "choose a tool first" gate.
            Assert.That(
                controller.TrySubmitDrop(HealthHeroClinicLayout.CarePlanPieceId, HealthHeroClinicLayout.PatientZoneId),
                Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(controller.State.Feedback, Is.EqualTo(HealthHeroController.NeedToolFirstFeedback));
            Assert.That(controller.State.Mistakes, Is.EqualTo(2));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator CompletedStepRejectsGently()
        {
            var appObject = NewApp(out var app);
            yield return ShowHealthHero(app);
            var controller = appObject.GetComponent<HealthHeroController>();

            Assert.That(
                controller.TrySubmitDrop(HealthHeroClinicLayout.SymptomClipboardPieceId, HealthHeroClinicLayout.PatientZoneId),
                Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(
                controller.TrySubmitDrop(HealthHeroClinicLayout.SymptomClipboardPieceId, HealthHeroClinicLayout.PatientZoneId),
                Is.EqualTo(DropSubmitResult.RejectedOccupied));
            Assert.That(controller.State.Feedback, Is.EqualTo(HealthHeroController.GentleOccupiedFeedback));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator CompletionViaDragsRoutesEmitterCeremonyRouterOnce()
        {
            var appObject = NewApp(out var app);
            yield return ShowHealthHero(app);
            var controller = appObject.GetComponent<HealthHeroController>();

            var completions = 0;
            controller.Completed += _ => completions++;

            foreach (var pieceId in HealthHeroClinicLayout.StepPieceIds)
            {
                Assert.That(
                    controller.TrySubmitDrop(pieceId, HealthHeroClinicLayout.PatientZoneId),
                    Is.EqualTo(DropSubmitResult.Accepted));
            }

            yield return null;

            // Emitter -> ceremony -> router, exactly as the button flow did.
            Assert.That(completions, Is.EqualTo(1), "Exactly one result per attempt.");
            Assert.That(GameObject.Find("CeremonyOverlay"), Is.Not.Null);
            var best = app.Session.GetBestResult(CareerConfig.HealthHeroId);
            Assert.That(best, Is.Not.Null);
            Assert.That(best.Tier, Is.EqualTo(CompletionTier.Degree), "A clean run still earns the Degree tier.");
            Assert.That(GameObject.Find("AchievementGalleryPanel"), Is.Null, "Gallery waits for the ceremony.");

            // Interaction lock: completion/ceremony raises the drag lock.
            Assert.That(controller.IsDragLocked, Is.True);
            Assert.That(controller.CanBeginDrag(HealthHeroClinicLayout.SymptomClipboardPieceId), Is.False);
            Assert.That(
                controller.TrySubmitDrop(HealthHeroClinicLayout.SymptomClipboardPieceId, HealthHeroClinicLayout.PatientZoneId),
                Is.EqualTo(DropSubmitResult.RejectedLocked));
            Assert.That(completions, Is.EqualTo(1), "Locked drops never emit a second result.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator TwoExplorationMistakesStillCompleteWithPracticeTier()
        {
            var appObject = NewApp(out var app);
            yield return ShowHealthHero(app);
            var controller = appObject.GetComponent<HealthHeroController>();

            // Unchanged success rule: more than one mistake = Practice, never failure.
            controller.TrySubmitDrop(HealthHeroClinicLayout.BandagePieceId, HealthHeroClinicLayout.PatientZoneId);
            controller.TrySubmitDrop(HealthHeroClinicLayout.BandagePieceId, HealthHeroClinicLayout.PatientZoneId);
            Assert.That(controller.State.Mistakes, Is.EqualTo(2));

            foreach (var pieceId in HealthHeroClinicLayout.StepPieceIds)
            {
                controller.TrySubmitDrop(pieceId, HealthHeroClinicLayout.PatientZoneId);
            }

            yield return null;

            var best = app.Session.GetBestResult(CareerConfig.HealthHeroId);
            Assert.That(best, Is.Not.Null);
            Assert.That(best.Tier, Is.EqualTo(CompletionTier.Practice));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator PieceSnapsBackToTrayFromNoZoneDrop()
        {
            var appObject = NewApp(out var app);
            yield return ShowHealthHero(app);
            var controller = appObject.GetComponent<HealthHeroController>();
            yield return WaitForPlayfield(controller);

            var piece = controller.PieceFor(HealthHeroClinicLayout.SymptomClipboardPieceId);
            Assert.That(piece.BeginDragProgrammatic(), Is.True);
            piece.DragTo(new Vector3(0f, 3.4f, 0f)); // empty sky — no zone
            piece.EndDragAt(new Vector3(0f, 3.4f, 0f));

            // Snap-back tween is 0.15-0.25s ease-out; allow it to finish.
            yield return new WaitForSecondsRealtime(0.5f);

            Assert.That(Vector3.Distance(piece.transform.position, piece.HomePosition), Is.LessThan(0.05f));
            Assert.That(piece.GetComponent<Collider2D>().enabled, Is.True);
            Assert.That(controller.IsPieceAccepted(HealthHeroClinicLayout.SymptomClipboardPieceId), Is.False);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator AcceptedStepRendersLockedOnMountAndCannotBePickedUp()
        {
            var appObject = NewApp(out var app);
            yield return ShowHealthHero(app);
            var controller = appObject.GetComponent<HealthHeroController>();

            // Accept BEFORE the playfield mounts: the mount must render step
            // state from the authoritative source, not from drag history (P22).
            Assert.That(
                controller.TrySubmitDrop(HealthHeroClinicLayout.SymptomClipboardPieceId, HealthHeroClinicLayout.PatientZoneId),
                Is.EqualTo(DropSubmitResult.Accepted));
            yield return WaitForPlayfield(controller);

            var piece = controller.PieceFor(HealthHeroClinicLayout.SymptomClipboardPieceId);
            var applied = HealthHeroClinicLayout.AppliedPosition(HealthHeroClinicLayout.SymptomClipboardPieceId);
            Assert.That(
                Vector3.Distance(piece.transform.position, new Vector3(applied.x, applied.y, 0f)),
                Is.LessThan(0.05f),
                "Accepted care pieces park at their applied anchor.");
            Assert.That(piece.BeginDragProgrammatic(), Is.False, "Accepted pieces are not draggable.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator WorldClearMidDragCancelsCleanlyWithoutOrphans()
        {
            var appObject = NewApp(out var app);
            yield return ShowHealthHero(app);
            var controller = appObject.GetComponent<HealthHeroController>();
            yield return WaitForPlayfield(controller);

            var piece = controller.PieceFor(HealthHeroClinicLayout.SymptomClipboardPieceId);
            Assert.That(piece.BeginDragProgrammatic(), Is.True);
            Assert.That(DraggablePiece.ActiveDrag, Is.EqualTo(piece));

            // Route change clears the world mid-drag (disconnect-equivalent teardown).
            app.ShowCampus();

            Assert.That(DraggablePiece.ActiveDrag, Is.Null, "World clear must cancel the active drag.");
            yield return null;
            Assert.That(piece == null, Is.True, "No orphaned drag piece survives the world clear.");
            Assert.That(GameObject.Find(HealthHeroClinicLayout.PlayfieldName), Is.Null);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator StaleRejectDoesNotBounceNewerDragOfSamePiece()
        {
            var appObject = NewApp(out var app);
            yield return ShowHealthHero(app);
            var controller = appObject.GetComponent<HealthHeroController>();

            // An in-flight submission goes stale the moment a newer drag of the
            // same piece begins (pickup invalidates it).
            var pieceId = HealthHeroClinicLayout.SymptomClipboardPieceId;
            var staleSubmissionId = controller.State.BeginSubmission(pieceId);
            controller.NotifyPickUp(pieceId);

            var feedbackBefore = controller.State.Feedback;
            controller.ProcessRejectedStep(pieceId, staleSubmissionId, HealthHeroRejectReason.AlreadyDone);
            Assert.That(controller.State.Feedback, Is.EqualTo(feedbackBefore),
                "A stale reject must not surface feedback or bounce the newer drag.");

            // Positive control: a CURRENT submission's reject does land.
            var currentSubmissionId = controller.State.BeginSubmission(pieceId);
            controller.ProcessRejectedStep(pieceId, currentSubmissionId, HealthHeroRejectReason.AlreadyDone);
            Assert.That(controller.State.Feedback, Is.EqualTo(HealthHeroController.GentleOccupiedFeedback));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator TrayOrderIsStableWithinAnAttemptAndChangesAcrossAttempts()
        {
            var appObject = NewApp(out var app);
            yield return ShowHealthHero(app);
            var controller = appObject.GetComponent<HealthHeroController>();

            // P13: within an attempt the derived order is deterministic...
            var firstOrder = controller.TrayPieceOrder.ToArray();
            Assert.That(controller.TrayPieceOrder.ToArray(), Is.EqualTo(firstOrder),
                "Same seed must always derive the same tray order.");
            Assert.That(firstOrder.OrderBy(id => id), Is.EqualTo(HealthHeroClinicLayout.PieceIds.OrderBy(id => id)),
                "The tray order is a permutation of every tool piece.");

            // ...and a fresh attempt reseeds into a different order.
            controller.ResetActivity();
            Assert.That(controller.TrayPieceOrder.ToArray(), Is.Not.EqualTo(firstOrder),
                "Consecutive attempts must present different tray orders.");

            Object.DestroyImmediate(appObject);
        }

        private static GameObject NewApp(out CareerQuestApp app)
        {
            var appObject = new GameObject("health-hero-drag-test");
            app = appObject.AddComponent<CareerQuestApp>();
            return appObject;
        }

        private static IEnumerator ShowHealthHero(CareerQuestApp app)
        {
            yield return null; // Start() renders the entry screen first.
            app.ShowHealthHero();
            yield return null;
        }

        private static IEnumerator WaitForPlayfield(HealthHeroController controller)
        {
            for (var i = 0; i < 240; i++)
            {
                if (controller.PieceFor(HealthHeroClinicLayout.SymptomClipboardPieceId) != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Health Hero drag playfield should mount after the room veil reveals.");
        }
    }
}
