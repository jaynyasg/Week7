using System;
using System.Collections.Generic;
using UnityEngine;

namespace CareerQuest
{
    public class PlayableHubController : MonoBehaviour
    {
        private readonly List<BuildingEntrance> _entrances = new();

        private CareerQuestApp _app;
        private PlayerAvatarController _player;
        private PlayerAvatarNetwork _networkPlayer;
        private HubCameraRig _cameraRig;

        public IReadOnlyList<BuildingEntrance> Entrances => _entrances;
        public PlayerAvatarController Player => _player;
        public bool IsVisible => gameObject.activeSelf;

        public static PlayableHubController Ensure()
        {
            var existing = FindAnyObjectByType<PlayableHubController>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return existing;
            }

            var hub = new GameObject("PlayableHub", typeof(PlayableHubController));
            return hub.GetComponent<PlayableHubController>();
        }

        public void Show(GameSession session, CareerQuestApp app)
        {
            _app = app;
            gameObject.SetActive(true);
            Clear();

            // Entrances come from the authored prefab's WorldAnchors export
            // (live instance, then prefab asset, then hard fallback constants),
            // plus the station-id fallback entrances for Party Pack stations
            // the authored set does not cover yet (U2).
            var anchors = WorldAnchors.ResolveActive();
            var entranceSet = WorldAnchors.ActiveEntrancesWithStations;
            foreach (var entranceAnchor in entranceSet)
            {
                AddEntrance(entranceAnchor);
            }

            // U8: one readable header per district above its door cluster, so the
            // ten-station campus reads as four districts (Quest Yard / Tech Lane /
            // Story Street / Care Corner) rather than one crowded row — on both
            // the authored prefab and the fallback ground.
            AddDistrictHeaders(entranceSet);

            var playerSpawn = anchors != null ? anchors.PlayerSpawn : WorldAnchors.FallbackPlayerSpawn;
            var followTarget = default(Transform);
            if (session != null && session.ConnectionMode.IsNetworked())
            {
                _networkPlayer = FindLocalNetworkPlayer();
                if (_networkPlayer != null)
                {
                    _networkPlayer.ConfigureCampusEntrances(_entrances, EnterEntrance);
                    followTarget = _networkPlayer.transform;
                }
            }
            else
            {
                var playerObject = new GameObject("HubPlayer", typeof(SpriteRenderer), typeof(AvatarRuntimeView), typeof(PlayerAvatarController));
                playerObject.transform.SetParent(transform, false);
                playerObject.transform.position = new Vector3(playerSpawn.x, playerSpawn.y, 0f);
                _player = playerObject.GetComponent<PlayerAvatarController>();
                _player.Configure(session, _entrances, EnterEntrance);
                followTarget = playerObject.transform;
            }

            var guideSpawn = anchors != null ? anchors.GuideSpawn : WorldAnchors.FallbackGuideSpawn;
            var guideObject = new GameObject("CampusGuide", typeof(SpriteRenderer), typeof(AvatarRuntimeView), typeof(CampusGuideController));
            guideObject.transform.SetParent(transform, false);
            guideObject.transform.position = new Vector3(guideSpawn.x, guideSpawn.y, 0f);
            var guide = guideObject.GetComponent<CampusGuideController>();
            guide.Configure("Walk into a door to start a quest!");

            // P10: first hub entry of the session — greet by avatar name and
            // pulse the nearest unplayed door. No-ops on later hub entries.
            var firstRunBeat = guideObject.AddComponent<FirstRunGuideBeat>();
            firstRunBeat.TryBegin(session, guide, _entrances, followTarget != null ? (Vector2)followTarget.position : playerSpawn);

            _cameraRig = gameObject.GetComponent<HubCameraRig>() ?? gameObject.AddComponent<HubCameraRig>();
            _cameraRig.Configure(CameraDirector.Ensure(), followTarget);
        }

        public void Hide()
        {
            Clear();
            gameObject.SetActive(false);
        }

        public bool TryEnter(ActivityRoute route)
        {
            var entrance = _entrances.Find(candidate => candidate.Route == route && !candidate.IsStationEntrance);
            if (entrance == null)
            {
                return false;
            }

            EnterEntrance(entrance);
            return true;
        }

        /// <summary>U2 generic branch: enter a Party Pack station entrance by station id.</summary>
        public bool TryEnterStation(string stationId)
        {
            var entrance = _entrances.Find(candidate => candidate.StationId == stationId);
            if (entrance == null)
            {
                return false;
            }

            EnterEntrance(entrance);
            return true;
        }

        private void AddEntrance(WorldAnchorEntrance anchor)
        {
            var entranceObject = new GameObject($"{anchor.Id}_Entrance", typeof(BuildingEntrance));
            entranceObject.transform.SetParent(transform, false);
            entranceObject.transform.position = new Vector3(anchor.Position.x, anchor.Position.y, 0f);

            var entrance = entranceObject.GetComponent<BuildingEntrance>();
            entrance.Configure(anchor.Route, anchor.ResolveStationId(), anchor.Label, anchor.Radius, EnterEntrance);
            _entrances.Add(entrance);

            AddEntranceMarker(entranceObject.transform, $"{anchor.Id}_EntranceMarker", anchor.AccentColor, anchor.Radius);

            // Door label is world-space TMP (DoorSign pattern) — the hub
            // TextMesh labels died in U4 per plan.
            var sign = entranceObject.AddComponent<DoorSign>();
            sign.Configure(anchor.Label, anchor.AccentColor, -0.62f, 330);
        }

