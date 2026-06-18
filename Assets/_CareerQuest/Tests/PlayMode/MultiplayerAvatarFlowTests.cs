using System.Collections;
using CareerQuest;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    public class MultiplayerAvatarFlowTests
    {
        [UnityTest]
        public IEnumerator ConfigureLocalPlayerAppliesAvatarAndSamePcControls()
        {
            var avatarObject = new GameObject(
                "network-avatar-config-test",
                typeof(NetworkObject),
                typeof(PlayerInputRouter),
                typeof(PlayerAvatarNetwork));
            yield return null;

            var router = avatarObject.GetComponent<PlayerInputRouter>();
            var avatar = avatarObject.GetComponent<PlayerAvatarNetwork>();

            avatar.ConfigureLocalPlayer("care_captain", PlayerControlScheme.SplitKeyboardP2);

            Assert.That(router.ControlScheme, Is.EqualTo(PlayerControlScheme.SplitKeyboardP2));
            Assert.That(avatar.AvatarId, Is.EqualTo("care_captain"));

            Object.Destroy(avatarObject);
        }

        [UnityTest]
        public IEnumerator NetworkedCampusDoesNotCreateLocalHubPlayerDuplicate()
        {
            var appObject = new GameObject("networked-campus-no-duplicate-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            app.Session.SetConnectionMode(ConnectionMode.HostP1);
            app.Session.PlayerCount = 1;
            app.ShowCampus();
            yield return null;

            var hub = Object.FindAnyObjectByType<PlayableHubController>();
            Assert.That(hub, Is.Not.Null);
            Assert.That(hub.Player, Is.Null, "Networked campus should use the owned PlayerAvatarNetwork, not a second local HubPlayer.");
            Assert.That(GameObject.Find("HubPlayer"), Is.Null);
            Assert.That(GameObject.Find(app.Session.SelectedAvatar.DisplayName), Is.Null,
                "The campus backdrop should not add a decorative copy of the selected player on top of playable avatars.");

            Object.Destroy(appObject);
            Object.Destroy(hub.gameObject);
        }

        [UnityTest]
        public IEnumerator NetworkAvatarsHideWhenARoomOwnsTheScreen()
        {
            var appObject = new GameObject("network-avatar-room-visibility-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            var p1 = MakeNetworkAvatar("room-visibility-p1");
            var p2 = MakeNetworkAvatar("room-visibility-p2");
            yield return null;

            app.Session.SetConnectionMode(ConnectionMode.HostP1);
            app.Session.PlayerCount = 2;
            app.ShowCampus();
            yield return null;

            Assert.That(p1.Renderer.enabled, Is.True);
            Assert.That(p2.Renderer.enabled, Is.True);
            Assert.That(p1.ToolBeltRenderer.enabled, Is.True);

            app.ShowLogicCourt();
            yield return null;

            Assert.That(p1.Renderer.enabled, Is.False,
                "Persistent network avatars should not render as duplicate room characters.");
            Assert.That(p2.Renderer.enabled, Is.False,
                "Persistent network avatars should not render as duplicate room characters.");
            Assert.That(p1.ToolBeltRenderer.enabled, Is.False,
                "Avatar accessories should hide with network avatars so gear does not float in front of room characters.");

            Object.Destroy(appObject);
            Object.Destroy(p1.Object);
            Object.Destroy(p2.Object);
            var hub = Object.FindAnyObjectByType<PlayableHubController>();
            if (hub != null)
            {
                Object.Destroy(hub.gameObject);
            }
        }

        private static (GameObject Object, SpriteRenderer Renderer, SpriteRenderer ToolBeltRenderer) MakeNetworkAvatar(string name)
        {
            var avatarObject = new GameObject(
                name,
                typeof(NetworkObject),
                typeof(PlayerInputRouter),
                typeof(PlayerAvatarNetwork));
            var layer = avatarObject.AddComponent<AvatarAccessoryLayer>();
            Assert.That(AccessoryRewardConfig.TryGetById("accessory.tool_belt", out var toolBelt), Is.True);
            layer.Apply(new[] { toolBelt });
            return (avatarObject, avatarObject.GetComponent<SpriteRenderer>(), layer.RendererFor(toolBelt.Id));
        }
    }
}
