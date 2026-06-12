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
            foreach (var entranceAnchor in WorldAnchors.ActiveEntrancesWithStations)
            {
                AddEntrance(entranceAnchor);
            }

            var playerSpawn = anchors != null ? anchors.PlayerSpawn : WorldAnchors.FallbackPlayerSpawn;
            var playerObject = new GameObject("HubPlayer", typeof(SpriteRenderer), typeof(AvatarRuntimeView), typeof(PlayerAvatarController));
            playerObject.transform.SetParent(transform, false);
            playerObject.transform.position = new Vector3(playerSpawn.x, playerSpawn.y, 0f);
            _player = playerObject.GetComponent<PlayerAvatarController>();
            _player.Configure(session, _entrances, EnterEntrance);

            var guideSpawn = anchors != null ? anchors.GuideSpawn : WorldAnchors.FallbackGuideSpawn;
            var guideObject = new GameObject("CampusGuide", typeof(SpriteRenderer), typeof(AvatarRuntimeView), typeof(CampusGuideController));
            guideObject.transform.SetParent(transform, false);
            guideObject.transform.position = new Vector3(guideSpawn.x, guideSpawn.y, 0f);
            var guide = guideObject.GetComponent<CampusGuideController>();
            guide.Configure("Walk into a door to start a quest!");

            // P10: first hub entry of the session — greet by avatar name and
            // pulse the nearest unplayed door. No-ops on later hub entries.
            var firstRunBeat = guideObject.AddComponent<FirstRunGuideBeat>();
            firstRunBeat.TryBegin(session, guide, _entrances, playerSpawn);

            _cameraRig = gameObject.GetComponent<HubCameraRig>() ?? gameObject.AddComponent<HubCameraRig>();
            _cameraRig.Configure(CameraDirector.Ensure(), playerObject.transform);
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

            AddEntranceMarker(entranceObject.transform, $"{anchor.Id}_EntranceMarker", anchor.AccentColor);

            // Door label is world-space TMP (DoorSign pattern) — the hub
            // TextMesh labels died in U4 per plan.
            var sign = entranceObject.AddComponent<DoorSign>();
            sign.Configure(anchor.Label, anchor.AccentColor, -0.62f, 330);
        }

        private static void AddEntranceMarker(Transform parent, string name, Color color)
        {
            var marker = new GameObject(name, typeof(SpriteRenderer));
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

            var renderer = marker.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetCatalog.SpriteFor("ui.confirm");
            renderer.color = new Color(color.r, color.g, color.b, 0.62f);
            renderer.sortingOrder = 305;
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
            _entrances.Clear();
            _player = null;

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
