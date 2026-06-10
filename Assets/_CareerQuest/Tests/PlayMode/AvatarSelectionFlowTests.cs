using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CareerQuest.Tests
{
    public class AvatarSelectionFlowTests
    {
        [UnityTest]
        public IEnumerator AvatarChoicePersistsIntoPlayRoute()
        {
            var gameObject = new GameObject("avatar-flow-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            app.ShowAvatarSelectionForPlay();
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.AvatarSelection));

            app.ChooseAvatar("logic_spark");

            Assert.That(app.Session.SelectedAvatar.Id, Is.EqualTo("logic_spark"));
            Assert.That(app.Session.Mode, Is.EqualTo(AppMode.Play));
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator AvatarChoicePersistsIntoShowcaseRoute()
        {
            var gameObject = new GameObject("avatar-showcase-flow-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            app.ShowAvatarSelectionForShowcase();
            app.ChooseAvatar("care_captain");
            yield return null;

            Assert.That(app.Session.SelectedAvatar.Id, Is.EqualTo("care_captain"));
            Assert.That(app.Session.Mode, Is.EqualTo(AppMode.Showcase));
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.ShowcaseProof));
            Assert.That(app.Session.HasSeededResults, Is.True);

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator AvatarSelectionScreenShowsQuestCharacterPresentation()
        {
            var gameObject = new GameObject("avatar-presentation-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            app.ShowAvatarSelectionForPlay();
            yield return null;

            AssertText("AvatarSelectionTitle", "Choose Your Quest Hero");
            AssertText("SelectedAvatarPassportTitle", "Quest Passport");
            AssertButtonText("AvatarConfirmButton", "Enter Campus");
            AssertText("sky_builderSelectedState", "Selected");
            AssertText("logic_sparkSelectedState", string.Empty);

            GameObject.Find("logic_sparkChooseButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(app.Session.SelectedAvatar.Id, Is.EqualTo("logic_spark"));
            AssertText("SelectedAvatarName", "Logic Spark");
            AssertText("sky_builderSelectedState", string.Empty);
            AssertText("logic_sparkSelectedState", "Selected");
            AssertButtonText("logic_sparkChooseButton", "Selected");

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
