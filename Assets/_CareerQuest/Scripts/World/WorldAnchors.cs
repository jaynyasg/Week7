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

        public WorldAnchorEntrance()
        {
        }

        public WorldAnchorEntrance(string id, ActivityRoute route, string label, Vector2 position, Color accentColor, float radius)
        {
            Id = id;
            Route = route;
            Label = label;
            Position = position;
            AccentColor = accentColor;
            Radius = radius;
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
    }
}
