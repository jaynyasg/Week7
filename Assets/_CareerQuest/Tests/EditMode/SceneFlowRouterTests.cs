using System;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class SceneFlowRouterTests
    {
        [Test]
        public void BeginPlayRoutesToConnectionWithoutSeededResults()
        {
            var session = new GameSession();
            var router = new SceneFlowRouter();

            var route = router.BeginPlay(session);

            Assert.That(route, Is.EqualTo(ActivityRoute.Connection));
            Assert.That(router.CurrentRoute, Is.EqualTo(ActivityRoute.Connection));
            Assert.That(session.CurrentRoute, Is.EqualTo(ActivityRoute.Connection));
            Assert.That(session.Mode, Is.EqualTo(AppMode.Play));
            Assert.That(session.HasSeededResults, Is.False);
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

            router.BeginPlay(session);
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
    }
}
