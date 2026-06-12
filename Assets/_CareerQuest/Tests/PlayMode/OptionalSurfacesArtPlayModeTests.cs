using System.Collections;
using CareerQuest;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U11 / AE4: nothing player-facing shows fallback art. Entering every
    /// optional room and the gallery must render zero `.fallback` / `missing.`
    /// sprites at runtime, and the gallery must read as a passport book with
    /// earned-vs-locked sticker states. These tests fail LOUDLY until
    /// CareerQuestOptionalArtBuilder.Generate has been run.
    /// </summary>
    public class OptionalSurfacesArtPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clean stale roots from earlier suites so the runtime sprite scan
            // only sees this test's hierarchy.
            DestroyAll<CareerQuestApp>();
            DestroyAll<CampusWorldController>();
            DestroyAll<PlayableHubController>();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(canvas.gameObject);
            }
        }

        private static void DestroyAll<T>() where T : Component
        {
            foreach (var component in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(component.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator EnteringEveryOptionalRoomShowsZeroFallbackArt()
        {
            var appObject = new GameObject("optional-art-scan-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            foreach (var entry in CareerQuestCatalog.OptionalEntries)
            {
                switch (entry.Id)
                {
                    case CareerQuestCatalog.AiLabId:
                        app.ShowAiLab();
                        break;
                    case CareerQuestCatalog.MusicStudioId:
                        app.ShowMusicStudio();
                        break;
                    case CareerQuestCatalog.RoboticsGarageId:
                        app.ShowRoboticsGarage();
                        break;
                    default:
                        app.ShowCommunityKitchen();
                        break;
                }

                yield return null;
                yield return null; // room veil reveal mounts the diorama

                AssertNoFallbackSpritesVisible(entry.Id);

                // The at-bar dressing: each optional room mounts its themed
                // interior backdrop (room.{activityId}) as final art.
                var backdrop = GameObject.Find($"{entry.Id}RoomBackdrop");
                Assert.That(backdrop, Is.Not.Null, $"{entry.Id} should mount a room backdrop.");
                var renderer = backdrop.GetComponent<SpriteRenderer>();
                Assert.That(renderer.sprite, Is.Not.Null, entry.Id);
                Assert.That(AssetCatalog.IsFinalArtSprite(renderer.sprite), Is.True,
                    $"{entry.Id} backdrop must be final art — run CareerQuestOptionalArtBuilder.Generate.");
            }

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator GalleryShowsZeroFallbackArt()
        {
            var appObject = new GameObject("gallery-art-scan-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            app.Session.RecordResult(CreateCatalogResult(CareerQuestCatalog.GetById(CareerConfig.DesignBuildId)));
            app.Session.RecordResult(CreateCatalogResult(CareerQuestCatalog.GetById(CareerQuestCatalog.MusicStudioId)));

            app.ShowGallery();
            yield return null;
            yield return null;

            AssertNoFallbackSpritesVisible("gallery");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator GalleryPassportRendersEarnedAndLockedStickerStates()
        {
            var appObject = new GameObject("gallery-passport-states-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var earnedEntry = CareerQuestCatalog.GetById(CareerConfig.DesignBuildId);
            app.Session.RecordResult(CreateCatalogResult(earnedEntry));

            app.ShowGallery();
            yield return null;

            // Passport book chrome.
            Assert.That(GameObject.Find("GalleryPassportBook"), Is.Not.Null);
            Assert.That(GameObject.Find("GalleryPassportSpine"), Is.Not.Null);
            Assert.That(GameObject.Find("GalleryPageEdgeA"), Is.Not.Null, "Stacked page edges sell the book silhouette.");

            // Earned sticker: career ring + gold stamp ring + final badge art.
            Assert.That(GameObject.Find($"{earnedEntry.Id}ChipStamp"), Is.Not.Null, "Earned chips carry the gold stamp ring.");
            var earnedIcon = GameObject.Find($"{earnedEntry.Id}ChipIcon");
            Assert.That(earnedIcon, Is.Not.Null, "Earned chips show the badge sticker art.");
            Assert.That(AssetCatalog.IsFinalArtSprite(earnedIcon.GetComponent<Image>().sprite), Is.True,
                "Earned badge sticker must be final art, not a runtime fallback.");

            var earnedRing = GameObject.Find($"{earnedEntry.Id}ChipRing").GetComponent<Image>();
            var careerColor = AssetCatalog.GetDefinition(earnedEntry.BadgeArtKey).PrimaryColor;
            Assert.That(earnedRing.color, Is.EqualTo(careerColor), "Earned ring uses the career identity color.");

            var earnedLabel = GameObject.Find($"{earnedEntry.Id}Badge").GetComponent<TextMeshProUGUI>();
            Assert.That(earnedLabel.text, Is.EqualTo(earnedEntry.BadgeName));

            // Locked slot: dimmed, no badge art, no stamp ring.
            var lockedEntry = CareerQuestCatalog.GetById(CareerQuestCatalog.AiLabId);
            Assert.That(GameObject.Find($"{lockedEntry.Id}ChipIcon"), Is.Null, "Locked slots show no badge art.");
            Assert.That(GameObject.Find($"{lockedEntry.Id}ChipStamp"), Is.Null, "Locked slots carry no stamp ring.");
            Assert.That(GameObject.Find($"{lockedEntry.Id}ChipHint"), Is.Not.Null);
            var lockedLabel = GameObject.Find($"{lockedEntry.Id}Badge").GetComponent<TextMeshProUGUI>();
            Assert.That(lockedLabel.text, Is.EqualTo("Locked"));

            // Skill tallies render as styled pills once a result exists.
            Assert.That(GameObject.Find("TraitSummary"), Is.Not.Null);
            Assert.That(GameObject.Find("TraitPill0"), Is.Not.Null, "Skill tallies render as paper pills.");

            Object.DestroyImmediate(appObject);
        }

        private static void AssertNoFallbackSpritesVisible(string state)
        {
            foreach (var renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                AssertSpriteIsNotFallback(state, renderer.gameObject.name, renderer.sprite);
            }

            foreach (var image in Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                AssertSpriteIsNotFallback(state, image.gameObject.name, image.sprite);
            }
        }

        private static void AssertSpriteIsNotFallback(string state, string objectName, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Assert.That(sprite.name, Does.Not.EndWith(SpriteFallbackFactory.FallbackSpriteSuffix),
                $"State '{state}': '{objectName}' renders a generated fallback sprite ('{sprite.name}').");
            Assert.That(sprite.name, Does.Not.StartWith("missing."),
                $"State '{state}': '{objectName}' renders a missing-definition sprite ('{sprite.name}').");
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
