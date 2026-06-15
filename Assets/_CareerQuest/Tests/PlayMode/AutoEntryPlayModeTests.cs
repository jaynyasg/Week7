using System.Collections;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U2 walk-into-door auto-entry over the full app stack: dwell-gated entry
    /// into the generic station branch, route-cooldown latch while mounting,
    /// return-to-campus grace, pending-door highlight, and the no-key-required
    /// instruction copy.
    /// </summary>
    public class AutoEntryPlayModeTests
    {
        [UnityTest]
        public IEnumerator WalkIntoStationEntranceAutoEntersAfterDwellAndReturnsWithGrace()
        {
            var gameObject = new GameObject("auto-entry-flow-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;
            yield return PlayModeTestBootstrap.EnterPlayCampus(app);

            var hub = Object.FindAnyObjectByType<PlayableHubController>();
            Assert.That(hub, Is.Not.Null);

            var player = hub.Player;
            player.AutoEntryAutoTick = false; // deterministic clock
            player.ResetAutoEntryClock(); // drop any grace/dwell accrued during the real-time mount frames
            var entrance = hub.Entrances.First(candidate => candidate.StationId == CareerQuestCatalog.VetClinicId);
            player.transform.position = entrance.transform.position;

            // Consume the mount grace, then dwell up to just under the window.
            player.TickAutoEntry(PlayerAvatarController.ReturnToCampusGraceSeconds);
            player.TickAutoEntry(PlayerAvatarController.AutoEntryDwellSeconds - 0.05f);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Campus),
                "Auto-entry must not fire before the dwell window completes.");

            player.TickAutoEntry(0.1f);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.PartyStation),
                "Standing in a station door for the dwell window opens it without any key press.");
            Assert.That(app.CurrentStationId, Is.EqualTo(CareerQuestCatalog.VetClinicId));
            yield return null;

            // U4: the generic station branch mounts the real station surface.
            Assert.That(GameObject.Find(PartyStationController.PanelName), Is.Not.Null,
                "The generic station branch mounts a playable station surface.");

            // Route cooldown: the latched avatar cannot double-enter while the
            // station route mounts.
            Assert.That(player == null || player.IsEntryLatched, Is.True);

            app.ShowCampus();
            yield return null;

            var remountedHub = Object.FindAnyObjectByType<PlayableHubController>();
            var returnedPlayer = remountedHub.Player;
            Assert.That(returnedPlayer, Is.Not.Null);
            returnedPlayer.AutoEntryAutoTick = false;
            returnedPlayer.ResetAutoEntryClock(); // deterministic: don't inherit the remount frame's grace accrual
            // The first hub's entrances were destroyed on route change; find the
            // same station's door on the remounted hub.
            var returnedEntrance = remountedHub.Entrances.First(
                candidate => candidate.StationId == CareerQuestCatalog.VetClinicId);
            returnedPlayer.transform.position = returnedEntrance.transform.position;

            // Return-to-campus grace: standing straight back in a door does not
            // immediately re-fire.
            returnedPlayer.TickAutoEntry(PlayerAvatarController.ReturnToCampusGraceSeconds - 0.1f);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Campus),
                "The return grace must absorb door contact right after coming back.");

            // After grace plus dwell, auto-entry works again (replay path).
            returnedPlayer.TickAutoEntry(0.1f + PlayerAvatarController.AutoEntryDwellSeconds + 0.05f);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.PartyStation),
                "After the grace window, walk-into-door entry re-arms.");

            Object.Destroy(gameObject);
            Object.Destroy(remountedHub.gameObject);
        }

        [UnityTest]
        public IEnumerator EdgeBrushDoesNotEnterAndPendingHighlightTracksTheDoor()
        {
            var gameObject = new GameObject("auto-entry-brush-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;
            yield return PlayModeTestBootstrap.EnterPlayCampus(app);

            var hub = Object.FindAnyObjectByType<PlayableHubController>();
            var player = hub.Player;
            player.AutoEntryAutoTick = false;
            player.TickAutoEntry(PlayerAvatarController.ReturnToCampusGraceSeconds);

            var entrance = hub.Entrances.First(candidate => candidate.StationId == CareerQuestCatalog.GreenCityId);
            var sign = entrance.GetComponent<DoorSign>();
            Assert.That(sign, Is.Not.Null, "Hub entrances carry DoorSigns (the highlight surface).");

            // One-frame edge brush: highlight appears, but no entry fires.
            player.transform.position = entrance.transform.position;
            player.TickAutoEntry(1f / 60f);
            Assert.That(player.PendingEntrance, Is.EqualTo(entrance), "The nearby highlight names the pending station.");
            Assert.That(sign.IsPulsing, Is.True, "The pending door pulses before the dwell completes.");
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));

            // Leaving the radius clears the highlight and resets the dwell.
            player.transform.position = WorldAnchors.AssetPlayerSpawn;
            player.TickAutoEntry(1f / 60f);
            Assert.That(player.PendingEntrance, Is.Null);
            Assert.That(sign.IsPulsing, Is.False, "Leaving the radius releases the highlight.");
            Assert.That(player.DwellElapsedSeconds, Is.EqualTo(0f));
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Campus),
                "A one-frame brush never opens a station.");

            Object.Destroy(gameObject);
            Object.Destroy(hub.gameObject);
        }

        [UnityTest]
        public IEnumerator ClickToEnterStillWorksOnStationEntrances()
        {
            var gameObject = new GameObject("auto-entry-click-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;
            yield return PlayModeTestBootstrap.EnterPlayCampus(app);

            var hub = Object.FindAnyObjectByType<PlayableHubController>();
            var player = hub.Player;
            player.AutoEntryAutoTick = false;
            var entrance = hub.Entrances.First(candidate => candidate.StationId == CareerQuestCatalog.NewsroomId);

            // Click convenience: a click on the entrance enters immediately —
            // no dwell required (the pointer-over guard in Update keeps UI and
            // drag targets in front of this path).
            Assert.That(player.TryEnterAt(entrance.transform.position), Is.True);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.PartyStation));
            Assert.That(app.CurrentStationId, Is.EqualTo(CareerQuestCatalog.NewsroomId));

            // Latched while the route mounts: a second click cannot double-enter.
            Assert.That(player == null || !player.TryEnterAt(entrance.transform.position), Is.True);
            yield return null;

            Object.Destroy(gameObject);
            Object.Destroy(hub.gameObject);
        }

        // ------------------------------------------------------------------
        // Campus copy: movement + walk-into-door entry, never "press E".
        // ------------------------------------------------------------------

        [Test]
        public void CampusInstructionCopySaysWalkIntoDoorsWithoutKeyPhrasing()
        {
            var session = new GameSession();
            var campusLine = InstructionStrip.ResolveMessage(session);

            Assert.That(campusLine, Does.Contain("Walk into"), "Campus copy teaches walk-into-door entry.");
            Assert.That(campusLine, Does.Not.Contain("press E"));
            Assert.That(campusLine, Does.Not.Contain("Enter doors"));
            Assert.That(campusLine, Does.Not.Contain("press Enter"));
            Assert.That(campusLine, Does.Not.Contain("Space"));
            Assert.That(campusLine.Length, Is.LessThanOrEqualTo(PartyStationValidator.MaxGuideLineLength),
                "Campus copy stays early-reader short.");
            Assert.That(PartyStationValidator.CheckCopySafety(campusLine, "campus strip"), Is.Empty,
                "Campus copy passes the shared copy-safety scan.");
        }

        [Test]
        public void StationRouteInstructionCopyIsStationAwareAndSafe()
        {
            var session = new GameSession();
            session.SetRoute(ActivityRoute.PartyStation);

            var stationLine = InstructionStrip.ResolveMessage(session, CareerQuestCatalog.VetClinicId);
            Assert.That(stationLine, Does.Contain("Vet Clinic"), "Station copy names the station's building.");
            Assert.That(stationLine.Length, Is.LessThanOrEqualTo(PartyStationValidator.MaxGuideLineLength));
            Assert.That(PartyStationValidator.CheckCopySafety(stationLine, "station strip"), Is.Empty);

            var fallbackLine = InstructionStrip.ResolveMessage(session, null);
            Assert.That(string.IsNullOrWhiteSpace(fallbackLine), Is.False,
                "An unknown station id still renders a safe generic line.");
            Assert.That(PartyStationValidator.CheckCopySafety(fallbackLine, "station strip fallback"), Is.Empty);
        }
    }
}
