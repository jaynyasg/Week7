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
                // ShootTarget hides the kit's launcher-source pieces (the player
                // launches them off the pad), so count inactive too — the leak
                // check is about non-accumulation, not on-screen visibility.
                Assert.That(Object.FindObjectsByType<DraggablePiece>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                    Is.EqualTo(RoboticsPieceCount), $"cycle {cycle}: pieces never accumulate");

                // Raise a level-2 hint highlight so churn covers pulse teardown.
                // ShootTarget pulses the next-expected toy on its (hidden) kit
                // piece, so count inactive too — same as the piece count above.
                controller.TrySubmitDrop("battery_toast", "slot.wheel_sandwich");
                controller.TrySubmitDrop("battery_toast", "slot.wheel_sandwich");
                Assert.That(Object.FindObjectsByType<ToyHintPulse>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                    Is.EqualTo(1), $"cycle {cycle}: the hint pulse is live before exit");

                app.ShowCampus();
                yield return null;
                yield return null; // deferred Destroy of the cleared world/UI

                Assert.That(Object.FindObjectsByType<DraggablePiece>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                    Is.EqualTo(0), $"cycle {cycle}: no orphaned drag pieces on campus");
                Assert.That(Object.FindObjectsByType<ToyHintPulse>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
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

        /// <summary>
        /// U5/U10: a creative station (Music Remix — ComposeSet + one meter
        /// widget), a care station (Vet Clinic — MatchAndCare), and a Wave 2
        /// station (Green City Builder — BalanceMeters with TWO meter widgets)
        /// join the replay-churn smoke after the Robotics baseline above. Meter
        /// widgets (including Green City's two), mark zones, and care surfaces
        /// must tear down like every other transient station object.
        /// </summary>
        [UnityTest]
        public IEnumerator CreativeAndCareStationChurnLeavesNoResidue()
        {
            var appObject = new GameObject("station-churn-pack-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = appObject.AddComponent<PartyStationController>();
            controller.AutoTick = false;
            controller.QuickPacing = true;

            var rewardEvents = 0;
            controller.RewardEventEmitted += _ => rewardEvents++;

            // Music: 3 sound layers + 1 reaction toy (the meter is a zone).
            // Vet: 1 clue + 1 helper + 3 care toys.
            // Green City: 4 city-piece toys + 2 meter zones (the U10 two-meter
            // widget teardown — both must clear on route change).
            var churnStations = new (string StationId, int PieceCount, int MeterCount)[]
            {
                (CareerQuestCatalog.MusicStudioId, 4, 1),
                (CareerQuestCatalog.VetClinicId, 5, 0),
                (CareerQuestCatalog.GreenCityId, 4, 2)
            };

            for (var cycle = 0; cycle < 2; cycle++)
            {
                foreach (var (stationId, pieceCount, meterCount) in churnStations)
                {
                    Assert.That(app.ShowPartyStation(stationId), Is.True, $"{stationId} cycle {cycle}");
                    yield return MountFrames();

                    Assert.That(CountActive(ToyInteractionKit.DefaultPlayfieldName), Is.EqualTo(1), $"{stationId} cycle {cycle}");
                    Assert.That(CountActive(PartyStationRenderer.SetRootName), Is.EqualTo(1), $"{stationId} cycle {cycle}");
                    Assert.That(Object.FindObjectsByType<DraggablePiece>(FindObjectsSortMode.None).Length,
                        Is.EqualTo(pieceCount), $"{stationId} cycle {cycle}: pieces never accumulate");
                    Assert.That(Object.FindObjectsByType<StationMeterWidget>(FindObjectsSortMode.None).Length,
                        Is.EqualTo(meterCount), $"{stationId} cycle {cycle}: one widget per meter zone");

                    app.ShowCampus();
                    yield return null;
                    yield return null; // deferred Destroy of the cleared world/UI

                    Assert.That(Object.FindObjectsByType<DraggablePiece>(FindObjectsSortMode.None).Length,
                        Is.EqualTo(0), $"{stationId} cycle {cycle}: no orphaned drag pieces");
                    Assert.That(Object.FindObjectsByType<StationMeterWidget>(FindObjectsSortMode.None).Length,
                        Is.EqualTo(0), $"{stationId} cycle {cycle}: no orphaned meter widgets");
                    Assert.That(CountActive(ToyInteractionKit.DefaultPlayfieldName), Is.EqualTo(0), $"{stationId} cycle {cycle}");
                    Assert.That(CountActive(PartyStationRenderer.SetRootName), Is.EqualTo(0), $"{stationId} cycle {cycle}");
                }
            }

            // One completion after all that churn still emits exactly once.
            Assert.That(app.ShowPartyStation(CareerQuestCatalog.VetClinicId), Is.True);
            yield return MountFrames();
            Assert.That(controller.TryCompleteWithGoldenSequence(), Is.True);
            Assert.That(rewardEvents, Is.EqualTo(1), "Churned mounts must not stack completion subscriptions.");
            Assert.That(app.Session.UniqueCompletedGames, Is.EqualTo(1));

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
            Assert.That(Object.FindObjectsByType<DraggablePiece>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                Is.EqualTo(RoboticsPieceCount));

            // The surviving surface is fully playable — a shot on the goal lands.
            Assert.That(controller.TrySubmitDrop("battery_toast", ToyPatternRules.GoalTargetId),
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
