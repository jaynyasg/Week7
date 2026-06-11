using System;
using System.Collections;
using UnityEngine;

namespace CareerQuest
{
    internal sealed class RoomVeilController
    {
        private readonly MonoBehaviour _host;
        private readonly CampusWorldBuilder _builder;
        private Coroutine _veilRoutine;

        public RoomVeilController(MonoBehaviour host, CampusWorldBuilder builder)
        {
            _host = host;
            _builder = builder;
        }

        public bool IsVeilActive { get; private set; }

        public void Cancel()
        {
            if (_veilRoutine != null)
            {
                _host.StopCoroutine(_veilRoutine);
                _veilRoutine = null;
            }

            IsVeilActive = false;
        }

        public void ShowRoom(Action buildRoom)
        {
            Cancel();
            _builder.ClearWorld();
            _builder.AddFullScreenVeil();
            IsVeilActive = true;
            _veilRoutine = _host.StartCoroutine(RevealRoom(buildRoom));
        }

        private IEnumerator RevealRoom(Action buildRoom)
        {
            yield return null;
            _builder.ClearWorld();
            IsVeilActive = false;
            buildRoom?.Invoke();
            _veilRoutine = null;
        }
    }
}
