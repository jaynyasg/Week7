using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
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

            world.ShowDesignBuild(session);

            Assert.That(world.IsRoomVeilActive, Is.True);
            Assert.That(GameObject.Find("RoomVeil"), Is.Not.Null);

            yield return null;

            Assert.That(world.IsRoomVeilActive, Is.False);
            Assert.That(GameObject.Find("RoomVeil"), Is.Null);

            Object.Destroy(worldObject);
        }
    }
}
