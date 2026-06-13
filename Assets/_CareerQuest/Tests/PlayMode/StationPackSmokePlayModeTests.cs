using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// All-station smoke: every in-plan station routes through the generic
    /// station-id branch, completes its default seed in quick/golden mode,
    /// emits exactly one normal MiniGameResult, returns to campus, and
    /// re-enters for a replay seed choice — all through the ONE shared
    /// PartyStationController, never a bespoke per-station path.
    ///
    /// ============================ FINAL (U11) ==============================
    /// <see cref="PlayableStationIds"/> is bound directly to
    /// CareerQuestCatalog.PartyStationIds — every in-plan station is playable
    /// through the shared controller, so this is the final all-ten smoke. The
    /// vestigial Wave 2 construction bookkeeping was retired in U11 (Wave 2
    /// gameplay was signed off in U10); the final-gate test below now asserts the
    /// invariant directly against the catalog.
    /// =======================================================================
    /// </summary>
    public class StationPackSmokePlayModeTests
    {
        /// <summary>
        /// THE PLAYABLE-SET SEAM. Bound directly to
        /// CareerQuestCatalog.PartyStationIds so this can never silently drift
        /// from the catalog — the all-station loop iterates the real ten.
        /// </summary>
        private static readonly string[] PlayableStationIds = CareerQuestCatalog.PartyStationIds;

        [SetUp]
        public void SetUp()
        {
            // Test isolation (SceneWipe leak history): stale roots from earlier
            // suites must not pollute object lookups or the result counts.
            PlayModeSceneScrubber.DestroyStaleAppRoots();
        }

        /// <summary>
        /// The plan's core all-station smoke: iterate the currently-playable
        /// station set through station-id routing, complete the default seed in
        /// quick mode, confirm exactly one result, return to campus, and re-enter
        /// for replay. One shared controller, no bespoke per-station code.
        /// </summary>
        [UnityTest]
        public IEnumerator PlayableStationsRouteCompleteReturnAndReplayThroughTheSharedController()
        {
            var appObject = new GameObject("station-pack-smoke-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            var rewardEvents = new List<StationRewardEvent>();
            controller.RewardEventEmitted += rewardEvents.Add;

            for (var index = 0; index < PlayableStationIds.Length; index++)
            {
                var stationId = PlayableStationIds[index];
                var definition = PartyStationDefinitions.GetById(stationId);

                // 1) Route in by station id — never a per-station enum/method.
                Assert.That(app.ShowPartyStation(stationId), Is.True, stationId);
                yield return MountFrames();
                Assert.That(app.CurrentRoute, Is.EqualTo(ActivityRoute.PartyStation), stationId);
                Assert.That(app.CurrentStationId, Is.EqualTo(stationId), stationId);
                Assert.That(controller.Seed.SeedId, Is.EqualTo(definition.DefaultSeed.SeedId),
                    $"{stationId}: first play uses the default seed.");

                // 2) Complete the default seed in quick/golden mode (shared seam).
                Assert.That(controller.TryCompleteWithGoldenSequence(), Is.True, stationId);

                // 3) Exactly one normal MiniGameResult, one reward event.
                var result = app.Session.GetBestResult(stationId);
                Assert.That(result, Is.Not.Null, stationId);
                Assert.That(result.Tier, Is.EqualTo(CompletionTier.Degree), stationId);
                Assert.That(app.Session.UniqueCompletedGames, Is.EqualTo(index + 1),
                    $"{stationId}: each station emits exactly one normal result.");
                Assert.That(rewardEvents.Count, Is.EqualTo(index + 1), stationId);

                // 4) Ceremony owns the handoff; skip it and return to campus.
                Assert.That(GameObject.Find("CeremonyOverlay"), Is.Not.Null, stationId);
                yield return new WaitForSecondsRealtime(CeremonyController.SkipDelaySeconds + 0.25f);
                Assert.That(app.TrySkipCeremony(), Is.True, stationId);
                yield return null;

                app.ShowCampus();
                yield return null;
                Assert.That(app.CurrentRoute, Is.EqualTo(ActivityRoute.Campus), stationId);

                // 5) Re-enter for replay — the seed choice (default + alternate)
                // opens for a completed station.
                Assert.That(app.ShowPartyStation(stationId), Is.True, $"{stationId}: re-enter for replay");
                yield return null;
                Assert.That(controller.IsSeedChoiceOpen, Is.True, $"{stationId}: replay offers a seed choice");
                Assert.That(
                    GameObject.Find($"{PartyStationController.SeedChoiceButtonPrefix}{definition.AlternateSeeds[0].SeedId}"),
                    Is.Not.Null,
                    $"{stationId}: replay exposes the alternate seed.");

                app.ShowCampus();
                yield return null;
            }

            // Every playable station completed once; the count never inflates.
            Assert.That(app.Session.UniqueCompletedGames, Is.EqualTo(PlayableStationIds.Length));

            Object.DestroyImmediate(appObject);
        }

        /// <summary>
        /// The plan's "core rooms continue to route + count toward reveal" half
        /// of the playable set: the three core rooms still mount their own
        /// controllers and emit results that count toward reveal readiness
        /// alongside the Party Pack stations (R2).
        /// </summary>
        [UnityTest]
        public IEnumerator CoreRoomsStillRouteAndCountTowardRevealAlongsideStations()
        {
            var appObject = new GameObject("station-pack-smoke-core-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            // Design Build has a built-in showcase auto-complete seam — the
            // smallest faithful way to record a core-room result.
            app.ShowDesignBuild(showcaseAutoComplete: true);
            yield return null;
            Assert.That(app.CurrentRoute, Is.EqualTo(ActivityRoute.DesignBuild));
            Assert.That(app.Session.GetBestResult(CareerConfig.DesignBuildId), Is.Not.Null,
                "The core Design Build room still emits a normal result.");

            app.ShowCampus();
            yield return null;

            // A core result plus a station completion both count toward the same
            // reveal-readiness budget (no separate channel).
            var controller = PrepareController(appObject);
            Assert.That(app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId), Is.True);
            yield return MountFrames();
            Assert.That(controller.TryCompleteWithGoldenSequence(), Is.True);

            Assert.That(app.Session.UniqueCompletedGames, Is.EqualTo(2),
                "Core room + Party Pack station both count once each.");
            Assert.That(app.Session.GamesNeededForReveal, Is.EqualTo(1),
                "Two unique completions leave one to go for the 3-completion reveal.");

            Object.DestroyImmediate(appObject);
        }

        /// <summary>
        /// The plan's campus-visibility scenario at the final bar: EVERY in-plan
        /// station shows exactly one campus door and routes into the shared
        /// controller from its definition without crashing. The construction-only
        /// framing is gone — every station is in the playable set.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryInPlanStationShowsADoorAndRoutesThroughTheSharedController()
        {
            // Final invariant: the playable set IS the full in-plan catalog.
            Assert.That(
                PlayableStationIds.OrderBy(id => id),
                Is.EquivalentTo(CareerQuestCatalog.PartyStationIds.OrderBy(id => id)),
                "The playable set must cover every in-plan station id.");

            var appObject = new GameObject("station-pack-smoke-doors-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            PrepareController(appObject);

            foreach (var stationId in CareerQuestCatalog.PartyStationIds)
            {
                // Each station has exactly one campus entrance (it is visible).
                Assert.That(
                    WorldAnchors.ActiveEntrancesWithStations.Count(e => e.ResolveStationId() == stationId),
                    Is.EqualTo(1),
                    $"Station '{stationId}' must show exactly one campus door.");

                // Routing in renders from the definition without crashing — the
                // generic branch handles every in-plan id with no bespoke path.
                Assert.That(app.ShowPartyStation(stationId), Is.True, stationId);
                yield return MountFrames();
                Assert.That(app.CurrentStationId, Is.EqualTo(stationId), stationId);

                app.ShowCampus();
                yield return null;
            }

            Object.DestroyImmediate(appObject);
        }

        /// <summary>
        /// THE FINAL GATE (R1 / AE7). The playable set is the full in-plan
        /// catalog, so NO station remains construction-only or "coming soon" —
        /// the final-build invariant. The Wave 2 construction bookkeeping was
        /// retired in U11; this asserts directly against the catalog.
        /// </summary>
        [Test]
        public void NoInPlanStationRemainsConstructionOnly()
        {
            var stillConstruction = CareerQuestCatalog.PartyStationIds
                .Where(id => !PlayableStationIds.Contains(id))
                .ToArray();

            // Final-build invariant (R1 / AE7): every in-plan station is playable
            // through generic station-id routing; none is construction-only or
            // "coming soon".
            Assert.That(stillConstruction, Is.Empty,
                "Final build: no in-plan station may remain construction-only or 'coming soon'.");
        }

        // ------------------------------------------------------------------
        // Helpers (PartyStationRoboticsPlayModeTests conventions)
        // ------------------------------------------------------------------

        private static PartyStationController PrepareController(GameObject appObject)
        {
            var controller = appObject.GetComponent<PartyStationController>()
                ?? appObject.AddComponent<PartyStationController>();
            controller.AutoTick = false; // deterministic clock
            controller.QuickPacing = true; // skip the intro hold; scoring unchanged
            return controller;
        }

        private static IEnumerator MountFrames()
        {
            // Frame 1: room veil reveals + room builds; frame 2: the station
            // playfield coroutine mounts pieces/zones; frame 3: settle.
            yield return null;
            yield return null;
            yield return null;
        }
    }
}
