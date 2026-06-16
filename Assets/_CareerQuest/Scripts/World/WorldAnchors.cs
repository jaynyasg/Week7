using System;
using System.Collections.Generic;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// One entrance exported by the authored campus prefab: stable id, route,
    /// kid-facing label, world position, accent color, and interaction radius.
    /// </summary>
    [Serializable]
    public sealed class WorldAnchorEntrance
    {
        public string Id;
        public ActivityRoute Route;
        public string Label;
        public Vector2 Position;
        public Color AccentColor;
        public float Radius;

        /// <summary>
        /// U2 station-id routing: set for entrances that enter through the
        /// generic <see cref="ActivityRoute.PartyStation"/> branch. May be
        /// empty on legacy serialized data — use <see cref="ResolveStationId"/>.
        /// </summary>
        public string StationId;

        public WorldAnchorEntrance()
        {
        }

        public WorldAnchorEntrance(string id, ActivityRoute route, string label, Vector2 position, Color accentColor, float radius)
            : this(id, route, null, label, position, accentColor, radius)
        {
        }

        public WorldAnchorEntrance(string id, ActivityRoute route, string stationId, string label, Vector2 position, Color accentColor, float radius)
        {
            Id = id;
            Route = route;
            StationId = stationId;
            Label = label;
            Position = position;
            AccentColor = accentColor;
            Radius = radius;
        }

        /// <summary>True when this entrance enters via the generic station branch.</summary>
        public bool IsStationEntrance => Route == ActivityRoute.PartyStation;

        /// <summary>
        /// Station identity with a legacy-data fallback: prefab assets built
        /// before U2 serialized no StationId, but their entrance Ids already
        /// equal the catalog/station ids (e.g. "ai_lab").
        /// </summary>
        public string ResolveStationId()
        {
            if (!string.IsNullOrWhiteSpace(StationId))
            {
                return StationId;
            }

            return CareerQuestCatalog.IsPartyStationId(Id) ? Id : null;
        }
    }

    /// <summary>
    /// The single coordinate truth for the campus hub. Lives on the CampusHub
    /// prefab root and exports entrances, the walk-clamp rect, and spawn points.
    ///
    /// Three consumers read it:
    /// - PlayableHubController (entrance placement and routing)
    /// - PlayerAvatarController (local walk clamp)
    /// - PlayerAvatarNetwork.ClampCampus (server clamp — MUST read the prefab
    ///   ASSET via the static accessors below, never a live instance, because
    ///   route navigation is per-client: the host can be inside a room with the
    ///   hub world cleared while a client walks the campus and streams move RPCs)
    ///
    /// If the prefab asset is missing, the static accessors fall back to the
    /// hard constants that mirror the legacy literals, so gameplay never breaks.
    /// </summary>
    public class WorldAnchors : MonoBehaviour
    {
        /// <summary>Resources path of the runtime copy of the CampusHub prefab.</summary>
        public const string PrefabResourcePath = "CareerQuest/World/CampusHub";

        /// <summary>Test seam: override the Resources path (set null to restore). Clears the asset cache.</summary>
        public static string PrefabResourcePathOverride
        {
            get => _prefabResourcePathOverride;
            set
            {
                _prefabResourcePathOverride = value;
                ResetAssetCache();
            }
        }

        public static string ActivePrefabResourcePath => _prefabResourcePathOverride ?? PrefabResourcePath;

        /// <summary>
        /// U8 ten-station campus walk bounds. Widened from the U2 rect so the
        /// full 13-door district map (3 core + 10 stations) lays out as four
        /// readable clusters with non-overlapping auto-entry circles instead of
        /// one crowded row. Player/guide spawn on the central plaza between the
        /// Quest Yard and the station rows.
        /// </summary>
        public static readonly Rect FallbackWalkBounds = new(-6.2f, -3.2f, 12.4f, 5f);

        public static readonly Vector2 FallbackPlayerSpawn = new(0f, -1.1f);
        public static readonly Vector2 FallbackGuideSpawn = new(1.5f, -1.1f);

        private static string _prefabResourcePathOverride;
        private static WorldAnchors _assetAnchors;
        private static bool _assetSearched;

        /// <summary>
        /// U8 core auto-entry radius (Quest Yard). Slightly larger than the
        /// station radius so the three flagship rooms read as the front-and-
        /// center quad; the visual building markers/signs render larger still.
        /// </summary>
        public const float CoreEntranceRadius = 0.6f;

        /// <summary>
        /// U8 readable district layout — the three core Quest Yard doors. The
        /// six converted-optional / first-six station rows sit below them via
        /// <see cref="FallbackStationEntrancesData"/>; together the authored
        /// prefab and these fallbacks form one clustered 13-door campus.
        /// </summary>
        private static readonly WorldAnchorEntrance[] FallbackEntrancesData =
        {
            // Quest Yard (core quad) — top and center, spread so the three main
            // buildings stop overlapping each other.
            new("design_build", ActivityRoute.DesignBuild, "Design Build", new Vector2(-1.7f, 0.7f), new Color(0.94f, 0.34f, 0.28f), CoreEntranceRadius),
            new("health_hero", ActivityRoute.HealthHero, "Health Hero", new Vector2(0f, 1.05f), new Color(0.36f, 0.78f, 0.6f), CoreEntranceRadius),
            new("logic_court", ActivityRoute.LogicCourt, "Logic Court", new Vector2(1.7f, 0.7f), new Color(0.96f, 0.62f, 0.18f), CoreEntranceRadius),
            // Tech Lane (left column) — converted optional rooms route by their
            // legacy ActivityRoute; spaceport joins via the station fallback.
            // Pulled in from the camera edge so the buildings stop clipping.
            new("ai_lab", ActivityRoute.AiLab, "AI Lab", new Vector2(-4.5f, 0.5f), new Color(0.28f, 0.66f, 0.94f), StationEntranceRadius),
            new("robotics_garage", ActivityRoute.RoboticsGarage, "Robotics", new Vector2(-4.6f, -0.9f), new Color(0.13f, 0.55f, 0.58f), StationEntranceRadius),
            // Story Street (right column) — music routes by legacy route;
            // game studio + newsroom join via the station fallback.
            new("music_studio", ActivityRoute.MusicStudio, "Music Studio", new Vector2(3.4f, -0.45f), new Color(0.62f, 0.52f, 0.86f), StationEntranceRadius),
            // Care Corner (bottom row) — kitchen routes by legacy route; vet,
            // weather, and green city join via the station fallback.
            new("community_kitchen", ActivityRoute.CommunityKitchen, "Kitchen", new Vector2(-2.3f, -2.2f), new Color(0.55f, 0.82f, 0.5f), StationEntranceRadius)
        };

        /// <summary>
        /// U2 auto-entry radius for the station-id entrances: smaller than the
        /// core radius so the four district clusters fit the walk rect with
        /// non-overlapping zones. The visual marker/sign may render larger than
        /// the actual entry circle (10-station campus layout rule).
        /// </summary>
        public const float StationEntranceRadius = 0.5f;

        /// <summary>
        /// U2 station-id entrances for the six Party Pack stations without a
        /// legacy route. They enter via the generic PartyStation branch and are
        /// appended to any authored entrance set that does not include them
        /// (see <see cref="ActiveEntrancesWithStations"/>). U8 placed them into
        /// their districts (Tech Lane / Story Street / Care Corner) so the full
        /// 13-door campus reads as four clusters; every circle is non-overlapping
        /// and inside the fallback walk rect.
        /// </summary>
        public static readonly WorldAnchorEntrance[] FallbackStationEntrancesData =
        {
            // Tech Lane (left column, continues ai_lab + robotics).
            new("spaceport", ActivityRoute.PartyStation, CareerQuestCatalog.SpaceportId, "Spaceport", new Vector2(-3.4f, -0.45f), new Color(0.08f, 0.26f, 0.55f), StationEntranceRadius),
            // Story Street (right column, continues music_studio). Pulled in from
            // the camera edge so game studio + newsroom stop clipping.
            new("game_studio", ActivityRoute.PartyStation, CareerQuestCatalog.GameStudioId, "Game Studio", new Vector2(4.6f, -0.9f), new Color(0.62f, 0.52f, 0.86f), StationEntranceRadius),
            new("newsroom", ActivityRoute.PartyStation, CareerQuestCatalog.NewsroomId, "Newsroom", new Vector2(4.5f, 0.5f), new Color(0.96f, 0.62f, 0.18f), StationEntranceRadius),
            // Care Corner (bottom row, continues community_kitchen) — spread wide
            // so the four bottom doors read as separate buildings.
            new("vet_clinic", ActivityRoute.PartyStation, CareerQuestCatalog.VetClinicId, "Vet Clinic", new Vector2(-0.9f, -2.55f), new Color(0.36f, 0.78f, 0.6f), StationEntranceRadius),
            new("weather_lab", ActivityRoute.PartyStation, CareerQuestCatalog.WeatherLabId, "Weather Lab", new Vector2(0.9f, -2.55f), new Color(0.28f, 0.66f, 0.94f), StationEntranceRadius),
            new("green_city", ActivityRoute.PartyStation, CareerQuestCatalog.GreenCityId, "Green City", new Vector2(2.3f, -2.2f), new Color(0.25f, 0.64f, 0.3f), StationEntranceRadius)
        };

        /// <summary>
        /// Readable district label per entrance id (U2 anchor-data seam; U8 owns
        /// the visual district layout that places each cluster spatially).
        /// Validation fails on any entrance id without a district label.
        /// </summary>
        private static readonly Dictionary<string, string> DistrictLabels = new()
        {
            ["design_build"] = QuestYardDistrict,
            ["health_hero"] = QuestYardDistrict,
            ["logic_court"] = QuestYardDistrict,
            ["ai_lab"] = TechLaneDistrict,
            ["robotics_garage"] = TechLaneDistrict,
            ["spaceport"] = TechLaneDistrict,
            ["music_studio"] = StoryStreetDistrict,
            ["game_studio"] = StoryStreetDistrict,
            ["newsroom"] = StoryStreetDistrict,
            ["community_kitchen"] = CareCornerDistrict,
            ["vet_clinic"] = CareCornerDistrict,
            ["weather_lab"] = CareCornerDistrict,
            ["green_city"] = CareCornerDistrict
        };

        public const string QuestYardDistrict = "Quest Yard";
        public const string TechLaneDistrict = "Tech Lane";
        public const string StoryStreetDistrict = "Story Street";
        public const string CareCornerDistrict = "Care Corner";

        /// <summary>The four readable campus districts (U8 layout), in front-to-back order.</summary>
        public static readonly string[] DistrictOrder =
        {
            QuestYardDistrict,
            TechLaneDistrict,
            StoryStreetDistrict,
            CareCornerDistrict
        };

        [SerializeField] private List<WorldAnchorEntrance> entrances = new();
        [SerializeField] private Rect walkBounds = FallbackWalkBounds;
        [SerializeField] private Vector2 playerSpawn = FallbackPlayerSpawn;
        [SerializeField] private Vector2 guideSpawn = FallbackGuideSpawn;

        public IReadOnlyList<WorldAnchorEntrance> Entrances => entrances;
        public Rect WalkBounds => walkBounds;
        public Vector2 PlayerSpawn => playerSpawn;
        public Vector2 GuideSpawn => guideSpawn;

        /// <summary>Editor-builder seam: populates the serialized data before SaveAsPrefabAsset.</summary>
        public void SetData(IEnumerable<WorldAnchorEntrance> entranceData, Rect bounds, Vector2 player, Vector2 guide)
        {
            entrances = new List<WorldAnchorEntrance>(entranceData);
            walkBounds = bounds;
            playerSpawn = player;
            guideSpawn = guide;
        }

        /// <summary>
        /// U8 single source of truth for the full authored 13-door campus: the
        /// three Quest Yard core doors plus the four converted legacy-route
        /// stations, then the six station-id doors. The hub prefab builder
        /// serializes EXACTLY this set so the live prefab and the fallback agree
        /// (same ids, positions, radii) and <see cref="ValidateEntrances"/>
        /// passes identically whether or not the prefab has been rebuilt.
        /// </summary>
        public static IReadOnlyList<WorldAnchorEntrance> FallbackEntrancesWithStations
        {
            get
            {
                var combined = new List<WorldAnchorEntrance>(FallbackEntrancesData);
                combined.AddRange(FallbackStationEntrancesData);
                return combined;
            }
        }

        /// <summary>
        /// Loads the WorldAnchors component from the prefab ASSET (never a live
        /// instance). Returns null when the prefab is missing — callers use the
        /// Fallback* constants in that case.
        /// </summary>
        public static WorldAnchors LoadAssetAnchors()
        {
            if (_assetSearched)
            {
                return _assetAnchors;
            }

            _assetSearched = true;
            var prefab = Resources.Load<GameObject>(ActivePrefabResourcePath);
            _assetAnchors = prefab != null ? prefab.GetComponent<WorldAnchors>() : null;
            return _assetAnchors;
        }

        /// <summary>Clears the cached asset lookup (tests, post-rebuild).</summary>
        public static void ResetAssetCache()
        {
            _assetSearched = false;
            _assetAnchors = null;
        }

        /// <summary>Asset-sourced walk bounds with hard fallback — the server clamp source.</summary>
        public static Rect AssetWalkBounds
        {
            get
            {
                var asset = LoadAssetAnchors();
                return asset != null ? asset.walkBounds : FallbackWalkBounds;
            }
        }

        /// <summary>Asset-sourced entrance set with hard fallback.</summary>
        public static IReadOnlyList<WorldAnchorEntrance> AssetEntrances
        {
            get
            {
                var asset = LoadAssetAnchors();
                return asset != null && asset.entrances.Count > 0 ? asset.entrances : FallbackEntrancesData;
            }
        }

        public static Vector2 AssetPlayerSpawn
        {
            get
            {
                var asset = LoadAssetAnchors();
                return asset != null ? asset.playerSpawn : FallbackPlayerSpawn;
            }
        }

        public static Vector2 AssetGuideSpawn
        {
            get
            {
                var asset = LoadAssetAnchors();
                return asset != null ? asset.guideSpawn : FallbackGuideSpawn;
            }
        }

        /// <summary>
        /// Prefers a live mounted instance (positions can be authored per scene
        /// in the future), then the asset, then the hard fallback. Used by the
        /// hub layer; the server clamp must keep using the Asset* accessors.
        /// </summary>
        public static WorldAnchors ResolveActive()
        {
            var live = FindFirstObjectByType<WorldAnchors>();
            return live != null ? live : LoadAssetAnchors();
        }

        public static IReadOnlyList<WorldAnchorEntrance> ActiveEntrances
        {
            get
            {
                var anchors = ResolveActive();
                return anchors != null && anchors.entrances.Count > 0 ? anchors.entrances : FallbackEntrancesData;
            }
        }

        public static Rect ActiveWalkBounds
        {
            get
            {
                var anchors = ResolveActive();
                return anchors != null ? anchors.walkBounds : FallbackWalkBounds;
            }
        }

        /// <summary>
        /// U2 hub entrance truth: the active anchored entrances plus the
        /// station-id fallback entrances for any Party Pack station the
        /// authored set does not cover yet (the pre-U8 prefab only authored the
        /// legacy seven). Authored station entrances always win over fallbacks.
        /// </summary>
        public static IReadOnlyList<WorldAnchorEntrance> ActiveEntrancesWithStations
        {
            get
            {
                var combined = new List<WorldAnchorEntrance>(ActiveEntrances);
                foreach (var station in FallbackStationEntrancesData)
                {
                    if (!combined.Exists(entrance => entrance.ResolveStationId() == station.StationId))
                    {
                        combined.Add(station);
                    }
                }

                return combined;
            }
        }

        /// <summary>Readable district label for an entrance id; null when unmapped.</summary>
        public static string DistrictLabelFor(string entranceId)
        {
            return entranceId != null && DistrictLabels.TryGetValue(entranceId, out var label) ? label : null;
        }

        /// <summary>
        /// U8 district-cluster centroid: the mean position of every entrance in
        /// <paramref name="entranceSet"/> tagged with <paramref name="district"/>.
        /// Returns false when the district has no entrances in the set.
        /// </summary>
        public static bool TryGetDistrictCenter(
            IReadOnlyList<WorldAnchorEntrance> entranceSet,
            string district,
            out Vector2 center)
        {
            center = Vector2.zero;
            if (entranceSet == null || string.IsNullOrEmpty(district))
            {
                return false;
            }

            var sum = Vector2.zero;
            var count = 0;
            foreach (var entrance in entranceSet)
            {
                if (DistrictLabelFor(entrance.Id) == district)
                {
                    sum += entrance.Position;
                    count++;
                }
            }

            if (count == 0)
            {
                return false;
            }

            center = sum / count;
            return true;
        }

        /// <summary>
        /// U8 readability gate (10-station campus layout rule): every entrance
        /// sits closer to its own district's other doors than to the nearest
        /// door of any OTHER district, so the four clusters read as districts
        /// rather than one crowded row. Returns the offending pairs (empty when
        /// the layout groups cleanly). Singleton districts are skipped (no
        /// within-district pair to compare against).
        /// </summary>
        public static IReadOnlyList<string> ValidateDistrictGrouping(IReadOnlyList<WorldAnchorEntrance> entranceSet)
        {
            var errors = new List<string>();
            if (entranceSet == null || entranceSet.Count == 0)
            {
                errors.Add("Entrance set is empty.");
                return errors;
            }

            foreach (var entrance in entranceSet)
            {
                var district = DistrictLabelFor(entrance.Id);
                if (string.IsNullOrEmpty(district))
                {
                    continue; // missing-district is reported by ValidateEntrances
                }

                var nearestSame = float.MaxValue;
                var nearestOther = float.MaxValue;
                foreach (var other in entranceSet)
                {
                    if (ReferenceEquals(other, entrance) || other.Id == entrance.Id)
                    {
                        continue;
                    }

                    var distance = Vector2.Distance(entrance.Position, other.Position);
                    if (DistrictLabelFor(other.Id) == district)
                    {
                        nearestSame = Mathf.Min(nearestSame, distance);
                    }
                    else
                    {
                        nearestOther = Mathf.Min(nearestOther, distance);
                    }
                }

                // Only enforce for districts that actually have a sibling door.
                if (nearestSame < float.MaxValue && nearestSame >= nearestOther)
                {
                    errors.Add(
                        $"Entrance '{entrance.Id}' ({district}) is closer to another district " +
                        $"(d={nearestOther:F2}) than to its own ({nearestSame:F2}) — districts must read as clusters.");
                }
            }

            return errors;
        }

        /// <summary>Validates the active hub entrance set (fallback-aware).</summary>
        public static IReadOnlyList<string> ValidateActiveEntrances()
        {
            return ValidateEntrances(ActiveEntrancesWithStations);
        }

        /// <summary>
        /// U2 layout safety gate: duplicate ids, unreadable labels, missing
        /// district labels, broken radii, station entrances without a resolvable
        /// catalog station id, and overlapping auto-entry circles all fail
        /// loudly here instead of shipping a confusing campus.
        /// </summary>
        public static IReadOnlyList<string> ValidateEntrances(IReadOnlyList<WorldAnchorEntrance> entranceSet)
        {
            var errors = new List<string>();
            if (entranceSet == null || entranceSet.Count == 0)
            {
                errors.Add("Entrance set is empty.");
                return errors;
            }

            var seenIds = new HashSet<string>();
            foreach (var entrance in entranceSet)
            {
                if (string.IsNullOrWhiteSpace(entrance.Id))
                {
                    errors.Add("Entrance has an empty id.");
                    continue;
                }

                if (!seenIds.Add(entrance.Id))
                {
                    errors.Add($"Duplicate entrance id '{entrance.Id}'.");
                }

                if (string.IsNullOrWhiteSpace(entrance.Label))
                {
                    errors.Add($"Entrance '{entrance.Id}' has no readable label.");
                }

                if (string.IsNullOrWhiteSpace(DistrictLabelFor(entrance.Id)))
                {
                    errors.Add($"Entrance '{entrance.Id}' has no readable district label.");
                }

                if (entrance.Radius <= 0f)
                {
                    errors.Add($"Entrance '{entrance.Id}' has a non-positive radius.");
                }

                if (entrance.IsStationEntrance && !CareerQuestCatalog.IsPartyStationId(entrance.ResolveStationId()))
                {
                    errors.Add($"Station entrance '{entrance.Id}' has no resolvable Party Pack station id.");
                }
            }

            for (var first = 0; first < entranceSet.Count; first++)
            {
                for (var second = first + 1; second < entranceSet.Count; second++)
                {
                    var a = entranceSet[first];
                    var b = entranceSet[second];
                    var distance = Vector2.Distance(a.Position, b.Position);
                    if (distance < a.Radius + b.Radius)
                    {
                        errors.Add($"Entrances '{a.Id}' and '{b.Id}' have overlapping auto-entry circles (distance {distance:F2} < {a.Radius + b.Radius:F2}).");
                    }
                }
            }

            return errors;
        }
    }
}
