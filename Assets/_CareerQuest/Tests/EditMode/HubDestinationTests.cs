using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    public class HubDestinationTests
    {
        [Test]
        public void EntranceDetectsNearbyWorldPositions()
        {
            var gameObject = new GameObject("entrance-test");
            gameObject.transform.position = new Vector3(2f, -1f, 0f);

            var entrance = gameObject.AddComponent<BuildingEntrance>();
            entrance.Configure(ActivityRoute.DesignBuild, "Design", 0.5f, null);

            Assert.That(entrance.Contains(new Vector2(2.25f, -1f)), Is.True);
            Assert.That(entrance.Contains(new Vector2(3f, -1f)), Is.False);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void PlayerCanEnterDestinationAtClickedPosition()
        {
            var enteredRoute = ActivityRoute.Entry;
            var entranceObject = new GameObject("logic-entrance-test");
            var entrance = entranceObject.AddComponent<BuildingEntrance>();
            entrance.Configure(ActivityRoute.LogicCourt, "Logic", 0.75f, entered => enteredRoute = entered.Route);

            var playerObject = new GameObject("player-test", typeof(SpriteRenderer), typeof(AvatarRuntimeView), typeof(PlayerAvatarController));
            var player = playerObject.GetComponent<PlayerAvatarController>();
            player.Configure(new GameSession(), new[] { entrance }, entered => enteredRoute = entered.Route);

            Assert.That(player.TryEnterAt(Vector2.zero), Is.True);
            Assert.That(enteredRoute, Is.EqualTo(ActivityRoute.LogicCourt));

            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(entranceObject);
        }

        [Test]
        public void StationEntranceCarriesStationIdIntoTheGenericBranch()
        {
            BuildingEntrance entered = null;
            var entranceObject = new GameObject("vet-entrance-test");
            var entrance = entranceObject.AddComponent<BuildingEntrance>();
            entrance.Configure(ActivityRoute.PartyStation, CareerQuestCatalog.VetClinicId, "Vet Clinic", 0.5f, null);

            var playerObject = new GameObject("station-player-test", typeof(SpriteRenderer), typeof(AvatarRuntimeView), typeof(PlayerAvatarController));
            var player = playerObject.GetComponent<PlayerAvatarController>();
            player.Configure(new GameSession(), new[] { entrance }, destination => entered = destination);

            Assert.That(entrance.IsStationEntrance, Is.True);
            Assert.That(player.TryEnterAt(Vector2.zero), Is.True);
            Assert.That(entered, Is.Not.Null);
            Assert.That(entered.StationId, Is.EqualTo(CareerQuestCatalog.VetClinicId));
            Assert.That(entered.Route, Is.EqualTo(ActivityRoute.PartyStation));

            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(entranceObject);
        }

        // ------------------------------------------------------------------
        // U2 walk-into-door auto-entry: dwell, brush, reset, latch, grace, and
        // highlight all run on the deterministic TickAutoEntry clock.
        // ------------------------------------------------------------------

        [Test]
        public void AutoEntryFiresOnlyAfterTheDwellWindow()
        {
            var (player, entrance, counter) = CreateAutoEntryRig();

            // Consume the configure-time grace, then dwell up to just below the threshold.
            player.TickAutoEntry(PlayerAvatarController.ReturnToCampusGraceSeconds);
            player.TickAutoEntry(PlayerAvatarController.AutoEntryDwellSeconds - 0.05f);
            Assert.That(counter.Count, Is.EqualTo(0), "Auto-entry must not fire before the dwell window completes.");
            Assert.That(player.PendingEntrance, Is.EqualTo(entrance), "The pending door highlights before entry.");

            player.TickAutoEntry(0.1f);
            Assert.That(counter.Count, Is.EqualTo(1), "Auto-entry fires once the dwell window completes.");
            Assert.That(counter.LastStationId, Is.EqualTo(CareerQuestCatalog.VetClinicId));

            DestroyAutoEntryRig(player, entrance);
        }

        [Test]
        public void OneFrameEdgeBrushDoesNotTriggerAutoEntry()
        {
            var (player, entrance, counter) = CreateAutoEntryRig();
            player.TickAutoEntry(PlayerAvatarController.ReturnToCampusGraceSeconds);

            // One frame inside, then step out: no entry, dwell resets, highlight clears.
            player.TickAutoEntry(1f / 60f);
            Assert.That(player.PendingEntrance, Is.EqualTo(entrance));

            player.transform.position = new Vector3(3f, 0f, 0f);
            player.TickAutoEntry(1f / 60f);
            Assert.That(player.PendingEntrance, Is.Null, "Leaving the radius clears the pending highlight.");
            Assert.That(player.DwellElapsedSeconds, Is.EqualTo(0f), "Leaving the radius resets the dwell timer.");
            Assert.That(counter.Count, Is.EqualTo(0));

            // Coming back starts the dwell from zero — the brush left no credit.
            player.transform.position = Vector3.zero;
            player.TickAutoEntry(PlayerAvatarController.AutoEntryDwellSeconds - 0.05f);
            Assert.That(counter.Count, Is.EqualTo(0), "Dwell restarts after an exit; a brush leaves no credit.");

            DestroyAutoEntryRig(player, entrance);
        }

        [Test]
        public void EntryLatchPreventsDoubleEntryWhileTheRouteMounts()
        {
            var (player, entrance, counter) = CreateAutoEntryRig();
            player.TickAutoEntry(PlayerAvatarController.ReturnToCampusGraceSeconds);
            player.TickAutoEntry(PlayerAvatarController.AutoEntryDwellSeconds + 0.1f);
            Assert.That(counter.Count, Is.EqualTo(1));
            Assert.That(player.IsEntryLatched, Is.True);

            // Still standing in the door while the next route mounts: nothing re-fires.
            player.TickAutoEntry(2f);
            Assert.That(player.TryEnterAt(Vector2.zero), Is.False, "Click entry is latched too while mounting.");
            Assert.That(player.TryEnterNearest(), Is.False);
            Assert.That(counter.Count, Is.EqualTo(1), "The route cooldown latch allows exactly one entry per hub mount.");

            DestroyAutoEntryRig(player, entrance);
        }

        [Test]
        public void ReturnToCampusGraceDelaysAutoEntryAfterRemount()
        {
            var (player, entrance, counter) = CreateAutoEntryRig();

            // Simulate a return to campus: the hub reconfigures the avatar.
            player.Configure(new GameSession(), new[] { entrance }, destination => counter.Record(destination));

            player.TickAutoEntry(PlayerAvatarController.ReturnToCampusGraceSeconds - 0.1f);
            player.TickAutoEntry(0.05f);
            Assert.That(counter.Count, Is.EqualTo(0), "No dwell accrues inside the return-to-campus grace window.");

            player.TickAutoEntry(PlayerAvatarController.AutoEntryDwellSeconds + 0.1f);
            Assert.That(counter.Count, Is.EqualTo(1), "After the grace window, dwell-based entry works again.");

            DestroyAutoEntryRig(player, entrance);
        }

        [Test]
        public void PendingEntranceHighlightPulsesTheDoorSign()
        {
            var (player, entrance, _) = CreateAutoEntryRig();
            var sign = entrance.gameObject.AddComponent<DoorSign>();

            player.TickAutoEntry(1f / 60f);
            Assert.That(sign.IsPulsing, Is.True, "Standing in a door pulses its sign so kids see which station opens.");

            player.transform.position = new Vector3(3f, 0f, 0f);
            player.TickAutoEntry(1f / 60f);
            Assert.That(sign.IsPulsing, Is.False, "Leaving the radius releases the highlight pulse.");

            DestroyAutoEntryRig(player, entrance);
        }

        private static (PlayerAvatarController player, BuildingEntrance entrance, EntryCounter counter) CreateAutoEntryRig()
        {
            var counter = new EntryCounter();
            var entranceObject = new GameObject("auto-entry-entrance");
            var entrance = entranceObject.AddComponent<BuildingEntrance>();
            entrance.Configure(ActivityRoute.PartyStation, CareerQuestCatalog.VetClinicId, "Vet Clinic", 0.5f, null);

            var playerObject = new GameObject("auto-entry-player", typeof(SpriteRenderer), typeof(AvatarRuntimeView), typeof(PlayerAvatarController));
            var player = playerObject.GetComponent<PlayerAvatarController>();
            player.AutoEntryAutoTick = false;
            player.Configure(new GameSession(), new[] { entrance }, destination => counter.Record(destination));
            player.transform.position = Vector3.zero;

            return (player, entrance, counter);
        }

        private static void DestroyAutoEntryRig(PlayerAvatarController player, BuildingEntrance entrance)
        {
            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(entrance.gameObject);
        }

        private sealed class EntryCounter
        {
            public int Count { get; private set; }
            public string LastStationId { get; private set; }

            public void Record(BuildingEntrance entrance)
            {
                Count++;
                LastStationId = entrance.StationId;
            }
        }
    }
}
