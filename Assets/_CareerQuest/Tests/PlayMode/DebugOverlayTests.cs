using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    public class DebugOverlayTests
    {
        [Test]
        public void DebugOverlayCanBindAndToggle()
        {
            var canvas = UiBuilder.EnsureCanvas();
            var overlayObject = new GameObject("debug-overlay-test");
            var overlay = overlayObject.AddComponent<DemoDebugOverlay>();

            overlay.Bind(new GameSession(), null);
            overlay.AttachTo(canvas.transform);
            overlay.Toggle();

            Assert.That(overlay, Is.Not.Null);

            Object.DestroyImmediate(overlayObject);
        }
    }
}
