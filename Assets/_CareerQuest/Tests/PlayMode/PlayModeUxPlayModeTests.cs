using System.Collections;
using CareerQuest;
using NUnit.Framework;
using TMPro;
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

            // Long kid-facing strings must wrap and auto-size instead of overflowing the strip.
            Assert.That(label.textWrappingMode, Is.EqualTo(TextWrappingModes.Normal));
            Assert.That(label.enableAutoSizing, Is.True);

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

        [UnityTest]
        public IEnumerator ActiveUiUsesTmpTypographyInEveryCoreState()
        {
            var appObject = new GameObject("ae5-typography-scan-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null; // Start() renders the entry screen.

            AssertNoLegacyUiText("entry");

            app.ShowAvatarSelectionForPlay();
            yield return null;
            AssertNoLegacyUiText("avatar-selection");

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);
            AssertNoLegacyUiText("campus");

            app.ShowDesignBuild(false);
            yield return null;
            AssertNoLegacyUiText("design-build");

            app.ShowHealthHero();
            yield return null;
            AssertNoLegacyUiText("health-hero");

            app.ShowLogicCourt();
            yield return null;
            AssertNoLegacyUiText("logic-court");

            app.ShowGallery();
            yield return null;
            AssertNoLegacyUiText("gallery");

            app.ShowReveal();
            yield return null;
            AssertNoLegacyUiText("reveal");

            Object.DestroyImmediate(appObject);
        }

        /// <summary>
        /// AE5 surface: active uGUI hierarchies must contain zero legacy Text components and no
        /// LegacyRuntime/Arial font references. World-space TextMesh is intentionally out of scope
        /// (it is retired by the U4/U5 world rebuild, not this typography pass).
        /// </summary>
        private static void AssertNoLegacyUiText(string state)
        {
            var legacyTexts = Object.FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Assert.That(legacyTexts, Is.Empty,
                $"State '{state}' should render zero legacy UnityEngine.UI.Text components.");

            foreach (var text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Assert.That(text.font, Is.Not.Null,
                    $"State '{state}': '{text.name}' should have a TMP font asset assigned.");

                var fontName = text.font.name;
                Assert.That(fontName.Contains("Arial") || fontName.Contains("LegacyRuntime"), Is.False,
                    $"State '{state}': '{text.name}' should not reference a legacy font (got '{fontName}').");
                Assert.That(
                    fontName.Contains(TypeStyles.DisplayFamily) || fontName.Contains(TypeStyles.BodyFamily),
                    Is.True,
                    $"State '{state}': '{text.name}' should use {TypeStyles.DisplayFamily} or {TypeStyles.BodyFamily} (got '{fontName}').");
            }
        }

        private static TextMeshProUGUI FindInstructionLabel()
        {
            foreach (var text in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
            {
                if (text.name == InstructionStrip.LabelName)
                {
                    return text;
                }
            }

            return null;
        }

        /// <summary>
        /// U6 migration: Design Build completes through the drag seam
        /// (TrySubmitDrop), not legacy button drivers. Completion auto-routes
        /// emitter → ceremony → router when the final piece lands.
        /// </summary>
        private static void CompleteDesignBuildRoom()
        {
            var controller = Object.FindAnyObjectByType<DesignBuildController>();
            Assert.That(controller, Is.Not.Null, "DesignBuildController should exist after ShowDesignBuild.");

            foreach (var pieceId in new[] { "clinic", "court", "studio", "lab", "art_tower" })
            {
                Assert.That(
                    controller.TrySubmitDrop(pieceId, pieceId),
                    Is.EqualTo(DropSubmitResult.Accepted),
                    $"Drop of '{pieceId}' should be accepted.");
            }
        }
    }
}
