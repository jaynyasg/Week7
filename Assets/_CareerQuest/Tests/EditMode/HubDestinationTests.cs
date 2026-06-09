using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    public class HubDestinationTests
    {
        [Test]
        public void EntranceDetectsNearbyWorldPositions()
        {
            var gameObject = new GameObject("entrance-test");
            gameObject.transform.position = new Vector3(2f, -1f, 0f);

            var entrance = gameObject.AddComponent<BuildingEntrance>();
            entrance.Configure(ActivityRoute.DesignBuild, "Design", 0.5f, null);

            Assert.That(entrance.Contains(new Vector2(2.25f, -1f)), Is.True);
            Assert.That(entrance.Contains(new Vector2(3f, -1f)), Is.False);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void PlayerCanEnterDestinationAtClickedPosition()
        {
            var enteredRoute = ActivityRoute.Entry;
            var entranceObject = new GameObject("logic-entrance-test");
            var entrance = entranceObject.AddComponent<BuildingEntrance>();
            entrance.Configure(ActivityRoute.LogicCourt, "Logic", 0.75f, route => enteredRoute = route);

            var playerObject = new GameObject("player-test", typeof(SpriteRenderer), typeof(AvatarRuntimeView), typeof(PlayerAvatarController));
            var player = playerObject.GetComponent<PlayerAvatarController>();
            player.Configure(new GameSession(), new[] { entrance }, route => enteredRoute = route);

            Assert.That(player.TryEnterAt(Vector2.zero), Is.True);
            Assert.That(enteredRoute, Is.EqualTo(ActivityRoute.LogicCourt));

            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(entranceObject);
        }
    }
}
