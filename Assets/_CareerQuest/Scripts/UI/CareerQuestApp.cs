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
        private ShowcaseDisclaimerController _disclaimer;
        private AchievementGalleryController _gallery;
        private CareerRevealController _reveal;
        private DemoDebugOverlay _debugOverlay;
        private ShowcasePresenter _showcasePresenter;

        public GameSession Session => _session;

        private void Awake()
        {
            _session = new GameSession();
            _canvas = UiBuilder.EnsureCanvas();
            _root = _canvas.GetComponent<RectTransform>();

            _networkBootstrap = GetComponent<NetworkBootstrap>();
            _entry = GetComponent<EntryScreenController>();
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
            _showcasePresenter.Stop();
            _session.StartMode(AppMode.Entry);
            ResetRoot();
            _entry.Render(_root, this);
            AttachDebug();
        }

        public void BeginPlay()
        {
            _showcasePresenter.Stop();
            _session.StartMode(AppMode.Play);
            ShowConnection();
        }

        public void ShowShowcaseDisclaimer()
        {
            ResetRoot();
            _disclaimer.Render(_root, this);
            AttachDebug();
        }

        public void BeginShowcase()
        {
            _session.SeedShowcase();
            _session.PlayerCount = 2;
            _showcasePresenter.Begin();
        }

        public void ShowConnection()
        {
            ResetRoot();
            var panel = UiBuilder.FullPanel(_root, "ConnectionPanel", new Color(0.88f, 0.94f, 0.96f));

            var title = UiBuilder.Text(panel, "ConnectionTitle", "Choose Connection", 40, TextAnchor.MiddleCenter, new Color(0.08f, 0.16f, 0.2f));
            UiBuilder.Place(title.rectTransform, 0f, 240f, 880f, 60f);

            var host = UiBuilder.Button(panel, "HostP1Button", "Host P1", () =>
            {
                _networkBootstrap.StartHostP1();
                _session.SetConnectionMode(ConnectionMode.HostP1);
                _session.PlayerCount = 1;
                ShowCampus();
            });
            UiBuilder.Place(host.GetComponent<RectTransform>(), -300f, 100f, 240f, 64f);

            var joinLocal = UiBuilder.Button(panel, "JoinLocalButton", "Join Localhost as P2", () =>
            {
                _networkBootstrap.JoinLocalhostP2();
                _session.SetConnectionMode(ConnectionMode.JoinLocalhostP2);
                _session.PlayerCount = 2;
                ShowCampus();
            });
            UiBuilder.Place(joinLocal.GetComponent<RectTransform>(), 0f, 100f, 280f, 64f);

            var solo = UiBuilder.Button(panel, "SoloFallbackButton", "Solo Fallback", () =>
            {
                _networkBootstrap.StartSoloFallback();
                _session.SetConnectionMode(ConnectionMode.SoloFallback);
                _session.PlayerCount = 1;
                ShowCampus();
            });
            UiBuilder.Place(solo.GetComponent<RectTransform>(), 300f, 100f, 240f, 64f);

            var input = UiBuilder.Input(panel, "LanAddressInput", "127.0.0.1");
            UiBuilder.Place(input.GetComponent<RectTransform>(), -110f, -20f, 250f, 48f);

            var joinLan = UiBuilder.Button(panel, "JoinLanButton", "Join LAN by IP", () =>
            {
                _networkBootstrap.JoinLanByIp(input.text);
                _session.SetConnectionMode(ConnectionMode.JoinLanByIp);
                _session.PlayerCount = 2;
                ShowCampus();
            });
            UiBuilder.Place(joinLan.GetComponent<RectTransform>(), 190f, -20f, 250f, 56f);

            var controls = UiBuilder.Text(panel, "ConnectionControls", $"{PlayerControlPreset.P1().Label}     {PlayerControlPreset.P2().Label}     {PlayerControlPreset.Solo().Label}", 20, TextAnchor.MiddleCenter, new Color(0.1f, 0.18f, 0.22f));
            UiBuilder.Place(controls.rectTransform, 0f, -120f, 1040f, 44f);

            AttachDebug();
        }

        public void ShowCampus()
        {
            ResetRoot();
            var panel = UiBuilder.FullPanel(_root, "CampusPanel", new Color(0.85f, 0.97f, 0.9f));

            var title = UiBuilder.Text(panel, "CampusTitle", "Free Campus", 40, TextAnchor.MiddleCenter, new Color(0.08f, 0.2f, 0.13f));
            UiBuilder.Place(title.rectTransform, 0f, 250f, 900f, 60f);

            var mode = UiBuilder.Text(panel, "CampusMode", $"Mode: {_session.Mode} / {_session.ConnectionMode}", 20, TextAnchor.MiddleCenter, new Color(0.1f, 0.2f, 0.14f));
            UiBuilder.Place(mode.rectTransform, 0f, 205f, 780f, 36f);

            AddCampusButton(panel, "Design Build Studio", -310f, 75f, () => ShowDesignBuild(false));
            AddCampusButton(panel, "Health Hero Clinic", 0f, 75f, () => ShowHealthHero());
            AddCampusButton(panel, "Logic Court", 310f, 75f, () => ShowLogicCourt());

            var labels = UiBuilder.Text(panel, "FutureLabels", "Future buildings: " + string.Join("  /  ", CareerConfig.FutureBuildingLabels), 20, TextAnchor.MiddleCenter, new Color(0.15f, 0.25f, 0.18f));
            UiBuilder.Place(labels.rectTransform, 0f, -30f, 1100f, 48f);

            var gallery = UiBuilder.Button(panel, "CampusGalleryButton", "Achievement Gallery", ShowGallery);
            UiBuilder.Place(gallery.GetComponent<RectTransform>(), -150f, -155f, 280f, 64f);

            var reveal = UiBuilder.Button(panel, "CampusRevealButton", "Career Reveal", ShowReveal);
            UiBuilder.Place(reveal.GetComponent<RectTransform>(), 160f, -155f, 250f, 64f);

            var entry = UiBuilder.Button(panel, "CampusEntryButton", "Entry", ShowEntry);
            UiBuilder.Place(entry.GetComponent<RectTransform>(), 0f, -245f, 200f, 54f);

            AttachDebug();
        }

        public void ShowShowcaseProofBeat()
        {
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
            ResetRoot();
            var controller = gameObject.GetComponent<HealthHeroController>() ?? gameObject.AddComponent<HealthHeroController>();
            controller.Render(_root, _session, this, CurrentResultSource());
            AttachDebug();
        }

        public void ShowLogicCourt()
        {
            ResetRoot();
            var controller = gameObject.GetComponent<LogicCourtController>() ?? gameObject.AddComponent<LogicCourtController>();
            controller.Render(_root, _session, this, CurrentResultSource());
            AttachDebug();
        }

        public void ShowGallery()
        {
            ResetRoot();
            _gallery.Render(_root, _session, this);
            AttachDebug();
        }

        public void ShowReveal()
        {
            ResetRoot();
            _reveal.Render(_root, _session, this);
            AttachDebug();
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
            UiBuilder.Place(button.GetComponent<RectTransform>(), x, y, 270f, 70f);
        }

        private void ResetRoot()
        {
            UiBuilder.Clear(_root);
        }

        private void AttachDebug()
        {
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
                    _session.SetConnectionMode(ConnectionMode.HostP1);
                    _session.PlayerCount = 1;
                    ShowCampus();
                    yield return new WaitForSeconds(6f);
                    break;
                case "client":
                    BeginPlay();
                    yield return new WaitForSeconds(1f);
                    _networkBootstrap.JoinLocalhostP2();
                    _session.SetConnectionMode(ConnectionMode.JoinLocalhostP2);
                    _session.PlayerCount = 2;
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
                    _session.SetConnectionMode(ConnectionMode.SoloFallback);
                    _session.PlayerCount = 1;
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
