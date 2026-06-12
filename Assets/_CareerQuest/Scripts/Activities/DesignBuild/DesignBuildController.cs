using System;
using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    public class DesignBuildController : ActivityRoomController
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
            BeginRoom(CareerConfig.DesignBuildId);
            ResetActivity();
            var panel = UiBuilder.FullPanel(parent, "DesignBuildPanel", new Color(0.88f, 0.95f, 1f, 0.04f));
            var blueprintReviewed = false;
            var helperUsed = false;
            RectTransform tray = null;

            var briefing = UiBuilder.Panel(panel, "DesignBuildBriefing", ActivityRoomChrome.DesignPaper);
            UiBuilder.Place(briefing, -300f, 282f, 620f, 94f);

            var title = UiBuilder.Text(briefing, "DesignBuildTitle", "Future City Workshop", 18, TextAnchor.MiddleLeft, ActivityRoomChrome.DesignInk, TypeRole.Display, TypeWeight.SemiBold);
            UiBuilder.Place(title.rectTransform, -132f, 26f, 340f, 24f);

            var feedback = UiBuilder.Text(briefing, "DesignBuildFeedback", _feedback, 13, TextAnchor.MiddleLeft, new Color(0.1f, 0.2f, 0.25f));
            UiBuilder.Place(feedback.rectTransform, -132f, 0f, 380f, 22f);

            var progress = UiBuilder.Text(briefing, "DesignBuildProgress", "Step 1: review the blueprint.", 12, TextAnchor.MiddleLeft, new Color(0.08f, 0.16f, 0.2f));
            UiBuilder.Place(progress.rectTransform, -132f, -26f, 380f, 20f);

            var review = UiBuilder.Button(briefing, "ReviewBlueprintButton", "Review", () =>
            {
                blueprintReviewed = true;
                _feedback = "Blueprint reviewed: every career building needs its matching lot.";
                feedback.text = _feedback;
                progress.text = "Step 2: ask the helper.";
            });
            UiBuilder.Place(review.GetComponent<RectTransform>(), 242f, 18f, 96f, 30f);
            ActivityRoomChrome.StyleButton(review, ActivityRoomChrome.ButtonPrimary, 15);

            var helper = UiBuilder.Button(briefing, "PatternHelperButton", "Helper", () =>
            {
                if (!blueprintReviewed)
                {
                    _feedback = "Review the blueprint first so the helper clue makes sense.";
                    feedback.text = _feedback;
                    return;
                }

                helperUsed = true;
                _feedback = "Helper clue: care, fairness, art, science, invention.";
                feedback.text = _feedback;
                progress.text = "Step 3: place all five pieces.";
                tray.gameObject.SetActive(true);
            });
            UiBuilder.Place(helper.GetComponent<RectTransform>(), 242f, -22f, 96f, 30f);
            ActivityRoomChrome.StyleButton(helper, ActivityRoomChrome.ButtonPrimary, 15);

            tray = UiBuilder.Panel(panel, "DesignBuildToolTray", new Color(0.95f, 0.99f, 1f, 0.74f));
            UiBuilder.Place(tray, -280f, -322f, 610f, 46f);
            tray.gameObject.SetActive(false);

            var trayLabel = UiBuilder.Text(tray, "DesignBuildTrayLabel", "Place", 12, TextAnchor.MiddleCenter, ActivityRoomChrome.DesignInk);
            UiBuilder.Place(trayLabel.rectTransform, -282f, 0f, 48f, 22f);

            var index = 0;
            foreach (var piece in Blueprint.Pieces)
            {
                var pieceButton = UiBuilder.Button(tray, $"{piece.Id}Button", piece.DisplayName, () =>
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
                        progress.text = "City complete. Finish to add your badge.";
                    }
                });

                UiBuilder.Place(pieceButton.GetComponent<RectTransform>(), -224f + index * 106f, 0f, 94f, 28f);
                ActivityRoomChrome.StyleButton(pieceButton, ActivityRoomChrome.ButtonPrimary, 12);
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

                var networkState = FindAnyObjectByType<DesignBuildNetworkState>();
                if (source == ResultSource.Multiplayer && networkState != null && networkState.IsSpawned && !networkState.Complete)
                {
                    _feedback = "Wait for both players to place all city pieces.";
                    feedback.text = _feedback;
                    return;
                }

                var result = CreateResult(source);
                Completed?.Invoke(result);
                TryCompleteRoom(session, app, result);
            });
            UiBuilder.Place(complete.GetComponent<RectTransform>(), 438f, -322f, 136f, 34f);
            ActivityRoomChrome.StyleButton(complete, ActivityRoomChrome.ButtonReady, 14);

            var campus = UiBuilder.Button(panel, "DesignBuildCampusButton", "Campus", () => ExitToCampus(app));
            UiBuilder.Place(campus.GetComponent<RectTransform>(), 568f, -322f, 106f, 34f);
            ActivityRoomChrome.StyleButton(campus, ActivityRoomChrome.ButtonPrimary, 14);
        }
    }
}
