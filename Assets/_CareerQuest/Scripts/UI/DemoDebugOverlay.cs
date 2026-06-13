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
            debugText = UiBuilder.Text(parent, "DemoDebugOverlay", string.Empty, 14, TextAnchor.UpperLeft, Color.white, TypeRole.Body, TypeWeight.SemiBold);
            debugText.color = Color.white;
            UiBuilder.Place(debugText.rectTransform, -460f, 260f, 330f, 150f);
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
                $"Reveal style: {synthesis.Style}\n" +
                $"Superpower: {synthesis.Superpower}\n" +
                $"Family: {synthesis.FamilySubhead}\n" +
                $"Primary combo: {combo}";
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
