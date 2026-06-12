using System;
using System.Collections;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Room transition host. Veil contract (characterized by HubWarmup tests):
    /// ShowRoom covers the shot immediately ("RoomVeil" object, IsVeilActive
    /// true); next frame the old world clears, the room builds, IsVeilActive
    /// flips false. New in U4 (P6): the cover is a SceneWipe paper curtain and
    /// after the room mounts a non-blocking "SceneWipeOpen" lift plays and
    /// destroys itself — it never extends the veil-active window.
    /// </summary>
    internal sealed class RoomVeilController
    {
        public const float OpenDurationSeconds = 0.3f;

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
            SceneWipe.CreateCover(_builder.Root);
            IsVeilActive = true;
            _veilRoutine = _host.StartCoroutine(RevealRoom(buildRoom));
        }

        private IEnumerator RevealRoom(Action buildRoom)
        {
            yield return null;
            _builder.ClearWorld();
            IsVeilActive = false;
            buildRoom?.Invoke();

            // P6: paper-wipe open over the freshly mounted room (self-destructs).
            var opener = SceneWipe.CreateCover(_builder.Root);
            opener.BeginOpen(OpenDurationSeconds);
            _veilRoutine = null;
        }
    }
}
