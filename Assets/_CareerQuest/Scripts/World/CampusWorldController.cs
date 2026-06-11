using UnityEngine;

namespace CareerQuest
{
    public class CampusWorldController : MonoBehaviour
    {
        private Transform _root;
        private Camera _camera;
        private CampusWorldBuilder _builder;
        private HubBootController _hubBoot;
        private RoomVeilController _roomVeil;

        public bool IsHubBootComplete => _hubBoot != null && _hubBoot.IsBootComplete;
        public bool IsHubDecorLoaded => _hubBoot != null && _hubBoot.IsDecorLoaded;
        public bool IsRoomVeilActive => _roomVeil != null && _roomVeil.IsVeilActive;

        public static CampusWorldController Ensure()
        {
            var existing = FindFirstObjectByType<CampusWorldController>();
            if (existing != null)
            {
                existing.EnsureSetup();
                return existing;
            }

            var world = new GameObject("CampusWorld", typeof(CampusWorldController));
            var controller = world.GetComponent<CampusWorldController>();
            controller.EnsureSetup();
            return controller;
        }

        public void ShowEntry(GameSession session)
        {
            BeginHub("Entry");
            _builder.AddHeroCharacters(session, session?.PlayerCount > 1);
        }

        public void ShowConnection(GameSession session)
        {
            BeginHub("Connection");
            _builder.AddNetworkProof(-2.2f, 0.1f, "Host", CampusWorldPalette.PlayerBlue);
            _builder.AddNetworkProof(2.2f, 0.1f, "Join", CampusWorldPalette.PlayerGold);
        }

        public void ShowCampus(GameSession session)
        {
            BeginHub("Campus");
            _builder.AddHeroCharacters(session, session != null && (session.PlayerCount > 1 || session.Mode == AppMode.Showcase));
        }

        public void ShowProof(GameSession session)
        {
            CancelBoot();
            _builder.ClearWorld();
            _builder.AddSky();
            _builder.AddGround();
            _builder.AddPath(new Vector2(0f, -0.9f), new Vector2(7.2f, 0.42f), 0f);
            _builder.AddBuilding("Shared Campus", 0f, 1.25f, 2.25f, 1.3f, CampusWorldPalette.Mint, CampusWorldPalette.TealRoof, 3);
            _builder.AddNetworkProof(-2.6f, -0.85f, "P1 Builder", CampusWorldPalette.PlayerBlue);
            _builder.AddNetworkProof(2.6f, -0.85f, "P2 Designer", CampusWorldPalette.PlayerGold);
            _builder.AddShape("ProofPulseA", CampusSpriteKind.Circle, new Vector2(-2.6f, -0.85f), new Vector2(1.35f, 1.35f), CampusWorldPalette.PlayerBlueSoft, 1);
            _builder.AddShape("ProofPulseB", CampusSpriteKind.Circle, new Vector2(2.6f, -0.85f), new Vector2(1.35f, 1.35f), CampusWorldPalette.PlayerGoldSoft, 1);
        }

        public void ShowDesignBuild(GameSession session)
        {
            BeginRoom(() => CampusRoomScenes.ShowDesignBuild(_builder, session));
        }

        public void ShowClinic(GameSession session)
        {
            BeginRoom(() => CampusRoomScenes.ShowClinic(_builder, session));
        }

        public void ShowCourt(GameSession session)
        {
            BeginRoom(() => CampusRoomScenes.ShowCourt(_builder, session));
        }

        public void ShowGallery(GameSession session)
        {
            BeginRoom(() => CampusRoomScenes.ShowGallery(_builder, session));
        }

        public void ShowReveal(GameSession session)
        {
            BeginRoom(() => CampusRoomScenes.ShowReveal(_builder, session));
        }

        public void ClearWorld()
        {
            CancelBoot();
            _builder?.ClearWorld();
        }

        private void BeginHub(string name)
        {
            EnsureSetup();
            _hubBoot.BuildCampus(name);
        }

        private void BeginRoom(System.Action buildRoom)
        {
            EnsureSetup();
            _roomVeil.ShowRoom(buildRoom);
        }

        private void CancelBoot()
        {
            _hubBoot?.Cancel();
            _roomVeil?.Cancel();
        }

        private void EnsureSetup()
        {
            if (_root == null)
            {
                _root = new GameObject("CampusWorldRoot").transform;
                _root.SetParent(transform, false);
            }

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    var cameraObject = new GameObject("CampusWorldCamera", typeof(Camera));
                    _camera = cameraObject.GetComponent<Camera>();
                    _camera.orthographic = true;
                    _camera.orthographicSize = 4.5f;
                    _camera.backgroundColor = CampusWorldPalette.Sky;
                    _camera.clearFlags = CameraClearFlags.SolidColor;
                    _camera.transform.position = new Vector3(0f, 0f, -10f);
                }
            }

            if (_builder == null)
            {
                _builder = new CampusWorldBuilder(_root);
                var entrances = new BuildingEntranceController(_builder);
                _hubBoot = new HubBootController(this, _builder, entrances);
                _roomVeil = new RoomVeilController(this, _builder);
            }
        }
    }
}
