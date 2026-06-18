using System.Collections;
using CareerQuest;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    public class CampusVisualAlignmentPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayModeSceneScrubber.DestroyStaleAppRoots();
        }

        [TearDown]
        public void TearDown()
        {
            FirstRunGuideBeat.ResetSessionFlag();
        }

        [UnityTest]
        public IEnumerator CampusGuideSpawnsLeftOfTheCentralLabelStack()
        {
            var appObject = new GameObject("campus-guide-alignment-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);

            var guideObject = GameObject.Find("CampusGuide");
            Assert.That(guideObject, Is.Not.Null);
            Assert.That(WorldAnchors.AssetGuideSpawn.x, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(guideObject.transform.position.x, Is.LessThanOrEqualTo(0.25f));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator CampusHudHintDoesNotOverlapThePassportButton()
        {
            var appObject = new GameObject("campus-hud-alignment-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);

            var hint = GameObject.Find("CampusControlsHint").GetComponent<TextMeshProUGUI>();
            var passport = GameObject.Find("CampusPassportButton").GetComponent<RectTransform>();
            Assert.That(hint.text, Does.Contain("WASD"));

            var canvas = Object.FindAnyObjectByType<Canvas>();
            var hintBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform, hint.rectTransform);
            var passportBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform, passport);
            Assert.That(Overlaps(hintBounds, passportBounds), Is.False, "HUD hint must not sit underneath the Passport button.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator SpaceportWireSubjectLeavesTheInstructionBannerClear()
        {
            var appObject = new GameObject("spaceport-wire-layout-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            var controller = appObject.AddComponent<PartyStationController>();
            controller.AutoTick = false;
            controller.QuickPacing = true;
            yield return null;

            app.ShowPartyStation(CareerQuestCatalog.SpaceportId);
            yield return MountFrames();

            var subject = GameObject.Find(StationSubjectView.RootName);
            var banner = GameObject.Find(PartyStationRenderer.WireBannerName);
            Assert.That(subject, Is.Not.Null);
            Assert.That(banner, Is.Not.Null);
            Assert.That(subject.transform.localPosition.x, Is.LessThan(-2.5f), "Spaceport subject moves out of the banner's center lane.");
            Assert.That(banner.transform.localPosition.y, Is.LessThan(1.8f), "Wire prompt sits below the subject art.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator PassportResultsUseStationNamesAndInsetTheirRowText()
        {
            var appObject = new GameObject("passport-result-alignment-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);
            RecordStation(app.Session, CareerQuestCatalog.SpaceportId);

            app.ShowPassport(PassportController.PassportPage.Results);
            yield return null;

            var row = GameObject.Find("PassportResultRow0").GetComponent<RectTransform>();
            var name = GameObject.Find("PassportResultName0").GetComponent<TextMeshProUGUI>();
            var tier = GameObject.Find("PassportResultTier0").GetComponent<RectTransform>();

            Assert.That(name.text, Is.EqualTo("Spaceport Connect"));
            Assert.That(tier.anchoredPosition.x + tier.sizeDelta.x * 0.5f, Is.LessThan(row.sizeDelta.x * 0.5f - 30f));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator RevealCelebrateGearStaysBelowTheFaceLine()
        {
            var avatarObject = new GameObject("reveal-gear-alignment-test", typeof(SpriteRenderer), typeof(AvatarRuntimeView));
            var view = avatarObject.GetComponent<AvatarRuntimeView>();
            view.ApplyAvatar("sky_builder");
            view.Animator.AutoTick = false;

            var session = new GameSession();
            RecordStation(session, CareerQuestCatalog.VetClinicId);
            RecordStation(session, CareerQuestCatalog.GameStudioId);
            RecordStation(session, CareerQuestCatalog.SpaceportId);

            view.BindAccessories(session, ceremonyContext: true);
            yield return null;

            view.TriggerCelebrate(0.8f);
            view.Animator.Tick(0.2f);
            yield return null;

            var layer = view.AccessoryLayer;
            var sash = layer.RendererFor("accessory.badge_sash");
            var patch = layer.RendererFor("accessory.mission_patch");
            Assert.That(sash, Is.Not.Null);
            Assert.That(patch, Is.Not.Null);
            Assert.That(sash.transform.localPosition.y, Is.LessThan(0f), "Badge sash should stay on the torso, not cross the face.");
            Assert.That(patch.transform.localPosition.y, Is.LessThan(0.12f), "Mission patch should sit on the chest during the reveal jump.");

            Object.DestroyImmediate(avatarObject);
        }

        private static IEnumerator MountFrames()
        {
            yield return null;
            yield return null;
            yield return null;
        }

        private static bool Overlaps(Bounds a, Bounds b)
        {
            return a.min.x <= b.max.x && a.max.x >= b.min.x
                && a.min.y <= b.max.y && a.max.y >= b.min.y;
        }

        private static void RecordStation(GameSession session, string stationId)
        {
            var definition = PartyStationDefinitions.GetById(stationId);
            var result = PartyStationController.BuildResult(
                definition,
                definition.DefaultSeed,
                ResultSource.Solo,
                complete: true,
                wrongAttempts: 0,
                playElapsedSeconds: 12f);
            session.RecordResult(result);
            session.AppendStationRewardEvent(new StationRewardEvent(
                stationId,
                definition.DefaultSeed.SeedId,
                result.Tier,
                result.Source,
                result.Summary,
                definition.AccessoryRewardId,
                result.TraitDeltas));
        }
    }
}
