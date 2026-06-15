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
    }
}
