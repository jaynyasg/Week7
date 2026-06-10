using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    [RequireComponent(typeof(NetworkBootstrap))]
    [RequireComponent(typeof(EntryScreenController))]
    [RequireComponent(typeof(AvatarSelectionController))]
    [RequireComponent(typeof(ShowcaseDisclaimerController))]
    [RequireComponent(typeof(AchievementGalleryController))]
    [RequireComponent(typeof(CareerRevealController))]
    [RequireComponent(typeof(DemoDebugOverlay))]
    [RequireComponent(typeof(ShowcasePresenter))]
    public class CareerQuestApp : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport unityTransport;

        private Canvas _canvas;
        private RectTransform _root;
        private GameSession _session;
        private NetworkBootstrap _networkBootstrap;
        private EntryScreenController _entry;
        private AvatarSelectionController _avatarSelection;
        private ShowcaseDisclaimerController _disclaimer;
        private AchievementGalleryController _gallery;
        private CareerRevealController _reveal;
        private DemoDebugOverlay _debugOverlay;
        private ShowcasePresenter _showcasePresenter;
        private CampusWorldController _world;
        private PlayableHubController _hub;
        private SceneFlowRouter _router;

        public GameSession Session => _session;
        public ActivityRoute CurrentRoute => _router.CurrentRoute;

        private void Awake()
        {
            _session = new GameSession();
            _router = new SceneFlowRouter();
            _canvas = UiBuilder.EnsureCanvas();
            _root = _canvas.GetComponent<RectTransform>();
            _world = CampusWorldController.Ensure();
            _hub = PlayableHubController.Ensure();

            _networkBootstrap = GetComponent<NetworkBootstrap>();
            _entry = GetComponent<EntryScreenController>();
            _avatarSelection = GetComponent<AvatarSelectionController>();
            _disclaimer = GetComponent<ShowcaseDisclaimerController>();
            _gallery = GetComponent<AchievementGalleryController>();
            _reveal = GetComponent<CareerRevealController>();
            _debugOverlay = GetComponent<DemoDebugOverlay>();
            _showcasePresenter = GetComponent<ShowcasePresenter>();

            if (networkManager == null)
            {
                networkManager = FindFirstObjectByType<NetworkManager>();
            }

            if (unityTransport == null)
            {
                unityTransport = FindFirstObjectByType<UnityTransport>();
            }

            _networkBootstrap.Bind(networkManager, unityTransport);
            _debugOverlay.Bind(_session, _networkBootstrap);
            _showcasePresenter.Bind(this, _session);
        }

        private void Start()
        {
            if (TryStartCommandLineSmoke())
            {
                return;
            }

            ShowEntry();
        }

        public void ShowEntry()
        {
            _hub.Hide();
            _showcasePresenter.Stop();
            _router.ShowEntry(_session);
            _world.ShowEntry(_session);
            ResetRoot();
            _entry.Render(_root, this);
            AttachDebug();
        }

        public void BeginPlay()
        {
            _showcasePresenter.Stop();
            _router.BeginPlay(_session);
            ShowConnection();
        }

        public void ShowAvatarSelectionForPlay()
        {
            ShowAvatarSelection(AppMode.Play);
        }

        public void ShowAvatarSelectionForShowcase()
        {
            ShowAvatarSelection(AppMode.Showcase);
        }

        public void ChooseAvatar(string avatarId)
        {
            var route = _router.ChooseAvatar(_session, avatarId);

            if (route == ActivityRoute.ShowcaseProof)
            {
                _showcasePresenter.Begin();
                return;
            }

            ShowConnection();
        }

        public void ShowShowcaseDisclaimer()
        {
            _hub.Hide();
            _router.ShowShowcaseDisclaimer(_session);
            _world.ShowEntry(_session);
            ResetRoot();
            _disclaimer.Render(_root, this);
            AttachDebug();
        }

        public void BeginShowcase()
        {
            _router.BeginShowcase(_session);
            _showcasePresenter.Begin();
        }

        public void ShowConnection()
        {
            _hub.Hide();
            _router.ShowConnection(_session);
            _world.ShowConnection(_session);
            ResetRoot();
            var overlay = UiBuilder.FullPanel(_root, "ConnectionOverlay", new Color(0.86f, 0.95f, 1f));
            var panel = UiBuilder.Panel(overlay, "ConnectionPanel", new Color(0.96f, 0.99f, 1f, 0.94f));
            UiBuilder.Place(panel, 0f, 12f, 920f, 548f);

            UiBuilder.Shape(panel, "ConnectionHeaderBand", new Color(0.06f, 0.25f, 0.34f, 0.95f), 0f, 226f, 920f, 96f);
            var title = UiBuilder.Text(panel, "ConnectionTitle", "Start Game", 38, TextAnchor.MiddleCenter, Color.white);
            UiBuilder.Place(title.rectTransform, 0f, 242f, 860f, 48f);

            var subtitle = UiBuilder.Text(panel, "ConnectionSubtitle", "Play solo now, or use local multiplayer when testing two players.", 18, TextAnchor.MiddleCenter, new Color(0.88f, 0.97f, 1f));
            UiBuilder.Place(subtitle.rectTransform, 0f, 206f, 820f, 32f);

            var solo = UiBuilder.Button(panel, "PlaySoloButton", "Play Solo", () =>
            {
                _networkBootstrap.StartSoloFallback();
                _router.UseConnectionMode(_session, ConnectionMode.SoloFallback, 1);
                ShowCampus();
            });
            UiBuilder.Place(solo.GetComponent<RectTransform>(), -282f, 104f, 244f, 74f);
            StyleConnectionButton(solo, new Color(0.05f, 0.49f, 0.43f), 28);

            var soloHint = UiBuilder.Text(panel, "PlaySoloHint", "Recommended", 16, TextAnchor.MiddleCenter, new Color(0.06f, 0.22f, 0.2f));
            UiBuilder.Place(soloHint.rectTransform, -282f, 50f, 244f, 30f);

            var host = UiBuilder.Button(panel, "HostLocalGameButton", "Host Game", () =>
            {
                _networkBootstrap.StartHostP1();
                _router.UseConnectionMode(_session, ConnectionMode.HostP1, 1);
                ShowCampus();
            });
            UiBuilder.Place(host.GetComponent<RectTransform>(), 0f, 104f, 244f, 68f);
            StyleConnectionButton(host, new Color(0.09f, 0.31f, 0.42f), 24);

            var hostHint = UiBuilder.Text(panel, "HostLocalHint", "Start a local session", 15, TextAnchor.MiddleCenter, new Color(0.1f, 0.18f, 0.22f));
            UiBuilder.Place(hostHint.rectTransform, 0f, 50f, 244f, 30f);

            var joinLocal = UiBuilder.Button(panel, "JoinThisComputerButton", "Join This PC", () =>
            {
                _networkBootstrap.JoinLocalhostP2();
                _router.UseConnectionMode(_session, ConnectionMode.JoinLocalhostP2, 2);
                ShowCampus();
            });
            UiBuilder.Place(joinLocal.GetComponent<RectTransform>(), 282f, 104f, 244f, 68f);
            StyleConnectionButton(joinLocal, new Color(0.09f, 0.31f, 0.42f), 24);

            var joinHint = UiBuilder.Text(panel, "JoinThisComputerHint", "Connect to a host on this computer", 15, TextAnchor.MiddleCenter, new Color(0.1f, 0.18f, 0.22f));
            UiBuilder.Place(joinHint.rectTransform, 282f, 50f, 280f, 34f);

            UiBuilder.Shape(panel, "ConnectionAdvancedDivider", new Color(0.06f, 0.25f, 0.34f, 0.18f), 0f, -12f, 760f, 2f);

            var advancedTitle = UiBuilder.Text(panel, "ConnectionAdvancedTitle", "Advanced: join by IP", 18, TextAnchor.MiddleLeft, new Color(0.06f, 0.16f, 0.2f));
            UiBuilder.Place(advancedTitle.rectTransform, -244f, -52f, 280f, 30f);

            var input = UiBuilder.Input(panel, "LanAddressInput", "127.0.0.1");
            UiBuilder.Place(input.GetComponent<RectTransform>(), -94f, -100f, 330f, 48f);

            var joinLan = UiBuilder.Button(panel, "JoinIpButton", "Join IP", () =>
            {
                _networkBootstrap.JoinLanByIp(input.text);
                _router.UseConnectionMode(_session, ConnectionMode.JoinLanByIp, 2);
                ShowCampus();
            });
            UiBuilder.Place(joinLan.GetComponent<RectTransform>(), 220f, -100f, 176f, 48f);
            StyleConnectionButton(joinLan, new Color(0.09f, 0.31f, 0.42f), 20);

            var advancedHint = UiBuilder.Text(panel, "ConnectionAdvancedHint", "Use IP join only when another device is hosting on the same network.", 15, TextAnchor.MiddleCenter, new Color(0.18f, 0.26f, 0.3f));
            UiBuilder.Place(advancedHint.rectTransform, 0f, -148f, 760f, 34f);

            var controls = UiBuilder.Text(panel, "ConnectionControls", "Campus controls: WASD or arrows to move. E / Space enters a door.", 16, TextAnchor.MiddleCenter, new Color(0.1f, 0.18f, 0.22f));
            UiBuilder.Place(controls.rectTransform, 0f, -212f, 760f, 36f);

            AttachDebug();
        }

        public void ShowCampus()
        {
            _router.ShowCampus(_session);
            _world.ShowCampus(_session);
            _hub.Show(_session, this);
            ResetRoot();
            var hud = UiBuilder.Panel(_root, "CampusHud", new Color(0.93f, 0.98f, 0.95f, 0.78f));
            UiBuilder.Place(hud, 0f, 286f, 1050f, 78f);

            var title = UiBuilder.Text(hud, "CampusTitle", "Free Campus", 30, TextAnchor.MiddleLeft, new Color(0.08f, 0.2f, 0.13f));
            UiBuilder.Place(title.rectTransform, -365f, 14f, 260f, 36f);

            var mode = UiBuilder.Text(hud, "CampusMode", $"Mode: {_session.Mode} / {_session.ConnectionMode}", 17, TextAnchor.MiddleLeft, new Color(0.1f, 0.2f, 0.14f));
            UiBuilder.Place(mode.rectTransform, -365f, -18f, 340f, 28f);

            var controls = UiBuilder.Text(hud, "CampusControls", "Move: WASD / arrows     Enter: E / Space     Click a door to enter", 17, TextAnchor.MiddleCenter, new Color(0.1f, 0.2f, 0.14f));
            UiBuilder.Place(controls.rectTransform, 110f, 12f, 590f, 30f);

            var labels = UiBuilder.Text(hud, "FutureLabels", "Future: " + string.Join(" / ", CareerConfig.FutureBuildingLabels), 15, TextAnchor.MiddleCenter, new Color(0.15f, 0.25f, 0.18f));
            UiBuilder.Place(labels.rectTransform, 110f, -18f, 590f, 26f);

            var actionBar = UiBuilder.Panel(_root, "CampusActionBar", new Color(0.93f, 0.98f, 0.95f, 0.72f));
            UiBuilder.Place(actionBar, 0f, -305f, 1120f, 68f);

            AddCampusButton(actionBar, "Design Build", -420f, 0f, () => ShowDesignBuild(false));
            AddCampusButton(actionBar, "Health Hero", -140f, 0f, () => ShowHealthHero());
            AddCampusButton(actionBar, "Logic Court", 140f, 0f, () => ShowLogicCourt());

            var gallery = UiBuilder.Button(actionBar, "CampusGalleryButton", "Gallery", ShowGallery);
            UiBuilder.Place(gallery.GetComponent<RectTransform>(), 360f, 0f, 160f, 46f);

            var revealLabel = _session.RevealReady ? "Reveal" : $"Reveal {_session.UniqueCompletedGames}/3";
            var reveal = UiBuilder.Button(actionBar, "CampusRevealButton", revealLabel, ShowReveal);
            UiBuilder.Place(reveal.GetComponent<RectTransform>(), 520f, 0f, 140f, 46f);

            AttachDebug();
        }

        public void ShowShowcaseProofBeat()
        {
            _hub.Hide();
            _router.ShowShowcaseProof(_session);
            _world.ShowProof(_session);
            ResetRoot();
            var panel = UiBuilder.FullPanel(_root, "ShowcaseProofPanel", new Color(0.86f, 0.91f, 1f));
            var title = UiBuilder.Text(panel, "ProofTitle", "Two-Client Proof", 40, TextAnchor.MiddleCenter, new Color(0.08f, 0.12f, 0.25f));
            UiBuilder.Place(title.rectTransform, 0f, 220f, 900f, 60f);

            var split = UiBuilder.Text(panel, "ProofBody", "Showcase simulates two local players for reliability.\nQA still proves real Netcode host/client movement.", 26, TextAnchor.MiddleCenter, new Color(0.1f, 0.14f, 0.24f));
            UiBuilder.Place(split.rectTransform, 0f, 80f, 920f, 110f);

            var left = UiBuilder.Text(panel, "ProofP1", "P1: Builder", 30, TextAnchor.MiddleCenter, Color.white);
            left.color = new Color(0.04f, 0.26f, 0.45f);
            UiBuilder.Place(left.rectTransform, -260f, -70f, 280f, 70f);

            var right = UiBuilder.Text(panel, "ProofP2", "P2: Designer", 30, TextAnchor.MiddleCenter, Color.white);
            right.color = new Color(0.45f, 0.28f, 0.02f);
            UiBuilder.Place(right.rectTransform, 260f, -70f, 280f, 70f);

            AttachDebug();
        }

        public void ShowDesignBuild(bool showcaseAutoComplete)
        {
            _hub.Hide();
            _router.ShowActivity(_session, ActivityRoute.DesignBuild);
            _world.ShowDesignBuild(_session);
            ResetRoot();
            var controller = gameObject.GetComponent<DesignBuildController>() ?? gameObject.AddComponent<DesignBuildController>();
            controller.Render(_root, _session, this, CurrentResultSource());

            if (showcaseAutoComplete)
            {
                controller.TryPlacePiece("clinic");
                controller.TryPlacePiece("court");
                controller.TryPlacePiece("studio");
                controller.TryPlacePiece("lab");
                controller.TryPlacePiece("art_tower");
                _session.RecordResult(controller.CreateResult(ResultSource.ShowcaseSeed));
            }

            AttachDebug();
        }

        public void ShowHealthHero()
        {
            _hub.Hide();
            _router.ShowActivity(_session, ActivityRoute.HealthHero);
            _world.ShowClinic(_session);
            ResetRoot();
            var controller = gameObject.GetComponent<HealthHeroController>() ?? gameObject.AddComponent<HealthHeroController>();
            controller.Render(_root, _session, this, CurrentResultSource());
            AttachDebug();
        }

        public void ShowLogicCourt()
        {
            _hub.Hide();
            _router.ShowActivity(_session, ActivityRoute.LogicCourt);
            _world.ShowCourt(_session);
            ResetRoot();
            var controller = gameObject.GetComponent<LogicCourtController>() ?? gameObject.AddComponent<LogicCourtController>();
            controller.Render(_root, _session, this, CurrentResultSource());
            AttachDebug();
        }

        public void ShowGallery()
        {
            _hub.Hide();
            _router.ShowGallery(_session);
            _world.ShowGallery(_session);
            ResetRoot();
            _gallery.Render(_root, _session, this);
            AttachDebug();
        }

        public void ShowReveal()
        {
            _hub.Hide();
            _router.ShowReveal(_session);
            _world.ShowReveal(_session);
            ResetRoot();
            _reveal.Render(_root, _session, this);
            AttachDebug();
        }

        public void QuitGame()
        {
            _router.Quit(_session);

            if (networkManager != null && (networkManager.IsHost || networkManager.IsClient || networkManager.IsServer))
            {
                networkManager.Shutdown();
            }

            Application.Quit(0);
        }

        private ResultSource CurrentResultSource()
        {
            if (_session.Mode == AppMode.Showcase)
            {
                return ResultSource.ShowcaseSeed;
            }

            if (_session.ConnectionMode == ConnectionMode.SoloFallback)
            {
                return ResultSource.SoloFallback;
            }

            return _session.ConnectionMode == ConnectionMode.None ? ResultSource.Solo : ResultSource.Multiplayer;
        }

        private void AddCampusButton(RectTransform panel, string label, float x, float y, UnityEngine.Events.UnityAction callback)
        {
            var button = UiBuilder.Button(panel, label.Replace(" ", string.Empty), label, () => callback.Invoke());
            UiBuilder.Place(button.GetComponent<RectTransform>(), x, y, 230f, 46f);
        }

        private static void StyleConnectionButton(Button button, Color color, int fontSize)
        {
            button.GetComponent<Image>().color = color;
            var label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            label.fontSize = fontSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = fontSize;
        }

        private void ResetRoot()
        {
            UiBuilder.Clear(_root);
        }

        private void ShowAvatarSelection(AppMode target)
        {
            _hub.Hide();
            _router.ShowAvatarSelection(_session, target);
            _world.ShowEntry(_session);
            ResetRoot();
            _avatarSelection.Render(_root, this);
            AttachDebug();
        }

        private void AttachDebug()
        {
            var exit = UiBuilder.SmallButton(_root, "ExitGameButton", "Exit Game", QuitGame);
            UiBuilder.Place(exit.GetComponent<RectTransform>(), 535f, 316f, 150f, 42f);
            _debugOverlay.AttachTo(_root);
        }

        private bool TryStartCommandLineSmoke()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-cq-smoke") < 0)
            {
                return false;
            }

            var mode = ValueAfter(args, "-cq-mode") ?? "solo";
            StartCoroutine(RunCommandLineSmoke(mode));
            return true;
        }

        private IEnumerator RunCommandLineSmoke(string mode)
        {
            Debug.Log($"CQ_SMOKE_START mode={mode}");

            switch (mode.ToLowerInvariant())
            {
                case "host":
                    BeginPlay();
                    _networkBootstrap.StartHostP1();
                    _router.UseConnectionMode(_session, ConnectionMode.HostP1, 1);
                    ShowCampus();
                    yield return new WaitForSeconds(6f);
                    break;
                case "client":
                    BeginPlay();
                    yield return new WaitForSeconds(1f);
                    _networkBootstrap.JoinLocalhostP2();
                    _router.UseConnectionMode(_session, ConnectionMode.JoinLocalhostP2, 2);
                    ShowCampus();
                    yield return new WaitForSeconds(2f);
                    LogSmoke("CQ_SMOKE_CONNECTED", mode);
                    yield return new WaitForSeconds(6f);
                    break;
                case "showcase":
                    BeginShowcase();
                    yield return new WaitForSeconds(7f);
                    break;
                default:
                    BeginPlay();
                    _networkBootstrap.StartSoloFallback();
                    _router.UseConnectionMode(_session, ConnectionMode.SoloFallback, 1);
                    ShowCampus();
                    yield return new WaitForSeconds(2f);
                    break;
            }

            LogSmoke("CQ_SMOKE_RESULT", mode);

            if (networkManager != null && (networkManager.IsHost || networkManager.IsClient || networkManager.IsServer))
            {
                networkManager.Shutdown();
            }

            Application.Quit(0);
        }

        private static string ValueAfter(string[] args, string flag)
        {
            var index = Array.IndexOf(args, flag);
            if (index < 0 || index + 1 >= args.Length)
            {
                return null;
            }

            return args[index + 1];
        }

        private void LogSmoke(string label, string mode)
        {
            var connectedClientCount = networkManager != null ? networkManager.ConnectedClientsIds.Count : 0;
            var isConnectedClient = networkManager != null && networkManager.IsConnectedClient;
            Debug.Log($"{label} mode={mode} status=\"{_networkBootstrap.Status}\" connectedClients={connectedClientCount} isConnectedClient={isConnectedClient} sessionMode={_session.Mode} revealReady={_session.RevealReady}");
        }
    }
}
