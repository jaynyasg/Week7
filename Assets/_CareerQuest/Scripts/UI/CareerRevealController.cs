using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    public class CareerRevealController : MonoBehaviour
    {
        private Coroutine _stageAnimation;

        public void Render(Transform parent, GameSession session, CareerQuestApp app)
        {
            if (_stageAnimation != null)
            {
                StopCoroutine(_stageAnimation);
                _stageAnimation = null;
            }

            var panel = UiBuilder.FullPanel(parent, "CareerRevealPanel", QuestStageUi.StageNight);
            QuestStageUi.MountStageBackdrop(panel, session.RevealReady);

            var passport = UiBuilder.Panel(panel, "RevealPassportCard", QuestStageUi.Paper);
            UiBuilder.Place(passport, 0f, 20f, 860f, 520f);

            var stripe = UiBuilder.Panel(passport, "RevealPassportStripe", QuestStageUi.PathGold);
            UiBuilder.Place(stripe, 0f, 232f, 860f, 12f);

            var title = UiBuilder.Text(
                passport,
                "RevealTitle",
                session.RevealReady ? "Your Future Paths!" : "Career Reveal Stage",
                48,
                TextAnchor.MiddleCenter,
                QuestStageUi.Ink);
            UiBuilder.Place(title.rectTransform, 0f, 170f, 780f, 64f);

            QuestStageUi.MountBadgeSlots(passport, session, 40f);

            if (!session.RevealReady)
            {
                var locked = UiBuilder.Text(
                    passport,
                    "RevealLocked",
                    "Complete 3 unique quest badges to unlock your career reveal.\n" + session.ConfidencePhrase() + ".",
                    24,
                    TextAnchor.MiddleCenter,
                    QuestStageUi.Ink);
                UiBuilder.Place(locked.rectTransform, 0f, -120f, 720f, 90f);

                var hint = UiBuilder.Text(
                    passport,
                    "RevealHint",
                    "Walk to another career door on campus to earn your next badge.",
                    18,
                    TextAnchor.MiddleCenter,
                    new Color(0.22f, 0.32f, 0.36f));
                UiBuilder.Place(hint.rectTransform, 0f, -175f, 680f, 40f);
            }
            else
            {
                var unlockBanner = UiBuilder.Text(
                    passport,
                    "RevealUnlockBanner",
                    "REVEAL UNLOCKED!",
                    32,
                    TextAnchor.MiddleCenter,
                    QuestStageUi.WorkshopTeal);
                UiBuilder.Place(unlockBanner.rectTransform, 0f, 115f, 520f, 44f);
                SetGraphicAlpha(unlockBanner, 0f);

                var matches = session.CoLeadMatches();
                var names = string.Join("  +  ", matches.Select(match => match.Career.DisplayName));
                var lead = UiBuilder.Text(passport, "RevealLead", names, 40, TextAnchor.MiddleCenter, new Color(0.05f, 0.35f, 0.28f));
                UiBuilder.Place(lead.rectTransform, 0f, -95f, 760f, 56f);
                SetGraphicAlpha(lead, 0f);

                var confidence = UiBuilder.Text(passport, "RevealConfidence", session.ConfidencePhrase(), 26, TextAnchor.MiddleCenter, QuestStageUi.WorkshopTeal);
                UiBuilder.Place(confidence.rectTransform, 0f, -145f, 640f, 36f);
                SetGraphicAlpha(confidence, 0f);

                var top = session.CareerMatches().FirstOrDefault();
                var tagline = top?.Career.Tagline ?? "A path worth exploring.";
                var body = UiBuilder.Text(
                    passport,
                    "RevealBody",
                    tagline + "\nThis is a strength clue from your quest badges — not a life assignment.",
                    22,
                    TextAnchor.MiddleCenter,
                    QuestStageUi.Ink);
                UiBuilder.Place(body.rectTransform, 0f, -205f, 740f, 72f);
                SetGraphicAlpha(body, 0f);

                MountCareerCards(passport, matches);
            }

            var gallery = UiBuilder.Button(panel, "RevealGalleryButton", "Gallery", app.ShowGallery);
            UiBuilder.Place(gallery.GetComponent<RectTransform>(), -150f, -285f, 220f, 58f);
            QuestStageUi.StyleSecondaryButton(gallery);

            var campus = UiBuilder.Button(panel, "RevealCampusButton", "Campus", app.ShowCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 150f, -285f, 220f, 58f);
            QuestStageUi.StylePrimaryButton(campus);

            _stageAnimation = StartCoroutine(PlayStageSequence(panel, passport, session));
        }

        private IEnumerator PlayStageSequence(RectTransform panel, RectTransform passport, GameSession session)
        {
            passport.localScale = Vector3.one * (session.RevealReady ? 0.88f : 1f);

            if (!session.RevealReady)
            {
                yield return PulseLockedProgress(passport, session);
                yield break;
            }

            yield return AnimateSpotlightSweep(panel, 0.45f);
            yield return ScaleRect(passport, 0.88f, 1f, 0.28f, EaseOutBack);
            yield return PunchBadgeSlots(passport, session.UniqueCompletedGames);
            yield return RevealUnlockedContent(passport);
            SpawnConfettiBurst(panel);
            yield return DriftConfetti(panel, 0.55f);
        }

        private static IEnumerator PulseLockedProgress(RectTransform passport, GameSession session)
        {
            var fill = passport.Find("RevealProgressFill") as RectTransform;
            if (fill == null)
            {
                yield break;
            }

            var baseWidth = Mathf.Max(24f, 420f * (session.UniqueCompletedGames / 3f));
            for (var pulse = 0; pulse < 2; pulse++)
            {
                yield return ScaleRect(fill, 1f, 1.08f, 0.18f, EaseOutQuad);
                yield return ScaleRect(fill, 1.08f, 1f, 0.18f, EaseInQuad);
            }

            fill.localScale = Vector3.one;
            _ = baseWidth;
        }

        private static IEnumerator AnimateSpotlightSweep(RectTransform panel, float duration)
        {
            var beamLeft = panel.Find("StageBeamLeft") as RectTransform;
            var beamRight = panel.Find("StageBeamRight") as RectTransform;
            var spot = panel.Find("StageSpot") as RectTransform;
            if (beamLeft == null || beamRight == null)
            {
                yield break;
            }

            SetRectAlpha(beamLeft, 0f);
            SetRectAlpha(beamRight, 0f);
            var leftImage = beamLeft.GetComponent<Image>();
            var rightImage = beamRight.GetComponent<Image>();

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var alpha = EaseOutQuad(0f, 1f, t);
                if (leftImage != null)
                {
                    leftImage.color = new Color(QuestStageUi.Spotlight.r, QuestStageUi.Spotlight.g, QuestStageUi.Spotlight.b, QuestStageUi.Spotlight.a * alpha);
                }

                if (rightImage != null)
                {
                    rightImage.color = new Color(QuestStageUi.Spotlight.r, QuestStageUi.Spotlight.g, QuestStageUi.Spotlight.b, QuestStageUi.Spotlight.a * alpha);
                }

                beamLeft.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(28f, 12f, t));
                beamRight.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-28f, -12f, t));
                yield return null;
            }

            if (spot != null)
            {
                yield return ScaleRect(spot, 0.6f, 1f, 0.2f, EaseOutBack);
            }
        }

        private static IEnumerator PunchBadgeSlots(RectTransform passport, int earnedCount)
        {
            for (var slot = 0; slot < 3; slot++)
            {
                var ring = passport.Find($"RevealBadgeSlot{slot}Ring") as RectTransform;
                if (ring == null)
                {
                    continue;
                }

                if (slot < earnedCount)
                {
                    yield return ScaleRect(ring, 0.5f, 1.15f, 0.12f, EaseOutBack);
                    yield return ScaleRect(ring, 1.15f, 1f, 0.1f, EaseInQuad);
                }
            }
        }

        private static IEnumerator RevealUnlockedContent(RectTransform passport)
        {
            var banner = passport.Find("RevealUnlockBanner")?.GetComponent<Text>();
            var lead = passport.Find("RevealLead")?.GetComponent<Text>();
            var confidence = passport.Find("RevealConfidence")?.GetComponent<Text>();
            var body = passport.Find("RevealBody")?.GetComponent<Text>();
            var cards = new List<RectTransform>();
            for (var i = 0; i < 3; i++)
            {
                var card = passport.Find($"RevealCareerCard{i}") as RectTransform;
                if (card != null)
                {
                    card.localScale = Vector3.zero;
                    cards.Add(card);
                }
            }

            if (banner != null)
            {
                yield return FadeGraphic(banner, 0f, 1f, 0.22f);
                yield return ScaleRect(banner.rectTransform, 0.7f, 1f, 0.18f, EaseOutBack);
            }

            for (var i = 0; i < cards.Count; i++)
            {
                yield return ScaleRect(cards[i], 0f, 1f, 0.16f, EaseOutBack);
            }

            if (lead != null)
            {
                yield return FadeGraphic(lead, 0f, 1f, 0.2f);
            }

            if (confidence != null)
            {
                yield return FadeGraphic(confidence, 0f, 1f, 0.18f);
            }

            if (body != null)
            {
                yield return FadeGraphic(body, 0f, 1f, 0.18f);
            }
        }

        private static void SpawnConfettiBurst(RectTransform panel)
        {
            var palette = new[]
            {
                QuestStageUi.PathGold,
                QuestStageUi.WorkshopTeal,
                QuestStageUi.Paper,
                new Color(0.95f, 0.45f, 0.35f)
            };

            for (var i = 0; i < 14; i++)
            {
                var color = palette[i % palette.Length];
                var piece = UiBuilder.Circle(panel, $"RevealConfetti{i}", color, 0f, 40f, 14f, 14f);
                piece.gameObject.AddComponent<RevealConfettiPiece>().Launch();
            }
        }

        private static IEnumerator DriftConfetti(RectTransform panel, float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            for (var i = 0; i < 14; i++)
            {
                var piece = panel.Find($"RevealConfetti{i}");
                if (piece != null)
                {
                    Destroy(piece.gameObject);
                }
            }
        }

        private static void MountCareerCards(RectTransform passport, IReadOnlyList<CareerMatch> matches)
        {
            var startX = matches.Count switch
            {
                1 => 0f,
                2 => -120f,
                _ => -200f
            };

            for (var i = 0; i < matches.Count && i < 3; i++)
            {
                var match = matches[i];
                var x = startX + i * 200f;
                var card = UiBuilder.Panel(passport, $"RevealCareerCard{i}", new Color(0.98f, 0.99f, 1f, 0.96f));
                UiBuilder.Place(card, x, -55f, 170f, 110f);
                card.localScale = Vector3.zero;

                var accent = UiBuilder.Panel(card, $"RevealCareerAccent{i}", QuestStageUi.PathGold);
                UiBuilder.Place(accent, 0f, 48f, 170f, 8f);

                var name = UiBuilder.Text(card, $"RevealCareerName{i}", match.Career.DisplayName, 20, TextAnchor.MiddleCenter, QuestStageUi.Ink);
                UiBuilder.Place(name.rectTransform, 0f, 10f, 150f, 32f);

                var score = UiBuilder.Text(card, $"RevealCareerScore{i}", "Top match", 14, TextAnchor.MiddleCenter, QuestStageUi.WorkshopTeal);
                UiBuilder.Place(score.rectTransform, 0f, -24f, 140f, 24f);
            }
        }

        private static IEnumerator ScaleRect(RectTransform target, float from, float to, float duration, System.Func<float, float, float, float> ease)
        {
            if (target == null)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = ease(0f, 1f, Mathf.Clamp01(elapsed / duration));
                var scale = Mathf.Lerp(from, to, t);
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            target.localScale = new Vector3(to, to, 1f);
        }

        private static IEnumerator FadeGraphic(Graphic graphic, float from, float to, float duration)
        {
            if (graphic == null)
            {
                yield break;
            }

            var elapsed = 0f;
            var color = graphic.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = EaseOutQuad(0f, 1f, Mathf.Clamp01(elapsed / duration));
                color.a = Mathf.Lerp(from, to, t);
                graphic.color = color;
                yield return null;
            }

            color.a = to;
            graphic.color = color;
        }

        private static void SetGraphicAlpha(Graphic graphic, float alpha)
        {
            if (graphic == null)
            {
                return;
            }

            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        private static void SetRectAlpha(RectTransform rect, float alpha)
        {
            var image = rect.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var color = image.color;
            color.a = alpha;
            image.color = color;
        }

        private static float EaseOutQuad(float from, float to, float t) => Mathf.Lerp(from, to, 1f - (1f - t) * (1f - t));
        private static float EaseInQuad(float from, float to, float t) => Mathf.Lerp(from, to, t * t);
        private static float EaseOutBack(float from, float to, float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            var eased = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            return Mathf.Lerp(from, to, eased);
        }

        private sealed class RevealConfettiPiece : MonoBehaviour
        {
            private Vector2 _velocity;
            private float _spin;
            private RectTransform _rect;

            public void Launch()
            {
                _rect = GetComponent<RectTransform>();
                _velocity = new Vector2(Random.Range(-420f, 420f), Random.Range(220f, 480f));
                _spin = Random.Range(-240f, 240f);
            }

            private void Update()
            {
                if (_rect == null)
                {
                    return;
                }

                _velocity.y -= 620f * Time.deltaTime;
                _rect.anchoredPosition += _velocity * Time.deltaTime;
                _rect.localRotation *= Quaternion.Euler(0f, 0f, _spin * Time.deltaTime);
            }
        }
    }
}
