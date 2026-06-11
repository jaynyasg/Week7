using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CareerQuest.Tests
{
    public class CeremonyFlowPlayModeTests
    {
        [UnityTest]
        public IEnumerator DesignBuildCompletionShowsCeremonyBeforeGallery()
        {
            var appObject = new GameObject("ceremony-flow-test");
            var app = appObject.AddComponent<CareerQuestApp>();

            yield return null;
            app.ShowDesignBuild(false);
            yield return null;

            CompleteDesignBuildRoom(appObject);

            var overlay = GameObject.Find("CeremonyOverlay");
            Assert.That(overlay, Is.Not.Null, "Ceremony overlay should appear after room completion.");
            Assert.That(overlay.activeSelf, Is.True);

            var gallery = GameObject.Find("AchievementGalleryPanel");
            Assert.That(gallery, Is.Null, "Gallery should stay hidden until ceremony finishes.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator CeremonySkipOpensGalleryAfterDelay()
        {
            var appObject = new GameObject("ceremony-skip-test");
            var app = appObject.AddComponent<CareerQuestApp>();

            yield return null;
            app.ShowDesignBuild(false);
            yield return null;

            CompleteDesignBuildRoom(appObject);
            yield return null;

            var skipButton = GameObject.Find("CeremonySkipButton");
            Assert.That(skipButton, Is.Not.Null);

            var skip = skipButton.GetComponent<Button>();
            Assert.That(skip.interactable, Is.False, "Skip should stay disabled until the delay elapses.");

            yield return new WaitForSecondsRealtime(CeremonyController.SkipDelaySeconds + 0.25f);

            Assert.That(skip.interactable, Is.True, "Skip should become available after the delay.");
            skip.onClick.Invoke();

            yield return null;

            var gallery = GameObject.Find("AchievementGalleryPanel");
            Assert.That(gallery, Is.Not.Null, "Gallery should open after skipping ceremony.");
            Assert.That(gallery.activeSelf, Is.True);

            Object.DestroyImmediate(appObject);
        }

        private static void CompleteDesignBuildRoom(GameObject appObject)
        {
            var reviewButton = FindButton("ReviewBlueprintButton");
            var helperButton = FindButton("PatternHelperButton");
            reviewButton.onClick.Invoke();
            helperButton.onClick.Invoke();

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
