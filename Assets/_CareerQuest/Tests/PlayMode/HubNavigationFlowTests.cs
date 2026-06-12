using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    public class HubNavigationFlowTests
    {
        [UnityTest]
        public IEnumerator CampusCreatesPlayableHubAndRoutesEntrances()
        {
            var gameObject = new GameObject("hub-flow-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;
            yield return PlayModeTestBootstrap.EnterPlayCampus(app);

            var hub = Object.FindAnyObjectByType<PlayableHubController>();
            Assert.That(hub, Is.Not.Null);
            Assert.That(hub.IsVisible, Is.True);
            Assert.That(hub.Player, Is.Not.Null);
            Assert.That(hub.Entrances.Count, Is.EqualTo(7));

            // Anchors-only exposure: entrance placement comes from WorldAnchors
            // (prefab instance/asset, or the hard fallback constants).
            var anchorEntrances = WorldAnchors.ActiveEntrances;
            Assert.That(anchorEntrances.Count, Is.EqualTo(7));
            for (var i = 0; i < anchorEntrances.Count; i++)
            {
                Assert.That(hub.Entrances[i].Route, Is.EqualTo(anchorEntrances[i].Route));
                Assert.That((Vector2)hub.Entrances[i].transform.position, Is.EqualTo(anchorEntrances[i].Position),
                    $"Entrance '{anchorEntrances[i].Id}' must sit at its WorldAnchors position.");
            }

            Assert.That(hub.TryEnter(ActivityRoute.HealthHero), Is.True);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.HealthHero));

            app.ShowCampus();
            yield return null;

            Assert.That(hub.TryEnter(ActivityRoute.MusicStudio), Is.True);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.MusicStudio));

            Object.Destroy(gameObject);
            Object.Destroy(hub.gameObject);
        }
    }
}
