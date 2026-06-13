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
    /// U8 all-station smoke: every CURRENTLY-PLAYABLE station routes through the
    /// generic station-id branch, completes its default seed in quick/golden
    /// mode, emits exactly one normal MiniGameResult, returns to campus, and
    /// re-enters for a replay seed choice — all through the ONE shared
    /// PartyStationController, never a bespoke per-station path.
    ///
    /// ============================ U10 / U11 SEAM ============================
    /// <see cref="PlayableStationIds"/> is the single switch that grows this
    /// smoke as the station pack lands:
    ///
    ///   - U8 (now):  the six first-wave stations are playable; the four Wave 2
    ///                stations (<see cref="Wave2StationIds"/>) show a temporary
    ///                construction-site presentation on the campus and are NOT
    ///                iterated here yet (U10 builds their gameplay).
    ///   - U10:       MOVE the four Wave 2 ids from <see cref="Wave2StationIds"/>
    ///                into <see cref="PlayableStationIds"/> (ideally make
    ///                PlayableStationIds == CareerQuestCatalog.PartyStationIds).
    ///                The full-pack loop below then covers all ten.
    ///   - U11:       the final-gate test
    ///                <see cref="NoInPlanStationRemainsConstructionOnly"/> flips
    ///                from documenting the seam to asserting that EVERY in-plan
    ///                station is playable (no "coming soon" / construction-only),
    ///                i.e. PlayableStationIds covers CareerQuestCatalog.PartyStationIds.
    /// =======================================================================
    /// </summary>
    public class StationPackSmokePlayModeTests
    {
        /// <summary>
        /// THE PLAYABLE-SET SEAM (U10 flips this). The station-id-routed stations
        /// that are fully playable today: the first-wave pack. U10 adds the Wave
        /// 2 ids here; U11 asserts this equals CareerQuestCatalog.PartyStationIds.
        /// </summary>
        private static readonly string[] PlayableStationIds =
        {
            CareerQuestCatalog.RoboticsGarageId,
            CareerQuestCatalog.AiLabId,
            CareerQuestCatalog.CommunityKitchenId,
            CareerQuestCatalog.MusicStudioId,
            CareerQuestCatalog.VetClinicId,
            CareerQuestCatalog.GameStudioId
        };

        /// <summary>
        /// Wave 2 stations — present as campus definitions + construction sites,
        /// but their gameplay sign-off is U10. The smoke must NOT iterate these
        /// to completion yet (they would otherwise turn this into a spurious
        /// pre-U10 failure surface). U10 empties this list into PlayableStationIds.
        /// </summary>
        private static readonly string[] Wave2StationIds =
        {
            CareerQuestCatalog.WeatherLabId,
            CareerQuestCatalog.SpaceportId,
            CareerQuestCatalog.NewsroomId,
            CareerQuestCatalog.GreenCityId
        };

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
        /// The plan's campus-visibility scenario: Wave 2 stations are visible on
        /// the campus (a door + a construction-site presentation) but are not yet
        /// signed off as playable. Routing into one must NOT crash — the shared
        /// controller renders the station from its definition — but this smoke
        /// deliberately does not assert their completion (U10 owns that).
        /// </summary>
        [UnityTest]
        public IEnumerator Wave2StationsAreVisibleAndRoutableButNotYetInThePlayableSet()
        {
            // The two sets partition the in-plan stations exactly: nothing is
            // double-counted, nothing is missing.
            Assert.That(PlayableStationIds.Intersect(Wave2StationIds), Is.Empty,
                "A station is either playable now or a Wave 2 construction site, never both.");
            Assert.That(
                PlayableStationIds.Concat(Wave2StationIds).OrderBy(id => id),
                Is.EquivalentTo(CareerQuestCatalog.PartyStationIds.OrderBy(id => id)),
                "Playable + Wave 2 must together cover every in-plan station id.");

            var appObject = new GameObject("station-pack-smoke-wave2-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            PrepareController(appObject);

            foreach (var stationId in Wave2StationIds)
            {
                // Each Wave 2 station has a campus entrance (it is visible).
                Assert.That(
                    WorldAnchors.ActiveEntrancesWithStations.Count(e => e.ResolveStationId() == stationId),
                    Is.EqualTo(1),
                    $"Wave 2 station '{stationId}' must still show a campus door.");

                // Routing in renders from the definition without crashing — the
                // generic branch already handles every in-plan id (U10 only adds
                // gameplay sign-off, not new routing).
                Assert.That(app.ShowPartyStation(stationId), Is.True, stationId);
                yield return MountFrames();
                Assert.That(app.CurrentStationId, Is.EqualTo(stationId), stationId);

                app.ShowCampus();
                yield return null;
            }

            Object.DestroyImmediate(appObject);
        }

        /// <summary>
        /// THE FINAL GATE (U11 flips this). At U8/U10 this documents the seam: it
        /// asserts the playable set partitions cleanly and reports which stations
        /// are still construction-only. When U10 has moved Wave 2 into the
        /// playable set, <see cref="Wave2StationIds"/> is empty and this test
        /// already passes its final-gate assertion — U11 then tightens the
        /// message to "no in-plan station may remain construction-only" and (if
        /// desired) deletes the Wave2 bookkeeping entirely.
        /// </summary>
        [Test]
        public void NoInPlanStationRemainsConstructionOnly()
        {
            var stillConstruction = CareerQuestCatalog.PartyStationIds
                .Where(id => !PlayableStationIds.Contains(id))
                .ToArray();

            // U8/U10 invariant: anything not yet playable is exactly a known
            // Wave 2 construction station — never a silently-dropped in-plan id.
            Assert.That(stillConstruction, Is.EquivalentTo(Wave2StationIds),
                "Every not-yet-playable in-plan station must be a tracked Wave 2 construction site.");

            // U11 FLIP: replace the line above with the final gate below (and set
            // Wave2StationIds = empty once U10 lands):
            //   Assert.That(stillConstruction, Is.Empty,
            //       "Final build: no in-plan station may remain construction-only or 'coming soon'.");
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