        /// <summary>
        /// U8 readable-district headers: a Fredoka world label centered over each
        /// district's door cluster (just above its highest door). The header uses
        /// the district's accent (the first entrance's color) and the WorldAnchors
        /// district centroid, so prefab and fallback campuses read the same four
        /// clusters. Districts with no live door are skipped.
        /// </summary>
        private void AddDistrictHeaders(IReadOnlyList<WorldAnchorEntrance> entranceSet)
        {
            foreach (var district in WorldAnchors.DistrictOrder)
            {
                if (!WorldAnchors.TryGetDistrictCenter(entranceSet, district, out var center))
                {
                    continue;
                }

                var topY = float.MinValue;
                var accent = new Color(0.96f, 0.77f, 0.36f);
                var found = false;
                foreach (var entrance in entranceSet)
                {
                    if (WorldAnchors.DistrictLabelFor(entrance.Id) != district)
                    {
                        continue;
                    }

                    if (!found)
                    {
                        accent = entrance.AccentColor;
                        found = true;
                    }

                    topY = Mathf.Max(topY, entrance.Position.y);
                }

                var headerObject = new GameObject($"District_{district.Replace(' ', '_')}");
                headerObject.transform.SetParent(transform, false);
                headerObject.transform.position = new Vector3(center.x, topY + 0.95f, 0f);
                headerObject.AddComponent<DoorSign>().Configure(district, accent, 0f, 332);
            }
        }

        /// <summary>
        /// A glowing "step here" doormat on the ground at the entry circle so every
        /// door reads as an obvious walk-in target, not just a floating name plate.
        /// Three stacked ground decals sized to the entry radius — a soft accent
        /// glow halo, a brighter accent pad, and a paper threshold rim — all below
        /// the buildings and the player so the avatar walks onto the mat. The mat
        /// is a child of the entrance object, so it pulses with the door on
        /// first-run focus (<see cref="DoorSign.SetPulsing"/>).
        /// </summary>
        private static void AddEntranceMarker(Transform parent, string name, Color color, float radius)
        {
            // Sorting band: the mat sits ABOVE the buildings (238-247) so it reads
            // as a doormat in front of its door, but BELOW the avatar (320) so the
            // player walks on top of it. Mirrors the old door marker's 305.
            var glow = new GameObject($"{name}Glow", typeof(SpriteRenderer));
            glow.transform.SetParent(parent, false);
            glow.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            glow.transform.localScale = new Vector3(radius * 3.8f, radius * 2.2f, 1f);
            var glowRenderer = glow.GetComponent<SpriteRenderer>();
            glowRenderer.sprite = CampusWorldSprites.Circle;
            glowRenderer.color = new Color(color.r, color.g, color.b, 0.32f);
            glowRenderer.sortingOrder = 300;

            var mat = new GameObject(name, typeof(SpriteRenderer));
            mat.transform.SetParent(parent, false);
            mat.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            mat.transform.localScale = new Vector3(radius * 2.8f, radius * 1.6f, 1f);
            var matRenderer = mat.GetComponent<SpriteRenderer>();
            matRenderer.sprite = CampusWorldSprites.Circle;
            matRenderer.color = new Color(color.r, color.g, color.b, 0.7f);
            matRenderer.sortingOrder = 301;

            var rim = new GameObject($"{name}Rim", typeof(SpriteRenderer));
            rim.transform.SetParent(parent, false);
            rim.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            rim.transform.localScale = new Vector3(radius * 1.85f, radius * 1.05f, 1f);
            var rimRenderer = rim.GetComponent<SpriteRenderer>();
            rimRenderer.sprite = CampusWorldSprites.Circle;
            rimRenderer.color = new Color(1f, 0.97f, 0.88f, 0.62f);
            rimRenderer.sortingOrder = 302;
        }

        /// <summary>
        /// U2 single dispatch seam for every door entry. Party Pack station
        /// entrances route by station id into the app's one generic station
        /// branch (KTD3); legacy core/optional doors keep their bespoke routes.
        /// </summary>
        private void EnterEntrance(BuildingEntrance entrance)
        {
            if (entrance == null)
            {
                return;
            }

            if (entrance.IsStationEntrance)
            {
                _app.ShowPartyStation(entrance.StationId);
                return;
            }

            EnterRoute(entrance.Route);
        }

        private void EnterRoute(ActivityRoute route)
        {
            switch (route)
            {
                case ActivityRoute.DesignBuild:
                    _app.ShowDesignBuild(false);
                    break;
                case ActivityRoute.HealthHero:
                    _app.ShowHealthHero();
                    break;
                case ActivityRoute.LogicCourt:
                    _app.ShowLogicCourt();
                    break;
                case ActivityRoute.AiLab:
                    _app.ShowAiLab();
                    break;
                case ActivityRoute.MusicStudio:
                    _app.ShowMusicStudio();
                    break;
                case ActivityRoute.RoboticsGarage:
                    _app.ShowRoboticsGarage();
                    break;
                case ActivityRoute.CommunityKitchen:
                    _app.ShowCommunityKitchen();
                    break;
                default:
                    throw new ArgumentException($"{route} is not a playable campus destination.", nameof(route));
            }
        }

        private void Clear()
        {
            _cameraRig?.ClearFollow();
            _networkPlayer?.ClearCampusEntrances();
            _entrances.Clear();
            _player = null;
            _networkPlayer = null;

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private static PlayerAvatarNetwork FindLocalNetworkPlayer()
        {
            var avatars = FindObjectsByType<PlayerAvatarNetwork>(FindObjectsInactive.Exclude);
            foreach (var avatar in avatars)
            {
                if (avatar != null && avatar.IsSpawned && avatar.IsOwner)
                {
                    return avatar;
                }
            }

            return null;
        }
    }
}
