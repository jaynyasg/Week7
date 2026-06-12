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
    /// U4 authored-world suite. These are the tests that fail LOUDLY when the
    /// CampusHub prefab has not been built (run CareerQuestHubPrefabBuilder
    /// GenerateBuildingArt then Build) — the rest of the suite stays green on
    /// the safe fallback ground.
    /// </summary>
    public class CampusHubWorldPlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            WorldAnchors.PrefabResourcePathOverride = null;
        }

        [UnityTest]
        public IEnumerator CampusRouteMountsPrefabWithParallaxBandsAnchorsAndAmbientMotion()
        {
            var worldObject = new GameObject("hub-prefab-test");
            var world = worldObject.AddComponent<CampusWorldController>();
            var session = new GameSession();
            yield return null;

            world.ShowCampus(session);
            yield return null;

            var hub = GameObject.Find("CampusHub");
            Assert.That(hub, Is.Not.Null, "CampusHub prefab should mount on the campus route — build it with CareerQuestHubPrefabBuilder.");

            var bands = hub.GetComponentsInChildren<ParallaxLayer>();
            Assert.That(bands.Length, Is.GreaterThanOrEqualTo(4), "The diorama needs 4+ parallax bands.");
            Assert.That(bands.Select(band => band.Factor).Distinct().Count(), Is.GreaterThanOrEqualTo(4), "Bands need distinct depths.");

            var anchors = hub.GetComponent<WorldAnchors>();
            Assert.That(anchors, Is.Not.Null, "The prefab root must export WorldAnchors.");
            Assert.That(anchors.Entrances.Count, Is.EqualTo(7));

            var ambient = hub.GetComponentsInChildren<AmbientMotion>();
            Assert.That(ambient.Length, Is.GreaterThanOrEqualTo(3), "Living campus (P9): clouds, flag, butterflies.");

            Object.Destroy(worldObject);
        }

        [UnityTest]
        public IEnumerator MissingPrefabFallsBackToMinimalGroundSafely()
        {
            WorldAnchors.PrefabResourcePathOverride = "CareerQuest/World/DoesNotExist";
            var worldObject = new GameObject("hub-fallback-test");
            var world = worldObject.AddComponent<CampusWorldController>();
            var session = new GameSession();
            yield return null;

            world.ShowCampus(session);

            Assert.That(world.IsHubBootComplete, Is.True);
            Assert.That(GameObject.Find("CampusHub"), Is.Null);
            Assert.That(GameObject.Find("CampusGrass"), Is.Not.Null, "Fallback ground keeps the route playable.");

            yield return null;
            Assert.That(world.IsHubDecorLoaded, Is.True);

            WorldAnchors.PrefabResourcePathOverride = null;
            Object.Destroy(worldObject);
        }

        [UnityTest]
        public IEnumerator EveryEntranceLiesInsideLocalAndServerWalkClamps()
        {
            yield return null;

            // Single-source verification: the local clamp consumes
            // WorldAnchors.ActiveWalkBounds, the server clamp consumes
            // WorldAnchors.AssetWalkBounds via PlayerAvatarNetwork.ClampCampus.
            var localBounds = WorldAnchors.ActiveWalkBounds;
            var serverBounds = WorldAnchors.AssetWalkBounds;

            foreach (var entrance in WorldAnchors.AssetEntrances)
            {
                Assert.That(localBounds.Contains(entrance.Position), Is.True,
                    $"Entrance '{entrance.Id}' at {entrance.Position} must lie inside the local walk clamp {localBounds}.");
                Assert.That(serverBounds.Contains(entrance.Position), Is.True,
                    $"Entrance '{entrance.Id}' at {entrance.Position} must lie inside the server walk clamp {serverBounds}.");

                var clamped = PlayerAvatarNetwork.ClampCampus(new Vector3(entrance.Position.x, entrance.Position.y, 0f));
                Assert.That((Vector2)clamped, Is.EqualTo(entrance.Position),
                    $"The server clamp must not move entrance '{entrance.Id}'.");
            }
        }

        [UnityTest]
        public IEnumerator StationEntrancesStayInsideWalkClampsAndNeverOverlap()
        {
            yield return null;

            // U2: the playable entrance set is the anchored entrances plus the
            // station-id doors. It must validate clean (non-overlapping entry
            // circles, readable labels and district labels, resolvable station
            // ids) and every door must be reachable under both walk clamps.
            var entrances = WorldAnchors.ActiveEntrancesWithStations;
            var errors = WorldAnchors.ValidateEntrances(entrances);
            Assert.That(errors, Is.Empty,
                $"Hub entrance layout must validate clean. Errors: {string.Join(" | ", errors)}");

            var localBounds = WorldAnchors.ActiveWalkBounds;
            var serverBounds = WorldAnchors.AssetWalkBounds;
            foreach (var entrance in entrances.Where(entrance => entrance.IsStationEntrance))
            {
                Assert.That(localBounds.Contains(entrance.Position), Is.True,
                    $"Station entrance '{entrance.Id}' at {entrance.Position} must lie inside the local walk clamp {localBounds}.");
                Assert.That(serverBounds.Contains(entrance.Position), Is.True,
                    $"Station entrance '{entrance.Id}' at {entrance.Position} must lie inside the server walk clamp {serverBounds}.");

                var clamped = PlayerAvatarNetwork.ClampCampus(new Vector3(entrance.Position.x, entrance.Position.y, 0f));
                Assert.That((Vector2)clamped, Is.EqualTo(entrance.Position),
                    $"The server clamp must not move station entrance '{entrance.Id}'.");
            }

            foreach (var stationId in CareerQuestCatalog.PartyStationIds)
            {
                Assert.That(entrances.Count(entrance => entrance.ResolveStationId() == stationId), Is.EqualTo(1),
                    $"Station '{stationId}' must resolve exactly one campus entrance.");
            }
        }

        [UnityTest]
        public IEnumerator ParallaxBandsTrackCameraAndReAnchorAfterRoomRoundTrip()
        {
            var worldObject = new GameObject("parallax-test");
            var world = worldObject.AddComponent<CampusWorldController>();
            var session = new GameSession();
            yield return null;

            world.ShowCampus(session);
            yield return null;

            var hub = GameObject.Find("CampusHub");
            Assert.That(hub, Is.Not.Null, "Parallax test requires the built CampusHub prefab.");

            var director = world.CameraDirector;
            director.AutoTick = false;

            var bands = hub.GetComponentsInChildren<ParallaxLayer>();
            var authoredPositions = bands.ToDictionary(band => band.name, band => band.transform.localPosition);

            // Drive a hub follow drift deterministically.
            var target = new GameObject("parallax-follow-target");
            target.transform.position = new Vector3(3.5f, -1.2f, 0f);
            director.BeginFollow(target.transform, CameraFollowSettings.HubDefault);

            var cameraBefore = director.Camera.transform.position;
            var bandsBefore = bands.ToDictionary(band => band.name, band => band.transform.localPosition);
            for (var i = 0; i < 30; i++)
            {
                director.Tick(1f / 60f);
            }

            var cameraDelta = director.Camera.transform.position - cameraBefore;
            Assert.That(cameraDelta.magnitude, Is.GreaterThan(0.05f), "The follow drift should move the camera.");

            foreach (var band in bands)
            {
                var expected = bandsBefore[band.name] + new Vector3(cameraDelta.x * band.Factor, cameraDelta.y * band.Factor, 0f);
                Assert.That((band.transform.localPosition - expected).magnitude, Is.LessThan(0.0001f),
                    $"Band '{band.name}' (factor {band.Factor}) must hold alignment with the camera delta.");
            }

            Object.Destroy(target);
            director.AutoTick = true;

            // Room round-trip: bands of the freshly mounted hub sit at the
            // authored offsets again (no accumulated drift, no jump).
            world.ShowDesignBuild(session);
            yield return null;
            yield return null;
            world.ShowCampus(session);
            yield return null;
            yield return null;

            var remountedHub = GameObject.Find("CampusHub");
            Assert.That(remountedHub, Is.Not.Null);
            foreach (var band in remountedHub.GetComponentsInChildren<ParallaxLayer>())
            {
                var authored = authoredPositions[band.name];
                Assert.That((band.transform.localPosition - authored).magnitude, Is.LessThan(0.05f),
                    $"Band '{band.name}' must re-anchor to its authored position after a room round-trip.");
            }

            Object.Destroy(worldObject);
        }

        [UnityTest]
        public IEnumerator EntryRouteShowsLiveCampusDioramaWithAmbientMotion()
        {
            var worldObject = new GameObject("entry-diorama-test");
            var world = worldObject.AddComponent<CampusWorldController>();
            var session = new GameSession();
            yield return null;

            // P8: ShowEntry is load-bearing — the title moment plays over the
            // live diorama with ambient motion running.
            world.ShowEntry(session);
            yield return null;

            var hub = GameObject.Find("CampusHub");
            Assert.That(hub, Is.Not.Null, "The entry route must mount the live campus diorama (P8).");

            var motions = hub.GetComponentsInChildren<AmbientMotion>();
            Assert.That(motions.Length, Is.GreaterThanOrEqualTo(3));

            // Ambient motion actually runs: ticking a drifting cloud moves it.
            var drifting = motions.First(motion => motion.Kind == AmbientMotionKind.Drift);
            drifting.AutoTick = false;
            var before = drifting.transform.localPosition;
            drifting.Tick(2f);
            Assert.That((drifting.transform.localPosition - before).magnitude, Is.GreaterThan(0.01f),
                "Drifting clouds must move when time advances.");

            Object.Destroy(worldObject);
        }
    }
}
