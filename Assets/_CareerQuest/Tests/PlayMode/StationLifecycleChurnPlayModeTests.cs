using System.Collections;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U4 lifecycle replay-churn smoke (design doc Lifecycle/performance rule):
    /// rapid enter/exit/replay cycles through the generic station route must
    /// never accumulate station roots, drag pieces, hint highlights, reward
    /// previews, guide panels, playfield coroutines, or duplicate
    /// subscriptions. Starts with Robotics (the U4 proof station); U5/U10 add
    /// a creative and a Wave 2 station to the same loop.
    /// </summary>
    public class StationLifecycleChurnPlayModeTests
    {
        private const int ChurnCycles = 4;
        private const int RoboticsPieceCount = 5; // 4 chain toys + 1 reaction toy

        [SetUp]
        public void SetUp()
        {
            // Test isolation (SceneWipe leak history): residue counting only
            // works from a scene that starts clean.
            PlayModeSceneScrubber.DestroyStaleAppRoots();
        }

        [UnityTest]
        public IEnumerator EnterExitReplayChurnLeavesNoResidueAndKeepsOneResult()
        {
            var appObject = new GameObject("station-churn-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = appObject.AddComponent<PartyStationController>();
            controller.AutoTick = false;
            controller.QuickPacing = true;

            var rewardEvents = 0;
            controller.RewardEventEmitted += _ => rewardEvents++;

            for (var cycle = 0; cycle < ChurnCycles; cycle++)
            {
                Assert.That(app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId), Is.True, $"cycle {cycle}");
                yield return MountFrames();

                // Exactly one playfield, one station set, one guide chip, one
                // reward preview — never one per cycle.
                Assert.That(CountActive(ToyInteractionKit.DefaultPlayfieldName), Is.EqualTo(1), $"cycle {cycle}");
                Assert.That(CountActive(PartyStationRenderer.SetRootName), Is.EqualTo(1), $"cycle {cycle}");
                Assert.That(CountActive(StationGuideView.PanelName), Is.EqualTo(1), $"cycle {cycle}");
                Assert.That(CountActive(StationRewardPreview.PanelName), Is.EqualTo(1), $"cycle {cycle}");
                Assert.That(Object.FindObjectsByType<DraggablePiece>(FindObjectsSortMode.None).Length,
                    Is.EqualTo(RoboticsPieceCount), $"cycle {cycle}: pieces never accumulate");

                // Raise a level-2 hint highlight so churn covers pulse teardown.
                controller.TrySubmitDrop("battery_toast", "slot.wheel_sandwich");
                controller.TrySubmitDrop("battery_toast", "slot.wheel_sandwich");
                Assert.That(Object.FindObjectsByType<ToyHintPulse>(FindObjectsSortMode.None).Length,
                    Is.EqualTo(1), $"cycle {cycle}: the hint pulse is live before exit");

                app.ShowCampus();
                yield return null;
                yield return null; // deferred Destroy of the cleared world/UI

                Assert.That(Object.FindObjectsByType<DraggablePiece>(FindObjectsSortMode.None).Length,
                    Is.EqualTo(0), $"cycle {cycle}: no orphaned drag pieces on campus");
                Assert.That(Object.FindObjectsByType<ToyHintPulse>(FindObjectsSortMode.None).Length,
                    Is.EqualTo(0), $"cycle {cycle}: no orphaned hint pulses");
                Assert.That(CountActive(ToyInteractionKit.DefaultPlayfieldName), Is.EqualTo(0), $"cycle {cycle}");
                Assert.That(CountActive(PartyStationRenderer.SetRootName), Is.EqualTo(0), $"cycle {cycle}");
                Assert.That(CountActive(StationGuideView.PanelName), Is.EqualTo(0), $"cycle {cycle}");
                Assert.That(DraggablePiece.ActiveDrag, Is.Null, $"cycle {cycle}: no stranded active drag");
            }

            // After all that churn, ONE completion still emits exactly one
            // result and one reward event — duplicate subscriptions from the
            // churned mounts would double both.
            Assert.That(app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId), Is.True);
            yield return MountFrames();

            foreach (var action in controller.Pattern.Rules.BuildGoldenActionSequence())
            {
                controller.TrySubmitDrop(action.ObjectId, action.TargetId, action.Value);
            }

            Assert.That(rewardEvents, Is.EqualTo(1), "Churned mounts must not stack completion subscriptions.");
            Assert.That(app.Session.UniqueCompletedGames, Is.EqualTo(1));
            Assert.That(app.Session.GetBestResult(CareerQuestCatalog.RoboticsGarageId), Is.Not.Null);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator RouteRaceRerenderMountsExactlyOnePlayfield()
        {
            var appObject = new GameObject("station-rerender-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = appObject.AddComponent<PartyStationController>();
            controller.AutoTick = false;
            controller.QuickPacing = true;

            // Same-frame double route (the route-race shape): the second render
            // must stop the first playfield coroutine and own the surface.
            Assert.That(app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId), Is.True);
            Assert.That(app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId), Is.True);
            yield return MountFrames();
            yield return null;

            Assert.That(CountActive(ToyInteractionKit.DefaultPlayfieldName), Is.EqualTo(1),
                "A rerender race never stacks playfields.");
            Assert.That(CountActive(StationGuideView.PanelName), Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<DraggablePiece>(FindObjectsSortMode.None).Length,
                Is.EqualTo(RoboticsPieceCount));

            // The surviving surface is fully playable.
            Assert.That(controller.TrySubmitDrop("battery_toast", "slot.battery_toast"),
                Is.EqualTo(DropSubmitResult.Accepted));

            Object.DestroyImmediate(appObject);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static IEnumerator MountFrames()
        {
            yield return null;
            yield return null;
            yield return null;
        }

        private static int CountActive(string objectName)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Count(transform => transform.name == objectName);
        }
    }
}
