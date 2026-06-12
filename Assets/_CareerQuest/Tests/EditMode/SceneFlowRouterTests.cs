using System;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class SceneFlowRouterTests
    {
        [Test]
        public void BeginPlayRoutesToCampusWithoutSeededResults()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();

            var route = router.BeginPlay(session);

            Assert.That(route, Is.EqualTo(ActivityRoute.Campus));
            Assert.That(router.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));
            Assert.That(session.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));
            Assert.That(session.Mode, Is.EqualTo(AppMode.Play));
            Assert.That(session.PlayerCount, Is.EqualTo(1));
            Assert.That(session.HasSeededResults, Is.False);
        }

        [Test]
        public void ConnectionRemainsAvailableAsSecondaryRoute()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();

            var route = router.ShowConnection(session);

            Assert.That(route, Is.EqualTo(ActivityRoute.Connection));
            Assert.That(router.CurrentRoute, Is.EqualTo(ActivityRoute.Connection));
            Assert.That(session.CurrentRoute, Is.EqualTo(ActivityRoute.Connection));
        }

        [Test]
        public void ShowcaseAvatarChoiceSeedsShowcaseAndProofRoute()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();

            router.ShowAvatarSelection(session, AppMode.Showcase);
            var route = router.ChooseAvatar(session, AvatarConfig.DefaultAvatarId);

            Assert.That(route, Is.EqualTo(ActivityRoute.ShowcaseProof));
            Assert.That(session.CurrentRoute, Is.EqualTo(ActivityRoute.ShowcaseProof));
            Assert.That(session.Mode, Is.EqualTo(AppMode.Showcase));
            Assert.That(session.PlayerCount, Is.EqualTo(2));
            Assert.That(session.HasSeededResults, Is.True);
        }

        [Test]
        public void ConnectionModeRoutesToCampusAndKeepsPlayerCount()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();

            router.ShowConnection(session);
            var route = router.UseConnectionMode(session, ConnectionMode.JoinLocalhostP2, 2);

            Assert.That(route, Is.EqualTo(ActivityRoute.Campus));
            Assert.That(session.ConnectionMode, Is.EqualTo(ConnectionMode.JoinLocalhostP2));
            Assert.That(session.PlayerCount, Is.EqualTo(2));
        }

        [Test]
        public void RevealRouteDoesNotBypassThreeGameGate()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();

            var route = router.ShowReveal(session);

            Assert.That(route, Is.EqualTo(ActivityRoute.Reveal));
            Assert.That(session.RevealReady, Is.False);
            Assert.That(session.GamesNeededForReveal, Is.EqualTo(3));
        }

        [Test]
        public void NonActivityRouteCannotBeShownAsActivity()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();

            Assert.Throws<ArgumentException>(() => router.ShowActivity(session, ActivityRoute.Gallery));
        }

        [Test]
        public void OptionalActivityRoutesCanBeShownAsActivity()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();

            var route = router.ShowActivity(session, ActivityRoute.MusicStudio);

            Assert.That(route, Is.EqualTo(ActivityRoute.MusicStudio));
            Assert.That(session.CurrentPhase, Is.EqualTo(SessionPhase.InRoom));
        }

        // ------------------------------------------------------------------
        // U2 generic station branch: ONE route value, station id carried
        // alongside (R7/KTD3).
        // ------------------------------------------------------------------

        [Test]
        public void PartyStationRouteCarriesTheStationId()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();
            router.BeginPlay(session);

            var route = router.ShowPartyStation(session, CareerQuestCatalog.VetClinicId);

            Assert.That(route, Is.EqualTo(ActivityRoute.PartyStation));
            Assert.That(router.CurrentRoute, Is.EqualTo(ActivityRoute.PartyStation));
            Assert.That(router.CurrentStationId, Is.EqualTo(CareerQuestCatalog.VetClinicId));
            Assert.That(session.CurrentRoute, Is.EqualTo(ActivityRoute.PartyStation));
            Assert.That(session.CurrentPhase, Is.EqualTo(SessionPhase.InRoom));
        }

        [Test]
        public void EveryPartyStationIdRoutesThroughTheSingleGenericBranch()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();
            router.BeginPlay(session);

            foreach (var stationId in CareerQuestCatalog.PartyStationIds)
            {
                router.ShowCampus(session);
                var route = router.ShowPartyStation(session, stationId);

                Assert.That(route, Is.EqualTo(ActivityRoute.PartyStation), stationId);
                Assert.That(router.CurrentStationId, Is.EqualTo(stationId));
            }
        }

        [Test]
        public void LeavingThePartyStationRouteClearsTheStationId()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();
            router.BeginPlay(session);
            router.ShowPartyStation(session, CareerQuestCatalog.GreenCityId);

            router.ShowCampus(session);

            Assert.That(router.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));
            Assert.That(router.CurrentStationId, Is.Null);
        }

        [Test]
        public void UnknownOrNonStationIdsCannotEnterTheGenericBranch()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();
            router.BeginPlay(session);

            Assert.Throws<ArgumentException>(() => router.ShowPartyStation(session, "not_a_station"));
            Assert.Throws<ArgumentException>(() => router.ShowPartyStation(session, CareerConfig.DesignBuildId));
            Assert.Throws<ArgumentException>(() => router.ShowPartyStation(session, null));
        }

        [Test]
        public void PartyStationIsNotALegacyMiniGameRoute()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();

            Assert.That(SceneFlowRouter.IsMiniGameRoute(ActivityRoute.PartyStation), Is.False,
                "The generic station branch never joins the legacy route lookups.");
            Assert.Throws<ArgumentException>(() => router.ShowActivity(session, ActivityRoute.PartyStation));
        }
    }
}
