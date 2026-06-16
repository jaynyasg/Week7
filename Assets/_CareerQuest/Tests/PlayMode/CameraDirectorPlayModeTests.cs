using System.Collections;
using System.Collections.Generic;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    public class CameraDirectorPlayModeTests
    {
        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void Cleanup()
        {
            foreach (var world in Object.FindObjectsByType<CampusWorldController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(world.gameObject);
            }

            foreach (var director in Object.FindObjectsByType<CameraDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var camera = director.Camera;
                Object.DestroyImmediate(director.gameObject);
                if (camera != null)
                {
                    Object.DestroyImmediate(camera.gameObject);
                }
            }

            foreach (var spawned in _spawned)
            {
                if (spawned != null)
                {
                    Object.DestroyImmediate(spawned);
                }
            }

            _spawned.Clear();
        }

        [UnityTest]
        public IEnumerator EnsureAdoptsOrCreatesMainCameraAndTagsIt()
        {
            var director = CameraDirector.Ensure();

            Assert.That(director.Camera, Is.Not.Null);
            Assert.That(director.Camera.CompareTag("MainCamera"), Is.True);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(director.CameraHost, Is.SameAs(director.Camera.gameObject));

            // The director camera is the game's only AudioListener (the world is
            // code/prefab built) — without it the whole game is silent.
            Assert.That(director.Camera.GetComponent<AudioListener>(), Is.Not.Null,
                "The director-owned camera must carry the AudioListener.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator FollowModeTracksMovingTargetWithinHubClamp()
        {
            var director = CameraDirector.Ensure();
            director.AutoTick = false;
            director.SetRouteShot(CameraShot.Default);

            var target = SpawnTarget(new Vector3(10f, 1.5f, 0f));
            director.BeginFollow(target.transform, CameraFollowSettings.HubDefault);

            Assert.That(director.ActiveMode, Is.EqualTo(CameraDirectorMode.Follow));
            Assert.That(director.IsRestored, Is.False);
            Assert.That(director.Camera.orthographicSize, Is.EqualTo(3.6f).Within(0.001f));

            TickMany(director, 120, 0.05f);
            var position = director.Camera.transform.position;
            Assert.That(position.x, Is.EqualTo(3.5f).Within(0.01f), "follow should clamp at the hub's +x edge");
            Assert.That(position.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(position.z, Is.EqualTo(-10f).Within(0.001f));

            target.transform.position = new Vector3(-10f, 0f, 0f);
            TickMany(director, 120, 0.05f);
            Assert.That(director.Camera.transform.position.x, Is.EqualTo(-3.5f).Within(0.01f), "follow should clamp at the hub's -x edge");

            target.transform.position = new Vector3(2f, 0f, 0f);
            TickMany(director, 120, 0.05f);
            Assert.That(director.Camera.transform.position.x, Is.EqualTo(2f).Within(0.01f), "inside the clamp the camera tracks the player x 1:1");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SwitchingToRouteShotSnapsAwayFromFollow()
        {
            var director = CameraDirector.Ensure();
            director.AutoTick = false;
            director.SetRouteShot(CameraShot.Default);

            var target = SpawnTarget(new Vector3(8f, 0f, 0f));
            director.BeginFollow(target.transform, CameraFollowSettings.HubDefault);
            TickMany(director, 60, 0.05f);
            Assert.That(director.Camera.transform.position.x, Is.Not.EqualTo(0f).Within(0.01f));

            var roomShot = CameraShot.Default;
            director.SetRouteShot(roomShot);

            Assert.That(director.ActiveMode, Is.EqualTo(CameraDirectorMode.FixedShot));
            Assert.That(director.IsRestored, Is.True);
            AssertCameraAtShot(director, roomShot);

            director.Tick(0.05f);
            AssertCameraAtShot(director, roomShot);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RoomRouteThroughWorldControllerResetsCamera()
        {
            var director = CameraDirector.Ensure();
            director.AutoTick = false;

            var world = CampusWorldController.Ensure();
            var session = new GameSession();
            world.ShowCampus(session);
            yield return null;
            yield return null;

            var target = SpawnTarget(new Vector3(9f, 0f, 0f));
            director.BeginFollow(target.transform, CameraFollowSettings.HubDefault);
            TickMany(director, 60, 0.05f);
            Assert.That(director.IsRestored, Is.False);

            world.ShowDesignBuild(session);

            Assert.That(director.ActiveMode, Is.EqualTo(CameraDirectorMode.FixedShot));
            Assert.That(director.IsRestored, Is.True);
            AssertCameraAtShot(director, CameraShot.Default);

            world.ClearWorld();
            Assert.That(director.IsRestored, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartingTweenWhileTweenActiveCancelsFirstCleanly()
        {
            var director = CameraDirector.Ensure();
            director.AutoTick = false;
            director.SetRouteShot(CameraShot.Default);

            var shotA = new CameraShot(new Vector3(6f, 2f, -10f), 6.5f);
            director.TweenToShot(shotA, 1f);
            director.Tick(0.4f);
            Assert.That(director.ActiveMode, Is.EqualTo(CameraDirectorMode.Tween));

            var positionAtSwitch = director.Camera.transform.position;
            var shotB = new CameraShot(new Vector3(-4f, 1f, -10f), 3.5f);
            director.TweenToShot(shotB, 1f);

            Assert.That(director.CurrentShot.Approximately(shotB), Is.True);
            Assert.That(
                (director.Camera.transform.position - positionAtSwitch).magnitude,
                Is.LessThan(0.0001f),
                "starting a new tween must not jump the camera");

            var previousDistance = Vector3.Distance(director.Camera.transform.position, shotB.Position);
            for (var i = 0; i < 12; i++)
            {
                director.Tick(0.1f);
                var distance = Vector3.Distance(director.Camera.transform.position, shotB.Position);
                Assert.That(distance, Is.LessThanOrEqualTo(previousDistance + 0.0001f), "cancelled tween must approach the new target monotonically");
                previousDistance = distance;
            }

            Assert.That(director.ActiveMode, Is.EqualTo(CameraDirectorMode.FixedShot));
            AssertCameraAtShot(director, shotB);
            Assert.That(director.IsRestored, Is.False, "a cinematic shot is not the route shot");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ForcedResetMidTweenRestoresRouteShotWithinOneTick()
        {
            var director = CameraDirector.Ensure();
            director.AutoTick = false;
            var routeShot = new CameraShot(new Vector3(1f, -0.5f, -10f), 5f);
            director.SetRouteShot(routeShot);

            director.TweenToShot(new CameraShot(new Vector3(7f, 3f, -10f), 7f), 2f);
            director.Tick(0.5f);
            Assert.That(director.IsRestored, Is.False);

            director.ResetToRouteShot();
            director.Tick(1f / 60f);

            Assert.That(director.ActiveMode, Is.EqualTo(CameraDirectorMode.FixedShot));
            Assert.That(director.IsRestored, Is.True);
            AssertCameraAtShot(director, routeShot);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HubCameraRigFramingIsPreservedThroughDirector()
        {
            var director = CameraDirector.Ensure();
            director.AutoTick = false;
            director.SetRouteShot(CameraShot.Default);

            var target = SpawnTarget(new Vector3(3f, 1f, 0f));
            var rigHost = new GameObject("HubRigHost", typeof(HubCameraRig));
            _spawned.Add(rigHost);
            var rig = rigHost.GetComponent<HubCameraRig>();
            rig.Configure(director, target.transform);

            Assert.That(director.ActiveMode, Is.EqualTo(CameraDirectorMode.Follow));
            Assert.That(director.Camera.orthographic, Is.True);
            Assert.That(director.Camera.orthographicSize, Is.EqualTo(3.6f).Within(0.001f), "campus framing size (zoomed-out follow)");

            TickMany(director, 120, 0.05f);
            var position = director.Camera.transform.position;
            Assert.That(position.x, Is.EqualTo(3.0f).Within(0.01f), "campus follow: clamp(target.x * 1.0, +-3.5)");
            Assert.That(position.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(position.z, Is.EqualTo(-10f).Within(0.001f));

            rigHost.SetActive(false);

            Assert.That(director.ActiveMode, Is.EqualTo(CameraDirectorMode.FixedShot));
            Assert.That(director.IsRestored, Is.True, "hiding the hub rig must restore the route shot");
            AssertCameraAtShot(director, CameraShot.Default);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TickFiresAfterCameraWriteWithDelta()
        {
            var director = CameraDirector.Ensure();
            director.AutoTick = false;
            director.SetRouteShot(CameraShot.Default);

            var observedDeltas = new List<Vector3>();
            director.AfterCameraWrite += delta => observedDeltas.Add(delta);

            director.TweenToShot(new CameraShot(new Vector3(4f, 0f, -10f), 4.5f), 1f);
            director.Tick(0.25f);
            director.Tick(0.25f);

            Assert.That(observedDeltas.Count, Is.EqualTo(2));
            Assert.That(observedDeltas[0].x, Is.GreaterThan(0f), "parallax consumers receive the post-write camera delta");
            Assert.That(director.LastCameraDelta, Is.EqualTo(observedDeltas[1]));
            yield return null;
        }

        private GameObject SpawnTarget(Vector3 position)
        {
            var target = new GameObject("CameraDirectorTestTarget");
            target.transform.position = position;
            _spawned.Add(target);
            return target;
        }

        private static void TickMany(CameraDirector director, int ticks, float deltaSeconds)
        {
            for (var i = 0; i < ticks; i++)
            {
                director.Tick(deltaSeconds);
            }
        }

        private static void AssertCameraAtShot(CameraDirector director, CameraShot shot)
        {
            var camera = director.Camera;
            Assert.That((camera.transform.position - shot.Position).magnitude, Is.LessThan(0.001f));
            Assert.That(camera.orthographic, Is.True);
            Assert.That(camera.orthographicSize, Is.EqualTo(shot.OrthographicSize).Within(0.001f));
        }
    }
}
