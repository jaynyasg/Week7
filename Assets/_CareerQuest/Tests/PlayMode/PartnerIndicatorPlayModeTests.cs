using System.Collections;
using CareerQuest;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U12 P17 partner-drag-indicator suite. Render-path scenarios drive the
    /// controllers' ApplyPartnerHeldPiece seam directly on a mounted room (the
    /// exact method the network path calls); held-list scenarios exercise the
    /// REAL host core (ApplyHeldPiece / HeldPieceIndexForPartner) with simulated
    /// partner client ids, mirroring the room network-seam suites. True
    /// two-client wire delivery remains a manual 2P evidence row (U14).
    /// </summary>
    public class PartnerIndicatorPlayModeTests
    {
        private const ulong SimulatedPartnerClientId = 2UL;

        // ------------------------------------------------------------------
        // Render path: highlight set / moved / cleared on the right piece.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator DesignBuildIndicatorShowsMovesAndClearsOnTrayPieces()
        {
            var appObject = NewApp(out var app);
            yield return null;
            app.ShowDesignBuild(false);
            yield return null;
            var controller = appObject.GetComponent<DesignBuildController>();
            yield return WaitForPiece(() => controller.PieceFor("clinic"));

            controller.ApplyPartnerHeldPiece("clinic");
            Assert.That(controller.PartnerHeldPieceId, Is.EqualTo("clinic"));
            Assert.That(PartnerHoldIndicator.IsShownOn(controller.PieceFor("clinic").gameObject), Is.True,
                "The partner-held piece renders the soft highlight.");
            Assert.That(controller.PieceFor("clinic").IsDragging, Is.False,
                "Indicator is a highlight only — never drag-position mirroring.");

            // Partner switches pieces: highlight moves, old piece clears.
            controller.ApplyPartnerHeldPiece("court");
            Assert.That(PartnerHoldIndicator.IsShownOn(controller.PieceFor("clinic").gameObject), Is.False,
                "The old piece clears the moment the partner holds another.");
            Assert.That(PartnerHoldIndicator.IsShownOn(controller.PieceFor("court").gameObject), Is.True);

            // Drop/reject/accept all surface as a null hold: highlight clears.
            controller.ApplyPartnerHeldPiece(null);
            Assert.That(controller.PartnerHeldPieceId, Is.Null);
            Assert.That(PartnerHoldIndicator.IsShownOn(controller.PieceFor("court").gameObject), Is.False);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator HealthHeroAndLogicCourtShareTheIndicatorPattern()
        {
            var appObject = NewApp(out var app);
            yield return null;

            // Health Hero (replicated pattern).
            app.ShowHealthHero();
            yield return null;
            var clinic = appObject.GetComponent<HealthHeroController>();
            yield return WaitForPiece(() => clinic.PieceFor(HealthHeroClinicLayout.SymptomClipboardPieceId));

            clinic.ApplyPartnerHeldPiece(HealthHeroClinicLayout.SymptomClipboardPieceId);
            Assert.That(
                PartnerHoldIndicator.IsShownOn(clinic.PieceFor(HealthHeroClinicLayout.SymptomClipboardPieceId).gameObject),
                Is.True);
            clinic.ApplyPartnerHeldPiece(null);
            Assert.That(
                PartnerHoldIndicator.IsShownOn(clinic.PieceFor(HealthHeroClinicLayout.SymptomClipboardPieceId).gameObject),
                Is.False);

            // Logic Court (replicated pattern).
            app.ShowLogicCourt();
            yield return null;
            var court = appObject.GetComponent<LogicCourtController>();
            yield return WaitForPiece(() => court.PieceFor(LogicCourtLayout.CaseFilePieceId));

            court.ApplyPartnerHeldPiece(LogicCourtLayout.CaseFilePieceId);
            Assert.That(
                PartnerHoldIndicator.IsShownOn(court.PieceFor(LogicCourtLayout.CaseFilePieceId).gameObject),
                Is.True);
            court.ApplyPartnerHeldPiece(null);
            Assert.That(
                PartnerHoldIndicator.IsShownOn(court.PieceFor(LogicCourtLayout.CaseFilePieceId).gameObject),
                Is.False);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator WorldClearDestroysIndicatorsWithoutOrphans()
        {
            var appObject = NewApp(out var app);
            yield return null;
            app.ShowDesignBuild(false);
            yield return null;
            var controller = appObject.GetComponent<DesignBuildController>();
            yield return WaitForPiece(() => controller.PieceFor("clinic"));

            controller.ApplyPartnerHeldPiece("clinic");
            Assert.That(Object.FindObjectsByType<PartnerHoldIndicator>(FindObjectsSortMode.None), Is.Not.Empty);

            // Route change clears the world mid-hold (disconnect-equivalent).
            app.ShowCampus();
            yield return null;
            yield return null;

            Assert.That(Object.FindObjectsByType<PartnerHoldIndicator>(FindObjectsSortMode.None), Is.Empty,
                "World clear must not orphan a partner highlight.");

            Object.DestroyImmediate(appObject);
        }

        // ------------------------------------------------------------------
        // Held-piece network seams: partner read, clear paths, disconnect sweep.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator HeldListExposesPartnerPieceAndIgnoresOwnHold()
        {
            yield return StartHost();
            var state = Object.FindAnyObjectByType<DesignBuildNetworkState>();
            var localClientId = NetworkManager.Singleton.LocalClientId;

            // Partner pickup surfaces through the partner read seam...
            state.ApplyHeldPiece(2, SimulatedPartnerClientId);
            Assert.That(state.HeldPieceIndexForPartner(localClientId), Is.EqualTo(2));

            // ...while the local player's own hold never reads as "partner".
            state.ApplyHeldPiece(1, localClientId);
            Assert.That(state.HeldPieceIndexForPartner(localClientId), Is.EqualTo(2));
            Assert.That(state.HeldPieceIndexForPartner(SimulatedPartnerClientId), Is.EqualTo(1));

            // Drop/reject clears the partner flag.
            state.ApplyHeldPiece(-1, SimulatedPartnerClientId);
            Assert.That(state.HeldPieceIndexForPartner(localClientId), Is.EqualTo(-1));

            state.ApplyHeldPiece(-1, localClientId);
            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        [UnityTest]
        public IEnumerator DisconnectSweepClearsHeldPiecesAcrossAllThreeRooms()
        {
            yield return StartHost();
            var localClientId = NetworkManager.Singleton.LocalClientId;
            var designBuild = Object.FindAnyObjectByType<DesignBuildNetworkState>();
            var healthHero = Object.FindAnyObjectByType<HealthHeroNetworkState>();
            var logicCourt = Object.FindAnyObjectByType<LogicCourtNetworkState>();

            designBuild.ApplyHeldPiece(1, SimulatedPartnerClientId);
            healthHero.ApplyHeldPiece(0, SimulatedPartnerClientId);
            logicCourt.ApplyHeldPiece(0, SimulatedPartnerClientId);

            // The server-side disconnect sweep (NetworkBootstrap) drops every
            // stale hold the departed client left behind.
            NetworkBootstrap.ClearHeldPiecesFor(SimulatedPartnerClientId);

            Assert.That(designBuild.HeldPieceIndexForPartner(localClientId), Is.EqualTo(-1));
            Assert.That(healthHero.HeldPieceIndexForPartner(localClientId), Is.EqualTo(-1));
            Assert.That(logicCourt.HeldPieceIndexForPartner(localClientId), Is.EqualTo(-1));

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        // ------------------------------------------------------------------
        // Helpers (house conventions).
        // ------------------------------------------------------------------

        private static GameObject NewApp(out CareerQuestApp app)
        {
            var appObject = new GameObject("partner-indicator-test");
            app = appObject.AddComponent<CareerQuestApp>();
            return appObject;
        }

        private static IEnumerator WaitForPiece(System.Func<DraggablePiece> piece)
        {
            for (var frame = 0; frame < 240; frame++)
            {
                if (piece() != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Drag playfield should mount after the room veil reveals.");
        }

        private static IEnumerator StartHost()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);
        }
    }
}
