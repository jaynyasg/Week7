using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CareerQuest.Tests
{
    public class PlayModeUxPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlayCampusHidesActionBarAndShowsHubInstructionStrip()
        {
            var appObject = new GameObject("play-ux-campus-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);

            Assert.That(GameObject.Find("CampusActionBar"), Is.Null);
            var label = FindInstructionLabel();
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Does.Contain("career door"));

            Object.Destroy(appObject);
        }

        [UnityTest]
        public IEnumerator InstructionStripUpdatesWhenEnteringRoomFromHub()
        {
            var appObject = new GameObject("play-ux-room-enter-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);

            var hub = Object.FindAnyObjectByType<PlayableHubController>();
            Assert.That(hub, Is.Not.Null);
            Assert.That(hub.TryEnter(ActivityRoute.HealthHero), Is.True);
            yield return null;

            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.HealthHero));
            var label = FindInstructionLabel();
            Assert.That(label, Is.Not.Null);
            Assert.That(label.text, Does.Contain("symptom"));

            Object.Destroy(appObject);
            if (hub != null)
            {
                Object.Destroy(hub.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator ShowcaseCampusKeepsActionBarWithoutPlayStrip()
        {
            var appObject = new GameObject("showcase-ux-campus-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            app.BeginShowcase();
            app.ShowCampus();
            yield return null;

            Assert.That(GameObject.Find("CampusActionBar"), Is.Not.Null);
            Assert.That(GameObject.Find(InstructionStrip.PanelName), Is.Null);

            Object.Destroy(appObject);
        }

        [UnityTest]
        public IEnumerator CeremonyHidesInstructionStripAndShowsSkipControl()
        {
            var appObject = new GameObject("ceremony-ux-strip-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);
            app.ShowDesignBuild(false);

            Assert.That(FindInstructionLabel(), Is.Not.Null, "Play mode should show instructions before ceremony.");

            CompleteDesignBuildRoom();
            yield return null;

            Assert.That(GameObject.Find(InstructionStrip.PanelName), Is.Null);
            Assert.That(GameObject.Find("CeremonyOverlay"), Is.Not.Null);
            Assert.That(GameObject.Find("CeremonySkipButton"), Is.Not.Null);

            Object.DestroyImmediate(appObject);
        }

        private static Text FindInstructionLabel()
        {
            foreach (var text in Resources.FindObjectsOfTypeAll<Text>())
            {
                if (text.name == InstructionStrip.LabelName)
                {
                    return text;
                }
            }

            return null;
        }

        private static void CompleteDesignBuildRoom()
        {
            FindButton("ReviewBlueprintButton").onClick.Invoke();
            FindButton("PatternHelperButton").onClick.Invoke();

            foreach (var pieceId in new[] { "clinic", "court", "studio", "lab", "art_tower" })
            {
                FindButton($"{pieceId}Button").onClick.Invoke();
            }

            FindButton("DesignBuildCompleteButton").onClick.Invoke();
        }

        private static Button FindButton(string name)
        {
            foreach (var button in Resources.FindObjectsOfTypeAll<Button>())
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            Assert.Fail($"{name} should exist.");
            return null;
        }
    }
}
