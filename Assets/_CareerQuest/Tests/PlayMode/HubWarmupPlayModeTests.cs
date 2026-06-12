using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// Characterizes the route-transition contract behind CampusWorldController:
    /// - Boot: IsHubBootComplete flips on the build frame; IsHubDecorLoaded one
    ///   frame later (unchanged from the legacy builder world).
    /// - Veil: "RoomVeil" covers the build frame; cleared on the next frame
    ///   when the room mounts. New in U4 (P6): a non-blocking "SceneWipeOpen"
    ///   paper lift plays after the room mounts and self-destructs.
    /// - P24 (deliberate behavior change from legacy): starting any new route
    ///   cancels the previous route's pending veil/boot coroutines, so a
    ///   cancelled transition can never wipe or pollute the new route's world.
    ///   (Legacy behavior: hub→room left the decor coroutine pending, which
    ///   injected hub decor into the room; room→hub left the veil reveal
    ///   pending, which wiped the freshly built hub and built the room.)
    /// </summary>
    public class HubWarmupPlayModeTests
    {
        [UnityTest]
        public IEnumerator HubBootCompletesImmediatelyAndDecorLoadsOnNextFrame()
        {
            var worldObject = new GameObject("hub-warmup-test");
            var world = worldObject.AddComponent<CampusWorldController>();
            var session = new GameSession();
            yield return null;

            world.ShowCampus(session);

            Assert.That(world.IsHubBootComplete, Is.True);
            Assert.That(world.IsHubDecorLoaded, Is.False);

            yield return null;

            Assert.That(world.IsHubDecorLoaded, Is.True);

            Object.Destroy(worldObject);
        }

        [UnityTest]
        public IEnumerator RoomVeilCoversTransitionUntilNextFrame()
        {
            var worldObject = new GameObject("room-veil-test");
            var world = worldObject.AddComponent<CampusWorldController>();
            var session = new GameSession();
            yield return null;

            // Isolation: earlier tests can leak self-ticking wipes under their
            // world roots; clear them so Find below sees only this test's wipe.
            foreach (var stale in Object.FindObjectsByType<SceneWipe>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(stale.gameObject);
            }

            world.ShowDesignBuild(session);

            Assert.That(world.IsRoomVeilActive, Is.True);
            Assert.That(GameObject.Find("RoomVeil"), Is.Not.Null);

            yield return null;

            Assert.That(world.IsRoomVeilActive, Is.False);
            Assert.That(GameObject.Find("RoomVeil"), Is.Null);

            // P6: the room mounted under a paper-wipe open that is purely
            // visual (veil flag already cleared) and destroys itself.
            Assert.That(GameObject.Find("BuildTable"), Is.Not.Null, "Room content should mount when the veil clears.");
            var opener = GameObject.Find(SceneWipe.OpenName);
            Assert.That(opener, Is.Not.Null, "The paper-wipe open should play over the mounted room.");

            var wipe = opener.GetComponent<SceneWipe>();
            wipe.AutoTick = false;
            wipe.Tick(1f); // beyond the open duration — finishes and self-destructs
            yield return null;

            Assert.That(wipe == null, Is.True, "The wipe must destroy itself when fully open.");

            Object.Destroy(worldObject);
        }

        [UnityTest]
        public IEnumerator HubRouteCancelsPendingRoomVeil()
        {
            var worldObject = new GameObject("p24-room-to-hub-test");
            var world = worldObject.AddComponent<CampusWorldController>();
            var session = new GameSession();
            yield return null;

            // Route room → hub inside the one-frame transition window.
            world.ShowDesignBuild(session);
            world.ShowCampus(session);

            Assert.That(world.IsRoomVeilActive, Is.False, "The new hub route must cancel the pending room veil.");
            Assert.That(world.IsHubBootComplete, Is.True);

            yield return null;
            yield return null;

            // Final world matches the final route: hub content, no orphaned room build.
            Assert.That(world.IsHubDecorLoaded, Is.True);
            Assert.That(world.IsRoomVeilActive, Is.False);
            Assert.That(GameObject.Find("BuildTable"), Is.Null, "The cancelled room build must never mount.");
            Assert.That(
                GameObject.Find("CampusHub") != null || GameObject.Find("CampusGrass") != null,
                Is.True,
                "The hub world (prefab or fallback ground) should be mounted.");

            Object.Destroy(worldObject);
        }

        [UnityTest]
        public IEnumerator RoomRouteCancelsPendingHubDecor()
        {
            var worldObject = new GameObject("p24-hub-to-room-test");
            var world = worldObject.AddComponent<CampusWorldController>();
            var session = new GameSession();
            yield return null;

            // Route hub → room inside the one-frame decor window.
            world.ShowCampus(session);
            world.ShowDesignBuild(session);

            Assert.That(world.IsHubBootComplete, Is.False, "The new room route must cancel the pending hub boot.");
            Assert.That(world.IsRoomVeilActive, Is.True);

            yield return null;
            yield return null;

            Assert.That(world.IsHubDecorLoaded, Is.False, "Cancelled hub decor must never finish loading.");
            Assert.That(world.IsRoomVeilActive, Is.False);
            Assert.That(GameObject.Find("BuildTable"), Is.Not.Null, "The room should mount normally.");
            Assert.That(GameObject.Find("CampusHub"), Is.Null, "No orphaned hub world inside the room.");
            Assert.That(GameObject.Find("CampusGrass"), Is.Null, "No orphaned hub fallback ground inside the room.");

            Object.Destroy(worldObject);
        }

        [UnityTest]
        public IEnumerator RouteABAWithinTransitionWindowEndsOnFinalRoute()
        {
            var worldObject = new GameObject("p24-aba-test");
            var world = worldObject.AddComponent<CampusWorldController>();
            var session = new GameSession();
            yield return null;

            // A → B → A inside one frame: design build → campus → design build.
            world.ShowDesignBuild(session);
            world.ShowCampus(session);
            world.ShowDesignBuild(session);

            Assert.That(world.IsRoomVeilActive, Is.True);
            Assert.That(world.IsHubBootComplete, Is.False);

            yield return null;
            yield return null;

            Assert.That(world.IsRoomVeilActive, Is.False);
            Assert.That(world.IsHubDecorLoaded, Is.False);
            Assert.That(GameObject.Find("BuildTable"), Is.Not.Null, "The final route's room must be the mounted world.");
            Assert.That(GameObject.Find("CampusHub"), Is.Null);
            Assert.That(GameObject.Find("CampusGrass"), Is.Null);

            Object.Destroy(worldObject);
        }
    }
}
