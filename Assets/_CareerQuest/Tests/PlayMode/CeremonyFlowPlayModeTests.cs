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
        public IEnumerator HealthHeroCompletionShowsCeremonyBeforeGallery()
        {
            var appObject = new GameObject("ceremony-health-hero-test");
            var app = appObject.AddComponent<CareerQuestApp>();

            yield return null;
            app.ShowHealthHero();
            yield return null;

            CompleteHealthHeroRoom(appObject);

            var overlay = GameObject.Find("CeremonyOverlay");
            Assert.That(overlay, Is.Not.Null, "Ceremony overlay should appear after Health Hero completion.");
            Assert.That(overlay.activeSelf, Is.True);

            var gallery = GameObject.Find("AchievementGalleryPanel");
            Assert.That(gallery, Is.Null, "Gallery should stay hidden until ceremony finishes.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator LogicCourtCompletionShowsCeremonyBeforeGallery()
        {
            var appObject = new GameObject("ceremony-logic-court-test");
            var app = appObject.AddComponent<CareerQuestApp>();

            yield return null;
            app.ShowLogicCourt();
            yield return null;

            CompleteLogicCourtRoom(appObject);

            var overlay = GameObject.Find("CeremonyOverlay");
            Assert.That(overlay, Is.Not.Null, "Ceremony overlay should appear after Logic Court completion.");
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

            // U7 migration: skip drives the TrySkipCeremony seam (the overlay
            // button shares the same guarded path), not onClick.Invoke.
            Assert.That(app.TrySkipCeremony(), Is.False, "Skip seam should refuse before the delay elapses.");

            yield return new WaitForSecondsRealtime(CeremonyController.SkipDelaySeconds + 0.25f);

            Assert.That(skip.interactable, Is.True, "Skip should become available after the delay.");
            Assert.That(app.TrySkipCeremony(), Is.True, "Skip seam should fire after the delay.");

            yield return null;

            var gallery = GameObject.Find("AchievementGalleryPanel");
            Assert.That(gallery, Is.Not.Null, "Gallery should open after skipping ceremony.");
            Assert.That(gallery.activeSelf, Is.True);

            Object.DestroyImmediate(appObject);
        }

        /// <summary>
        /// U6/U10 migration: all three core rooms are drag rooms — completion is
        /// driven through the TrySubmitDrop seams (the route emitter → ceremony →
        /// router contract is unchanged). FindButton drivers remain only in the
        /// optional-room suites.
        /// </summary>
        private static void CompleteDesignBuildRoom(GameObject appObject)
        {
            var controller = appObject.GetComponent<DesignBuildController>();
            Assert.That(controller, Is.Not.Null, "DesignBuildController should exist after ShowDesignBuild.");

            foreach (var pieceId in new[] { "clinic", "court", "studio", "lab", "art_tower" })
            {
                Assert.That(
                    controller.TrySubmitDrop(pieceId, pieceId),
                    Is.EqualTo(DropSubmitResult.Accepted),
                    $"Drop of '{pieceId}' should be accepted.");
            }
        }

        /// <summary>U10: the three care steps play as ordered drags onto the patient zone.</summary>
        private static void CompleteHealthHeroRoom(GameObject appObject)
        {
            var controller = appObject.GetComponent<HealthHeroController>();
            Assert.That(controller, Is.Not.Null, "HealthHeroController should exist after ShowHealthHero.");

            foreach (var pieceId in HealthHeroClinicLayout.StepPieceIds)
            {
                Assert.That(
                    controller.TrySubmitDrop(pieceId, HealthHeroClinicLayout.PatientZoneId),
                    Is.EqualTo(DropSubmitResult.Accepted),
                    $"Care step '{pieceId}' should be accepted on the patient zone.");
            }
        }

        /// <summary>U10: case review is a podium drag; each sort is a zone drag.</summary>
        private static void CompleteLogicCourtRoom(GameObject appObject)
        {
            var controller = appObject.GetComponent<LogicCourtController>();
            Assert.That(controller, Is.Not.Null, "LogicCourtController should exist after ShowLogicCourt.");

            Assert.That(
                controller.TrySubmitDrop(LogicCourtLayout.CaseFilePieceId, LogicCourtLayout.PodiumZoneId),
                Is.EqualTo(DropSubmitResult.Accepted),
                "Case file should be accepted on the podium.");

            foreach (var pieceId in LogicCourtLayout.EvidencePieceIds)
            {
                Assert.That(
                    controller.TrySubmitDrop(pieceId, LogicCourtLayout.CorrectZoneFor(pieceId)),
                    Is.EqualTo(DropSubmitResult.Accepted),
                    $"Evidence '{pieceId}' should be accepted in its correct zone.");
            }
        }
    }
}
