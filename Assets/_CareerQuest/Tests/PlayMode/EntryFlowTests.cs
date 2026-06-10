using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Entry));

            app.BeginPlay();
            Assert.That(app.Session.Mode, Is.EqualTo(AppMode.Play));
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Connection));
            Assert.That(app.Session.HasSeededResults, Is.False);

            app.BeginShowcase();
            yield return null;

            Assert.That(app.Session.Mode, Is.EqualTo(AppMode.Showcase));
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.ShowcaseProof));
            Assert.That(app.Session.HasSeededResults, Is.True);

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator ConnectionScreenUsesPlayerFacingChoices()
        {
            var gameObject = new GameObject("connection-screen-copy-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            app.BeginPlay();
            yield return null;

            AssertText("ConnectionTitle", "Start Game");
            AssertButtonText("PlaySoloButton", "Play Solo");
            AssertButtonText("HostLocalGameButton", "Host Game");
            AssertButtonText("JoinThisComputerButton", "Join This PC");
            AssertButtonText("JoinIpButton", "Join IP");
            Assert.That(GameObject.Find("HostP1Button"), Is.Null);
            Assert.That(GameObject.Find("SoloFallbackButton"), Is.Null);

            Object.Destroy(gameObject);
        }

        private static void AssertText(string objectName, string expected)
        {
            var textObject = GameObject.Find(objectName);
            Assert.That(textObject, Is.Not.Null, $"{objectName} should exist.");
            Assert.That(textObject.GetComponent<Text>().text, Is.EqualTo(expected));
        }

        private static void AssertButtonText(string buttonName, string expected)
        {
            var buttonObject = GameObject.Find(buttonName);
            Assert.That(buttonObject, Is.Not.Null, $"{buttonName} should exist.");
            Assert.That(buttonObject.GetComponentInChildren<Text>().text, Is.EqualTo(expected));
        }
    }
}
