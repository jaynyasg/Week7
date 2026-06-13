using TMPro;
using UnityEngine;

namespace CareerQuest
{
    public class DemoDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool visible;
        [SerializeField] private TextMeshProUGUI debugText;

        private GameSession _session;
        private NetworkBootstrap _networkBootstrap;

        public void Bind(GameSession session, NetworkBootstrap networkBootstrap)
        {
            _session = session;
            _networkBootstrap = networkBootstrap;
        }

        public void Toggle()
        {
            visible = !visible;
            if (debugText != null)
            {
                debugText.gameObject.SetActive(visible);
            }
        }

        public void AttachTo(Transform parent)
        {
            debugText = UiBuilder.Text(parent, "DemoDebugOverlay", string.Empty, 13, TextAnchor.UpperLeft, Color.white, TypeRole.Body, TypeWeight.SemiBold);
            debugText.color = Color.white;
            // U9: taller to fit the party-run + classroom-access observability block.
            UiBuilder.Place(debugText.rectTransform, -460f, 210f, 340f, 280f);
            debugText.gameObject.SetActive(visible);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.BackQuote))
            {
                Toggle();
            }

            if (!visible || debugText == null || _session == null)
            {
                return;
            }

            var synthesis = RevealSynthesis.Resolve(_session);
            var combo = synthesis.PrimaryCombo != null ? synthesis.PrimaryCombo.Id : "none";

            debugText.text =
                $"Mode: {_session.Mode}\n" +
                $"Connection: {_session.ConnectionMode}\n" +
                $"Phase: {_session.CurrentPhase}\n" +
                $"Network: {(_networkBootstrap != null ? _networkBootstrap.Status : "n/a")}\n" +
                $"Players: {_session.PlayerCount}\n" +
                $"Showcase: {_session.CurrentShowcaseStep}\n" +
                $"Last result: {_session.LastResultId}\n" +
                $"Source: {_session.DebugSourceSummary}\n" +
                $"Unique done: {_session.UniqueCompletedGames}\n" +
                $"Stations: {CompletedStationCount()}/{CareerQuestCatalog.PartyStationIds.Length} (skyline pieces)\n" +
                $"Reveal style: {synthesis.Style}\n" +
                $"Superpower: {synthesis.Superpower}\n" +
                $"Family: {synthesis.FamilySubhead}\n" +
                $"Primary combo: {combo}\n" +
                PartyRunDebugBlock() +
                ClassroomAccessDebugBlock();
        }

        /// <summary>
        /// U9 observability (design doc): guided run active/complete flags,
        /// current round index, ordered station ids, current station id, and the
        /// selected seed id. Privacy-safe (KTD12) — every field is a content id,
        /// a flag, or a count. No child names, rosters, free text, telemetry, or
        /// persistent identifiers (those simply do not exist in this session
        /// model). Earned accessory + matched combo ids are content ids too.
        /// </summary>
        private string PartyRunDebugBlock()
        {
            var run = _session.PartyRun;
            var stations = run.StationIds.Count > 0 ? string.Join(",", run.StationIds) : "-";
            var currentStation = run.CurrentStationId ?? "-";
            var currentSeed = run.CurrentSeedId ?? "-";
            var lastAccessory = "-";
            var recent = _session.RewardLog.Recent(1);
            if (recent.Count > 0 && !string.IsNullOrEmpty(recent[0].AccessoryRewardId))
            {
                lastAccessory = recent[0].AccessoryRewardId;
            }

            return
                $"Run: active={run.IsActive} complete={run.IsComplete} round={run.CurrentRound}/{run.RoundCount}\n" +
                $"Run stations: {stations}\n" +
                $"Run current: {currentStation} seed={currentSeed}\n" +
                $"Last accessory: {lastAccessory}\n";
        }

        /// <summary>U9 classroom-access observability — flags only, never child data.</summary>
        private string ClassroomAccessDebugBlock()
        {
            var access = _session.ClassroomAccess;
            return
                $"Quiet: {access.QuietMode} · Pointer-first: {access.PointerFirst}\n" +
                $"Non-color cues: {access.NonColorCues} · Early-reader: {access.EarlyReaderCopy}";
        }

        /// <summary>
        /// U8 observability: how many of the ten in-plan stations have a recorded
        /// best result this session — the same derivation the campus skyline
        /// evolution pieces use (session-derived, presentation-only; no child
        /// data, KTD12). Surfaces station-pack progress at a glance for demos/QA.
        /// </summary>
        private int CompletedStationCount()
        {
            var count = 0;
            foreach (var stationId in CareerQuestCatalog.PartyStationIds)
            {
                if (_session.GetBestResult(stationId) != null)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Proof seam (design doc: Observability — preview reveal styles / seed
        /// completion counts). Completes distinct Party Pack stations until the
        /// session reaches <paramref name="uniqueCompletions"/> so a demo can
        /// jump straight to a 3/5/8/10 reveal style. Session-only and
        /// presentation-only (KTD8/KTD12) — no persisted or child data. Returns
        /// the style the synthesis now resolves to.
        /// </summary>
        public RevealStyle PreviewRevealStyle(int uniqueCompletions)
        {
            if (_session == null)
            {
                return RevealStyle.PreReveal;
            }

            var target = Mathf.Clamp(uniqueCompletions, 0, CareerQuestCatalog.PartyStationIds.Length);
            foreach (var stationId in CareerQuestCatalog.PartyStationIds)
            {
                if (_session.UniqueCompletedGames >= target)
                {
                    break;
                }

                if (_session.GetBestResult(stationId) != null)
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

            return RevealSynthesis.StyleFor(_session.UniqueCompletedGames);
        }
    }
}
