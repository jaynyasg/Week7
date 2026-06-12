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

        [UnityTest]
        public IEnumerator HealthAndLogicControlsStayInHudTrays()
        {
            var gameObject = new GameObject("optional-room-layout-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            app.BeginPlay();
            app.ShowHealthHero();
            yield return null;

            var healthHud = RectFor("HealthHeroQuestHud");
            var healthTray = RectFor("HealthHeroToolTray");
            var healthCheck = RectFor("HealthHeroCheckButton");
            var healthComplete = RectFor("HealthHeroCompleteButton");
            var healthPrompt = GameObject.Find("HealthHeroPrompt").GetComponent<TextMeshProUGUI>();

            Assert.That(healthHud.anchoredPosition.y, Is.GreaterThan(230f));
            Assert.That(Top(healthTray), Is.LessThan(-280f));
            Assert.That(healthCheck.sizeDelta.y, Is.LessThanOrEqualTo(38f));
            Assert.That(healthComplete.sizeDelta.y, Is.LessThanOrEqualTo(44f));
            Assert.That(healthPrompt.fontSize, Is.LessThanOrEqualTo(18));

            app.ShowLogicCourt();
            yield return null;

            var logicHud = RectFor("LogicCourtQuestHud");
            var logicTray = RectFor("LogicCourtEvidenceTray");
            var logicReview = RectFor("LogicCourtReviewButton");
            var logicClosing = RectFor("LogicCourtClosingButton");
            var logicPrompt = GameObject.Find("LogicCourtPrompt").GetComponent<TextMeshProUGUI>();

            Assert.That(logicHud.anchoredPosition.y, Is.GreaterThan(230f));
            Assert.That(Top(logicTray), Is.LessThan(-280f));
            Assert.That(logicReview.sizeDelta.y, Is.LessThanOrEqualTo(38f));
            Assert.That(logicClosing.sizeDelta.y, Is.LessThanOrEqualTo(44f));
            Assert.That(logicPrompt.fontSize, Is.LessThanOrEqualTo(18));

            Object.Destroy(gameObject);
        }

        private static RectTransform RectFor(string objectName)
        {
            var gameObject = GameObject.Find(objectName);
            Assert.That(gameObject, Is.Not.Null, $"{objectName} should exist.");
            return gameObject.GetComponent<RectTransform>();
        }

        private static float Top(RectTransform rectTransform)
        {
            return rectTransform.anchoredPosition.y + rectTransform.sizeDelta.y * 0.5f;
        }

        private static void CompleteOptionalRoom(string panelPrefix)
        {
            GameObject.Find($"{panelPrefix}StepButton").GetComponent<Button>().onClick.Invoke();
            GameObject.Find($"{panelPrefix}CompleteButton").GetComponent<Button>().onClick.Invoke();
        }
    }
}
