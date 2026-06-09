using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    public class EntryFlowTests
    {
        [UnityTest]
        public IEnumerator PlayAndShowcaseRouteToDistinctModes()
        {
            var gameObject = new GameObject("app-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            Assert.That(app.Session.Mode, Is.EqualTo(AppMode.Entry));

            app.BeginPlay();
            Assert.That(app.Session.Mode, Is.EqualTo(AppMode.Play));
            Assert.That(app.Session.HasSeededResults, Is.False);

            app.BeginShowcase();
            yield return null;

            Assert.That(app.Session.Mode, Is.EqualTo(AppMode.Showcase));
            Assert.That(app.Session.HasSeededResults, Is.True);

            Object.Destroy(gameObject);
        }
    }
}
