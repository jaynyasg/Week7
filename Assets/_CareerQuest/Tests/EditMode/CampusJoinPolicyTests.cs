using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class CampusJoinPolicyTests
    {
        [Test]
        public void HubAllowsJoinWhenOnePlayerConnected()
        {
            Assert.That(CampusJoinPolicy.CanJoin(SessionPhase.Hub, 1), Is.True);
            Assert.That(CampusJoinPolicy.GetRejectionMessage(SessionPhase.Hub, 1), Is.Empty);
        }

        [Test]
        public void GalleryAllowsJoinWhenOnePlayerConnected()
        {
            Assert.That(CampusJoinPolicy.CanJoin(SessionPhase.Gallery, 1), Is.True);
            Assert.That(CampusJoinPolicy.GetRejectionMessage(SessionPhase.Gallery, 1), Is.Empty);
        }

        [Test]
        public void InRoomRejectsLateJoinWithKidFacingReason()
        {
            Assert.That(CampusJoinPolicy.CanJoin(SessionPhase.InRoom, 1), Is.False);
            Assert.That(
                CampusJoinPolicy.GetRejectionMessage(SessionPhase.InRoom, 1),
                Does.Contain("activity room"));
        }

        [Test]
        public void CeremonyRejectsLateJoinWithKidFacingReason()
        {
            Assert.That(CampusJoinPolicy.CanJoin(SessionPhase.Ceremony, 1), Is.False);
            Assert.That(
                CampusJoinPolicy.GetRejectionMessage(SessionPhase.Ceremony, 1),
                Does.Contain("finishing an activity"));
        }

        [Test]
        public void FullLobbyRejectsThirdJoinAttempt()
        {
            Assert.That(CampusJoinPolicy.CanJoin(SessionPhase.Hub, 2), Is.False);
            Assert.That(
                CampusJoinPolicy.GetRejectionMessage(SessionPhase.Hub, 2),
                Does.Contain("two players"));
        }

        [Test]
        public void SessionPhaseFromRouteMapsCampusToHub()
        {
            Assert.That(GameSession.PhaseFromRoute(ActivityRoute.Campus), Is.EqualTo(SessionPhase.Hub));
            Assert.That(GameSession.PhaseFromRoute(ActivityRoute.Connection), Is.EqualTo(SessionPhase.Hub));
        }

        [Test]
        public void SessionPhaseFromRouteMapsMiniGamesToInRoom()
        {
            Assert.That(GameSession.PhaseFromRoute(ActivityRoute.DesignBuild), Is.EqualTo(SessionPhase.InRoom));
            Assert.That(GameSession.PhaseFromRoute(ActivityRoute.HealthHero), Is.EqualTo(SessionPhase.InRoom));
            Assert.That(GameSession.PhaseFromRoute(ActivityRoute.LogicCourt), Is.EqualTo(SessionPhase.InRoom));
        }

        [Test]
        public void SessionPhaseFromRouteMapsGallery()
        {
            Assert.That(GameSession.PhaseFromRoute(ActivityRoute.Gallery), Is.EqualTo(SessionPhase.Gallery));
        }
    }
}
