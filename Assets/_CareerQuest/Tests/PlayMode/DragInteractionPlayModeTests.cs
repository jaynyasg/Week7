using System.Collections;
using System.Collections.Generic;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U6 drag framework tests. All gameplay assertions drive the programmatic
    /// seams (TrySubmitDrop / IsPieceAccepted / drag-lock / reject handler) —
    /// never synthetic pointer events. The pointer shell itself is exercised via
    /// the same BeginDragProgrammatic/DragTo/EndDragAt methods its handlers call,
    /// plus a programmatic EventSystem raycast audit for the "full-screen panel
    /// eats the drag" regression.
    /// </summary>
    public class DragInteractionPlayModeTests
    {
        [UnityTest]
        public IEnumerator SeamAcceptsValidDropAndUpdatesHud()
        {
            var appObject = NewApp(out var app);
            yield return ShowDesignBuild(app);
            var controller = appObject.GetComponent<DesignBuildController>();

            Assert.That(controller.TrySubmitDrop("clinic", "clinic"), Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(controller.IsPieceAccepted("clinic"), Is.True);
            Assert.That(controller.State.Feedback, Does.Contain("Accepted"));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator WrongSlotDropBouncesWithGentleCopy()
        {
            var appObject = NewApp(out var app);
            yield return ShowDesignBuild(app);
            var controller = appObject.GetComponent<DesignBuildController>();

            var rejectedPieceId = (string)null;
            controller.DropRejected += pieceId => rejectedPieceId = pieceId;

            Assert.That(controller.TrySubmitDrop("clinic", "court"), Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(controller.IsPieceAccepted("clinic"), Is.False);
            Assert.That(controller.State.Feedback, Is.EqualTo(DesignBuildController.GentleWrongSlotFeedback));
            Assert.That(rejectedPieceId, Is.EqualTo("clinic"));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator OccupiedSlotRejectsGently()
        {
            var appObject = NewApp(out var app);
            yield return ShowDesignBuild(app);
            var controller = appObject.GetComponent<DesignBuildController>();

            Assert.That(controller.TrySubmitDrop("clinic", "clinic"), Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(controller.TrySubmitDrop("clinic", "clinic"), Is.EqualTo(DropSubmitResult.RejectedOccupied));
            Assert.That(controller.State.Feedback, Is.EqualTo(DesignBuildController.GentleOccupiedFeedback));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator CompletionViaDragsRoutesEmitterCeremonyRouterOnce()
        {
            var appObject = NewApp(out var app);
            yield return ShowDesignBuild(app);
            var controller = appObject.GetComponent<DesignBuildController>();

            var completions = 0;
            controller.Completed += _ => completions++;

            foreach (var piece in controller.Blueprint.Pieces)
            {
                controller.TrySubmitDrop(piece.Id, piece.Id);
            }

            yield return null;

            // Emitter -> ceremony -> router, exactly as the button flow did.
            Assert.That(completions, Is.EqualTo(1), "Exactly one result per attempt.");
            Assert.That(GameObject.Find("CeremonyOverlay"), Is.Not.Null);
            Assert.That(app.Session.GetBestResult(CareerConfig.DesignBuildId), Is.Not.Null);
            Assert.That(GameObject.Find("AchievementGalleryPanel"), Is.Null, "Gallery waits for the ceremony.");

            // Interaction lock: completion/ceremony raises the drag lock.
            Assert.That(controller.IsDragLocked, Is.True);
            Assert.That(controller.CanBeginDrag("clinic"), Is.False);
            Assert.That(controller.TrySubmitDrop("clinic", "clinic"), Is.EqualTo(DropSubmitResult.RejectedLocked));
            Assert.That(completions, Is.EqualTo(1), "Locked drops never emit a second result.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator PieceSnapsBackToTrayFromNoZoneDrop()
        {
            var appObject = NewApp(out var app);
            yield return ShowDesignBuild(app);
            var controller = appObject.GetComponent<DesignBuildController>();
            yield return WaitForPlayfield(controller);

            var piece = controller.PieceFor("clinic");
            Assert.That(piece.BeginDragProgrammatic(), Is.True);
            piece.DragTo(new Vector3(0f, 3.4f, 0f)); // empty sky — no zone
            piece.EndDragAt(new Vector3(0f, 3.4f, 0f));

            // Snap-back tween is 0.15-0.25s ease-out; allow it to finish.
            yield return new WaitForSecondsRealtime(0.5f);

            Assert.That(Vector3.Distance(piece.transform.position, piece.HomePosition), Is.LessThan(0.05f));
            Assert.That(piece.GetComponent<Collider2D>().enabled, Is.True);
            Assert.That(controller.IsPieceAccepted("clinic"), Is.False);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator AcceptedPieceRendersLockedAndCannotBePickedUp()
        {
            var appObject = NewApp(out var app);
            yield return ShowDesignBuild(app);
            var controller = appObject.GetComponent<DesignBuildController>();

            // Accept BEFORE the playfield mounts: the mount must render slot
            // state from the authoritative source, not from drag history (P22).
            Assert.That(controller.TrySubmitDrop("clinic", "clinic"), Is.EqualTo(DropSubmitResult.Accepted));
            yield return WaitForPlayfield(controller);

            var piece = controller.PieceFor("clinic");
            var zone = controller.ZoneFor("clinic");
            Assert.That(zone.IsOccupied, Is.True);
            Assert.That(Vector3.Distance(piece.transform.position, zone.transform.position), Is.LessThan(0.05f));
            Assert.That(piece.BeginDragProgrammatic(), Is.False, "Accepted pieces are not draggable.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator PointerRaycastReachesPieceWithHudMounted()
        {
            var appObject = NewApp(out var app);
            yield return ShowDesignBuild(app);
            var controller = appObject.GetComponent<DesignBuildController>();
            yield return WaitForPlayfield(controller);

            // Framework attach point: ONE Physics2DRaycaster on CameraDirector's CameraHost.
            var cameraHost = CameraDirector.Ensure().CameraHost;
            Assert.That(cameraHost.GetComponent<Physics2DRaycaster>(), Is.Not.Null);

            // Raycast-target audit: with the room HUD mounted, no non-Button
            // image may block the world (the known "drag doesn't work" failure).
            foreach (var image in Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (image.GetComponent<Button>() != null || image.GetComponent<TMPro.TMP_InputField>() != null)
                {
                    continue;
                }

                Assert.That(image.raycastTarget, Is.False,
                    $"'{image.name}' must not block pointer raycasts (UiBuilder non-blocking defaults).");
            }

            // Pointer-down over a piece resolves to the piece, not UI.
            var piece = controller.PieceFor("clinic");
            var camera = CameraDirector.Ensure().Camera;
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = camera.WorldToScreenPoint(piece.transform.position)
            };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);

            Assert.That(results, Is.Not.Empty, "Raycast should hit the piece collider.");
            Assert.That(results[0].gameObject, Is.EqualTo(piece.gameObject),
                "The piece must be the topmost raycast target under the pointer.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator WorldClearMidDragCancelsCleanlyWithoutOrphans()
        {
            var appObject = NewApp(out var app);
            yield return ShowDesignBuild(app);
            var controller = appObject.GetComponent<DesignBuildController>();
            yield return WaitForPlayfield(controller);

            var piece = controller.PieceFor("clinic");
            Assert.That(piece.BeginDragProgrammatic(), Is.True);
            Assert.That(DraggablePiece.ActiveDrag, Is.EqualTo(piece));

            // Route change clears the world mid-drag (disconnect-equivalent teardown).
            app.ShowCampus();

            Assert.That(DraggablePiece.ActiveDrag, Is.Null, "World clear must cancel the active drag.");
            yield return null;
            Assert.That(piece == null, Is.True, "No orphaned drag piece survives the world clear.");
            Assert.That(GameObject.Find(DesignBuildStudioLayout.PlayfieldName), Is.Null);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator StaleRejectDoesNotBounceNewerDragOfSamePiece()
        {
            var appObject = NewApp(out var app);
            yield return ShowDesignBuild(app);
            var controller = appObject.GetComponent<DesignBuildController>();

            // An in-flight submission goes stale the moment a newer drag of the
            // same piece begins (pickup invalidates it).
            var staleSubmissionId = controller.State.BeginSubmission("clinic");
            controller.NotifyPickUp("clinic");

            var feedbackBefore = controller.State.Feedback;
            controller.ProcessRejectedPlacement("clinic", staleSubmissionId, DesignBuildRejectReason.AlreadyPlaced);
            Assert.That(controller.State.Feedback, Is.EqualTo(feedbackBefore),
                "A stale reject must not surface feedback or bounce the newer drag.");

            // Positive control: a CURRENT submission's reject does land.
            var currentSubmissionId = controller.State.BeginSubmission("clinic");
            controller.ProcessRejectedPlacement("clinic", currentSubmissionId, DesignBuildRejectReason.AlreadyPlaced);
            Assert.That(controller.State.Feedback, Is.EqualTo(DesignBuildController.GentleOccupiedFeedback));

            Object.DestroyImmediate(appObject);
        }

        private static GameObject NewApp(out CareerQuestApp app)
        {
            var appObject = new GameObject("drag-interaction-test");
            app = appObject.AddComponent<CareerQuestApp>();
            return appObject;
        }

        private static IEnumerator ShowDesignBuild(CareerQuestApp app)
        {
            yield return null; // Start() renders the entry screen first.
            app.ShowDesignBuild(false);
            yield return null;
        }

        private static IEnumerator WaitForPlayfield(DesignBuildController controller)
        {
            for (var i = 0; i < 240; i++)
            {
                if (controller.PieceFor("clinic") != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Drag playfield should mount after the room veil reveals.");
        }
    }
}
