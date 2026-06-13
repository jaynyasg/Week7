using System.Collections;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    public class HubNavigationFlowTests
    {
        [UnityTest]
        public IEnumerator CampusCreatesPlayableHubAndRoutesEntrances()
        {
            var gameObject = new GameObject("hub-flow-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;
            yield return PlayModeTestBootstrap.EnterPlayCampus(app);

            var hub = Object.FindAnyObjectByType<PlayableHubController>();
            Assert.That(hub, Is.Not.Null);
            Assert.That(hub.IsVisible, Is.True);
            Assert.That(hub.Player, Is.Not.Null);

            // Anchors-only exposure: entrance placement comes from WorldAnchors
            // (prefab instance/asset, or the hard fallback constants), plus the
            // U2 station-id entrances for Party Pack stations the authored set
            // does not cover yet (7 legacy + 6 station doors).
            var anchorEntrances = WorldAnchors.ActiveEntrancesWithStations;
            Assert.That(anchorEntrances.Count, Is.EqualTo(13));
            Assert.That(hub.Entrances.Count, Is.EqualTo(anchorEntrances.Count));
            for (var i = 0; i < anchorEntrances.Count; i++)
            {
                Assert.That(hub.Entrances[i].Route, Is.EqualTo(anchorEntrances[i].Route));
                Assert.That((Vector2)hub.Entrances[i].transform.position, Is.EqualTo(anchorEntrances[i].Position),
                    $"Entrance '{anchorEntrances[i].Id}' must sit at its WorldAnchors position.");
            }

            // Every Party Pack station id has exactly one campus door.
            foreach (var stationId in CareerQuestCatalog.PartyStationIds)
            {
                Assert.That(hub.Entrances.Count(entrance => entrance.StationId == stationId), Is.EqualTo(1),
                    $"Station '{stationId}' must have exactly one campus entrance.");
            }

            Assert.That(hub.TryEnter(ActivityRoute.HealthHero), Is.True);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.HealthHero));

            app.ShowCampus();
            yield return null;

            // U5: legacy optional doors land on the converted station surface
            // through the generic station branch (the door itself is unchanged).
            Assert.That(hub.TryEnter(ActivityRoute.MusicStudio), Is.True);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.PartyStation));
            Assert.That(app.CurrentStationId, Is.EqualTo(CareerQuestCatalog.MusicStudioId));

            // U2 generic station branch: a station-id door routes by station id
            // into ActivityRoute.PartyStation — no enum value per station.
            app.ShowCampus();
            yield return null;

            Assert.That(hub.TryEnterStation(CareerQuestCatalog.VetClinicId), Is.True);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.PartyStation));
            Assert.That(app.CurrentStationId, Is.EqualTo(CareerQuestCatalog.VetClinicId));

            // Returning to campus clears the station id and re-arms the doors.
            app.ShowCampus();
            yield return null;

            Assert.That(app.CurrentStationId, Is.Null);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));

            Object.Destroy(gameObject);
            Object.Destroy(hub.gameObject);
        }
    }
}
