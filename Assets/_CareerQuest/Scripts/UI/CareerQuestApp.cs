using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
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
        private TextMeshProUGUI _connectionStatusText;
        private bool _ceremonyActive;
        private CeremonyController _ceremonyController;
        private Coroutine _ceremonyCoroutine;
        private GameObject _ceremonyOverlay;
        private TextMeshProUGUI _ceremonyTitleText;
        private TextMeshProUGUI _ceremonyMessageText;
        private TextMeshProUGUI _ceremonyBadgeText;
        private RectTransform _ceremonyBadgeStamp;
        private Button _ceremonySkipButton;
        private readonly List<GameObject> _ceremonyConfetti = new();
        private TextMeshProUGUI _instructionStripText;
        private bool _sessionChangedSubscribed;
        private AudioDirector _audioDirector;
        private PauseMenuController _pauseMenu;
        private PassportController _passport;
        private AccessorySpotlightController _accessorySpotlight;
        private PartyStationController _partyStation;
        private PartyRunPresenter _partyRunPresenter;
        private FacilitatorControlsController _facilitatorControls;
        private CeremonySubPhase _lastCeremonySubPhase;
        // P19: session-scoped memory of which city pieces already played their
        // arrival fanfare — one fanfare per piece per app session.
        private readonly HashSet<string> _cityPieceFanfares = new();

        public GameSession Session => _session;
        public ActivityRoute CurrentRoute => _router.CurrentRoute;

        /// <summary>U2: station id active on the generic PartyStation route; null elsewhere.</summary>
        public string CurrentStationId => _router.CurrentStationId;

        public bool IsCeremonyActive => _ceremonyActive;

        private void Awake()
        {
            _session = new GameSession();
            // Fresh app session — the P10 first-run guide beat may play again.
            FirstRunGuideBeat.ResetSessionFlag();
            _router = new SceneFlowRouter();
            _canvas = UiBuilder.EnsureCanvas();
            _root = _canvas.GetComponent<RectTransform>();
            _world = CampusWorldController.Ensure();
            _hub = PlayableHubController.Ensure();
            // U8: the three-tier audio system rides the app object so it
            // survives ClearWorld — route changes crossfade, never cut.
            _audioDirector = AudioDirector.AttachTo(gameObject);

            // U13: the Escape pause menu rides the app object too (UI-overlay
            // only — it never touches Time.timeScale in a networked session).
            _pauseMenu = PauseMenuController.AttachTo(gameObject);
            _pauseMenu.Bind(this, _audioDirector);

            // U9: the guided Party Run presenter and the facilitator controls
            // ride the app object (session-only; KTD7/R19). The presenter mounts
            // on campus when a run is active; the facilitator controls live in
            // the pause surface. Both are bound here, mounted on demand.
            _partyRunPresenter = PartyRunPresenter.AttachTo(gameObject);
            _partyRunPresenter.Bind(this, _session);
            _facilitatorControls = FacilitatorControlsController.AttachTo(gameObject);
            _facilitatorControls.Bind(this);
            _pauseMenu.BindFacilitatorControls(_facilitatorControls);

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
            EnsureSessionChangedSubscription();
        }

        private void OnEnable()
        {
            if (_networkBootstrap != null)
            {
                _networkBootstrap.ClientConnectionLost += HandleClientConnectionLost;
            }
        }

        private void OnDisable()
        {
            if (_networkBootstrap != null)
            {
                _networkBootstrap.ClientConnectionLost -= HandleClientConnectionLost;
            }

            UnbindCampusSessionState();
            UnsubscribeSessionChanged();

            // U9: clear the process-wide quiet/reduced-motion gate so it never
            // leaks into a later app instance (or test suite).
            ClassroomAccessSettings.ResetStatics();
            AudioCueCatalog.ResetQuietMode();
        }

        private void Start()
        {
            if (TryStartCommandLineSmoke())
            {
                return;
            }

            ShowEntry();
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePauseMenu();
            }
        }

        /// <summary>
        /// U13 Escape seam (the key handler and the PlayMode tests share it).
        /// Toggles the pause menu in the hub and in rooms. During the ceremony
        /// overlay and the reveal cinematic beats Escape is IGNORED (not
        /// deferred): a deferred open would pop the menu at an unpredictable
        /// later moment (kid-confusing), the cinematic owns the camera so a
        /// competing modal must not mount over it, and both states already show
        /// their own control (Skip). Pressing Escape again after the beat ends
        /// works normally. Returns true when the toggle acted.
        /// </summary>
        public bool TogglePauseMenu()
        {
            if (_pauseMenu == null)
            {
                return false;
            }

            if (_pauseMenu.IsOpen)
            {
                _pauseMenu.Close();
                return true;
            }

            if (IsPauseSuppressed)
            {
                return false;
            }

            _pauseMenu.Open(_root);
            return _pauseMenu.IsOpen;
        }

        /// <summary>Escape suppression window: ceremony overlay + reveal cinematic beats.</summary>
        public bool IsPauseSuppressed =>
            _ceremonyActive || (_reveal != null && _reveal.IsCinematicActive);

        /// <summary>Test/QA seam for the pause shell.</summary>
        public PauseMenuController PauseMenu => _pauseMenu;

        /// <summary>U9 test/QA seam: the guided Party Run presenter.</summary>
        public PartyRunPresenter PartyRunPresenter => _partyRunPresenter;

        /// <summary>U9 test/QA seam: the facilitator controls.</summary>
        public FacilitatorControlsController FacilitatorControls => _facilitatorControls;

        // ------------------------------------------------------------------
        // U9: guided Party Run + classroom access seams (R18/R19).
        //
        // KTD7 proof: the guided run is a PRESENTER over session-only state.
        // Starting/continuing/quitting it changes ONLY PartyRunState — never the
        // best results, accessories, badges, traits, or evolution pieces — and
        // the campus doors stay free-choice the whole time (a station entered
        // outside the run never advances it; see HandleStationRewardEvent).
        // ------------------------------------------------------------------

        /// <summary>
        /// The standard demo route (design doc 90-second demo): the first rounds
        /// of the party row, each on its default seed (null = station default).
        /// </summary>
        public static readonly string[] DefaultDemoRouteStationIds =
        {
            CareerQuestCatalog.RoboticsGarageId,
            CareerQuestCatalog.MusicStudioId,
            CareerQuestCatalog.CommunityKitchenId,
            CareerQuestCatalog.AiLabId
        };

        /// <summary>
        /// Starts a guided Party Run over the given ordered station ids (seed ids
        /// optional, parallel). Presenter only: it sets PartyRunState and re-shows
        /// the campus so the run panel mounts. Earned results are untouched.
        /// </summary>
        public bool StartPartyRun(IReadOnlyList<string> stationIds, IReadOnlyList<string> seedIds = null)
        {
            if (_session == null || !_session.PartyRun.Start(stationIds, seedIds))
            {
                return false;
            }

            if (!_ceremonyActive)
            {
                ShowCampus();
            }

            return true;
        }

        /// <summary>Starts the standard demo route (U11 proof routes drive this).</summary>
        public bool StartDemoRoute()
        {
            return StartPartyRun(DefaultDemoRouteStationIds);
        }

        /// <summary>
        /// Continue the run: routes to the current round's station on its selected
        /// seed. The station completion (not this call) advances the run. Returns
        /// false when no run is active or the current station id is unknown.
        /// </summary>
        public bool ContinuePartyRun()
        {
            var run = _session?.PartyRun;
            if (run == null || !run.IsActive || run.IsComplete)
            {
                return false;
            }

            var stationId = run.CurrentStationId;
            if (string.IsNullOrEmpty(stationId))
            {
                return false;
            }

            return ShowPartyStation(stationId);
        }

        /// <summary>
        /// Quit the run: clears ONLY guided sequencing (PartyRunState) and returns
        /// to campus. Every earned result, accessory, badge, trait, and evolution
        /// piece is preserved (design doc: "Quit clears only guided-run state").
        /// </summary>
        public void QuitPartyRun()
        {
            _session?.PartyRun.Clear();
            if (!_ceremonyActive)
            {
                ShowCampus();
            }
        }

        /// <summary>
        /// Restart the guided demo route: a fresh pass over the SAME session
        /// (re-seeds the run from round one) WITHOUT clearing earned results.
        /// </summary>
        public bool RestartDemoRoute()
        {
            _session?.PartyRun.Clear();
            return StartDemoRoute();
        }

        /// <summary>Facilitator "return to campus" — leaves a room/run detour, clears nothing.</summary>
        public void ReturnToCampus()
        {
            if (!_ceremonyActive)
            {
                ShowCampus();
            }
        }

        /// <summary>
        /// Facilitator "start over": the ONLY control that clears session-earned
        /// results. Resets results + the guided run to a fresh play session and
        /// returns to campus. Classroom access settings (a stickier preference)
        /// are intentionally preserved.
        /// </summary>
        public void StartOver()
        {
            if (_session == null)
            {
                return;
            }

            _session.ResetResults(); // clears best results + PartyRunState
            if (!_ceremonyActive)
            {
                ShowCampus();
            }
        }

        /// <summary>
        /// Sets the reduced-motion + quiet-audio classroom mode and threads the
        /// flag to every gameplay surface (R19): the accessory spotlight, the
        /// camera flourish, the scene wipes (static gate), and the audio gate —
        /// while completion clarity is preserved. CareerQuestApp is the one
        /// place the flag fans out from ClassroomAccessSettings.
        /// </summary>
        public void SetQuietMode(bool quiet)
        {
            if (_session == null)
            {
                return;
            }

            _session.ClassroomAccess.QuietMode = quiet; // pushes the static gate
            ApplyClassroomAccess();
        }

        /// <summary>
        /// Pushes the live ClassroomAccess flags to the held surfaces. Called on
        /// every settings change AND on each campus/room (re)entry, so a
        /// freshly-created CameraDirector or spotlight adopts the current mode.
        /// </summary>
        private void ApplyClassroomAccess()
        {
            if (_session == null)
            {
                return;
            }

            var quiet = _session.ClassroomAccess.QuietMode;

            // The ClassroomAccess.QuietMode setter already mirrors the static
            // reduced-motion/quiet-audio gate (read by SceneWipe). Here we drive
            // the audio gate (idempotent) and push the flag to the HELD surfaces,
            // so a CameraDirector/spotlight created by this (re)entry adopts the
            // current mode even when the flag itself did not change.
            AudioCueCatalog.SetQuietMode(quiet);

            if (_world != null && _world.CameraDirector != null)
            {
                _world.CameraDirector.ReducedMotion = quiet;
            }

            if (_accessorySpotlight != null)
            {
                _accessorySpotlight.QuietMode = quiet;
            }
        }

        /// <summary>
        /// U13 Exit to Title: routes through the EXISTING teardown paths — the
        /// same duties HandleClientConnectionLost carries (ceremony/cinematic/
        /// drag cancel, session-flag reset, network unbind + shutdown) — then
        /// re-routes to the entry title via the normal world API. Never a raw
        /// scene reload.
        /// </summary>
        public void ExitToTitle()
        {
            CancelCeremony(); // safe no-op when idle; tears down drag/cinematic/camera
            FirstRunGuideBeat.ResetSessionFlag();
            UnbindCampusSessionState();

            if (networkManager != null && (networkManager.IsHost || networkManager.IsClient || networkManager.IsServer))
            {
                // Intentional shutdown — the local disconnect callback must not
                // bounce the player to the "host disconnected" error screen.
                _networkBootstrap?.SuppressLocalDisconnectNotice();
                networkManager.Shutdown();
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
            ShowCampus();
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

            ShowCampus();
        }

        public bool ShowVisualQaState(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return false;
            }

            switch (state.Trim().ToLowerInvariant())
            {
                case "avatar":
                case "avatar-selection":
                    ShowAvatarSelectionForPlay();
                    return true;
                case "campus":
                    BeginPlay();
                    return true;
                case "design-build":
                case "design":
                    BeginPlay();
                    ShowDesignBuild(false);
                    return true;
                case "health":
                case "health-hero":
                    BeginPlay();
                    ShowHealthHero();
                    return true;
                case "logic":
                case "logic-court":
                    BeginPlay();
                    ShowLogicCourt();
                    return true;
                case "music-studio":
                case "music":
                    BeginPlay();
                    ShowMusicStudio();
                    return true;
                case "ai-lab":
                case "space-lab":
                    BeginPlay();
                    ShowAiLab();
                    return true;
                case "robotics":
                case "robotics-garage":
                    BeginPlay();
                    ShowRoboticsGarage();
                    return true;
                case "kitchen":
                case "community-kitchen":
                    BeginPlay();
                    ShowCommunityKitchen();
                    return true;
                case "vet":
                case "vet-clinic":
                    BeginPlay();
                    return ShowPartyStation(CareerQuestCatalog.VetClinicId);
                case "game-studio":
                    BeginPlay();
                    return ShowPartyStation(CareerQuestCatalog.GameStudioId);
                case "weather":
                case "weather-lab":
                    BeginPlay();
                    return ShowPartyStation(CareerQuestCatalog.WeatherLabId);
                case "spaceport":
                    BeginPlay();
                    return ShowPartyStation(CareerQuestCatalog.SpaceportId);
                case "newsroom":
                    BeginPlay();
                    return ShowPartyStation(CareerQuestCatalog.NewsroomId);
                case "green-city":
                    BeginPlay();
                    return ShowPartyStation(CareerQuestCatalog.GreenCityId);
                case "accessory-fit":
                    // Complete the party stations (their results carry the
                    // station accessories AccessoryRewardConfig maps — unlike the
                    // showcase core-room seed, which earns none) and stay on
                    // campus: the campus player avatar is the accessory-bearing
                    // AvatarRuntimeView (the mid-play station hero is a plain
                    // figure), so it wears the derived one-per-slot set here.
                    BeginPlay();
                    SeedAccessoryFitResults(null);
                    return true;
                case "gallery":
                    BeginPlay();
                    ShowGallery();
                    return true;
                case "reveal-locked":
                    BeginPlay();
                    ShowReveal();
                    return true;
                case "reveal-unlocked":
                    BeginPlay();
                    SeedVisualQaResults();
                    ShowReveal();
                    return true;
                default:
                    return false;
            }
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
            var title = UiBuilder.Text(panel, "ConnectionTitle", "Start Game", TypeStyles.ScreenTitle, TextAnchor.MiddleCenter, Color.white, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(title.rectTransform, 0f, 242f, 860f, 48f);

            var subtitle = UiBuilder.Text(panel, "ConnectionSubtitle", "Play solo now, or use local multiplayer when testing two players.", 18, TextAnchor.MiddleCenter, new Color(0.88f, 0.97f, 1f));
            UiBuilder.Place(subtitle.rectTransform, 0f, 206f, 820f, 32f);

            _connectionStatusText = UiBuilder.Text(panel, "ConnectionStatusText", string.Empty, 17, TextAnchor.MiddleCenter, new Color(0.55f, 0.12f, 0.12f));
            UiBuilder.Place(_connectionStatusText.rectTransform, 0f, 168f, 820f, 36f);
            _connectionStatusText.gameObject.SetActive(false);

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

            var host = UiBuilder.Button(panel, "HostLocalGameButton", "Host Game", () => StartCoroutine(ConnectAsHost()));
            UiBuilder.Place(host.GetComponent<RectTransform>(), 0f, 104f, 244f, 68f);
            StyleConnectionButton(host, new Color(0.09f, 0.31f, 0.42f), 24);

            var hostHint = UiBuilder.Text(panel, "HostLocalHint", "Start a local session", 15, TextAnchor.MiddleCenter, new Color(0.1f, 0.18f, 0.22f));
            UiBuilder.Place(hostHint.rectTransform, 0f, 50f, 244f, 30f);

            var joinLocal = UiBuilder.Button(panel, "JoinThisComputerButton", "Join This PC", () => StartCoroutine(ConnectAsLocalClient()));
            UiBuilder.Place(joinLocal.GetComponent<RectTransform>(), 282f, 104f, 244f, 68f);
            StyleConnectionButton(joinLocal, new Color(0.09f, 0.31f, 0.42f), 24);

            var joinHint = UiBuilder.Text(panel, "JoinThisComputerHint", "Connect to a host on this computer", 15, TextAnchor.MiddleCenter, new Color(0.1f, 0.18f, 0.22f));
            UiBuilder.Place(joinHint.rectTransform, 282f, 50f, 280f, 34f);

            UiBuilder.Shape(panel, "ConnectionAdvancedDivider", new Color(0.06f, 0.25f, 0.34f, 0.18f), 0f, -12f, 760f, 2f);

            var advancedTitle = UiBuilder.Text(panel, "ConnectionAdvancedTitle", "Advanced: join by IP", 18, TextAnchor.MiddleLeft, new Color(0.06f, 0.16f, 0.2f));
            UiBuilder.Place(advancedTitle.rectTransform, -244f, -52f, 280f, 30f);

            var input = UiBuilder.Input(panel, "LanAddressInput", "127.0.0.1");
            UiBuilder.Place(input.GetComponent<RectTransform>(), -94f, -100f, 330f, 48f);

            var joinLan = UiBuilder.Button(panel, "JoinIpButton", "Join IP", () => StartCoroutine(ConnectAsLanClient(input.text)));
            UiBuilder.Place(joinLan.GetComponent<RectTransform>(), 220f, -100f, 176f, 48f);
            StyleConnectionButton(joinLan, new Color(0.09f, 0.31f, 0.42f), 20);

            var advancedHint = UiBuilder.Text(panel, "ConnectionAdvancedHint", "Use IP join only when another device is hosting on the same network.", 15, TextAnchor.MiddleCenter, new Color(0.18f, 0.26f, 0.3f));
            UiBuilder.Place(advancedHint.rectTransform, 0f, -148f, 760f, 34f);

            var controls = UiBuilder.Text(panel, "ConnectionControls", "Campus controls: WASD or arrows to move. Walk into a door to enter.", 16, TextAnchor.MiddleCenter, new Color(0.1f, 0.18f, 0.22f));
            UiBuilder.Place(controls.rectTransform, 0f, -212f, 760f, 36f);

            AttachDebug();
        }

        public void ShowConnectionError(string message)
        {
            ShowConnection();
            if (_connectionStatusText == null)
            {
                return;
            }

            _connectionStatusText.text = message;
            _connectionStatusText.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
        }

        private IEnumerator ConnectAsHost()
        {
            yield return ConnectAndShowCampus(ConnectionMode.HostP1, 1, () => _networkBootstrap.StartHostP1());
        }

        /// <summary>
        /// QA seam for the 2P matrix smoke (TwoPlayerMatrixSmoke): the exact
        /// connect path the connection buttons use — including session-state
        /// binding and the campus route. Check
        /// <see cref="NetworkBootstrap.LastConnectionSucceeded"/> afterwards.
        /// </summary>
        public IEnumerator ConnectForQa(bool asHost)
        {
            return asHost ? ConnectAsHost() : ConnectAsLocalClient();
        }

        private IEnumerator ConnectAsLocalClient()
        {
            yield return ConnectAndShowCampus(ConnectionMode.JoinLocalhostP2, 2, () => _networkBootstrap.JoinLocalhostP2());
        }

        private IEnumerator ConnectAsLanClient(string address)
        {
            yield return ConnectAndShowCampus(ConnectionMode.JoinLanByIp, 2, () => _networkBootstrap.JoinLanByIp(address));
        }

        private IEnumerator ConnectAndShowCampus(ConnectionMode mode, int playerSlot, Func<bool> startNetwork)
        {
            if (!startNetwork())
            {
                ShowConnectionError("Could not start network. Try again or use Play Solo.");
                yield break;
            }

            _router.UseConnectionMode(_session, mode, playerSlot);
            yield return _networkBootstrap.WaitForConnection(12f);

            if (!_networkBootstrap.LastConnectionSucceeded)
            {
                var message = string.IsNullOrWhiteSpace(_networkBootstrap.LastConnectionError)
                    ? "Could not connect. Check the host is running, then try again."
                    : _networkBootstrap.LastConnectionError;
                ShowConnectionError(message);
                yield break;
            }

            BindCampusSessionState();
            ShowCampus();
        }

        private void BindCampusSessionState()
        {
            var state = CampusSessionState.Instance;
            if (state == null)
            {
                return;
            }

            if (networkManager != null && networkManager.IsServer)
            {
                state.BindHostSession(_session);
                state.ServerSyncPlayerCount(networkManager.ConnectedClientsIds.Count);
            }
            else if (networkManager != null && networkManager.IsConnectedClient)
            {
                state.Changed -= HandleCampusSessionChanged;
                state.Changed += HandleCampusSessionChanged;
                state.ApplyToGameSession(_session);
            }
        }

        private void UnbindCampusSessionState()
        {
            var state = CampusSessionState.Instance;
            if (state != null)
            {
                state.UnbindHostSession();
                state.Changed -= HandleCampusSessionChanged;
            }

            _session.ClearNetworkReadModel();
        }

        private void HandleCampusSessionChanged()
        {
            CampusSessionState.Instance?.ApplyToGameSession(_session);
            RefreshInstructionStrip();
        }

        private void HandleSessionChanged()
        {
            RefreshInstructionStrip();
        }

        private void HandleClientConnectionLost()
        {
            // U7: CancelCeremony now carries world/camera/drag teardown duties,
            // so it runs unconditionally (it is a safe no-op when idle) — a
            // disconnect mid-reveal-cinematic or mid-drag tears down cleanly.
            CancelCeremony();

            // Session-scoped flags reset on disconnect (System-Wide Impact note).
            FirstRunGuideBeat.ResetSessionFlag();

            UnbindCampusSessionState();
            if (networkManager != null && (networkManager.IsHost || networkManager.IsClient || networkManager.IsServer))
            {
                networkManager.Shutdown();
            }

            ShowConnectionError("The host disconnected. Start a new game or join again.");
        }

        public void ShowCampus()
        {
            if (_ceremonyActive)
            {
                return;
            }

            _router.ShowCampus(_session);
            _world.ShowCampus(_session);
            _hub.Show(_session, this);
            // R18/P19: earned-badge city pieces join the skyline on every
            // campus entry; pieces that are new this session arrive with the
            // fanfare (sparkle + cue + camera nudge) exactly once.
            CampusEvolutionController.Mount(
                _world.WorldRoot,
                _session,
                _cityPieceFanfares,
                _world.CameraDirector,
                () => _hub != null && _hub.Player != null ? _hub.Player.transform : null);
            ResetRoot();
            MountCampusHud();
            MountEmoteBar();

            if (UsesPlayInstructionStrip)
            {
                MountInstructionStrip();
            }
            else
            {
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
            }

            // U6 passport entry point: a kid-large corner button in every campus
            // mode (the instruction-strip play path has no action bar), reaching
            // the tabbed passport the same way the gallery is reached.
            MountPassportButton();

            // U9: the guided Party Run panel mounts here ONLY when a run is
            // active (free-choice campus shows nothing extra). The campus doors
            // stay free-choice regardless — the panel is a presenter overlay.
            _partyRunPresenter?.MountOnCampus(_root);

            // U9: re-push the classroom access flags so a CameraDirector created
            // by this campus build adopts the current reduced-motion mode.
            ApplyClassroomAccess();

            AttachDebug();
        }

        /// <summary>
        /// U13 (U9 owner-review fold): the campus top HUD is a DESIGN.md paper
        /// card — compact, player-facing only: avatar identity chip (sprite +
        /// name + role vibe), the badge progress meter (three chips + count),
        /// and ONE short controls hint. Utility/debug text ('Free Campus',
        /// 'Mode: Play / None', the Future-buildings list) is gone from the
        /// player HUD; mode/connection state lives in DemoDebugOverlay
        /// (BackQuote), where debug info belongs.
        /// </summary>
        private void MountCampusHud()
        {
            var hud = UiBuilder.Panel(_root, "CampusHud", QuestStageUi.Paper);
            UiBuilder.Place(hud, 0f, 288f, 1000f, 74f);

            // Path Gold base stripe grounds the card (DESIGN quest-card stripe).
            UiBuilder.Shape(hud, "CampusHudStripe", QuestStageUi.PathGold, 0f, -33f, 1000f, 4f);

            // --- Avatar identity chip (left) ---
            var avatar = _session.SelectedAvatar;
            UiBuilder.Circle(hud, "CampusAvatarRing", avatar.AccentColor, -448f, 0f, 56f, 56f);
            var avatarSprite = AssetCatalog.SpriteFor(avatar.SpriteAssetId);
            if (avatarSprite != null)
            {
                var iconObject = new GameObject("CampusAvatarIcon", typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(hud, false);
                var icon = iconObject.GetComponent<Image>();
                icon.sprite = avatarSprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                UiBuilder.Place(iconObject.GetComponent<RectTransform>(), -448f, 2f, 46f, 46f);
            }

            var avatarName = UiBuilder.Text(hud, "CampusAvatarName", avatar.DisplayName, 24, TextAnchor.MiddleLeft, QuestStageUi.Ink, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(avatarName.rectTransform, -290f, 12f, 250f, 32f);

            var avatarRole = UiBuilder.Text(hud, "CampusAvatarRole", avatar.Role, 14, TextAnchor.MiddleLeft, new Color(0.27f, 0.36f, 0.4f));
            UiBuilder.Place(avatarRole.rectTransform, -290f, -14f, 250f, 22f);

            // --- Badge progress meter (center): three chips + count ---
            var earnedCount = Mathf.Clamp(_session.UniqueCompletedGames, 0, 3);
            for (var slot = 0; slot < 3; slot++)
            {
                var filled = slot < earnedCount;
                var x = -20f + slot * 40f;
                UiBuilder.Circle(hud, $"CampusBadgeChip{slot}Ring", filled ? QuestStageUi.PathGold : QuestStageUi.PaperShadow, x, 2f, 30f, 30f);
                UiBuilder.Circle(hud, $"CampusBadgeChip{slot}", filled ? QuestStageUi.PathGold : QuestStageUi.Paper, x, 2f, 22f, 22f);
            }

            var badgeLabel = UiBuilder.Text(hud, "CampusBadgeMeterLabel", $"{earnedCount}/3 badges", 16, TextAnchor.MiddleLeft, QuestStageUi.Ink, TypeRole.Body, TypeWeight.SemiBold);
            UiBuilder.Place(badgeLabel.rectTransform, 130f, 2f, 130f, 26f);

            // --- ONE short controls hint (right); details live in the pause menu/strip ---
            var controls = UiBuilder.Text(hud, "CampusControlsHint", "Move: WASD · Walk into a door", 15, TextAnchor.MiddleRight, new Color(0.27f, 0.36f, 0.4f));
            UiBuilder.Place(controls.rectTransform, 332f, 0f, 300f, 26f);
        }

        /// <summary>
        /// P16 emote bar: three kid-large icon buttons (heart/star/wave) that
        /// send FIXED emote IDs through EmoteRelay — no text exists anywhere in
        /// the path (no-chat privacy boundary). Visible only when networked:
        /// in solo there is no partner to wave at, so the bar is hidden (hub
        /// toys carry solo delight). Mounted with the campus HUD, so it lives
        /// and dies with the hub route (rooms never show it).
        /// </summary>
        private void MountEmoteBar()
        {
            var relay = EmoteRelay.Instance;
            var networked = networkManager != null && (networkManager.IsHost || networkManager.IsConnectedClient);
            if (relay == null || !relay.IsSpawned || !networked)
            {
                return;
            }

            var bar = UiBuilder.Panel(_root, "EmoteBar", new Color(0.93f, 0.98f, 0.95f, 0.78f));
            UiBuilder.Place(bar, -494f, -224f, 252f, 96f);

            var label = UiBuilder.Text(bar, "EmoteBarLabel", "Send a cheer!", 14, TextAnchor.MiddleCenter, new Color(0.1f, 0.2f, 0.14f));
            UiBuilder.Place(label.rectTransform, 0f, 36f, 240f, 22f);

            AddEmoteButton(bar, EmoteId.Heart, -80f);
            AddEmoteButton(bar, EmoteId.Star, 0f);
            AddEmoteButton(bar, EmoteId.Wave, 80f);
        }

        private static void AddEmoteButton(RectTransform bar, EmoteId emote, float x)
        {
            var button = UiBuilder.Button(bar, $"Emote{emote}Button", string.Empty, () => EmoteRelay.Instance?.SendEmote(emote));
            UiBuilder.Place(button.GetComponent<RectTransform>(), x, -10f, 64f, 64f); // kid-large (≥44px)
            button.GetComponent<Image>().color = new Color(1f, 0.969f, 0.878f, 0.95f); // DESIGN Paper

            var iconObject = new GameObject($"Emote{emote}Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(button.transform, false);
            var icon = iconObject.GetComponent<Image>();
            icon.sprite = EmoteBubble.SpriteFor(emote);
            icon.color = EmoteBubble.IconTint(emote);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UiBuilder.Place(iconObject.GetComponent<RectTransform>(), 0f, 0f, 48f, 48f);
        }

        public void ShowShowcaseProofBeat()
        {
            _hub.Hide();
            _router.ShowShowcaseProof(_session);
            _world.ShowProof(_session);
            ResetRoot();
            var panel = UiBuilder.FullPanel(_root, "ShowcaseProofPanel", new Color(0.86f, 0.91f, 1f));
            var title = UiBuilder.Text(panel, "ProofTitle", "Two-Client Proof", 40, TextAnchor.MiddleCenter, new Color(0.08f, 0.12f, 0.25f), TypeRole.Display, TypeWeight.SemiBold);
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
            if (_ceremonyActive)
            {
                return;
            }

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

            MountInstructionStrip();
            AttachDebug();
        }

        public void ShowHealthHero()
        {
            if (_ceremonyActive)
            {
                return;
            }

            _hub.Hide();
            _router.ShowActivity(_session, ActivityRoute.HealthHero);
            _world.ShowClinic(_session);
            ResetRoot();
            var controller = gameObject.GetComponent<HealthHeroController>() ?? gameObject.AddComponent<HealthHeroController>();
            controller.Render(_root, _session, this, CurrentResultSource());
            MountInstructionStrip();
            AttachDebug();
        }

        public void ShowLogicCourt()
        {
            if (_ceremonyActive)
            {
                return;
            }

            _hub.Hide();
            _router.ShowActivity(_session, ActivityRoute.LogicCourt);
            _world.ShowCourt(_session);
            ResetRoot();
            var controller = gameObject.GetComponent<LogicCourtController>() ?? gameObject.AddComponent<LogicCourtController>();
            controller.Render(_root, _session, this, CurrentResultSource());
            MountInstructionStrip();
            AttachDebug();
        }

        // U5: the three remaining legacy optional rooms join Robotics on the
        // real station surface — every legacy entry point (hub door, QA
        // states) lands on the generic station branch, and the
        // OptionalRoomController bridge retires with them.
        public void ShowAiLab()
        {
            ShowPartyStation(CareerQuestCatalog.AiLabId);
        }

        public void ShowMusicStudio()
        {
            ShowPartyStation(CareerQuestCatalog.MusicStudioId);
        }

        public void ShowRoboticsGarage()
        {
            // U4 (KTD6): Robotics Rescue is the first converted Party Pack
            // station — every legacy entry point (hub door, QA states) lands on
            // the real station surface through the generic station branch.
            ShowPartyStation(CareerQuestCatalog.RoboticsGarageId);
        }

        public void ShowCommunityKitchen()
        {
            ShowPartyStation(CareerQuestCatalog.CommunityKitchenId);
        }

        /// <summary>
        /// U2 generic station branch (KTD3): the ONE mount path for every Party
        /// Pack station, keyed by station id — never one method per station.
        /// U5 converted the last legacy optional rooms, so every Party Pack id
        /// mounts the real station surface here. Returns false for
        /// unknown/non-station ids.
        /// </summary>
        public bool ShowPartyStation(string stationId)
        {
            if (_ceremonyActive)
            {
                return false;
            }

            if (!CareerQuestCatalog.IsPartyStationId(stationId) || !CareerQuestCatalog.TryGetById(stationId, out var entry))
            {
                return false;
            }

            _hub.Hide();
            _router.ShowPartyStation(_session, stationId);
            _world.ShowPartyStation(_session, entry);
            ResetRoot();
            // U9: a station-entry camera adopts the current reduced-motion mode.
            ApplyClassroomAccess();
            MountPartyStationSurface(entry);
            MountInstructionStrip();
            AttachDebug();
            return true;
        }

        /// <summary>
        /// U4: mounts the real station play surface (PartyStationController —
        /// definition-driven render, seed selection, toy play, hint ladder, and
        /// exactly one MiniGameResult through the duplicate gate) for every
        /// station-id routed entry. The U2 routing path above is untouched.
        /// </summary>
        private void MountPartyStationSurface(CatalogEntry entry)
        {
            var controller = gameObject.GetComponent<PartyStationController>() ?? gameObject.AddComponent<PartyStationController>();

            // U6 reward seam: every station completion appends exactly one
            // reward event to the session log (R11). The controller is reused
            // across mounts, so subscribe once (unsubscribe-then-subscribe is
            // idempotent) and feed the session, never a second scoring channel.
            if (_partyStation != controller)
            {
                if (_partyStation != null)
                {
                    _partyStation.RewardEventEmitted -= HandleStationRewardEvent;
                }

                controller.RewardEventEmitted -= HandleStationRewardEvent;
                controller.RewardEventEmitted += HandleStationRewardEvent;
                _partyStation = controller;
            }

            controller.Render(_root, _session, this, CurrentResultSource(), entry.Id);
        }

        /// <summary>
        /// U6: one reward event per station completion. The session log appends
        /// it (combo-spark eligibility derives from the completed set), then the
        /// station-end accessory spotlight plays the "you unlocked X!" beat over
        /// the room. Presentation only (KTD8) — scoring already happened.
        /// </summary>
        private void HandleStationRewardEvent(StationRewardEvent stationEvent)
        {
            var rewardEvent = _session.AppendStationRewardEvent(stationEvent);

            // U9 (KTD7): advance the guided run ONLY when this completion matches
            // the run's current round. A free-choice or out-of-order completion
            // never moves the sequence (PartyRunState owns the guard), so normal
            // campus play stays fully independent of an active run.
            _session.PartyRun.NoteStationCompleted(stationEvent.StationId);

            ShowAccessorySpotlight(rewardEvent);
        }

        private void ShowAccessorySpotlight(RewardEvent rewardEvent)
        {
            if (rewardEvent == null || _root == null)
            {
                return;
            }

            _accessorySpotlight ??= gameObject.GetComponent<AccessorySpotlightController>()
                ?? gameObject.AddComponent<AccessorySpotlightController>();

            // U9: quiet mode (calm classroom/party run) gates the spotlight pulse
            // + auto-dismiss; the card still renders so the unlock reads clearly.
            _accessorySpotlight.Show(_root, rewardEvent, _session.ClassroomAccess.QuietMode);
        }

        public void ShowGallery()
        {
            if (_ceremonyActive)
            {
                return;
            }

            ShowGalleryInternal();
        }

        /// <summary>
        /// U6 Quest Passport surface: the tabbed Badges/Gear/Combos/Results book,
        /// all session-derived. It shares the Gallery route + phase (it is the
        /// gallery's richer cousin — both are "book" surfaces), so no new
        /// ActivityRoute value or replicated route int is introduced. Reached the
        /// same way the gallery is (a campus HUD button + a gallery cross-link).
        /// </summary>
        public void ShowPassport()
        {
            ShowPassport(PassportController.PassportPage.Badges);
        }

        public void ShowPassport(PassportController.PassportPage page)
        {
            if (_ceremonyActive)
            {
                return;
            }

            _hub.Hide();
            _router.ShowGallery(_session);
            _world.ShowGallery(_session);
            ResetRoot();
            _passport ??= gameObject.GetComponent<PassportController>() ?? gameObject.AddComponent<PassportController>();
            _passport.Render(_root, _session, this, page);
            AttachDebug();
        }

        public void ShowReveal()
        {
            if (_ceremonyActive)
            {
                return;
            }

            _hub.Hide();
            _router.ShowReveal(_session);
            _world.ShowReveal(_session);
            ResetRoot();
            // U9: reduced motion reaches the reveal cinematic camera (the flourish
            // tween snaps; the result copy over the lit stage still reads).
            ApplyClassroomAccess();
            AnnounceRevealStartIfHost();
            _reveal.Render(_root, _session, this);
            AttachDebug();
        }

        /// <summary>
        /// U7 sync moment: the host announces the reveal start through
        /// CampusSessionState. Only clients already on the reveal route consume
        /// it (as one input of their start latch) — nobody is force-navigated.
        /// </summary>
        private void AnnounceRevealStartIfHost()
        {
            if (!_session.RevealReady)
            {
                return;
            }

            var state = CampusSessionState.Instance;
            if (state != null && networkManager != null && networkManager.IsServer)
            {
                state.ServerAnnounceRevealStart();
            }
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

        /// <summary>
        /// U6: the always-available campus Passport button (top-right corner,
        /// under the Exit button). Both campus modes mount it, so the passport
        /// is reachable in normal play where the action bar is absent.
        /// </summary>
        private void MountPassportButton()
        {
            var passport = UiBuilder.SmallButton(_root, "CampusPassportButton", "Passport", ShowPassport);
            UiBuilder.Place(passport.GetComponent<RectTransform>(), 535f, 268f, 150f, 42f);
            QuestStageUi.StyleSecondaryButton(passport);
        }

        private static void StyleConnectionButton(Button button, Color color, int fontSize)
        {
            button.GetComponent<Image>().color = color;
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null)
            {
                return;
            }

            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = 16;
            label.fontSizeMax = fontSize;
        }

        private void ResetRoot()
        {
            // U7 single-teardown chokepoint: every route change (exit actions,
            // disconnect → connection screen, re-renders) flows through here,
            // so a live reveal cinematic always stops, active drags cancel, and
            // the camera restores before the next screen mounts.
            _reveal?.CancelCinematic();

            // U6: the station-end accessory spotlight is a _root child — drop it
            // explicitly so its active state never outlives the surface.
            _accessorySpotlight?.Dismiss();

            UiBuilder.Clear(_root);
            _instructionStripText = null;
        }

        private bool UsesPlayInstructionStrip => InstructionStrip.ShouldShowForMode(_session.Mode);

        private void EnsureSessionChangedSubscription()
        {
            if (_session == null || _sessionChangedSubscribed)
            {
                return;
            }

            _session.Changed += HandleSessionChanged;
            _sessionChangedSubscribed = true;
        }

        private void UnsubscribeSessionChanged()
        {
            if (_session == null || !_sessionChangedSubscribed)
            {
                return;
            }

            _session.Changed -= HandleSessionChanged;
            _sessionChangedSubscribed = false;
        }

        private void MountInstructionStrip()
        {
            if (!UsesPlayInstructionStrip || _ceremonyActive || _root == null)
            {
                return;
            }

            _instructionStripText = InstructionStrip.Build(_root, _session, _router.CurrentStationId);
        }

        private void RefreshInstructionStrip()
        {
            if (_instructionStripText == null)
            {
                return;
            }

            InstructionStrip.Refresh(_instructionStripText, _session, _router.CurrentStationId);
        }

        private void HideInstructionStrip()
        {
            if (_instructionStripText != null)
            {
                var panel = _instructionStripText.transform.parent;
                if (panel != null)
                {
                    Destroy(panel.gameObject);
                }

                _instructionStripText = null;
                return;
            }

            var existing = _root != null ? _root.Find(InstructionStrip.PanelName) : null;
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }
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
            var visualState = ValueAfter(args, "-cq-visual-state");
            if (!string.IsNullOrWhiteSpace(visualState))
            {
                StartCoroutine(RunCommandLineVisualState(visualState, ValueAfter(args, "-cq-screenshot")));
                return true;
            }

            if (Array.IndexOf(args, "-cq-smoke") < 0)
            {
                return false;
            }

            var mode = ValueAfter(args, "-cq-mode") ?? "solo";
            StartCoroutine(RunCommandLineSmoke(mode));
            return true;
        }

        private IEnumerator RunCommandLineVisualState(string state, string screenshotPath)
        {
            Debug.Log($"CQ_VISUAL_STATE_START state={state}");
            var shown = ShowVisualQaState(state);
            Debug.Log($"CQ_VISUAL_STATE_READY state={state} shown={shown} route={_session.CurrentRoute} revealReady={_session.RevealReady}");

            if (shown && !string.IsNullOrWhiteSpace(screenshotPath))
            {
                // U7: the reveal is now an in-world cinematic (~5.6s of beats
                // after the stage mounts) — wait past resolve so the screenshot
                // captures the result copy over the lit stage, not mid-beat.
                var waitSeconds = state.Contains("reveal", System.StringComparison.OrdinalIgnoreCase) ? 7.5f : 2f;
                yield return new WaitForSeconds(waitSeconds);
                yield return new WaitForEndOfFrame();
                var directory = Path.GetDirectoryName(screenshotPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                ScreenCapture.CaptureScreenshot(screenshotPath);
                Debug.Log($"CQ_VISUAL_STATE_SCREENSHOT state={state} path={screenshotPath}");
            }

            yield return new WaitForSeconds(shown && !string.IsNullOrWhiteSpace(screenshotPath) ? 0.5f : 2f);
            Application.Quit(shown ? 0 : 2);
        }

        private IEnumerator RunCommandLineSmoke(string mode)
        {
            Debug.Log($"CQ_SMOKE_START mode={mode}");

            switch (mode.ToLowerInvariant())
            {
                case "host":
                    yield return ConnectAndShowCampus(ConnectionMode.HostP1, 1, () => _networkBootstrap.StartHostP1());
                    if (!_networkBootstrap.LastConnectionSucceeded)
                    {
                        Application.Quit(2);
                        yield break;
                    }

                    yield return new WaitForSeconds(6f);
                    break;
                case "client":
                    yield return new WaitForSeconds(1f);
                    yield return ConnectAndShowCampus(ConnectionMode.JoinLocalhostP2, 2, () => _networkBootstrap.JoinLocalhostP2());
                    if (!_networkBootstrap.LastConnectionSucceeded)
                    {
                        Application.Quit(2);
                        yield break;
                    }

                    yield return new WaitForSeconds(2f);
                    LogSmoke("CQ_SMOKE_CONNECTED", mode);
                    yield return new WaitForSeconds(6f);
                    break;
                case "2p-host":
                case "2p-client":
                {
                    // Automated 2P matrix evidence (docs/qa rows a–f): the
                    // dedicated harness owns its CQ_2P_* log lines and exit code.
                    var matrix = gameObject.GetComponent<TwoPlayerMatrixSmoke>() ?? gameObject.AddComponent<TwoPlayerMatrixSmoke>();
                    yield return matrix.Run(this, mode);
                    yield break;
                }

                case "showcase":
                    BeginShowcase();
                    yield return new WaitForSeconds(7f);
                    break;
                default:
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

        private void SeedVisualQaResults()
        {
            foreach (var result in ShowcaseSeedConfig.CreativeTechnicalBuilderResults())
            {
                _session.RecordResult(new MiniGameResult(
                    result.ActivityId,
                    result.DisplayName,
                    result.Tier,
                    ResultSource.Solo,
                    result.TraitDeltas,
                    result.TimeRemaining,
                    result.Accuracy,
                    result.Summary));
            }
        }

        /// <summary>
        /// QA-only seed for the accessory-fit visual state: completes the party
        /// stations (pass a station id to skip one, or null to complete all), so
        /// AccessoryResolver derives the one-per-slot set the campus avatar
        /// renders. Mirrors the DemoDebugOverlay proof seam; session-only and
        /// presentation-only (KTD8/KTD12).
        /// </summary>
        private void SeedAccessoryFitResults(string exceptStationId)
        {
            foreach (var stationId in CareerQuestCatalog.PartyStationIds)
            {
                if (stationId == exceptStationId)
                {
                    continue;
                }

                var definition = PartyStationDefinitions.GetById(stationId);
                _session.RecordResult(PartyStationController.BuildResult(
                    definition,
                    definition.DefaultSeed,
                    ResultSource.Solo,
                    complete: true,
                    wrongAttempts: 0,
                    playElapsedSeconds: 12f));
            }
        }

        public void CompleteActivity(MiniGameResult result)
        {
            if (result == null || _ceremonyActive)
            {
                return;
            }

            var lifecycle = new ActivityLifecycle(result.ActivityId);
            lifecycle.MarkComplete();

            var emitter = new ActivityResultEmitter();
            if (!emitter.TryRecord(_session, lifecycle.State, result) && _session.GetBestResult(result.ActivityId) == null)
            {
                return;
            }

            lifecycle.BeginCeremony();
            _router.BeginCeremony(_session);
            _ceremonyCoroutine = StartCoroutine(RunCeremony(result));
        }

        private void ShowGalleryInternal()
        {
            _hub.Hide();
            _router.ShowGallery(_session);
            _world.ShowGallery(_session);
            ResetRoot();
            _gallery.Render(_root, _session, this);
            AttachDebug();
        }

        private IEnumerator RunCeremony(MiniGameResult result)
        {
            _ceremonyActive = true;
            HideInstructionStrip();
            var presentation = FeedbackController.ForResult(result);
            _ceremonyController = new CeremonyController(result);
            BuildCeremonyOverlay(presentation);

            // U8: the fanfare rides the director's dedicated stoppable source —
            // skip (and any cancel path) ducks it via TearDownCeremonyOverlay.
            _audioDirector ??= AudioDirector.AttachTo(gameObject);
            _audioDirector.PlayFanfare(presentation.CueId);
            _lastCeremonySubPhase = CeremonySubPhase.Celebration;

            while (!_ceremonyController.IsComplete)
            {
                _ceremonyController.Tick(Time.unscaledDeltaTime);
                PlayCeremonySubPhaseCues();
                UpdateCeremonyOverlay(presentation);
                if (_ceremonySkipButton != null)
                {
                    _ceremonySkipButton.interactable = _ceremonyController.CanSkip;
                }

                yield return null;
            }

            TearDownCeremonyOverlay();
            _ceremonyActive = false;
            _ceremonyController = null;
            _ceremonyCoroutine = null;
            ShowGalleryInternal();
        }

        private void BuildCeremonyOverlay(CeremonyPresentation presentation)
        {
            var overlayRect = UiBuilder.FullPanel(_root, "CeremonyOverlay", QuestStageUi.StageNight);
            _ceremonyOverlay = overlayRect.gameObject;
            // Modal opt-in: the ceremony overlay DOES block pointer raycasts so
            // the room beneath (drag pieces, exit buttons) is unreachable while
            // the ceremony plays (UiBuilder panels default non-blocking in U6).
            _ceremonyOverlay.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;

            QuestStageUi.MountStageBackdrop(overlayRect, unlocked: true);

            var card = UiBuilder.Panel(overlayRect, "CeremonyCard", QuestStageUi.Paper);
            UiBuilder.Place(card, 0f, 20f, 780f, 440f);

            var stripe = UiBuilder.Panel(card, "CeremonyStripe", presentation.AccentColor);
            UiBuilder.Place(stripe, 0f, 198f, 780f, 10f);

            _ceremonyTitleText = UiBuilder.Text(card, "CeremonyTitle", presentation.Title, 40, TextAnchor.MiddleCenter, QuestStageUi.Ink, TypeRole.Display, TypeWeight.Bold);
            UiBuilder.Place(_ceremonyTitleText.rectTransform, 0f, 150f, 700f, 52f);

            _ceremonyBadgeStamp = UiBuilder.Circle(card, "CeremonyBadgeStamp", presentation.AccentColor, 0f, 40f, 120f, 120f);

            _ceremonyBadgeText = UiBuilder.Text(card, "CeremonyBadge", presentation.BadgeLabel, 20, TextAnchor.MiddleCenter, Color.white);
            UiBuilder.Place(_ceremonyBadgeText.rectTransform, 0f, 40f, 110f, 36f);

            _ceremonyMessageText = UiBuilder.Text(card, "CeremonyMessage", presentation.Message, 20, TextAnchor.MiddleCenter, QuestStageUi.Ink);
            UiBuilder.Place(_ceremonyMessageText.rectTransform, 0f, -70f, 680f, 80f);

            _ceremonySkipButton = UiBuilder.Button(card, "CeremonySkipButton", "Skip", () =>
            {
                if (_ceremonyController != null && _ceremonyController.CanSkip)
                {
                    _ceremonyController.Skip();
                }
            });
            UiBuilder.Place(_ceremonySkipButton.GetComponent<RectTransform>(), 0f, -165f, 200f, 48f);
            QuestStageUi.StyleSecondaryButton(_ceremonySkipButton);
            _ceremonySkipButton.interactable = false;

            SpawnCeremonyConfetti(presentation);
        }

        /// <summary>
        /// P1: the ceremony celebration moment uses a real ParticleSystem (no
        /// hand-rolled confetti). The bursts live in world space flanking the
        /// ceremony card; the overlay's full-screen backdrop is alpha-clamped
        /// translucent (UiBuilder.FullPanel), so they read through it. Tracked so
        /// the single teardown can drop them early on skip/cancel.
        /// </summary>
        private void SpawnCeremonyConfetti(CeremonyPresentation presentation)
        {
            var director = _world != null ? _world.CameraDirector : null;
            if (director == null)
            {
                return;
            }

            var shot = director.Camera.transform.position;
            _ceremonyConfetti.Add(ParticlePoof.ConfettiBurst(
                new Vector3(shot.x - 2.7f, shot.y - 1.8f, 0f), QuestStageUi.PathGold, presentation.AccentColor));
            _ceremonyConfetti.Add(ParticlePoof.ConfettiBurst(
                new Vector3(shot.x + 2.7f, shot.y - 1.8f, 0f), presentation.AccentColor, QuestStageUi.PathGold, 36));
        }

        /// <summary>U8: the badge-stamp thunk lands as the Feedback subphase begins.</summary>
        private void PlayCeremonySubPhaseCues()
        {
            if (_ceremonyController == null)
            {
                return;
            }

            var subPhase = _ceremonyController.CurrentSubPhase;
            if (subPhase != _lastCeremonySubPhase && subPhase == CeremonySubPhase.Feedback)
            {
                AudioCueCatalog.TryPlay(AudioCueIds.BadgeStamp);
            }

            _lastCeremonySubPhase = subPhase;
        }

        private void UpdateCeremonyOverlay(CeremonyPresentation presentation)
        {
            if (_ceremonyController == null || _ceremonyMessageText == null)
            {
                return;
            }

            _ceremonyMessageText.text = _ceremonyController.CurrentSubPhase switch
            {
                CeremonySubPhase.Celebration => presentation.Message,
                CeremonySubPhase.Feedback => "Badge stamped! Your quest passport just got a new sticker.",
                CeremonySubPhase.Transition => "Opening your achievement gallery...",
                _ => presentation.Message
            };

            if (_ceremonyBadgeStamp != null)
            {
                var pulse = 1f + Mathf.Sin(_ceremonyController.ElapsedSeconds * 4f) * 0.06f;
                _ceremonyBadgeStamp.localScale = new Vector3(pulse, pulse, 1f);
            }
        }

        private void TearDownCeremonyOverlay()
        {
            // U8: any ceremony end (natural, skip, cancel/disconnect) ducks a
            // still-playing fanfare — the next screen never inherits it.
            _audioDirector?.StopFanfare();

            if (_ceremonyOverlay != null)
            {
                Destroy(_ceremonyOverlay);
                _ceremonyOverlay = null;
            }

            // Confetti systems self-destroy after 3s; an early teardown
            // (skip/cancel/disconnect) must not leave them playing over the
            // next screen.
            foreach (var confetti in _ceremonyConfetti)
            {
                if (confetti != null)
                {
                    Destroy(confetti);
                }
            }

            _ceremonyConfetti.Clear();

            _ceremonyTitleText = null;
            _ceremonyMessageText = null;
            _ceremonyBadgeText = null;
            _ceremonyBadgeStamp = null;
            _ceremonySkipButton = null;
        }

        private void CancelCeremony()
        {
            if (_ceremonyCoroutine != null)
            {
                StopCoroutine(_ceremonyCoroutine);
                _ceremonyCoroutine = null;
            }

            TearDownCeremonyOverlay();
            _ceremonyActive = false;
            _ceremonyController = null;

            // U7: this path gained world/camera/drag teardown duties beyond the
            // UI-only cleanup — disconnects mid-cinematic or mid-drag restore a
            // known camera shot and never strand a dragged piece.
            DraggablePiece.CancelActiveDrag();
            _reveal?.CancelCinematic();
            _world?.CameraDirector?.ResetToRouteShot();
        }

        /// <summary>
        /// Ceremony skip seam (locked pacing contract unchanged: available
        /// after 3s, ceremony completes at 12s). The overlay button and the
        /// PlayMode tests share this path.
        /// </summary>
        public bool TrySkipCeremony()
        {
            if (_ceremonyController == null || !_ceremonyController.CanSkip)
            {
                return false;
            }

            _ceremonyController.Skip();
            return true;
        }
    }
}
