using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CareerQuest
{
    public class ShowcasePresenter : MonoBehaviour
    {
        private readonly List<ShowcaseStep> _steps = new();
        private CareerQuestApp _app;
        private GameSession _session;
        private Coroutine _running;
        private ShowcaseStationsMontage _montage;

        public IReadOnlyList<ShowcaseStep> Steps => _steps;
        public ShowcaseStep CurrentStep { get; private set; }
        public event Action<ShowcaseStep> StepChanged;

        private void Awake()
        {
            if (_steps.Count == 0)
            {
                BuildDefaultSequence();
            }
        }

        public void Bind(CareerQuestApp app, GameSession session)
        {
            _app = app;
            _session = session;
        }

        public void BuildDefaultSequence()
        {
            _steps.Clear();
            _steps.Add(new ShowcaseStep("connection", "Two-Client Proof", 1.25f));
            _steps.Add(new ShowcaseStep("campus", "Free Campus + Future Labels", 1.25f));
            // Stations survey beat: sells the breadth of the ten career stations
            // and their distinct verbs before the cooperative build moment.
            _steps.Add(new ShowcaseStep("stations", "Ten Career Stations", 1.6f));
            _steps.Add(new ShowcaseStep("build", "Future City Design Build", 1.25f));
            _steps.Add(new ShowcaseStep("gallery", "Achievement Gallery", 1.25f));
            _steps.Add(new ShowcaseStep("reveal", "Career Reveal", 1.25f));
        }

        public void Begin()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
            }

            _running = StartCoroutine(RunSequence());
        }

        public void Stop()
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                _running = null;
            }

            if (_montage != null)
            {
                _montage.Hide();
            }
        }

        private IEnumerator RunSequence()
        {
            foreach (var step in _steps)
            {
                CurrentStep = step;
                if (_session != null)
                {
                    _session.CurrentShowcaseStep = step.Title;
                }

                StepChanged?.Invoke(step);
                RouteStep(step);
                yield return new WaitForSeconds(step.DurationSeconds);
            }

            _running = null;
        }

        private void RouteStep(ShowcaseStep step)
        {
            if (_app == null)
            {
                return;
            }

            // The stations montage is a standalone overlay (its own canvas
            // subtree). Clear it before every beat renders, then re-show it only
            // for the stations survey — so it never lingers over another beat.
            Montage().Hide();

            switch (step.Id)
            {
                case "connection":
                    _app.ShowShowcaseProofBeat();
                    break;
                case "campus":
                    _app.ShowCampus();
                    break;
                case "stations":
                    Montage().Show(ShowcaseSeedConfig.MontageStations());
                    break;
                case "build":
                    _app.ShowDesignBuild(showcaseAutoComplete: true);
                    break;
                case "gallery":
                    _app.ShowGallery();
                    break;
                case "reveal":
                    _app.ShowReveal();
                    break;
            }
        }

        private ShowcaseStationsMontage Montage()
        {
            if (_montage == null)
            {
                _montage = GetComponent<ShowcaseStationsMontage>()
                    ?? gameObject.AddComponent<ShowcaseStationsMontage>();
            }

            return _montage;
        }
    }
}
