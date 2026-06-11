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

            app.BeginPlay();
            app.ShowCampus();
            yield return null;

            var hub = Object.FindAnyObjectByType<PlayableHubController>();
            Assert.That(hub, Is.Not.Null);
            Assert.That(hub.IsVisible, Is.True);
            Assert.That(hub.Player, Is.Not.Null);
            Assert.That(hub.Entrances.Count, Is.EqualTo(7));

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
