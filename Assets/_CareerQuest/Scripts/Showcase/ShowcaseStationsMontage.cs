using System.Collections.Generic;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Showcase "stations" beat view (presentation only). Renders a self-contained
    /// overlay — its own subtree on the shared CareerQuest canvas — surveying a few
    /// representative career stations and their distinct verbs, then tears itself
    /// down on Hide(). It never touches CareerQuestApp's UI root, so it composes
    /// with the time-locked presenter sequence without coupling to activity
    /// rendering or the gallery/reveal surfaces.
    /// </summary>
    public class ShowcaseStationsMontage : MonoBehaviour
    {
        private GameObject _root;

        public bool IsShowing => _root != null;

        public void Show(IReadOnlyList<ShowcaseMontageEntry> entries)
        {
            Hide();

            if (entries == null || entries.Count == 0)
            {
                return;
            }

            var canvas = UiBuilder.EnsureCanvas();

            _root = new GameObject("ShowcaseStationsMontage", typeof(RectTransform));
            _root.transform.SetParent(canvas.transform, false);
            var rootRect = _root.GetComponent<RectTransform>();
            StretchFull(rootRect);
            _root.transform.SetAsLastSibling();

            // Near-opaque paper backdrop so the montage reads as its own screen
            // (UiBuilder.FullPanel caps alpha at a wash, which we do not want here).
            var backdrop = UiBuilder.Panel(rootRect, "MontageBackdrop", new Color(0.96f, 0.92f, 0.82f, 0.98f));
            StretchFull(backdrop);

            var title = UiBuilder.Text(rootRect, "MontageTitle", "Ten Career Stations", 46, TextAnchor.MiddleCenter, QuestStageUi.Ink, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(title.rectTransform, 0f, 250f, 1100f, 64f);

            var subtitle = UiBuilder.Text(rootRect, "MontageSubtitle", "Every career plays a different way", 24, TextAnchor.MiddleCenter, new Color(0.25f, 0.32f, 0.36f));
            UiBuilder.Place(subtitle.rectTransform, 0f, 196f, 1000f, 40f);

            var count = entries.Count;
            var step = 280f;
            var firstX = -(count - 1) * step * 0.5f;
            for (var i = 0; i < count; i++)
            {
                BuildCard(rootRect, entries[i], firstX + i * step);
            }

            var footer = UiBuilder.Text(rootRect, "MontageFooter", "Play any three to unlock your Career Reveal.", 20, TextAnchor.MiddleCenter, QuestStageUi.PathGold, TypeRole.Body, TypeWeight.SemiBold);
            UiBuilder.Place(footer.rectTransform, 0f, -250f, 900f, 36f);
        }

        public void Hide()
        {
            if (_root != null)
            {
                Destroy(_root);
                _root = null;
            }
        }

        private static void BuildCard(RectTransform parent, ShowcaseMontageEntry entry, float x)
        {
            var card = UiBuilder.Panel(parent, $"MontageCard_{entry.Verb}", QuestStageUi.Paper);
            UiBuilder.Place(card, x, 0f, 250f, 300f);

            var footerEdge = UiBuilder.Panel(card, "CardEdge", QuestStageUi.PaperShadow);
            UiBuilder.Place(footerEdge, 0f, -150f, 250f, 10f);

            var name = UiBuilder.Text(card, "CardTitle", entry.Title, 24, TextAnchor.MiddleCenter, QuestStageUi.Ink, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(name.rectTransform, 0f, 110f, 230f, 40f);

            var chip = UiBuilder.Panel(card, "CardVerbChip", QuestStageUi.PathGold);
            UiBuilder.Place(chip, 0f, 40f, 150f, 44f);
            var verb = UiBuilder.Text(chip, "CardVerb", entry.Verb, 22, TextAnchor.MiddleCenter, QuestStageUi.Ink, TypeRole.Body, TypeWeight.Bold);
            UiBuilder.Place(verb.rectTransform, 0f, 0f, 150f, 44f);

            var blurb = UiBuilder.Text(card, "CardBlurb", entry.Blurb, 17, TextAnchor.UpperCenter, new Color(0.25f, 0.32f, 0.36f));
            UiBuilder.Place(blurb.rectTransform, 0f, -28f, 220f, 110f);
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
