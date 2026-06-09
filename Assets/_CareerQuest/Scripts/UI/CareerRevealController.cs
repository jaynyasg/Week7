using System.Linq;
using UnityEngine;

namespace CareerQuest
{
    public class CareerRevealController : MonoBehaviour
    {
        public void Render(Transform parent, GameSession session, CareerQuestApp app)
        {
            var panel = UiBuilder.FullPanel(parent, "CareerRevealPanel", new Color(0.9f, 0.96f, 0.9f));

            var title = UiBuilder.Text(panel, "RevealTitle", "Career Reveal", 44, TextAnchor.MiddleCenter, new Color(0.08f, 0.18f, 0.12f));
            UiBuilder.Place(title.rectTransform, 0f, 240f, 900f, 70f);

            if (!session.RevealReady)
            {
                var locked = UiBuilder.Text(panel, "RevealLocked", $"Play all 3 themed games to unlock your career reveal.\nCompleted: {session.UniqueCompletedGames}/3. {session.ConfidencePhrase()}.", 28, TextAnchor.MiddleCenter, new Color(0.1f, 0.15f, 0.14f));
                UiBuilder.Place(locked.rectTransform, 0f, 68f, 920f, 120f);
            }
            else
            {
                var matches = session.CoLeadMatches();
                var names = string.Join(" + ", matches.Select(match => match.Career.DisplayName));
                var lead = UiBuilder.Text(panel, "RevealLead", names, 42, TextAnchor.MiddleCenter, new Color(0.05f, 0.22f, 0.15f));
                UiBuilder.Place(lead.rectTransform, 0f, 120f, 980f, 70f);

                var confidence = UiBuilder.Text(panel, "RevealConfidence", session.ConfidencePhrase(), 26, TextAnchor.MiddleCenter, new Color(0.15f, 0.25f, 0.18f));
                UiBuilder.Place(confidence.rectTransform, 0f, 58f, 780f, 42f);

                var top = session.CareerMatches().FirstOrDefault();
                var tagline = top?.Career.Tagline ?? "A path worth exploring.";
                var body = UiBuilder.Text(panel, "RevealBody", $"{tagline}\nThis is a strength-based clue, not a life assignment. You earned it by completing all three games.", 24, TextAnchor.MiddleCenter, new Color(0.1f, 0.16f, 0.14f));
                UiBuilder.Place(body.rectTransform, 0f, -45f, 960f, 110f);
            }

            var gallery = UiBuilder.Button(panel, "RevealGalleryButton", "Gallery", app.ShowGallery);
            UiBuilder.Place(gallery.GetComponent<RectTransform>(), -130f, -205f, 220f, 64f);

            var campus = UiBuilder.Button(panel, "RevealCampusButton", "Campus", app.ShowCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 130f, -205f, 220f, 64f);
        }
    }
}
