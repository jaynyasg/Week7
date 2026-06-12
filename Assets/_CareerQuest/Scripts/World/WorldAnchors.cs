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

        /// <summary>Hard fallback walk bounds (legacy PlayerAvatarController literals).</summary>
        public static readonly Rect FallbackWalkBounds = new(-5.25f, -2.45f, 10.5f, 3.0f);

        public static readonly Vector2 FallbackPlayerSpawn = new(0f, -1.55f);
        public static readonly Vector2 FallbackGuideSpawn = new(1.65f, -1.55f);

        private static string _prefabResourcePathOverride;
        private static WorldAnchors _assetAnchors;
        private static bool _assetSearched;

        private static readonly WorldAnchorEntrance[] FallbackEntrancesData =
        {
            new("design_build", ActivityRoute.DesignBuild, "Design Build", new Vector2(-3f, -0.26f), new Color(0.94f, 0.34f, 0.28f), 0.72f),
            new("health_hero", ActivityRoute.HealthHero, "Health Hero", new Vector2(0f, -0.18f), new Color(0.36f, 0.78f, 0.6f), 0.72f),
            new("logic_court", ActivityRoute.LogicCourt, "Logic Court", new Vector2(3f, -0.26f), new Color(0.96f, 0.62f, 0.18f), 0.72f),
            new("ai_lab", ActivityRoute.AiLab, "AI Lab", new Vector2(-4.45f, -1.75f), new Color(0.28f, 0.66f, 0.94f), 0.72f),
            new("music_studio", ActivityRoute.MusicStudio, "Music Studio", new Vector2(-2.05f, -2f), new Color(0.62f, 0.52f, 0.86f), 0.72f),
            new("robotics_garage", ActivityRoute.RoboticsGarage, "Robotics", new Vector2(2.05f, -2f), new Color(0.13f, 0.55f, 0.58f), 0.72f),
            new("community_kitchen", ActivityRoute.CommunityKitchen, "Kitchen", new Vector2(4.45f, -1.75f), new Color(0.55f, 0.82f, 0.5f), 0.72f)
        };

        /// <summary>
        /// U2 auto-entry radius for the six new station entrances: smaller than
        /// the legacy 0.72 so ten non-overlapping zones fit the walk rect (U8
        /// re-spaces the final district layout; markers/signs may render larger
        /// than the actual entry circle).
        /// </summary>
        public const float StationEntranceRadius = 0.5f;

        /// <summary>
        /// U2 station-id entrances for the six Party Pack stations without a
        /// legacy route. They enter via the generic PartyStation branch and are
        /// appended to any authored entrance set that does not include them
        /// (see <see cref="ActiveEntrancesWithStations"/>). Positions sit inside
        /// the fallback walk rect and keep every entry circle non-overlapping.
        /// </summary>
        public static readonly WorldAnchorEntrance[] FallbackStationEntrancesData =
        {
            new("vet_clinic", ActivityRoute.PartyStation, CareerQuestCatalog.VetClinicId, "Vet Clinic", new Vector2(-4.8f, 0.45f), new Color(0.36f, 0.78f, 0.6f), StationEntranceRadius),
            new("game_studio", ActivityRoute.PartyStation, CareerQuestCatalog.GameStudioId, "Game Studio", new Vector2(-1.6f, 0.45f), new Color(0.62f, 0.52f, 0.86f), StationEntranceRadius),
            new("weather_lab", ActivityRoute.PartyStation, CareerQuestCatalog.WeatherLabId, "Weather Lab", new Vector2(1.6f, 0.45f), new Color(0.28f, 0.66f, 0.94f), StationEntranceRadius),
            new("spaceport", ActivityRoute.PartyStation, CareerQuestCatalog.SpaceportId, "Spaceport", new Vector2(4.8f, 0.45f), new Color(0.08f, 0.26f, 0.55f), StationEntranceRadius),
            new("newsroom", ActivityRoute.PartyStation, CareerQuestCatalog.NewsroomId, "Newsroom", new Vector2(0f, -2.35f), new Color(0.96f, 0.62f, 0.18f), StationEntranceRadius),
            new("green_city", ActivityRoute.PartyStation, CareerQuestCatalog.GreenCityId, "Green City", new Vector2(-3.3f, -2.4f), new Color(0.25f, 0.64f, 0.3f), StationEntranceRadius)
        };

        /// <summary>
        /// Readable district label per entrance id (U2 anchor-data seam; U8 owns
        /// the visual district layout). Validation fails on any entrance id
        /// without a district label.
        /// </summary>
        private static readonly Dictionary<string, string> DistrictLabels = new()
        {
            ["design_build"] = "Quest Yard",
            ["health_hero"] = "Quest Yard",
            ["logic_court"] = "Quest Yard",
            ["ai_lab"] = "Tech Lane",
            ["robotics_garage"] = "Tech Lane",
            ["spaceport"] = "Tech Lane",
            ["music_studio"] = "Story Street",
            ["game_studio"] = "Story Street",
            ["newsroom"] = "Story Street",
            ["community_kitchen"] = "Care Corner",
            ["vet_clinic"] = "Care Corner",
            ["weather_lab"] = "Care Corner",
            ["green_city"] = "Care Corner"
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
