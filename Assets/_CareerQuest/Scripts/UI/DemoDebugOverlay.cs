using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    public class DemoDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool visible;
        [SerializeField] private Text debugText;

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
            debugText = UiBuilder.Text(parent, "DemoDebugOverlay", string.Empty, 14, TextAnchor.UpperLeft, Color.white);
            debugText.color = Color.white;
            debugText.gameObject.AddComponent<Outline>().effectColor = Color.black;
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

            debugText.text =
                $"Mode: {_session.Mode}\n" +
                $"Connection: {_session.ConnectionMode}\n" +
                $"Network: {(_networkBootstrap != null ? _networkBootstrap.Status : "n/a")}\n" +
                $"Players: {_session.PlayerCount}\n" +
                $"Showcase: {_session.CurrentShowcaseStep}\n" +
                $"Last result: {_session.LastResultId}\n" +
                $"Source: {_session.DebugSourceSummary}";
        }
    }
}
