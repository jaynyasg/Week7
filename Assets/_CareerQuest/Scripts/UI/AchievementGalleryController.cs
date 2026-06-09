using System.Linq;
using UnityEngine;

namespace CareerQuest
{
    public class AchievementGalleryController : MonoBehaviour
    {
        public void Render(Transform parent, GameSession session, CareerQuestApp app)
        {
            var panel = UiBuilder.FullPanel(parent, "AchievementGalleryPanel", new Color(0.98f, 0.94f, 0.84f));
            var title = UiBuilder.Text(panel, "GalleryTitle", "Achievement Gallery", 42, TextAnchor.MiddleCenter, new Color(0.16f, 0.12f, 0.08f));
            UiBuilder.Place(title.rectTransform, 0f, 250f, 900f, 60f);

            var y = 160f;
            foreach (var activity in CareerConfig.Activities)
            {
                var result = session.GetBestResult(activity.Id);
                var label = result == null
                    ? $"{activity.BadgeName}: planned"
                    : $"{activity.BadgeName}: {result.Tier} - {result.Summary}";
                var color = result == null ? new Color(0.35f, 0.35f, 0.35f) : new Color(0.08f, 0.28f, 0.2f);
                var text = UiBuilder.Text(panel, $"{activity.Id}Badge", label, 22, TextAnchor.MiddleLeft, color);
                UiBuilder.Place(text.rectTransform, 0f, y, 900f, 46f);
                y -= 58f;
            }

            var traits = string.Join("  /  ", session.CareerDna.TopTraits(5).Select(trait => $"{trait.Trait} +{trait.Delta}"));
            if (string.IsNullOrWhiteSpace(traits))
            {
                traits = "Play one activity to build Career DNA.";
            }

            var traitText = UiBuilder.Text(panel, "TraitSummary", traits, 22, TextAnchor.MiddleCenter, new Color(0.1f, 0.16f, 0.18f));
            UiBuilder.Place(traitText.rectTransform, 0f, -70f, 980f, 56f);

            var reveal = UiBuilder.Button(panel, "RevealButton", "Reveal Careers", app.ShowReveal);
            UiBuilder.Place(reveal.GetComponent<RectTransform>(), -140f, -185f, 260f, 66f);

            var campus = UiBuilder.Button(panel, "GalleryCampusButton", "Campus", app.ShowCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 140f, -185f, 220f, 66f);
        }
    }
}
