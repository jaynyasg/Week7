using System.Collections;
using CareerQuest;
using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U12 P16 emote-relay suite, host-side (house convention: the host-only
    /// harness exercises the REAL host core — ApplyEmote rate limiting and the
    /// ShowEmoteFor render path — with simulated partner client ids; true
    /// two-client wire delivery remains a manual 2P evidence row, U14).
    ///
    /// Privacy invariant covered structurally: the emote bubble hierarchy holds
    /// NO text component of any kind — emotes are fixed IDs, never text.
    /// </summary>
    public class EmoteRelayPlayModeTests
    {
        private const ulong SimulatedPartnerClientId = 2UL;

        [UnityTest]
        public IEnumerator RenderPathShowsBubbleAboveSenderAvatarAndSelfHides()
        {
            yield return StartHost();
            var relay = FindRelay();
            relay.AutoTick = false;
            var localClientId = NetworkManager.Singleton.LocalClientId;
            var avatar = FindAvatar(localClientId);
            Assert.That(avatar, Is.Not.Null, "Host player avatar should spawn with the session.");

            var rendersBefore = relay.RenderedEmoteCount;
            Assert.That(relay.ShowEmoteFor(localClientId, EmoteId.Heart), Is.True);
            Assert.That(relay.RenderedEmoteCount - rendersBefore, Is.EqualTo(1));
            Assert.That(relay.LastRenderedClientId, Is.EqualTo(localClientId));
            Assert.That(relay.LastRenderedEmote, Is.EqualTo(EmoteId.Heart));

            // The bubble rides the SENDER's avatar, above its head.
            var bubble = avatar.GetComponentInChildren<EmoteBubble>(true);
            Assert.That(bubble, Is.Not.Null, "The emote bubble parents to the sender's avatar.");
            Assert.That(bubble.IsVisible, Is.True);
            Assert.That(bubble.ShownEmote, Is.EqualTo(EmoteId.Heart));
            Assert.That(bubble.transform.localPosition.y, Is.GreaterThan(1f), "Bubble renders above the avatar.");

            // Timed self-hide (deterministic clock — no real-time wait).
            bubble.AutoTick = false;
            bubble.Tick(EmoteRelay.BubbleSeconds + 0.1f);
            Assert.That(bubble.IsVisible, Is.False, "The bubble self-hides after its beat.");

            relay.AutoTick = true;
            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator HostRateLimitDropsExcessEmotesGentlyPerSender()
        {
            yield return StartHost();
            var relay = FindRelay();
            relay.AutoTick = false;
            var localClientId = NetworkManager.Singleton.LocalClientId;
            var rendersBefore = relay.RenderedEmoteCount;

            // 6 rapid sends inside one rate window → exactly 1 accepted/rendered.
            var accepted = 0;
            for (var send = 0; send < 6; send++)
            {
                if (relay.ApplyEmote(EmoteId.Star, localClientId))
                {
                    accepted++;
                }
            }

            Assert.That(accepted, Is.EqualTo(1), "Excess emotes are dropped gently (no error, no response).");
            Assert.That(relay.RenderedEmoteCount - rendersBefore, Is.EqualTo(1), "Renders stay bounded under spam.");

            // No deferred double-delivery sneaks in a frame later.
            yield return null;
            Assert.That(relay.RenderedEmoteCount - rendersBefore, Is.EqualTo(1));

            // The rate limit is PER SENDER: the partner's first emote still lands
            // host-side (no avatar spawned for the simulated id — accept only).
            Assert.That(relay.ApplyEmote(EmoteId.Heart, SimulatedPartnerClientId), Is.True);

            // The window reopens with time.
            relay.Tick(EmoteRelay.MinSecondsBetweenEmotes + 0.05f);
            Assert.That(relay.ApplyEmote(EmoteId.Wave, localClientId), Is.True);

            relay.AutoTick = true;
            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator EmoteBubbleContainsNoTextComponentAnywhere()
        {
            yield return StartHost();
            var relay = FindRelay();
            var localClientId = NetworkManager.Singleton.LocalClientId;
            Assert.That(relay.ShowEmoteFor(localClientId, EmoteId.Wave), Is.True);

            var avatar = FindAvatar(localClientId);
            var bubble = avatar.GetComponentInChildren<EmoteBubble>(true);
            Assert.That(bubble, Is.Not.Null);

            // No-chat privacy boundary is structural: pure iconography only.
            Assert.That(bubble.GetComponentsInChildren<TMP_Text>(true), Is.Empty,
                "The emote bubble must carry no TMP text — emotes are fixed IDs, never text.");
            Assert.That(bubble.GetComponentsInChildren<UnityEngine.UI.Text>(true), Is.Empty,
                "The emote bubble must carry no legacy text either.");
            Assert.That(bubble.GetComponentsInChildren<SpriteRenderer>(true), Is.Not.Empty,
                "The emote renders as sprite iconography.");

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator RelayStateResetsOnDisconnectShutdown()
        {
            yield return StartHost();
            var relay = FindRelay();
            relay.AutoTick = false;
            var localClientId = NetworkManager.Singleton.LocalClientId;
            Assert.That(relay.ApplyEmote(EmoteId.Heart, localClientId), Is.True);

            // Disconnect/shutdown: session-scoped emote state resets cleanly.
            yield return NetcodePlayModeHarness.ShutdownNetwork();
            yield return null;

            Assert.That(EmoteRelay.Instance, Is.Null, "Despawn clears the singleton.");
            Assert.That(relay == null || !relay.IsSpawned, Is.True);
            if (relay != null)
            {
                Assert.That(relay.ApplyEmote(EmoteId.Heart, localClientId), Is.False,
                    "A despawned relay accepts nothing.");
                relay.AutoTick = true;
            }
        }

        private static IEnumerator StartHost()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);
            yield return WaitForLocalAvatar();
        }

        private static IEnumerator WaitForLocalAvatar(float timeoutSeconds = 8f)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline
                && FindAvatar(NetworkManager.Singleton.LocalClientId) == null)
            {
                yield return null;
            }
        }

        private static EmoteRelay FindRelay()
        {
            var relay = Object.FindAnyObjectByType<EmoteRelay>();
            Assert.That(relay, Is.Not.Null,
                "EmoteRelay should ride the CampusSessionState scene object (U12).");
            Assert.That(relay.IsSpawned, Is.True, "EmoteRelay should be spawned after host start.");
            Assert.That(EmoteRelay.Instance, Is.EqualTo(relay));
            return relay;
        }

        private static PlayerAvatarNetwork FindAvatar(ulong clientId)
        {
            foreach (var avatar in Object.FindObjectsByType<PlayerAvatarNetwork>(FindObjectsSortMode.None))
            {
                if (avatar.IsSpawned && avatar.OwnerClientId == clientId)
                {
                    return avatar;
                }
            }

            return null;
        }
    }
}
