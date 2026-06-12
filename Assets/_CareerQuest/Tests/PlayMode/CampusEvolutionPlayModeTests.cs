using System.Collections;
using System.Collections.Generic;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// R18 + P19: earning a badge pops the corresponding city piece on the
    /// campus skyline with an arrival fanfare (sparkle + cue + camera nudge)
    /// exactly once per piece per session; re-entering the hub shows the piece
    /// persisted without re-fanfare. Beats are driven through the deterministic
    /// Tick seams (no real-time waits).
    /// </summary>
    public class CampusEvolutionPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            DestroyAll<CareerQuestApp>();
            DestroyAll<CampusWorldController>();
            DestroyAll<PlayableHubController>();
            DestroyAll<CampusEvolutionController>();
        }

        private static void DestroyAll<T>() where T : Component
        {
            foreach (var component in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component != null && component.gameObject != null)
                {
                    Object.DestroyImmediate(component.gameObject);
                }
            }
        }

        [UnityTest]
        public IEnumerator EarnedBadgePopsCityPieceWithFanfareOnceAndPersistsForSession()
        {
            var appObject = new GameObject("evolution-fanfare-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);

            // No badges yet — the skyline is empty.
            var beforeEarn = Object.FindAnyObjectByType<CampusEvolutionController>();
            Assert.That(beforeEarn, Is.Not.Null, "The evolution layer mounts with the campus.");
            Assert.That(beforeEarn.SpawnedPieceCount, Is.EqualTo(0));
            Assert.That(beforeEarn.IsFanfareActive, Is.False);

            // Earn a badge, then return to the hub: the piece arrives with fanfare.
            app.Session.RecordResult(CreateCatalogResult(CareerQuestCatalog.GetById(CareerConfig.DesignBuildId)));
            app.ShowCampus();

            // Grab the controller before any Update can run so the whole beat
            // sequence is driven through the deterministic tick seam.
            var evolution = Object.FindAnyObjectByType<CampusEvolutionController>();
            Assert.That(evolution, Is.Not.Null);
            evolution.AutoTick = false;
            Assert.That(evolution.HasPiece(CareerConfig.DesignBuildId), Is.True, "The earned badge pops its city piece.");
            Assert.That(evolution.FanfaresQueuedThisMount, Is.EqualTo(1), "Exactly one new piece celebrates.");
            Assert.That(evolution.IsFanfareActive, Is.True);
            yield return null;

            // Deterministic ticks: drive the beat sequence and the camera.
            var director = CameraDirector.Ensure();
            director.AutoTick = false;

            DriveToCompletion(evolution, director);

            // Camera respected route restoration: the hub player exists, so the
            // director is handed back to hub follow framing.
            Assert.That(director.ActiveMode, Is.EqualTo(CameraDirectorMode.Follow),
                "After the nudge returns, the camera re-engages hub follow.");

            var piece = evolution.PieceFor(CareerConfig.DesignBuildId);
            Assert.That(piece, Is.Not.Null);
            Assert.That(piece.localScale.x, Is.GreaterThan(0.01f), "The piece lands at full scale.");

            director.AutoTick = true;

            // Room round-trip: the piece persists for the session, silently.
            app.ShowDesignBuild(false);
            yield return null;
            app.ShowCampus();
            yield return null;

            var remounted = Object.FindAnyObjectByType<CampusEvolutionController>();
            Assert.That(remounted, Is.Not.Null);
            Assert.That(remounted.HasPiece(CareerConfig.DesignBuildId), Is.True, "The piece persists on hub re-entry.");
            Assert.That(remounted.FanfaresQueuedThisMount, Is.EqualTo(0), "No re-fanfare for an already celebrated piece.");
            Assert.That(remounted.IsFanfareActive, Is.False);
            var remountedPiece = remounted.PieceFor(CareerConfig.DesignBuildId);
            Assert.That(remountedPiece.localScale.x, Is.GreaterThan(0.01f), "Persisted pieces mount at full scale immediately.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator FanfareMemoryIsInstanceScopedAndFiresExactlyOncePerPiece()
        {
            var rootObject = new GameObject("evolution-direct-root");
            var session = new GameSession();
            session.RecordResult(CreateCatalogResult(CareerQuestCatalog.GetById(CareerQuestCatalog.AiLabId)));
            session.RecordResult(CreateCatalogResult(CareerQuestCatalog.GetById(CareerQuestCatalog.RoboticsGarageId)));
            var memory = new HashSet<string>();
            yield return null;

            // First mount: both new pieces queue fanfares (sequenced one at a time).
            var first = CampusEvolutionController.Mount(rootObject.transform, session, memory, null, null);
            first.AutoTick = false;
            Assert.That(first.SpawnedPieceCount, Is.EqualTo(2));
            Assert.That(first.FanfaresQueuedThisMount, Is.EqualTo(2));
            Assert.That(memory, Is.EquivalentTo(new[] { CareerQuestCatalog.AiLabId, CareerQuestCatalog.RoboticsGarageId }),
                "Memory records pieces at queue time so an interrupted fanfare can never replay.");

            var guard = 0;
            while (first.IsFanfareActive && guard++ < 600)
            {
                first.Tick(1f / 30f);
            }

            Assert.That(first.IsFanfareActive, Is.False, "Both fanfares complete within the deterministic budget.");
            Assert.That(first.PieceFor(CareerQuestCatalog.AiLabId).localScale.x, Is.GreaterThan(0.01f));
            Assert.That(first.PieceFor(CareerQuestCatalog.RoboticsGarageId).localScale.x, Is.GreaterThan(0.01f));
            Object.DestroyImmediate(first.gameObject);

            // Second mount with the same session memory: pieces persist, zero fanfares.
            var second = CampusEvolutionController.Mount(rootObject.transform, session, memory, null, null);
            second.AutoTick = false;
            Assert.That(second.SpawnedPieceCount, Is.EqualTo(2));
            Assert.That(second.FanfaresQueuedThisMount, Is.EqualTo(0));
            Assert.That(second.IsFanfareActive, Is.False);

            // A fresh memory set (new session) celebrates again — the memory is
            // instance-scoped, never static.
            Object.DestroyImmediate(second.gameObject);
            var freshMemory = new HashSet<string>();
            var third = CampusEvolutionController.Mount(rootObject.transform, session, freshMemory, null, null);
            third.AutoTick = false;
            Assert.That(third.FanfaresQueuedThisMount, Is.EqualTo(2));

            Object.DestroyImmediate(rootObject);
        }

        private static void DriveToCompletion(CampusEvolutionController evolution, CameraDirector director)
        {
            var guard = 0;
            while (evolution.IsFanfareActive && guard++ < 600)
            {
                evolution.Tick(1f / 30f);
                director.Tick(1f / 30f);
            }

            Assert.That(evolution.IsFanfareActive, Is.False, "Fanfare must complete within the deterministic budget.");
        }

        private static MiniGameResult CreateCatalogResult(CatalogEntry entry)
        {
            return new MiniGameResult(
                entry.Id,
                entry.DisplayName,
                CompletionTier.Degree,
                ResultSource.SoloFallback,
                new[] { new TraitDelta("Focus", 3) },
                30f,
                0.9f,
                $"Completed {entry.DisplayName}.");
        }
    }
}
