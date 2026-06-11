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

            AddEntrance("DesignBuildEntrance", "Design Build", ActivityRoute.DesignBuild, new Vector2(-3f, -0.26f), new Color(0.94f, 0.34f, 0.28f));
            AddEntrance("HealthHeroEntrance", "Health Hero", ActivityRoute.HealthHero, new Vector2(0f, -0.18f), new Color(0.36f, 0.78f, 0.6f));
            AddEntrance("LogicCourtEntrance", "Logic Court", ActivityRoute.LogicCourt, new Vector2(3f, -0.26f), new Color(0.96f, 0.62f, 0.18f));
            AddEntrance("AiLabEntrance", "AI Lab", ActivityRoute.AiLab, new Vector2(-4.45f, -1.75f), new Color(0.28f, 0.66f, 0.94f));
            AddEntrance("MusicStudioEntrance", "Music Studio", ActivityRoute.MusicStudio, new Vector2(-2.05f, -2f), new Color(0.62f, 0.52f, 0.86f));
            AddEntrance("RoboticsEntrance", "Robotics", ActivityRoute.RoboticsGarage, new Vector2(2.05f, -2f), new Color(0.13f, 0.55f, 0.58f));
            AddEntrance("KitchenEntrance", "Kitchen", ActivityRoute.CommunityKitchen, new Vector2(4.45f, -1.75f), new Color(0.55f, 0.82f, 0.5f));

            var playerObject = new GameObject("HubPlayer", typeof(SpriteRenderer), typeof(AvatarRuntimeView), typeof(PlayerAvatarController));
            playerObject.transform.SetParent(transform, false);
            playerObject.transform.position = new Vector3(0f, -1.55f, 0f);
            _player = playerObject.GetComponent<PlayerAvatarController>();
            _player.Configure(session, _entrances, EnterRoute);

            var guideObject = new GameObject("CampusGuide", typeof(SpriteRenderer), typeof(AvatarRuntimeView), typeof(CampusGuideController));
            guideObject.transform.SetParent(transform, false);
            guideObject.transform.position = new Vector3(1.65f, -1.55f, 0f);
            guideObject.GetComponent<CampusGuideController>().Configure("Move to a door, then press E.");

            _cameraRig = gameObject.GetComponent<HubCameraRig>() ?? gameObject.AddComponent<HubCameraRig>();
            _cameraRig.Configure(Camera.main, playerObject.transform);
        }

        public void Hide()
        {
            Clear();
            gameObject.SetActive(false);
        }

        public bool TryEnter(ActivityRoute route)
        {
            var entrance = _entrances.Find(candidate => candidate.Route == route);
            if (entrance == null)
            {
                return false;
            }

            EnterRoute(route);
            return true;
        }

        private void AddEntrance(string name, string label, ActivityRoute route, Vector2 position, Color color)
        {
            var entranceObject = new GameObject(name, typeof(BuildingEntrance));
            entranceObject.transform.SetParent(transform, false);
            entranceObject.transform.position = new Vector3(position.x, position.y, 0f);

            var entrance = entranceObject.GetComponent<BuildingEntrance>();
            entrance.Configure(route, label, 0.72f, EnterRoute);
            _entrances.Add(entrance);

            AddEntranceMarker(entranceObject.transform, $"{name}Marker", color);
            AddEntranceLabel(entranceObject.transform, $"{name}Label", label);
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
            renderer.sortingOrder = 18;
        }

        private static void AddEntranceLabel(Transform parent, string name, string label)
        {
            var labelObject = new GameObject(name, typeof(TextMesh));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, -0.62f, 0f);

            var text = labelObject.GetComponent<TextMesh>();
            text.text = label;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = 0.032f;
            text.fontSize = 28;
            text.color = new Color(0.05f, 0.09f, 0.11f);

            var renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.sortingOrder = 22;
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
