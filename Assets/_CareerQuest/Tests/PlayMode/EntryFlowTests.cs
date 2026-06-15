using System.Collections;
using CareerQuest;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CareerQuest.Tests
{
    public class EntryFlowTests
    {
        [UnityTest]
        public IEnumerator PlayAndShowcaseRouteToDistinctModes()
        {
            var gameObject = new GameObject("app-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            Assert.That(app.Session.Mode, Is.EqualTo(AppMode.Entry));
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Entry));

            app.BeginPlay();
            Assert.That(app.Session.Mode, Is.EqualTo(AppMode.Play));
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));
            Assert.That(app.Session.HasSeededResults, Is.False);

            app.BeginShowcase();
            yield return null;

            Assert.That(app.Session.Mode, Is.EqualTo(AppMode.Showcase));
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.ShowcaseProof));
            Assert.That(app.Session.HasSeededResults, Is.True);

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator ConnectionScreenUsesPlayerFacingChoices()
        {
            var gameObject = new GameObject("connection-screen-copy-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            app.ShowConnection();
            yield return null;

            AssertText("ConnectionTitle", "Start Game");
            AssertButtonText("PlaySoloButton", "Play Solo");
            AssertButtonText("HostLocalGameButton", "Host Game");
            AssertButtonText("JoinThisComputerButton", "Join This PC");
            AssertButtonText("JoinIpButton", "Join IP");
            AssertText("ConnectionControls", "Controls: solo uses WASD. Same-PC multiplayer uses P1 WASD/F and P2 IJKL/Enter.");
            Assert.That(GameObject.Find("HostP1Button"), Is.Null);
            Assert.That(GameObject.Find("SoloFallbackButton"), Is.Null);

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator MultiplayerButtonsChooseAvatarBeforeConnecting()
        {
            var gameObject = new GameObject("connection-avatar-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            app.ShowConnection();
            yield return null;

            GameObject.Find("HostLocalGameButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.AvatarSelection));
            Assert.That(GameObject.Find("AvatarSelectionPanel"), Is.Not.Null);
            AssertButtonText("AvatarConfirmButton", "Host Game");

            GameObject.Find("AvatarBackButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(GameObject.Find("ConnectionPanel"), Is.Not.Null);
            GameObject.Find("JoinThisComputerButton").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.AvatarSelection));
            AssertButtonText("AvatarConfirmButton", "Join Game");

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator EntryScreenKeepsMultiplayerSecondary()
        {
            var gameObject = new GameObject("entry-multiplayer-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            AssertButtonText("PlayButton", "Play");
            AssertButtonText("ShowcaseButton", "Showcase");
            AssertButtonText("MultiplayerTestingButton", "Multiplayer");

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator EntryIsATitleMomentOverTheLiveCampus()
        {
            var gameObject = new GameObject("entry-title-moment-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            // P8: the wordmark renders over a live hub-style world (the authored
            // prefab when built, otherwise the safe fallback ground), and the
            // entry panel no longer hides the world behind an opaque fill.
            AssertText("Title", "Career Quest Campus");
            Assert.That(
                GameObject.Find("CampusHub") != null || GameObject.Find("CampusGrass") != null,
                Is.True,
                "The entry screen should render over a live campus world.");

            var panel = GameObject.Find("EntryPanel");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.GetComponent<UnityEngine.UI.Image>().color.a, Is.LessThan(0.01f),
                "The entry panel must stay clear so the diorama carries the screen.");

            // Play → avatar select routing unchanged.
            app.ShowAvatarSelectionForPlay();
            yield return null;
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.AvatarSelection));

            Object.Destroy(gameObject);
        }

        [UnityTest]
        public IEnumerator VisualQaStatesRouteToNamedScreens()
        {
            var gameObject = new GameObject("visual-qa-state-test");
            var app = gameObject.AddComponent<CareerQuestApp>();
            yield return null;

            Assert.That(app.ShowVisualQaState("avatar"), Is.True);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.AvatarSelection));

            Assert.That(app.ShowVisualQaState("campus"), Is.True);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));

            Assert.That(app.ShowVisualQaState("design-build"), Is.True);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.DesignBuild));

            Assert.That(app.ShowVisualQaState("reveal-locked"), Is.True);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Reveal));
            Assert.That(app.Session.RevealReady, Is.False);

            Assert.That(app.ShowVisualQaState("reveal-unlocked"), Is.True);
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.Reveal));
            Assert.That(app.Session.RevealReady, Is.True);

            Assert.That(app.ShowVisualQaState("unknown-state"), Is.False);

            Object.Destroy(gameObject);
        }

        private static void AssertText(string objectName, string expected)
        {
            var textObject = GameObject.Find(objectName);
            Assert.That(textObject, Is.Not.Null, $"{objectName} should exist.");
            Assert.That(textObject.GetComponent<TextMeshProUGUI>().text, Is.EqualTo(expected));
        }

        private static void AssertButtonText(string buttonName, string expected)
        {
            var buttonObject = GameObject.Find(buttonName);
            Assert.That(buttonObject, Is.Not.Null, $"{buttonName} should exist.");
            Assert.That(buttonObject.GetComponentInChildren<TextMeshProUGUI>().text, Is.EqualTo(expected));
        }
    }
}
