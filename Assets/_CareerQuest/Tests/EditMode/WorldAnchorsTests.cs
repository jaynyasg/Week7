using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U2 anchor-data and validation seams: station-id entrances for all 10
    /// Party Pack stations, non-overlapping auto-entry circles, and readable
    /// district labels (the visual district layout itself lands in U8).
    /// </summary>
    public class WorldAnchorsTests
    {
        [TearDown]
        public void TearDown()
        {
            WorldAnchors.PrefabResourcePathOverride = null;
        }

        [Test]
        public void ActiveEntranceSetCoversAllTenPartyStations()
        {
            // Force the fallback constants so the assertion does not depend on
            // whether the authored prefab has been rebuilt yet.
            WorldAnchors.PrefabResourcePathOverride = "CareerQuest/World/DoesNotExist";

            var entrances = WorldAnchors.ActiveEntrancesWithStations;
            Assert.That(entrances.Count, Is.EqualTo(13), "7 legacy entrances + 6 station-id entrances.");

            foreach (var stationId in CareerQuestCatalog.PartyStationIds)
            {
                Assert.That(entrances.Count(entrance => entrance.ResolveStationId() == stationId), Is.EqualTo(1),
                    $"Exactly one campus entrance must resolve station id '{stationId}'.");
            }

            // The six new stations enter via the single generic branch — no
            // ActivityRoute value per station (R7/KTD3).
            foreach (var entrance in entrances.Where(entrance => entrance.IsStationEntrance))
            {
                Assert.That(entrance.Route, Is.EqualTo(ActivityRoute.PartyStation), entrance.Id);
                Assert.That(CareerQuestCatalog.IsPartyStationId(entrance.StationId), Is.True, entrance.Id);
            }
        }

        [Test]
        public void ActiveEntranceSetPassesLayoutValidation()
        {
            WorldAnchors.PrefabResourcePathOverride = "CareerQuest/World/DoesNotExist";

            var errors = WorldAnchors.ValidateActiveEntrances();

            Assert.That(errors, Is.Empty,
                $"The shipped entrance set must validate clean. Errors: {string.Join(" | ", errors)}");
        }

        [Test]
        public void StationEntrancesLieInsideTheFallbackWalkBounds()
        {
            foreach (var entrance in WorldAnchors.FallbackStationEntrancesData)
            {
                Assert.That(WorldAnchors.FallbackWalkBounds.Contains(entrance.Position), Is.True,
                    $"Station entrance '{entrance.Id}' at {entrance.Position} must be reachable inside the walk clamp.");
            }
        }

        [Test]
        public void ValidationCatchesOverlappingEntranceRadii()
        {
            var overlapping = new[]
            {
                new WorldAnchorEntrance("design_build", ActivityRoute.DesignBuild, "Design Build", new Vector2(0f, 0f), Color.red, 0.72f),
                new WorldAnchorEntrance("health_hero", ActivityRoute.HealthHero, "Health Hero", new Vector2(1f, 0f), Color.green, 0.72f)
            };

            var errors = WorldAnchors.ValidateEntrances(overlapping);

            Assert.That(errors.Any(error => error.Contains("overlapping")), Is.True,
                $"Overlapping auto-entry circles must fail validation. Errors: {string.Join(" | ", errors)}");
        }

        [Test]
        public void ValidationCatchesMissingDistrictAndReadableLabels()
        {
            var unmapped = new[]
            {
                new WorldAnchorEntrance("mystery_door", ActivityRoute.DesignBuild, "Mystery", new Vector2(0f, 0f), Color.gray, 0.5f),
                new WorldAnchorEntrance("health_hero", ActivityRoute.HealthHero, " ", new Vector2(3f, 0f), Color.green, 0.5f)
            };

            var errors = WorldAnchors.ValidateEntrances(unmapped);

            Assert.That(errors.Any(error => error.Contains("mystery_door") && error.Contains("district")), Is.True,
                "An entrance id without a district label must fail validation.");
            Assert.That(errors.Any(error => error.Contains("health_hero") && error.Contains("readable label")), Is.True,
                "An entrance without a readable label must fail validation.");
        }

        [Test]
        public void ValidationCatchesStationEntrancesWithoutACatalogStationId()
        {
            var broken = new[]
            {
                new WorldAnchorEntrance("vet_clinic", ActivityRoute.PartyStation, "mystery_station", "Vet Clinic", new Vector2(0f, 0f), Color.cyan, 0.5f)
            };

            var errors = WorldAnchors.ValidateEntrances(broken);

            Assert.That(errors.Any(error => error.Contains("vet_clinic") && error.Contains("station id")), Is.True,
                "A PartyStation entrance must resolve a known Party Pack station id.");
        }

        [Test]
        public void LegacySerializedEntrancesResolveStationIdsFromTheirIds()
        {
            // Prefab assets built before U2 serialized no StationId; converted
            // optional rooms still resolve their station identity from the id.
            var legacyOptional = new WorldAnchorEntrance("ai_lab", ActivityRoute.AiLab, "AI Lab", new Vector2(-4.45f, -1.75f), Color.blue, 0.72f);
            var coreRoom = new WorldAnchorEntrance("design_build", ActivityRoute.DesignBuild, "Design Build", new Vector2(-3f, -0.26f), Color.red, 0.72f);

            Assert.That(legacyOptional.ResolveStationId(), Is.EqualTo(CareerQuestCatalog.AiLabId));
            Assert.That(coreRoom.ResolveStationId(), Is.Null, "Core rooms are not Party Pack stations.");
        }

        [Test]
        public void EveryKnownEntranceIdHasAReadableDistrictLabel()
        {
            WorldAnchors.PrefabResourcePathOverride = "CareerQuest/World/DoesNotExist";

            foreach (var entrance in WorldAnchors.ActiveEntrancesWithStations)
            {
                var district = WorldAnchors.DistrictLabelFor(entrance.Id);
                Assert.That(string.IsNullOrWhiteSpace(district), Is.False,
                    $"Entrance '{entrance.Id}' needs a readable district label.");
            }
        }

        /// <summary>
        /// U8 readable-districts layout: the full 13-door campus groups into four
        /// spatial clusters (Quest Yard core + Tech Lane + Story Street + Care
        /// Corner). Every door sits nearer its own district than any other, so
        /// the campus reads as districts rather than one crowded row.
        /// </summary>
        [Test]
        public void TenStationLayoutGroupsIntoReadableDistrictClusters()
        {
            WorldAnchors.PrefabResourcePathOverride = "CareerQuest/World/DoesNotExist";

            var entrances = WorldAnchors.ActiveEntrancesWithStations;
            var grouping = WorldAnchors.ValidateDistrictGrouping(entrances);
            Assert.That(grouping, Is.Empty,
                $"District clusters must read cleanly. Errors: {string.Join(" | ", grouping)}");

            // Each of the four districts owns a distinct cluster centroid.
            var centers = new System.Collections.Generic.List<Vector2>();
            foreach (var district in WorldAnchors.DistrictOrder)
            {
                Assert.That(WorldAnchors.TryGetDistrictCenter(entrances, district, out var center), Is.True, district);
                centers.Add(center);
            }

            for (var first = 0; first < centers.Count; first++)
            {
                for (var second = first + 1; second < centers.Count; second++)
                {
                    Assert.That(Vector2.Distance(centers[first], centers[second]), Is.GreaterThan(1.5f),
                        $"District centroids {WorldAnchors.DistrictOrder[first]} and {WorldAnchors.DistrictOrder[second]} must be visibly apart.");
                }
            }
        }

        /// <summary>
        /// U8 controlled auto-entry radii (10-station campus layout rule): the
        /// real entry circles stay small (core 0.6, station 0.5) even though the
        /// visual building markers/signs render larger, so walking the campus
        /// never trips an unintended door.
        /// </summary>
        [Test]
        public void AutoEntryRadiiStayControlledForEveryDoor()
        {
            WorldAnchors.PrefabResourcePathOverride = "CareerQuest/World/DoesNotExist";

            foreach (var entrance in WorldAnchors.ActiveEntrancesWithStations)
            {
                Assert.That(entrance.Radius, Is.GreaterThan(0f), entrance.Id);
                Assert.That(entrance.Radius, Is.LessThanOrEqualTo(WorldAnchors.CoreEntranceRadius), entrance.Id);
                if (entrance.IsStationEntrance)
                {
                    Assert.That(entrance.Radius, Is.EqualTo(WorldAnchors.StationEntranceRadius), entrance.Id);
                }
            }
        }
    }
}
