using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U3 wiring and orchestration coverage: the kit mounts pieces/zones from a
    /// seed's pattern rules, the pattern controller runs the attempt lifecycle
    /// (hint ladder, submission ids, completion latch), and teardown cancels
    /// active drags, clears highlight pulses, drops subscribers, and removes
    /// every transient toy object.
    /// </summary>
    public class ToyInteractionKitTests
    {
        private sealed class StubDragHost : IDragDropHost
        {
            public bool AllowDrag = true;

            public bool CanBeginDrag(string pieceId)
            {
                return AllowDrag;
            }

            public void NotifyPickUp(string pieceId)
            {
            }

            public void NotifyRelease(string pieceId)
            {
            }

            public void HandleDrop(DraggablePiece piece, DropZone zone)
            {
                piece?.SnapToHome();
            }

            public bool WouldAcceptDrop(string pieceId, string zoneId)
            {
                return false;
            }
        }

        private ToyInteractionKit _kit;

        [TearDown]
        public void TearDown()
        {
            DraggablePiece.CancelActiveDrag();
            _kit?.Teardown();
            _kit = null;

            var leftover = GameObject.Find(ToyInteractionKit.DefaultPlayfieldName);
            if (leftover != null)
            {
                Object.DestroyImmediate(leftover);
            }
        }

        private static ToyPatternController ControllerFor(string stationId)
        {
            var definition = PartyStationDefinitions.GetById(stationId);
            return new ToyPatternController(definition, definition.DefaultSeed);
        }

        private ToyInteractionKit MountRobotics(out ToyPatternController controller, out StubDragHost host)
        {
            controller = ControllerFor(CareerQuestCatalog.RoboticsGarageId);
            host = new StubDragHost();
            _kit = new ToyInteractionKit();
            _kit.Mount(null, controller, host, spriteFor: _ => null);
            return _kit;
        }

        [Test]
        public void MountCreatesPiecesAndZonesFromSeedRules()
        {
            var kit = MountRobotics(out var controller, out _);

            // Robotics (ShootTarget): one shared goal zone (the rescue spot); the
            // kit still mounts one zone per rule TargetId, whatever the verb.
            Assert.That(kit.Zones.Count, Is.EqualTo(controller.Rules.TargetIds.Count));
            foreach (var targetId in controller.Rules.TargetIds)
            {
                Assert.That(kit.ZoneFor(targetId), Is.Not.Null, $"Zone {targetId} should be mounted.");
                Assert.That(kit.ZoneFor(targetId).ZoneId, Is.EqualTo(targetId));
            }

            // Pieces: every chain toy plus the reaction toy (no-dead-toys), no meters.
            Assert.That(kit.PieceFor("battery_toast"), Is.Not.Null);
            Assert.That(kit.PieceFor("rescue_flag"), Is.Not.Null, "Reaction toys mount as pokeable pieces.");
            Assert.That(kit.Pieces.Count, Is.EqualTo(5));
        }

        [Test]
        public void RemountTearsDownThePreviousPlayfield()
        {
            var kit = MountRobotics(out var controller, out var host);
            var firstRoot = kit.Root;

            kit.Mount(null, controller, host, spriteFor: _ => null);

            Assert.That(firstRoot == null, Is.True, "The previous playfield root is destroyed on remount.");
            Assert.That(kit.IsMounted, Is.True);
            Assert.That(kit.PieceFor("battery_toast"), Is.Not.Null);
        }

        [Test]
        public void HintLadderEscalatesOnWrongAttemptsAndRecoversOnAccept()
        {
            var controller = ControllerFor(CareerQuestCatalog.RoboticsGarageId);
            Assert.That(controller.HintLevel, Is.EqualTo(0));
            Assert.That(controller.CurrentHintLine, Is.Null);

            // First wrong attempt -> text clue.
            controller.TrySubmitAction(new ToyAction("battery_toast", "slot.wheel_sandwich"));
            Assert.That(controller.HintLevel, Is.EqualTo(1));
            Assert.That(controller.CurrentHintLine, Is.EqualTo(controller.Seed.HintLine));
            Assert.That(controller.HighlightObjectId, Is.Null, "Level 1 never highlights.");

            // Second wrong attempt -> escalation line plus object highlight.
            controller.TrySubmitAction(new ToyAction("battery_toast", "slot.wheel_sandwich"));
            Assert.That(controller.HintLevel, Is.EqualTo(ToyPatternController.MaxHintLevel));
            Assert.That(controller.CurrentHintLine, Is.EqualTo(controller.Seed.EscalationHintLine));
            Assert.That(controller.HighlightObjectId, Is.EqualTo(controller.Rules.NextExpectedObjectId));

            // An accepted action (a shot on the goal) recovers the ladder.
            controller.TrySubmitAction(new ToyAction("battery_toast", ToyPatternRules.GoalTargetId));
            Assert.That(controller.HintLevel, Is.EqualTo(0));
            Assert.That(controller.HighlightObjectId, Is.Null);
        }

        [Test]
        public void IdleTimeRaisesTheFirstHint()
        {
            var controller = ControllerFor(CareerQuestCatalog.RoboticsGarageId);

            controller.NoteIdle(ToyPatternController.IdleHintSeconds - 0.5f);
            Assert.That(controller.HintLevel, Is.EqualTo(0));

            controller.NoteIdle(0.5f);
            Assert.That(controller.HintLevel, Is.EqualTo(1));
        }

        [Test]
        public void CompletedFiresOnceAndLocksFurtherSubmissions()
        {
            var controller = ControllerFor(CareerQuestCatalog.RoboticsGarageId);
            var completedCount = 0;
            var lastReject = ToyRejectReason.None;
            controller.Completed += () => completedCount++;
            controller.ActionRejected += (_, reason) => lastReject = reason;

            foreach (var action in controller.Rules.BuildGoldenActionSequence())
            {
                Assert.That(controller.TrySubmitAction(action).IsAccepted, Is.True);
            }

            Assert.That(completedCount, Is.EqualTo(1));
            Assert.That(controller.Complete, Is.True);

            // Locked: completion bounces further submissions gently and never
            // re-fires Completed (completion idempotence).
            var locked = controller.TrySubmitAction(new ToyAction("battery_toast", ToyPatternRules.GoalTargetId));
            Assert.That(locked.RejectReason, Is.EqualTo(ToyRejectReason.Locked));
            Assert.That(lastReject, Is.EqualTo(ToyRejectReason.Locked));
            Assert.That(completedCount, Is.EqualTo(1));
        }

        [Test]
        public void ResetForAttemptReplaysCleanlyAfterCompletion()
        {
            var controller = ControllerFor(CareerQuestCatalog.RoboticsGarageId);
            foreach (var action in controller.Rules.BuildGoldenActionSequence())
            {
                controller.TrySubmitAction(action);
            }

            controller.MarkResultEmitted();
            Assert.That(controller.IsLocked, Is.True);

            controller.ResetForAttempt();

            Assert.That(controller.IsLocked, Is.False);
            Assert.That(controller.Complete, Is.False);
            Assert.That(controller.HintLevel, Is.EqualTo(0));
            Assert.That(controller.TrySubmitAction(
                new ToyAction("battery_toast", ToyPatternRules.GoalTargetId)).IsAccepted, Is.True);
        }

        [Test]
        public void SubmissionIdsMakeStaleRejectsRecognizable()
        {
            var controller = ControllerFor(CareerQuestCatalog.RoboticsGarageId);

            var first = controller.BeginSubmission("battery_toast");
            Assert.That(controller.IsCurrentSubmission("battery_toast", first), Is.True);

            // A new pickup invalidates the in-flight submission (stale reject path).
            controller.InvalidatePendingSubmission("battery_toast");
            Assert.That(controller.IsCurrentSubmission("battery_toast", first), Is.False);

            // A newer submission supersedes the old id: the old echo is stale.
            var second = controller.BeginSubmission("battery_toast");
            Assert.That(controller.IsCurrentSubmission("battery_toast", first), Is.False);
            Assert.That(controller.IsCurrentSubmission("battery_toast", second), Is.True);

            // Resolving the submission makes a double-delivered reject ignorable.
            controller.CompleteSubmission("battery_toast");
            Assert.That(controller.IsCurrentSubmission("battery_toast", second), Is.False);
        }

        [Test]
        public void EmptyActionRejectsAsUnknownObject()
        {
            var controller = ControllerFor(CareerQuestCatalog.RoboticsGarageId);
            string rejectedObjectId = "sentinel";
            controller.ActionRejected += (objectId, _) => rejectedObjectId = objectId;

            var result = controller.TrySubmitAction(default);

            Assert.That(result.RejectReason, Is.EqualTo(ToyRejectReason.UnknownObject));
            Assert.That(rejectedObjectId, Is.Null);
        }

        [Test]
        public void AuthoritativeMirrorRendersAcceptedSharedStateWithoutLocalValidation()
        {
            var controller = ControllerFor(CareerQuestCatalog.SpaceportId);

            // A client mirrors host-accepted progress even out of sequence order.
            controller.ApplyAuthoritativeAccept("orbit_arrow");
            Assert.That(controller.Rules.IsAccepted("orbit_arrow"), Is.True);
            Assert.That(controller.Complete, Is.False, "Clients never complete locally on their own.");

            controller.ApplyAuthoritativeHint(2);
            Assert.That(controller.HintLevel, Is.EqualTo(2));
        }

        [Test]
        public void ApplyDropOutcomeHandlesAcceptedPendingAndRejectPaths()
        {
            var kit = MountRobotics(out _, out _);
            var piece = kit.PieceFor("battery_toast");
            var feel = piece.GetComponent<DragFeel>();
            feel.AutoTick = false;

            ToyInteractionKit.ApplyDropOutcome(piece, DropSubmitResult.Pending);
            Assert.That(piece.IsAwaitingResult, Is.True);

            piece.IsAwaitingResult = false;
            piece.transform.position = new Vector3(5f, 5f, 0f);
            ToyInteractionKit.ApplyDropOutcome(piece, DropSubmitResult.RejectedWrongSlot);
            feel.Tick(1f); // fast-forward the snap-back tween
            Assert.That((piece.transform.position - piece.HomePosition).magnitude, Is.LessThan(0.01f));

            // Accepted is a no-op (the accept path renders the lockdown itself).
            piece.transform.position = new Vector3(3f, 3f, 0f);
            ToyInteractionKit.ApplyDropOutcome(piece, DropSubmitResult.Accepted);
            Assert.That(piece.transform.position.x, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void LockAcceptedPieceParksTheToyOnItsTargetZone()
        {
            var kit = MountRobotics(out var controller, out _);
            var zone = kit.ZoneFor(controller.Rules.ExpectedTargetFor("battery_toast"));

            kit.LockAcceptedPiece("battery_toast", celebrate: false, accentColor: Color.white);

            var piece = kit.PieceFor("battery_toast");
            Assert.That((piece.transform.position - zone.transform.position).magnitude, Is.LessThan(0.01f));

            kit.UnlockPiece("battery_toast");
            Assert.That((piece.transform.position - piece.HomePosition).magnitude, Is.LessThan(0.01f));
        }

        [Test]
        public void HintHighlightPulsesTheNextToyAndClears()
        {
            var kit = MountRobotics(out _, out _);

            kit.SetHintHighlight("battery_toast");
            Assert.That(kit.HighlightedObjectId, Is.EqualTo("battery_toast"));
            Assert.That(ToyHintPulse.IsShownOn(kit.PieceFor("battery_toast").gameObject), Is.True);

            // Moving the highlight clears the previous pulse (same-frame readable).
            kit.SetHintHighlight("wheel_sandwich");
            Assert.That(ToyHintPulse.IsShownOn(kit.PieceFor("battery_toast").gameObject), Is.False);
            Assert.That(ToyHintPulse.IsShownOn(kit.PieceFor("wheel_sandwich").gameObject), Is.True);

            kit.ClearHintHighlight();
            Assert.That(kit.HighlightedObjectId, Is.Null);
            Assert.That(ToyHintPulse.IsShownOn(kit.PieceFor("wheel_sandwich").gameObject), Is.False);
        }

        [Test]
        public void ApplyPartnerHoldShowsTheIndicatorOnTheHeldToy()
        {
            var kit = MountRobotics(out _, out _);

            var held = ToyInteractionKit.ApplyPartnerHold(kit.Pieces, null, "battery_toast");

            Assert.That(held, Is.EqualTo("battery_toast"));
            Assert.That(PartnerHoldIndicator.IsShownOn(kit.PieceFor("battery_toast").gameObject), Is.True);
        }

        [Test]
        public void TeardownCancelsActiveDragClearsPulsesSubscribersAndToys()
        {
            var kit = MountRobotics(out var controller, out _);
            var completedFired = false;
            controller.Completed += () => completedFired = true;

            var piece = kit.PieceFor("battery_toast");
            Assert.That(piece.BeginDragProgrammatic(), Is.True);
            Assert.That(DraggablePiece.ActiveDrag, Is.EqualTo(piece));
            kit.SetHintHighlight("wheel_sandwich");

            kit.Teardown();

            Assert.That(DraggablePiece.ActiveDrag, Is.Null, "Teardown cancels the active drag.");
            Assert.That(kit.IsMounted, Is.False);
            Assert.That(kit.HighlightedObjectId, Is.Null);
            Assert.That(kit.Pieces.Count, Is.EqualTo(0));
            Assert.That(GameObject.Find(ToyInteractionKit.DefaultPlayfieldName), Is.Null,
                "Teardown removes every transient toy object.");

            // Subscribers were dropped: completing the rules after teardown
            // can never re-fire into a torn-down surface.
            foreach (var action in controller.Rules.BuildGoldenActionSequence())
            {
                controller.TrySubmitAction(action);
            }

            Assert.That(controller.Complete, Is.True);
            Assert.That(completedFired, Is.False, "Teardown unsubscribed the Completed event.");
        }
    }
}
