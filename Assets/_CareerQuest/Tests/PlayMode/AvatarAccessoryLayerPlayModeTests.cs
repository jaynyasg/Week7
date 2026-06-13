using System.Collections;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U6 accessory visual layer (design doc Accessory Display rule). The layers
    /// are child SpriteRenderers under the avatar: they follow the avatar
    /// transform for free, flip with the host renderer's facing, sort against the
    /// host via each definition's SortingOffset, and never float off or swallow
    /// the avatar (placeholder token height normalized). Slot and ceremony rules
    /// are enforced defensively in the layer too. All derivation is session-read
    /// only (KTD8) — the layer never writes session state.
    /// </summary>
    public class AvatarAccessoryLayerPlayModeTests
    {
        [UnityTest]
        public IEnumerator EarnedAccessoriesMountChildLayersThatFollowSortAndFlipWithTheAvatar()
        {
            var (avatarObject, view, host) = MakeAvatar("accessory-follow-avatar");
            var session = new GameSession();
            // Microphone (Hand slot) has a non-zero anchor x, so the flip-mirror
            // assertion below is meaningful (a Torso item anchors at x=0).
            RecordStation(session, CareerQuestCatalog.MusicStudioId);

            view.BindAccessories(session, ceremonyContext: false);
            yield return null;

            var layer = view.AccessoryLayer;
            Assert.That(layer, Is.Not.Null);
            Assert.That(layer.VisibleAccessoryIds, Does.Contain("accessory.microphone"));

            var renderer = layer.RendererFor("accessory.microphone");
            Assert.That(renderer, Is.Not.Null, "The earned accessory mounts a live child renderer.");
            Assert.That(renderer.transform.parent, Is.EqualTo(avatarObject.transform), "Layers are children of the avatar (follow for free).");
            Assert.That(renderer.sprite, Is.Not.Null, "Placeholder token art still resolves to a sprite, never null.");

            // Token height is normalized to a small local span — never floats off
            // or swallows the avatar.
            var worldHeight = renderer.sprite.bounds.size.y * renderer.transform.lossyScale.y;
            Assert.That(worldHeight, Is.GreaterThan(0.05f).And.LessThan(2f), "Token stays a small, on-body size.");

            // Sorts against the host via the definition's SortingOffset.
            yield return null; // let LateUpdate sync facing/sorting
            Assert.That(renderer.sortingLayerID, Is.EqualTo(host.sortingLayerID));
            Assert.That(renderer.sortingOrder, Is.GreaterThan(host.sortingOrder), "Accessory sorts above the host body.");

            // Facing follows the host renderer's flipX (the animator drives flipX
            // from facing; the layer mirrors it AND its anchor x). Drive facing
            // through the view so the animator does not overwrite a direct flip.
            view.SetLocomotion(true, 1f);
            yield return null;
            Assert.That(host.flipX, Is.False);
            var rightX = renderer.transform.localPosition.x;
            Assert.That(renderer.flipX, Is.False);

            view.SetLocomotion(true, -1f);
            yield return null;
            Assert.That(host.flipX, Is.True, "Facing left flips the host sprite.");
            Assert.That(renderer.flipX, Is.True, "Layer mirrors the host facing.");
            Assert.That(renderer.transform.localPosition.x, Is.EqualTo(-rightX).Within(0.0001f), "Anchor x mirrors on flip.");

            // Follows the avatar transform: moving the root moves the layer by
            // the same delta (it is a child, so it follows for free).
            var beforeMove = renderer.transform.position;
            avatarObject.transform.position += new Vector3(3f, 1f, 0f);
            yield return null;
            var delta = renderer.transform.position - beforeMove;
            Assert.That(delta.x, Is.EqualTo(3f).Within(0.001f), "Layer follows the avatar transform in x.");
            Assert.That(delta.y, Is.EqualTo(1f).Within(0.001f), "Layer follows the avatar transform in y.");

            Object.Destroy(avatarObject);
        }

        [UnityTest]
        public IEnumerator CampusPlayShowsAtMostOneAccessoryPerSlotAndHidesCeremonyOnly()
        {
            var (avatarObject, view, _) = MakeAvatar("accessory-slot-rules");

            // 8 unique completions: two Torso accessories ever earned PLUS the
            // ceremony-only Star Robe (also Torso). Campus play shows one Torso.
            var session = new GameSession();
            foreach (var stationId in new[]
                     {
                         CareerQuestCatalog.RoboticsGarageId, // Tool Belt (Torso)
                         CareerQuestCatalog.AiLabId,
                         CareerQuestCatalog.MusicStudioId,
                         CareerQuestCatalog.CommunityKitchenId,
                         CareerQuestCatalog.VetClinicId,
                         CareerQuestCatalog.GameStudioId,
                         CareerQuestCatalog.WeatherLabId,
                         CareerQuestCatalog.SpaceportId // Mission Patch (Torso)
                     })
            {
                RecordStation(session, stationId);
            }

            view.BindAccessories(session, ceremonyContext: false);
            yield return null;

            var layer = view.AccessoryLayer;
            var visibleIds = layer.VisibleAccessoryIds.ToList();

            // No two visible accessories share a slot.
            var slots = visibleIds
                .Select(id => AccessoryRewardConfig.TryGetById(id, out var accessory) ? accessory.Slot : (AccessorySlot?)null)
                .Where(slot => slot.HasValue)
                .Select(slot => slot.Value)
                .ToList();
            Assert.That(slots.Distinct().Count(), Is.EqualTo(slots.Count), "At most one visible accessory per slot in campus play.");

            // Ceremony-only Star Robe is earned but NOT visible in campus play.
            Assert.That(visibleIds, Does.Not.Contain("accessory.star_robe"), "Ceremony-only gear hidden in campus.");

            // Flip to ceremony context: the ceremony-only item appears in place.
            layer.SetCeremonyContext(true);
            yield return null;
            Assert.That(layer.VisibleAccessoryIds, Does.Contain("accessory.star_robe"), "Ceremony context reveals the Star Robe.");

            Object.Destroy(avatarObject);
        }

        [UnityTest]
        public IEnumerator LayerRefreshesWhenTheSessionGainsAnAccessory()
        {
            var (avatarObject, view, _) = MakeAvatar("accessory-live-refresh");
            var session = new GameSession();

            view.BindAccessories(session, ceremonyContext: false);
            yield return null;
            Assert.That(view.AccessoryLayer.VisibleCount, Is.EqualTo(0), "No accessories before any completion.");

            RecordStation(session, CareerQuestCatalog.CommunityKitchenId); // Chef Hat (Head)
            yield return null;

            Assert.That(view.AccessoryLayer.VisibleAccessoryIds, Does.Contain("accessory.chef_hat"),
                "Session change re-derives and mounts the new accessory with no stored wardrobe.");

            Object.Destroy(avatarObject);
        }

        private static (GameObject, AvatarRuntimeView, SpriteRenderer) MakeAvatar(string name)
        {
            var avatarObject = new GameObject(name, typeof(SpriteRenderer), typeof(AvatarRuntimeView));
            var view = avatarObject.GetComponent<AvatarRuntimeView>();
            view.ApplyAvatar("sky_builder");
            // Freeze the frame animator so the host sprite (and thus the
            // accessory anchor extents) stays constant across facing flips —
            // the flip-mirror assertion needs a stable host sprite.
            view.Animator.AutoTick = false;
            var host = avatarObject.GetComponent<SpriteRenderer>();
            return (avatarObject, view, host);
        }

        private static void RecordStation(GameSession session, string stationId)
        {
            var definition = PartyStationDefinitions.GetById(stationId);
            session.RecordResult(PartyStationController.BuildResult(
                definition, definition.DefaultSeed, ResultSource.Solo, complete: true, wrongAttempts: 0, playElapsedSeconds: 12f));
        }
    }
}
