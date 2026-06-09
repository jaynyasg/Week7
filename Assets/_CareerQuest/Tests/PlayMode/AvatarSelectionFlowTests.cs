using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Connection));

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
    }
}
