using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// U6 reward seam payload (U4): exactly ONE of these fires per station
    /// completion, right when the normal MiniGameResult is emitted. It carries
    /// the presentation facts the session reward event log (U6) needs beyond
    /// the result contract — the selected seed id and the derived accessory id.
    /// U4 deliberately does NOT build RewardEventLog; U6 subscribes to
    /// <see cref="PartyStationController.RewardEventEmitted"/> and owns storage.
    /// </summary>
    public readonly struct StationRewardEvent
    {
        public StationRewardEvent(
            string stationId,
            string seedId,
            CompletionTier tier,
            ResultSource source,
            string summary,
            string accessoryRewardId,
            IReadOnlyList<TraitDelta> traitDeltas)
        {
            StationId = stationId;
            SeedId = seedId;
            Tier = tier;
            Source = source;
            Summary = summary;
            AccessoryRewardId = accessoryRewardId;
            TraitDeltas = traitDeltas;
        }

        public string StationId { get; }
        public string SeedId { get; }
        public CompletionTier Tier { get; }
        public ResultSource Source { get; }
        public string Summary { get; }
        public string AccessoryRewardId { get; }
        public IReadOnlyList<TraitDelta> TraitDeltas { get; }
    }

    /// <summary>
    /// U4: the ONE reusable Party Pack station owner (KTD4/KTD6) — proven with
    /// Robotics Rescue before content multiplies. Consumes a
    /// <see cref="PartyStationDefinition"/> + <see cref="ToyPatternController"/>
    /// and owns: seed selection (default on first play, choice on replay),
    /// the guide intro beat + reward preview, the toy playfield over
    /// <see cref="ToyInteractionKit"/>, the hint ladder + gentle rejects, 2P
    /// glue through <see cref="StationProgressNetworkState"/>, and exactly one
    /// normal MiniGameResult through the existing room lifecycle + duplicate
    /// gate. Station controllers never mutate reveal, gallery, ranking,
    /// accessories, or evolution directly (R9) — everything downstream derives
    /// from the result and the reward-event seam.
    ///
    /// All gameplay flows through the programmatic seams
    /// (<see cref="TrySubmitDrop"/>, <see cref="ChooseSeed"/>, state queries);
    /// the pointer shell is a thin layer over them, mirroring the converted
    /// drag rooms. Deterministic clock: Tick(deltaSeconds) drives the intro
    /// hold and idle-hint timing; Update only forwards Time.deltaTime when
    /// AutoTick is on (house idiom).
    /// </summary>
    public class PartyStationController : ActivityRoomController, IDragDropHost
    {
        /// <summary>Design doc Station intro rule: 3-5s theatrical beat before play.</summary>
        public const float IntroHoldSeconds = 3f;

        /// <summary>Design doc pacing rule upper bound — the result's time budget.</summary>
        public const float PacingBudgetSeconds = 75f;

        public const string PanelName = "PartyStationPanel";
        public const string HudPrefix = "PartyStation";
        public const string SeedChoicePanelName = "StationSeedChoicePanel";
        public const string SeedChoiceButtonPrefix = "SeedChoice_";

        public const string GentleNoZoneFeedback = "Set the toy on a glowing spot to try it.";

        /// <summary>
        /// Legacy optional rooms already converted to the real station surface.
        /// U4 converts Robotics Rescue (the proof gate); U5 flips AI Lab,
        /// Music, and Kitchen, then this list retires with the legacy bridge.
        /// </summary>
        public static readonly string[] ConvertedLegacyStationIds = { CareerQuestCatalog.RoboticsGarageId };

        public static bool IsConvertedLegacyStation(string stationId)
        {
            return Array.IndexOf(ConvertedLegacyStationIds, stationId) >= 0;
        }

        private static readonly Color PathGold = new(0.953f, 0.769f, 0.357f);
        private static readonly Color HudPaper = new(1f, 0.97f, 0.86f, 0.94f);

        private readonly PartyStationRoomState _state = new();
        private readonly ToyInteractionKit _kit = new();
        private readonly HashSet<string> _renderedAccepted = new();

        private GameSession _session;
        private CareerQuestApp _app;
        private ResultSource _source;
        private PartyStationDefinition _definition;
        private PartyStationSeedDefinition _seed;
        private ToyPatternController _pattern;
        private StationProgressNetworkState _network;
        private bool _networkSubscribed;

        private Transform _uiParent;
        private TextMeshProUGUI _promptText;
        private TextMeshProUGUI _statusText;
        private StationGuideView _guide;
        private StationRewardPreview _rewardPreview;
        private RectTransform _seedChoicePanel;
        private Coroutine _playfieldRoutine;
        private Color _accent = PathGold;
        private bool _introComplete;
        private float _introElapsed;
        private float _playElapsed;
        private float _idleSeconds;
        private int _renderedHintLevel;
        private string _partnerHeldObjectId;

        public PartyStationRoomState State => _state;
        public PartyStationDefinition Definition => _definition;
        public PartyStationSeedDefinition Seed => _seed;
        public ToyPatternController Pattern => _pattern;

        /// <summary>Real-time clock toggle (house idiom). Tests set false and drive Tick.</summary>
        public bool AutoTick { get; set; } = true;

        /// <summary>
        /// Demo/proof pacing seam (R3): skips the intro hold so the station is
        /// playable immediately. It never touches the rules, the seed, or
        /// <see cref="BuildResult"/> — quick pacing is presentation only and
        /// can never change scoring.
        /// </summary>
        public bool QuickPacing { get; set; }

        public bool IsIntroComplete => _introComplete;
        public bool IsSeedChoiceOpen { get; private set; }

        public event Action<MiniGameResult> Completed;

        /// <summary>U6 seam: fired exactly once per completion (see <see cref="StationRewardEvent"/>).</summary>
        public event Action<StationRewardEvent> RewardEventEmitted;

        /// <summary>
        /// Single drag-lock flag: intro hold, emitted result, authoritative
        /// completion, and the ceremony all lock the surface. Drag handlers
        /// check it client-side; the host completion guard covers the server.
        /// </summary>
        public bool IsDragLocked =>
            !_introComplete
            || _pattern == null
            || _pattern.ResultEmitted
            || AuthoritativeComplete
            || (_app != null && _app.IsCeremonyActive);

        private bool UsesNetworkState =>
            _source == ResultSource.Multiplayer
            && _network != null
            && _network.IsSpawned
            && _definition != null
            && _network.HasActiveStation
            && _network.StationId == _definition.Id;

        private bool IsHostAuthority =>
            _source == ResultSource.Multiplayer && _network != null && _network.IsSpawned && _network.IsServer;

        private bool AuthoritativeComplete =>
            UsesNetworkState ? _network.Complete : _pattern != null && _pattern.Complete;

        /// <summary>0 = none, 1 = text clue, 2 = clue + toy highlight (shared in 2P).</summary>
        public int HintLevel => UsesNetworkState ? _network.HintLevel : _pattern?.HintLevel ?? 0;

        /// <summary>Seed copy for the current hint level — never authored here.</summary>
        public string CurrentHintLine =>
            _seed == null || HintLevel <= 0 ? null
            : HintLevel >= ToyPatternController.MaxHintLevel ? _seed.EscalationHintLine
            : _seed.HintLine;

        public string HighlightObjectId =>
            UsesNetworkState ? _network.HighlightObjectId : _pattern?.HighlightObjectId;

        /// <summary>Test/QA seam: the live piece object for a toy id (post-mount).</summary>
        public DraggablePiece PieceFor(string objectId)
        {
            return _kit.PieceFor(objectId);
        }

        /// <summary>Test/QA seam: the live drop zone for a target id (post-mount).</summary>
        public DropZone ZoneFor(string targetId)
        {
            return _kit.ZoneFor(targetId);
        }

        public bool IsToyAccepted(string objectId)
        {
            if (_pattern == null)
            {
                return false;
            }

            return UsesNetworkState ? _network.IsObjectAccepted(objectId) : _pattern.Rules.IsAccepted(objectId);
        }

        public void Render(Transform parent, GameSession session, CareerQuestApp app, ResultSource source, string stationId)
        {
            if (!PartyStationDefinitions.TryGetById(stationId, out var definition))
            {
                Debug.LogError($"PartyStationController: no station definition for '{stationId}'.");
                return;
            }

            BeginRoom(stationId);
            _session = session;
            _app = app;
            _source = source;
            _definition = definition;
            _accent = PartyStationRenderer.AccentFor(definition);
            _uiParent = parent;

            // Re-entry discipline: a previous surface (route churn, rerender)
            // can never leak drags, pulses, subscriptions, or coroutines into
            // this one.
            StopPlayfieldRoutine();
            UnsubscribeNetwork();
            TeardownSurface();

            _network = CampusSessionState.Instance != null ? CampusSessionState.Instance.StationProgress : null;

            UiBuilder.FullPanel(parent, PanelName, new Color(0.93f, 0.97f, 1f, 0.04f));
            var refs = ActivityRoomChrome.MountQuestHud(
                parent,
                HudPrefix,
                HudPaper,
                _accent,
                definition.DisplayName,
                definition.Prompt,
                "Get ready...");
            _promptText = refs.Prompt;
            _statusText = refs.Status;

            var campus = UiBuilder.Button(parent, $"{HudPrefix}CampusButton", "Campus", ExitStationToCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 568f, -238f, 112f, 38f);
            ActivityRoomChrome.StyleButton(campus, ActivityRoomChrome.ButtonPrimary, 14);

            // 2P clients adopt the host's validated seed; otherwise first play
            // enters the default seed and a completed replay opens the choice.
            var adoptedSeed = !IsHostAuthority && _source == ResultSource.Multiplayer
                ? PartyStationRoomState.AdoptNetworkSeed(definition, _network)
                : null;
            if (adoptedSeed != null)
            {
                BeginSeed(adoptedSeed);
                return;
            }

            if (_state.ShouldOfferSeedChoice(definition, session))
            {
                ShowSeedChoice(parent);
                return;
            }

            BeginSeed(definition.DefaultSeed);
        }

        /// <summary>
        /// Replay seed selection seam — the choice buttons and the tests share
        /// it. Returns false when no choice is open or the seed id is unknown.
        /// </summary>
        public bool ChooseSeed(string seedId)
        {
            if (!IsSeedChoiceOpen || _definition == null || !_definition.TryGetSeed(seedId, out var seed))
            {
                return false;
            }

            IsSeedChoiceOpen = false;
            if (_seedChoicePanel != null)
            {
                Destroy(_seedChoicePanel.gameObject);
                _seedChoicePanel = null;
            }

            BeginSeed(seed);
            return true;
        }

        /// <summary>
        /// THE drop seam (same contract as the converted drag rooms). Drops
        /// resolve here in solo and multiplayer; the pointer shell and the
        /// tests both call it. Value is only meaningful for Meter toys.
        /// </summary>
        public DropSubmitResult TrySubmitDrop(string pieceId, string targetId, int value = 0)
        {
            if (_pattern == null)
            {
                return DropSubmitResult.RejectedUnknownPiece;
            }

            if (IsDragLocked)
            {
                return DropSubmitResult.RejectedLocked;
            }

            var objectDefinition = ObjectDefinitionFor(pieceId);
            if (objectDefinition == null)
            {
                return DropSubmitResult.RejectedUnknownPiece;
            }

            if (!objectDefinition.IsChainRole)
            {
                // Helper/Wildcard/Reaction/Bonus toys: react visibly, never
                // progress, never bounce (no-dead-toys rule).
                PlayToyReaction(pieceId);
                return DropSubmitResult.Accepted;
            }

            if (UsesNetworkState)
            {
                // Host and client surfaces submit through the same
                // authoritative core, so shared state is the single source of
                // truth (clients never complete optimistically).
                return SubmitThroughNetwork(pieceId, targetId, value);
            }

            // Solo: the exact rules the host validation core runs (KTD5).
            var result = _pattern.TrySubmitAction(new ToyAction(pieceId, targetId, value));
            switch (result.Kind)
            {
                case ToySubmissionKind.Accepted:
                    HandleToyAccepted(pieceId, celebrate: true);
                    TryAutoComplete();
                    return DropSubmitResult.Accepted;
                case ToySubmissionKind.ReactionOnly:
                    PlayToyReaction(pieceId);
                    return DropSubmitResult.Accepted;
                default:
                    return result.ToDropSubmitResult();
            }
        }

        /// <summary>
        /// Quick/demo completion seam (proof routes): plays the seed's golden
        /// action sequence through the normal drop seam — identical scoring,
        /// identical result path, zero special-case completion code.
        /// </summary>
        public bool TryCompleteWithGoldenSequence()
        {
            if (_pattern == null || IsDragLocked)
            {
                return false;
            }

            foreach (var action in _pattern.Rules.BuildGoldenActionSequence())
            {
                TrySubmitDrop(action.ObjectId, action.TargetId, action.Value);
            }

            return AuthoritativeComplete;
        }

        public MiniGameResult CreateResult(ResultSource source)
        {
            return BuildResult(_definition, _seed, source, AuthoritativeComplete, _pattern?.WrongAttempts ?? 0, _playElapsed);
        }

        /// <summary>
        /// Pure result contract for one station attempt — station id, display
        /// name, tier, source, time/accuracy, seed result summary, and the
        /// definition's trait deltas (R9/R10). Pacing flags are deliberately
        /// NOT an input: quick pacing can never change scoring.
        /// </summary>
        public static MiniGameResult BuildResult(
            PartyStationDefinition definition,
            PartyStationSeedDefinition seed,
            ResultSource source,
            bool complete,
            int wrongAttempts,
            float playElapsedSeconds)
        {
            if (definition == null)
            {
                return null;
            }

            seed ??= definition.DefaultSeed;
            var rules = ToyPatternRules.ForSeed(definition, seed);
            var requiredActions = rules.RequiredCount + rules.MeterObjectIds.Count;
            var totalAttempts = requiredActions + Mathf.Max(0, wrongAttempts);

            return new MiniGameResult(
                definition.Id,
                definition.DisplayName,
                complete ? CompletionTier.Degree : CompletionTier.Practice,
                source,
                definition.TraitDeltas,
                Mathf.Max(0f, PacingBudgetSeconds - Mathf.Max(0f, playElapsedSeconds)),
                totalAttempts == 0 ? 0f : Mathf.Clamp01((float)requiredActions / totalAttempts),
                complete
                    ? seed.ResultSummary
                    : $"You practiced at the {definition.DisplayName} station. Play again to finish the quest.");
        }

        /// <summary>Deterministic clock seam — intro hold and idle-hint timing.</summary>
        public void Tick(float deltaSeconds)
        {
            if (_pattern == null || deltaSeconds <= 0f || IsSeedChoiceOpen)
            {
                return;
            }

            if (_promptText == null)
            {
                return; // route torn down — a stale surface never self-ticks
            }

            if (!_introComplete)
            {
                _introElapsed += deltaSeconds;
                if (QuickPacing || _introElapsed >= IntroHoldSeconds)
                {
                    CompleteIntro();
                }

                return;
            }

            if (AuthoritativeComplete || _pattern.ResultEmitted)
            {
                return;
            }

            _playElapsed += deltaSeconds;

            if (UsesNetworkState)
            {
                // Shared hint ladder: the host escalates on shared idle time;
                // every peer renders the synced level/highlight (R16).
                if (IsHostAuthority && _network.HintLevel < ToyPatternController.MaxHintLevel)
                {
                    _idleSeconds += deltaSeconds;
                    if (_idleSeconds >= ToyPatternController.IdleHintSeconds)
                    {
                        _idleSeconds = 0f;
                        _network.ServerEscalateHint();
                    }
                }

                return;
            }

            _pattern.NoteIdle(deltaSeconds);
            if (_pattern.HintLevel != _renderedHintLevel)
            {
                RefreshHintPresentation();
            }
        }

        // ------------------------------------------------------------------
        // IDragDropHost — the pointer shell delegates every decision here.
        // ------------------------------------------------------------------

        public bool CanBeginDrag(string pieceId)
        {
            return !IsDragLocked && !IsToyAccepted(pieceId);
        }

        public void NotifyPickUp(string pieceId)
        {
            // A new pickup invalidates any in-flight submission so a late
            // reject for the old submission reads as stale (P21).
            _pattern?.InvalidatePendingSubmission(pieceId);

            if (UsesNetworkState)
            {
                _network.SetHeldPiece(pieceId); // P17 presence flag
            }

            AudioCueCatalog.TryPlay(AudioCueIds.DragPickup);
        }

        public void NotifyRelease(string pieceId)
        {
            if (UsesNetworkState)
            {
                _network.ClearHeldPiece();
            }
        }

        public void HandleDrop(DraggablePiece piece, DropZone zone)
        {
            if (piece == null)
            {
                return;
            }

            if (zone == null)
            {
                if (!AuthoritativeComplete)
                {
                    _guide?.ShowHint(GentleNoZoneFeedback);
                }

                piece.SnapToHome();
                return;
            }

            ToyInteractionKit.ApplyDropOutcome(piece, TrySubmitDrop(piece.PieceId, zone.ZoneId));
        }

        public bool WouldAcceptDrop(string pieceId, string zoneId)
        {
            return !IsDragLocked
                && !IsToyAccepted(pieceId)
                && _pattern != null
                && string.Equals(_pattern.Rules.ExpectedTargetFor(pieceId), zoneId, StringComparison.Ordinal);
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private void BeginSeed(PartyStationSeedDefinition seed)
        {
            if (_definition == null || seed == null)
            {
                return;
            }

            _seed = seed;
            _state.RecordSeedChoice(_definition.Id, seed.SeedId);

            _pattern = new ToyPatternController(_definition, seed);
            _pattern.Completed += HandlePatternCompleted;
            _pattern.ActionRejected += HandlePatternRejected;

            _introComplete = QuickPacing;
            _introElapsed = 0f;
            _playElapsed = 0f;
            _idleSeconds = 0f;
            _renderedHintLevel = 0;
            _renderedAccepted.Clear();
            _partnerHeldObjectId = null;
            _pattern.ExternalLock = !_introComplete;

            if (_source == ResultSource.Multiplayer && _network != null && _network.IsSpawned)
            {
                if (IsHostAuthority)
                {
                    PartyStationRoomState.HostBeginOrJoin(_network, _definition.Id, seed.SeedId);
                }

                _state.SyncedAttemptNumber = _network.AttemptNumber;
                _network.Changed += HandleNetworkChanged;
                _network.ActionRejected += HandleNetworkRejected;
                _networkSubscribed = true;
            }

            if (_promptText != null)
            {
                _promptText.text = _definition.ResolvePrompt(seed);
            }

            _guide = StationGuideView.Mount(_uiParent, _definition, seed, _accent);
            _guide?.ShowIntro();
            _rewardPreview = StationRewardPreview.Mount(_uiParent, _definition, seed, _accent);
            UpdateProgress();

            _playfieldRoutine = StartCoroutine(MountPlayfieldWhenRoomRevealed());
        }

        private void ShowSeedChoice(Transform parent)
        {
            IsSeedChoiceOpen = true;
            SetStatus("Pick a quest to play.");

            _seedChoicePanel = UiBuilder.Panel(parent, SeedChoicePanelName, new Color(1f, 0.97f, 0.88f, 0.97f));
            UiBuilder.Place(_seedChoicePanel, 0f, 20f, 640f, 240f);
            UiBuilder.Shape(_seedChoicePanel, "StationSeedChoiceStripe", _accent, 0f, 110f, 640f, 8f);

            var title = UiBuilder.Text(_seedChoicePanel, "StationSeedChoiceTitle", "Play it again — pick a quest!", 24, TextAnchor.MiddleCenter, new Color(0.098f, 0.196f, 0.235f), TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(title.rectTransform, 0f, 72f, 600f, 36f);

            var hint = UiBuilder.Text(_seedChoicePanel, "StationSeedChoiceHint", "Replay the quest you know, or try the remix.", 15, TextAnchor.MiddleCenter, new Color(0.27f, 0.36f, 0.4f));
            UiBuilder.Place(hint.rectTransform, 0f, 36f, 600f, 26f);

            var seeds = _definition.Seeds;
            for (var index = 0; index < seeds.Count; index++)
            {
                var seed = seeds[index];
                var button = UiBuilder.Button(
                    _seedChoicePanel,
                    $"{SeedChoiceButtonPrefix}{seed.SeedId}",
                    seed.DisplayName,
                    () => ChooseSeed(seed.SeedId));
                var x = seeds.Count <= 1 ? 0f : -155f + index * 310f;
                UiBuilder.Place(button.GetComponent<RectTransform>(), x, -48f, 280f, 64f);
                ActivityRoomChrome.StyleButton(button, seed.IsDefault ? ActivityRoomChrome.ButtonPrimary : _accent, 17);
            }
        }

        private IEnumerator MountPlayfieldWhenRoomRevealed()
        {
            var world = CampusWorldController.Ensure();
            var safety = 0;
            while (world.IsRoomVeilActive && _promptText != null && safety++ < 600)
            {
                yield return null;
            }

            if (_promptText == null)
            {
                _playfieldRoutine = null;
                yield break; // route changed before the room revealed
            }

            BuildPlayfield(world.WorldRoot);
            _playfieldRoutine = null;
        }

        private void BuildPlayfield(Transform worldRoot)
        {
            if (worldRoot == null || _pattern == null)
            {
                return;
            }

            var rules = _pattern.Rules;
            var trayCount = 0;
            foreach (var objectDefinition in rules.Objects)
            {
                if (objectDefinition.Role != PartyStationObjectRole.Meter)
                {
                    trayCount++;
                }
            }

            var pieceCount = trayCount;
            var targetCount = rules.TargetIds.Count;
            _kit.Mount(
                worldRoot,
                _pattern,
                this,
                spriteFor: PartyStationRenderer.ResolveToySprite,
                trayPositionFor: index => PartyStationRenderer.TrayPosition(index, pieceCount),
                targetPositionFor: index => PartyStationRenderer.TargetPosition(index, targetCount));
            PartyStationRenderer.DecoratePlayfield(_kit, rules, _accent);

            // Pre-existing shared progress (joining a partner mid-attempt)
            // renders on mount without celebration spam.
            SyncFromNetwork(celebrateNew: false);
            UpdateProgress();
        }

        private DropSubmitResult SubmitThroughNetwork(string pieceId, string targetId, int value)
        {
            var submissionId = _pattern.BeginSubmission(pieceId);
            _network.SubmitAction(pieceId, targetId, value, submissionId);

            // On the host the server RPC runs inline — the accept may have
            // already landed by the time SubmitAction returns.
            SyncFromNetwork(celebrateNew: true);
            if (IsToyAccepted(pieceId)
                || (_pattern.Rules.IsMeterObject(pieceId) && _network.MeterValue(pieceId) == value))
            {
                _pattern.CompleteSubmission(pieceId);
                UpdateProgress();
                TryAutoComplete();
                return DropSubmitResult.Accepted;
            }

            return DropSubmitResult.Pending;
        }

        private void HandlePatternCompleted()
        {
            TryAutoComplete();
        }

        private void HandlePatternRejected(string objectId, ToyRejectReason reason)
        {
            if (reason == ToyRejectReason.Locked)
            {
                // Intro hold / post-completion bounce: quiet — the guide line
                // already shows the right beat (intro or success copy).
                return;
            }

            // Gentle reject (design doc): the wrong attempt already escalated
            // the hint ladder, so the guide simply speaks the seed's hint copy.
            AudioCueCatalog.TryPlay(AudioCueIds.DropReject);
            _idleSeconds = 0f;
            RefreshHintPresentation();
        }

        private void HandleNetworkChanged()
        {
            if (!_networkSubscribed)
            {
                return;
            }

            if (_promptText == null)
            {
                // Room torn down (route change) — drop the subscription lazily.
                UnsubscribeNetwork();
                return;
            }

            if (UsesNetworkState && _network.AttemptNumber != _state.SyncedAttemptNumber)
            {
                // Partner started a fresh attempt after completion — re-open.
                _state.SyncedAttemptNumber = _network.AttemptNumber;
                ResetAttemptVisuals();
            }

            SyncFromNetwork(celebrateNew: true);
            UpdateProgress();
            TryAutoComplete();
        }

        private void HandleNetworkRejected(int objectIndex, int submissionId, ToyRejectReason reason)
        {
            // Host's own rejects invoke synchronously inside the submit call
            // stack — always defer one frame before reacting (P21).
            StartCoroutine(DeferredNetworkReject(objectIndex, submissionId, reason));
        }

        private IEnumerator DeferredNetworkReject(int objectIndex, int submissionId, ToyRejectReason reason)
        {
            yield return null;
            if (_promptText == null)
            {
                UnsubscribeNetwork();
                yield break;
            }

            var objectId = _network != null ? _network.ObjectIdFor(objectIndex) : null;
            if (objectId == null || _pattern == null || !_pattern.IsCurrentSubmission(objectId, submissionId))
            {
                yield break; // stale — a newer drag of the toy is in flight
            }

            _pattern.CompleteSubmission(objectId);
            var piece = _kit.PieceFor(objectId);
            if (piece != null)
            {
                piece.IsAwaitingResult = false;
                if (!piece.IsDragging)
                {
                    piece.SnapToHome();
                }
            }

            // Reject feedback lands on the submitting surface only; the wrong
            // attempt also asks the host for the next shared hint level, so
            // both players see the same clue state (R16).
            AudioCueCatalog.TryPlay(AudioCueIds.DropReject);
            _guide?.ShowHint(_seed != null ? _seed.HintLine : null);
            _network?.RequestHint();
        }

        private void HandleToyAccepted(string objectId, bool celebrate)
        {
            _idleSeconds = 0f;

            if (_pattern != null && _pattern.Rules.IsMeterObject(objectId))
            {
                UpdateProgress();
                return;
            }

            _renderedAccepted.Add(objectId);
            _kit.LockAcceptedPiece(objectId, celebrate && Application.isPlaying, _accent);
            if (celebrate)
            {
                AudioCueCatalog.TryPlay(AudioCueIds.DropAccept);
            }

            RefreshHintPresentation();
            UpdateProgress();
        }

        private void SyncFromNetwork(bool celebrateNew)
        {
            if (!UsesNetworkState || _pattern == null)
            {
                return;
            }

            foreach (var objectId in _pattern.Rules.DraggableObjectIds)
            {
                var accepted = _network.IsObjectAccepted(objectId);
                if (accepted && !_renderedAccepted.Contains(objectId))
                {
                    _pattern.ApplyAuthoritativeAccept(objectId);
                    _pattern.CompleteSubmission(objectId);
                    HandleToyAccepted(objectId, celebrateNew);
                }
                else if (!accepted && _renderedAccepted.Contains(objectId))
                {
                    // Fresh attempt: the toy returns to the tray.
                    _renderedAccepted.Remove(objectId);
                    _kit.UnlockPiece(objectId);
                }
            }

            foreach (var meterId in _pattern.Rules.MeterObjectIds)
            {
                _pattern.ApplyAuthoritativeMeter(meterId, _network.MeterValue(meterId));
            }

            _pattern.ApplyAuthoritativeHint(_network.HintLevel);
            RefreshHintPresentation();
            _partnerHeldObjectId = ToyInteractionKit.ApplyPartnerHold(
                _kit.Pieces, _partnerHeldObjectId, PartnerHeldObjectIdFromState());
        }

        private void ResetAttemptVisuals()
        {
            if (_pattern == null)
            {
                return;
            }

            _pattern.ResetForAttempt();
            _pattern.ExternalLock = !_introComplete;
            foreach (var objectId in _renderedAccepted)
            {
                _kit.UnlockPiece(objectId);
            }

            _renderedAccepted.Clear();
            _playElapsed = 0f;
            _idleSeconds = 0f;
            _guide?.ShowIntro();
            UpdateProgress();
        }

        private void RefreshHintPresentation()
        {
            var line = CurrentHintLine;
            if (_guide != null && _guide.IsAlive && !AuthoritativeComplete)
            {
                if (line != null)
                {
                    _guide.ShowHint(line);
                }
                else
                {
                    _guide.ShowIntro();
                }
            }

            var highlight = HighlightObjectId;
            if (highlight != null)
            {
                _kit.SetHintHighlight(highlight);
            }
            else
            {
                _kit.ClearHintHighlight();
            }

            _renderedHintLevel = HintLevel;
        }

        private void CompleteIntro()
        {
            _introComplete = true;
            if (_pattern != null)
            {
                _pattern.ExternalLock = false;
            }

            UpdateProgress();
        }

        private void PlayToyReaction(string objectId)
        {
            var piece = _kit.PieceFor(objectId);
            if (piece != null)
            {
                if (Application.isPlaying)
                {
                    ParticlePoof.Burst(piece.transform.position, _accent);
                }

                piece.SnapToHome();
            }

            AudioCueCatalog.TryPlay(AudioCueIds.ToyBell);
        }

        private void TryAutoComplete()
        {
            if (_pattern == null || _pattern.ResultEmitted || !AuthoritativeComplete)
            {
                return;
            }

            if (_session == null || _app == null)
            {
                return; // seam-only usage without a rendered room
            }

            _pattern.MarkResultEmitted(); // raises the drag lock with completion
            _state.MarkCompleted(_definition.Id);

            _guide?.ShowSuccess();
            _rewardPreview?.MarkEarned();
            _kit.ClearHintHighlight();
            SetStatus("Quest complete! Badge ceremony starting...");

            var result = CreateResult(_source);

            if (IsHostAuthority)
            {
                // Compact shared completion fact (R17) — never names or text.
                _network.ServerRecordRewardFact(_definition.Id, result.Tier);
            }

            // U6 seam: one reward event per completion; replays append events
            // even when the best result does not change (R11).
            RewardEventEmitted?.Invoke(new StationRewardEvent(
                _definition.Id,
                _seed.SeedId,
                result.Tier,
                result.Source,
                result.Summary,
                _definition.AccessoryRewardId,
                result.TraitDeltas));

            Completed?.Invoke(result);
            TryCompleteRoom(_session, _app, result);
        }

        private void ExitStationToCampus()
        {
            if (IsHostAuthority)
            {
                _network.ServerEndStation(); // reward facts persist (session log)
            }

            StopPlayfieldRoutine();
            UnsubscribeNetwork();
            TeardownSurface();
            ExitToCampus(_app);
        }

        private void UpdateProgress()
        {
            if (_pattern == null)
            {
                return;
            }

            if (AuthoritativeComplete)
            {
                SetStatus("Quest complete! Badge ceremony starting...");
                return;
            }

            if (!_introComplete)
            {
                SetStatus("Get ready...");
                return;
            }

            var required = _pattern.Rules.RequiredCount;
            var accepted = UsesNetworkState ? _network.AcceptedCount : _pattern.Rules.AcceptedCount;
            SetStatus($"{accepted}/{required} toys placed.");
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }

        private PartyStationObjectDefinition ObjectDefinitionFor(string objectId)
        {
            if (_pattern == null || string.IsNullOrEmpty(objectId))
            {
                return null;
            }

            foreach (var definition in _pattern.Rules.Objects)
            {
                if (definition.ObjectId == objectId)
                {
                    return definition;
                }
            }

            return null;
        }

        private string PartnerHeldObjectIdFromState()
        {
            if (!UsesNetworkState)
            {
                return null;
            }

            var manager = Unity.Netcode.NetworkManager.Singleton;
            var localClientId = manager != null ? manager.LocalClientId : 0UL;
            return _network.ObjectIdFor(_network.HeldPieceIndexForPartner(localClientId));
        }

        private void StopPlayfieldRoutine()
        {
            if (_playfieldRoutine != null)
            {
                StopCoroutine(_playfieldRoutine);
                _playfieldRoutine = null;
            }
        }

        private void TeardownSurface()
        {
            _kit.Teardown();

            if (_pattern != null)
            {
                _pattern.Completed -= HandlePatternCompleted;
                _pattern.ActionRejected -= HandlePatternRejected;
                _pattern.Teardown();
                _pattern = null;
            }

            _guide = null;
            _rewardPreview = null;
            _seedChoicePanel = null;
            _renderedAccepted.Clear();
            _partnerHeldObjectId = null;
            IsSeedChoiceOpen = false;
            _introComplete = false;
            _introElapsed = 0f;
            _playElapsed = 0f;
            _idleSeconds = 0f;
            _renderedHintLevel = 0;
        }

        private void UnsubscribeNetwork()
        {
            if (_networkSubscribed && _network != null)
            {
                _network.Changed -= HandleNetworkChanged;
                _network.ActionRejected -= HandleNetworkRejected;
            }

            _networkSubscribed = false;
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeNetwork();
            StopPlayfieldRoutine();
            _kit.Teardown();
        }
    }
}
