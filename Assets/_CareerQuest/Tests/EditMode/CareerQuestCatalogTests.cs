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

        [Test]
        public void PartyStationIdsCoverAllTenStationsWithCatalogEntries()
        {
            Assert.That(CareerQuestCatalog.PartyStationIds.Length, Is.EqualTo(10));
            Assert.That(CareerQuestCatalog.PartyStationIds.Distinct().Count(), Is.EqualTo(10));

            foreach (var stationId in CareerQuestCatalog.PartyStationIds)
            {
                Assert.That(CareerQuestCatalog.TryGetById(stationId, out var entry), Is.True, stationId);
                Assert.That(entry.Id, Is.EqualTo(stationId));
                Assert.That(entry.BadgeArtKey, Is.EqualTo($"badge.{stationId}"), stationId);
            }
        }

        [Test]
        public void PartyOnlyEntriesUseStationIdRoutingAndStayOutOfLegacySurfaces()
        {
            Assert.That(CareerQuestCatalog.PartyStationEntries.Count, Is.EqualTo(6));
            Assert.That(CareerQuestCatalog.AllWithPartyStations.Count(), Is.EqualTo(13));

            foreach (var entry in CareerQuestCatalog.PartyStationEntries)
            {
                Assert.That(entry.UsesStationIdRouting, Is.True, entry.Id);
                Assert.That(entry.IsCore, Is.False, entry.Id);
                // Station-id routed entries never resolve from route lookups —
                // the legacy All/OptionalEntries surfaces stay unchanged.
                Assert.That(CareerQuestCatalog.All.Select(legacy => legacy.Id), Does.Not.Contain(entry.Id));
            }

            Assert.That(CareerQuestCatalog.IsPlayableRoute(ActivityRoute.Campus), Is.False);
        }

        [Test]
        public void TryGetByIdResolvesLegacyAndPartyStationEntries()
        {
            Assert.That(CareerQuestCatalog.TryGetById(CareerQuestCatalog.RoboticsGarageId, out var legacy), Is.True);
            Assert.That(legacy.UsesStationIdRouting, Is.False);

            Assert.That(CareerQuestCatalog.TryGetById(CareerQuestCatalog.VetClinicId, out var party), Is.True);
            Assert.That(party.UsesStationIdRouting, Is.True);
            Assert.That(CareerQuestCatalog.GetById(CareerQuestCatalog.VetClinicId).Id, Is.EqualTo(CareerQuestCatalog.VetClinicId));

            Assert.That(CareerQuestCatalog.TryGetById("not_a_station", out _), Is.False);
            Assert.That(CareerQuestCatalog.TryGetById(null, out _), Is.False);
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
