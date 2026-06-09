using System;
using UnityEngine;

namespace CareerQuest
{
    public class DesignBuildController : MonoBehaviour
    {
        private FutureCityBlueprint _blueprint;
        private int _acceptedPlacements;
        private string _feedback = "Place city pieces into the future skyline.";

        public FutureCityBlueprint Blueprint => _blueprint ??= FutureCityBlueprint.CreateDefault();
        public event Action<MiniGameResult> Completed;

        public void ResetActivity()
        {
            _blueprint = FutureCityBlueprint.CreateDefault();
            _acceptedPlacements = 0;
            _feedback = "Place city pieces into the future skyline.";
        }

        public bool TryPlacePiece(string pieceId)
        {
            var placed = Blueprint.TryPlace(pieceId);
            if (placed)
            {
                _acceptedPlacements++;
                _feedback = $"Accepted {pieceId.Replace('_', ' ')} into the Future City.";
            }
            else
            {
                _feedback = "That spot is already solved. Try another contribution.";
            }

            return placed;
        }

        public MiniGameResult CreateResult(ResultSource source)
        {
            var tier = Blueprint.Complete ? CompletionTier.Degree : CompletionTier.Practice;
            var accuracy = Blueprint.Slots.Count == 0 ? 0f : (float)_acceptedPlacements / Blueprint.Slots.Count;
            return new MiniGameResult(
                CareerConfig.DesignBuildId,
                "Future City Design Build",
                tier,
                source,
                new[]
                {
                    new TraitDelta("Building", tier == CompletionTier.Degree ? 5 : 3),
                    new TraitDelta("Spatial Thinking", tier == CompletionTier.Degree ? 5 : 3),
                    new TraitDelta("Creativity", 4),
                    new TraitDelta("Reasoning", 3),
                    new TraitDelta("Collaboration", source == ResultSource.Multiplayer ? 3 : 1)
                },
                45f,
                accuracy,
                tier == CompletionTier.Degree
                    ? "Completed a skyline where helping, law, art, science, and invention fit together."
                    : "Practiced city design and found several strong matches.");
        }

        public void Render(Transform parent, GameSession session, CareerQuestApp app, ResultSource source)
        {
            ResetActivity();
            var panel = UiBuilder.FullPanel(parent, "DesignBuildPanel", new Color(0.88f, 0.95f, 1f));
            var blueprintReviewed = false;
            var helperUsed = false;

            var title = UiBuilder.Text(panel, "DesignBuildTitle", "Future City Design Build", 38, TextAnchor.MiddleCenter, new Color(0.08f, 0.18f, 0.25f));
            UiBuilder.Place(title.rectTransform, 0f, 250f, 900f, 60f);

            var feedback = UiBuilder.Text(panel, "DesignBuildFeedback", _feedback, 22, TextAnchor.MiddleCenter, new Color(0.1f, 0.2f, 0.25f));
            UiBuilder.Place(feedback.rectTransform, 0f, 190f, 900f, 50f);

            var progress = UiBuilder.Text(panel, "DesignBuildProgress", "Step 1: review the blueprint before placing city pieces.", 20, TextAnchor.MiddleCenter, new Color(0.08f, 0.16f, 0.2f));
            UiBuilder.Place(progress.rectTransform, 0f, 146f, 980f, 42f);

            var review = UiBuilder.Button(panel, "ReviewBlueprintButton", "Review Blueprint", () =>
            {
                blueprintReviewed = true;
                _feedback = "Blueprint reviewed: each career building needs the matching lot.";
                feedback.text = _feedback;
                progress.text = "Step 2: ask the Pattern Helper, then place each piece.";
            });
            UiBuilder.Place(review.GetComponent<RectTransform>(), -190f, 96f, 230f, 50f);

            var helper = UiBuilder.Button(panel, "PatternHelperButton", "Pattern Helper", () =>
            {
                if (!blueprintReviewed)
                {
                    _feedback = "Review the blueprint first so the helper clue makes sense.";
                    feedback.text = _feedback;
                    return;
                }

                helperUsed = true;
                _feedback = "Helper clue: match purpose to place - care, fairness, art, science, invention.";
                feedback.text = _feedback;
                progress.text = "Step 3: place all five pieces, then finish the build.";
            });
            UiBuilder.Place(helper.GetComponent<RectTransform>(), 190f, 96f, 230f, 50f);

            var index = 0;
            foreach (var piece in Blueprint.Pieces)
            {
                var pieceButton = UiBuilder.Button(panel, $"{piece.Id}Button", piece.DisplayName, () =>
                {
                    if (!blueprintReviewed || !helperUsed)
                    {
                        _feedback = "Prepare first: review the blueprint and use the Pattern Helper.";
                        feedback.text = _feedback;
                        return;
                    }

                    var networkState = FindAnyObjectByType<DesignBuildNetworkState>();
                    if (source == ResultSource.Multiplayer && networkState != null && networkState.IsSpawned)
                    {
                        networkState.SubmitPlacement(piece.Id);
                    }

                    TryPlacePiece(piece.Id);
                    feedback.text = _feedback;
                    progress.text = $"{_acceptedPlacements}/5 city pieces placed.";

                    if (Blueprint.Complete)
                    {
                        progress.text = "Future City complete. Finish the build to add your badge.";
                    }
                });

                UiBuilder.Place(pieceButton.GetComponent<RectTransform>(), -360f + index * 180f, 70f, 150f, 62f);
                index++;
            }

            var complete = UiBuilder.Button(panel, "DesignBuildCompleteButton", "Finish Build", () =>
            {
                if (!Blueprint.Complete)
                {
                    _feedback = $"Place all five city pieces first. Current progress: {_acceptedPlacements}/5.";
                    feedback.text = _feedback;
                    return;
                }

                var result = CreateResult(source);
                session.RecordResult(result);
                Completed?.Invoke(result);
                app.ShowGallery();
            });
            UiBuilder.Place(complete.GetComponent<RectTransform>(), -130f, -150f, 230f, 64f);

            var campus = UiBuilder.Button(panel, "DesignBuildCampusButton", "Campus", app.ShowCampus);
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 130f, -150f, 210f, 64f);
        }
    }
}
