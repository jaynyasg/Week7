using System.Collections;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    public class CampusPresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator CampusGuideUsesCompactRenderScale()
        {
            var guideObject = new GameObject(
                "guide-scale-test",
                typeof(SpriteRenderer),
                typeof(AvatarRuntimeView),
                typeof(CampusGuideController));

            var guide = guideObject.GetComponent<CampusGuideController>();
            guide.Configure("Walk into a door to start a quest!");
            yield return null;

            var view = guideObject.GetComponent<AvatarRuntimeView>();
            var renderer = guideObject.GetComponent<SpriteRenderer>();

            Assert.That(view.BaseRenderScale.x, Is.EqualTo(CampusGuideController.GuideRenderScale).Within(0.001f));
            Assert.That(renderer.transform.localScale.x, Is.EqualTo(CampusGuideController.GuideRenderScale).Within(0.001f));

            Object.Destroy(guideObject);
        }

        [UnityTest]
        public IEnumerator EntryRouteUsesLoweredTitleScreenFraming()
        {
            var worldObject = new GameObject("entry-title-framing-test");
            var world = worldObject.AddComponent<CampusWorldController>();
            yield return null;

            world.ShowEntry(new GameSession());
            yield return null;

            Assert.That(world.CameraDirector.RouteShot.Approximately(CampusWorldController.EntryTitleShot), Is.True);
            Assert.That(world.CameraDirector.RouteShot.Position.y, Is.GreaterThan(CameraShot.Default.Position.y));

            Object.Destroy(worldObject);
        }
    }
}
