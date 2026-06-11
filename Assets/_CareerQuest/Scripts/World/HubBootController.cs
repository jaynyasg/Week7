using System;
using System.Collections;
using UnityEngine;

namespace CareerQuest
{
    internal sealed class HubBootController
    {
        private readonly MonoBehaviour _host;
        private readonly CampusWorldBuilder _builder;
        private readonly BuildingEntranceController _entrances;
        private Coroutine _decorRoutine;

        public HubBootController(MonoBehaviour host, CampusWorldBuilder builder, BuildingEntranceController entrances)
        {
            _host = host;
            _builder = builder;
            _entrances = entrances;
        }

        public bool IsBootComplete { get; private set; }

        public bool IsDecorLoaded { get; private set; }

        public void Cancel()
        {
            if (_decorRoutine != null)
            {
                _host.StopCoroutine(_decorRoutine);
                _decorRoutine = null;
            }

            IsBootComplete = false;
            IsDecorLoaded = false;
        }

        public void BuildCampus(string name)
        {
            Cancel();
            _builder.ClearWorld();
            _builder.AddSky();
            _builder.AddGround();
            _builder.AddPath(new Vector2(0f, -0.96f), new Vector2(8.6f, 0.42f), 0f);
            _builder.AddPath(new Vector2(0f, -0.5f), new Vector2(0.46f, 3.2f), 0f);
            _builder.AddPlaza(name);
            IsBootComplete = true;
            IsDecorLoaded = false;
            _decorRoutine = _host.StartCoroutine(LoadDecor());
        }

        private IEnumerator LoadDecor()
        {
            yield return null;
            _entrances.AddHubDecor();
            IsDecorLoaded = true;
            _decorRoutine = null;
        }
    }
}
