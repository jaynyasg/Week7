using System.Collections;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CareerQuest.Tests
{
    public class OptionalMiniGameFlowTests
    {
        [Test]
        public void OptionalMiniGamesFeedSameBestResultContract()
        {
            var session = new GameSession();
            var healthObject = new GameObject("health-flow-test");
            var courtObject = new GameObject("court-flow-test");
            var health = healthObject.AddComponent<HealthHeroController>();
            var court = courtObject.AddComponent<LogicCourtController>();

            session.RecordResult(health.CreateResult(true, ResultSource.SoloFallback));
            session.RecordResult(court.CreateResult(true, ResultSource.SoloFallback));

            Assert.That(session.GetBestResult(CareerConfig.HealthHeroId), Is.Not.Null);
            Assert.That(session.GetBestResult(CareerConfig.LogicCourtId), Is.Not.Null);
            Assert.That(session.RevealReady, Is.False);
            Assert.That(session.ConfidencePhrase(), Is.EqualTo("One more game"));

            Object.DestroyImmediate(healthObject);
            Object.DestroyImmediate(courtObject);
        }

        [UnityTest]
        public IEnumerator MusicStudioCompletionShowsCeremonyBeforeGallery()
        {
            var gameObject = new GameObject("optional-music-studio-test");
            var app = gameObject.AddComponent<CareerQuestApp>();

            yield return null;
            app.ShowMusicStudio();
            yield return null;

            CompleteOptionalRoom("MusicStudio");

            var overlay = GameObject.Find("CeremonyOverlay");
            Assert.That(overlay, Is.Not.Null, "Ceremony overlay should appear after optional room completion.");
            Assert.That(overlay.activeSelf, Is.True);

            var gallery = GameObject.Find("AchievementGalleryPanel");
            Assert.That(gallery, Is.Null, "Gallery should stay hidden until ceremony finishes.");

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator OptionalHubRoomsExposePlayableEntrances()
        {
            var gameObject = new GameObject("optional-hub-entrances-test");
            var app = gameObject.AddComponent<CareerQuestApp>();

            yield return null;
            yield return PlayModeTestBootstrap.EnterPlayCampus(app);

            var hub = Object.FindAnyObjectByType<PlayableHubController>();
            Assert.That(hub, Is.Not.Null);
            Assert.That(hub.Entrances.Count, Is.EqualTo(7));
            Assert.That(hub.Entrances.Any(entrance => entrance.Route == ActivityRoute.MusicStudio), Is.True);
            Assert.That(hub.Entrances.Any(entrance => entrance.Route == ActivityRoute.RoboticsGarage), Is.True);

            Object.Destroy(gameObject);
        }

        /// <summary>
        /// U10 migration: Health Hero and Logic Court are drag rooms — the button
        /// trays are gone. The rooms render the quest HUD (top band), a Campus
        /// exit (bottom edge), and play through their TrySubmitDrop seams; legacy
        /// step/complete buttons must NOT come back (mirrors the Design Build
        /// chrome guard).
        /// </summary>
        [UnityTest]
        public IEnumerator HealthAndLogicRenderDragRoomChrome()
        {
            var gameObject = new GameObject("core-room-chrome-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            app.ShowHealthHero();
            yield return null;

            var healthHud = RectFor("HealthHeroQuestHud");
            var healthCampus = RectFor("HealthHeroCampusButton");
            var healthPrompt = GameObject.Find("HealthHeroPrompt").GetComponent<TextMeshProUGUI>();

            Assert.That(healthHud.anchoredPosition.y, Is.GreaterThan(220f));
            Assert.That(healthCampus.anchoredPosition.y, Is.LessThan(-280f));
            Assert.That(healthCampus.sizeDelta.y, Is.LessThanOrEqualTo(44f));
            Assert.That(healthPrompt.fontSize, Is.LessThanOrEqualTo(18));
            Assert.That(healthPrompt.text, Does.Contain("patient"));

            Assert.That(GameObject.Find("HealthHeroToolTray"), Is.Null, "Drag conversion retires the button tray.");
            Assert.That(GameObject.Find("HealthHeroCheckButton"), Is.Null);
            Assert.That(GameObject.Find("HealthHeroCompleteButton"), Is.Null);

            app.ShowLogicCourt();
            yield return null;

            var logicHud = RectFor("LogicCourtQuestHud");
            var logicCampus = RectFor("LogicCourtCampusButton");
            var logicPrompt = GameObject.Find("LogicCourtPrompt").GetComponent<TextMeshProUGUI>();

            Assert.That(logicHud.anchoredPosition.y, Is.GreaterThan(220f));
            Assert.That(logicCampus.anchoredPosition.y, Is.LessThan(-280f));
            Assert.That(logicCampus.sizeDelta.y, Is.LessThanOrEqualTo(44f));
            Assert.That(logicPrompt.fontSize, Is.LessThanOrEqualTo(18));
            Assert.That(logicPrompt.text, Does.Contain("case"));

            Assert.That(GameObject.Find("LogicCourtEvidenceTray"), Is.Null, "Drag conversion retires the button tray.");
            Assert.That(GameObject.Find("LogicCourtReviewButton"), Is.Null);
            Assert.That(GameObject.Find("LogicCourtClosingButton"), Is.Null);

            Object.DestroyImmediate(gameObject);
        }

        private static RectTransform RectFor(string objectName)
        {
            var gameObject = GameObject.Find(objectName);
            Assert.That(gameObject, Is.Not.Null, $"{objectName} should exist.");
            return gameObject.GetComponent<RectTransform>();
        }

        private static void CompleteOptionalRoom(string panelPrefix)
        {
            GameObject.Find($"{panelPrefix}StepButton").GetComponent<Button>().onClick.Invoke();
            GameObject.Find($"{panelPrefix}CompleteButton").GetComponent<Button>().onClick.Invoke();
        }
    }
}
