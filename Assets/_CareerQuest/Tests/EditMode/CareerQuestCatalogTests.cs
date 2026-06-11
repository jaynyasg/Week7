using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class CareerQuestCatalogTests
    {
        [Test]
        public void CatalogListsCoreAndOptionalActivities()
        {
            Assert.That(CareerQuestCatalog.All.Count, Is.EqualTo(7));
            Assert.That(CareerQuestCatalog.All.Count(entry => entry.IsCore), Is.EqualTo(3));
            Assert.That(CareerQuestCatalog.OptionalEntries.Count(), Is.EqualTo(4));
        }

        [Test]
        public void OptionalRoutesArePlayableMiniGameRoutes()
        {
            Assert.That(SceneFlowRouter.IsMiniGameRoute(ActivityRoute.AiLab), Is.True);
            Assert.That(SceneFlowRouter.IsMiniGameRoute(ActivityRoute.MusicStudio), Is.True);
            Assert.That(SceneFlowRouter.IsMiniGameRoute(ActivityRoute.RoboticsGarage), Is.True);
            Assert.That(SceneFlowRouter.IsMiniGameRoute(ActivityRoute.CommunityKitchen), Is.True);
        }

        [Test]
        public void OptionalResultsCountTowardRevealEligibility()
        {
            var session = new GameSession();
            var music = CareerQuestCatalog.GetById(CareerQuestCatalog.MusicStudioId);
            var robotics = CareerQuestCatalog.GetById(CareerQuestCatalog.RoboticsGarageId);
            var kitchen = CareerQuestCatalog.GetById(CareerQuestCatalog.CommunityKitchenId);

            session.RecordResult(CreateCatalogResult(music));
            session.RecordResult(CreateCatalogResult(robotics));
            Assert.That(session.RevealReady, Is.False);

            session.RecordResult(CreateCatalogResult(kitchen));
            Assert.That(session.RevealReady, Is.True);
        }

        [Test]
        public void GetByRouteReturnsMatchingCatalogEntry()
        {
            var entry = CareerQuestCatalog.GetByRoute(ActivityRoute.MusicStudio);

            Assert.That(entry.Id, Is.EqualTo(CareerQuestCatalog.MusicStudioId));
            Assert.That(entry.BuildingName, Is.EqualTo("Music Studio"));
        }

        private static MiniGameResult CreateCatalogResult(CatalogEntry entry)
        {
            return new MiniGameResult(
                entry.Id,
                entry.DisplayName,
                CompletionTier.Degree,
                ResultSource.SoloFallback,
                new[] { new TraitDelta("Focus", 3) },
                30f,
                0.9f,
                $"Completed {entry.DisplayName}.");
        }
    }
}
