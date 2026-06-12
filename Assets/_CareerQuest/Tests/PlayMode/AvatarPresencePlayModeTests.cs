using System.Collections;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U5 character-presence suite. The frame-set assertions fail LOUDLY when
    /// the curated character art has not been copied (run
    /// CareerQuestCharacterArtCurator.Curate) — mirroring the
    /// CampusHubWorldPlayModeTests convention for the authored hub prefab.
    /// </summary>
    public class AvatarPresencePlayModeTests
    {
        private const string CurateHint =
            "Curated Kenney character frames are missing — run CareerQuestCharacterArtCurator.Curate.";

        [TearDown]
        public void TearDown()
        {
            FirstRunGuideBeat.ResetSessionFlag();
        }

        // ------------------------------------------------------------------
        // (a) Local avatar: walk frames + facing while moving, idle at rest.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator LocalAvatarShowsWalkFramesWithFacingAndIdlesAtRest()
        {
            var avatarObject = new GameObject("presence-local-avatar", typeof(SpriteRenderer), typeof(AvatarRuntimeView));
            var view = avatarObject.GetComponent<AvatarRuntimeView>();
            view.ApplyAvatar("sky_builder");
            var animator = view.Animator;
            animator.AutoTick = false;
            yield return null;

            Assert.That(animator.HasWalkFrames, Is.True, CurateHint);

            var renderer = avatarObject.GetComponent<SpriteRenderer>();

            // Walk left: walk state, flipped facing, frames advance over ticks.
            view.SetLocomotion(true, -1f);
            Assert.That(animator.CurrentState, Is.EqualTo(SpriteFrameAnimator.AnimState.Walk));
            Assert.That(renderer.flipX, Is.True, "Facing left must flip the sprite.");

            var firstWalkSprite = renderer.sprite;
            Assert.That(AssetCatalog.IsFinalArtSprite(firstWalkSprite), Is.True, CurateHint);

            var seenChange = false;
            for (var i = 0; i < 6; i++)
            {
                animator.Tick(0.1f);
                if (renderer.sprite != firstWalkSprite)
                {
                    seenChange = true;
                }
            }

            Assert.That(seenChange, Is.True, "Walk frames must cycle while moving.");

            // Walk right flips back.
            view.SetLocomotion(true, 1f);
            Assert.That(renderer.flipX, Is.False);

            // At rest: idle state, facing retained, base idle sprite shown.
            view.SetLocomotion(true, -1f);
            view.SetLocomotion(false, 0f);
            Assert.That(animator.CurrentState, Is.EqualTo(SpriteFrameAnimator.AnimState.Idle));
            Assert.That(renderer.flipX, Is.True, "Idle must keep the last facing.");
            Assert.That(renderer.sprite, Is.SameAs(AssetCatalog.SpriteFor("avatar.sky_builder")));

            // Gentle idle bob: the breathing scale pulse moves with time.
            var scaleBefore = renderer.transform.localScale.y;
            animator.Tick(0.27f);
            Assert.That(renderer.transform.localScale.y, Is.Not.EqualTo(scaleBefore).Within(0.00001f),
                "Idle must show a gentle bob (scale breathing) as time advances.");

            Object.Destroy(avatarObject);
        }

        [UnityTest]
        public IEnumerator CelebrateLoopsCheerFramesThenReturnsToIdle()
        {
            var avatarObject = new GameObject("presence-celebrate-avatar", typeof(SpriteRenderer), typeof(AvatarRuntimeView));
            var view = avatarObject.GetComponent<AvatarRuntimeView>();
            view.ApplyAvatar("art_inventor");
            var animator = view.Animator;
            animator.AutoTick = false;
            yield return null;

            Assert.That(animator.HasCelebrateFrames, Is.True, CurateHint);

            view.TriggerCelebrate(0.8f);
            Assert.That(animator.CurrentState, Is.EqualTo(SpriteFrameAnimator.AnimState.Celebrate));

            animator.Tick(0.3f);
            Assert.That(animator.CurrentState, Is.EqualTo(SpriteFrameAnimator.AnimState.Celebrate));

            animator.Tick(0.6f);
            Assert.That(animator.CurrentState, Is.EqualTo(SpriteFrameAnimator.AnimState.Idle),
                "Celebrate must end after its duration and return to idle.");

            Object.Destroy(avatarObject);
        }

        // ------------------------------------------------------------------
        // (b) Remote avatar at rest does NOT flicker under lerp residue.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator RemoteLocomotionDeadzoneAbsorbsLerpResidueWithoutFlicker()
        {
            const float dt = 1f / 60f;
            var filter = new RemoteLocomotionFilter();
            filter.Reset(Vector3.zero);

            // Real movement at hub speed reads as walking with correct facing.
            var position = Vector3.zero;
            for (var i = 0; i < 30; i++)
            {
                position += new Vector3(4f * dt, 0f, 0f);
                filter.Step(position, dt);
            }

            Assert.That(filter.IsMoving, Is.True);
            Assert.That(filter.FacingX, Is.EqualTo(1f));

            // Movement stops; the network lerp leaves a decaying residual tail.
            var target = position + new Vector3(0.2f, 0f, 0f);
            var flickered = false;
            var becameIdle = false;
            for (var i = 0; i < 240; i++)
            {
                position = Vector3.Lerp(position, target, Mathf.Min(1f, 20f * dt));
                filter.Step(position, dt);

                if (!filter.IsMoving)
                {
                    becameIdle = true;
                }
                else if (becameIdle)
                {
                    flickered = true;
                }
            }

            Assert.That(becameIdle, Is.True, "The filter must settle to idle when motion stops.");
            Assert.That(flickered, Is.False, "Idle state must hold under lerp residue — no walk/idle flicker.");

            // Sub-deadzone jitter never re-enters walk and never flips facing.
            for (var i = 0; i < 120; i++)
            {
                position += new Vector3((i % 2 == 0 ? -1f : 1f) * 0.0008f, 0f, 0f);
                filter.Step(position, dt);
                Assert.That(filter.IsMoving, Is.False, "Tiny deltas below the deadzone must keep idle state stable.");
            }

            Assert.That(filter.FacingX, Is.EqualTo(1f), "Facing must not flip on jitter (hysteresis).");
            yield return null;
        }

        // ------------------------------------------------------------------
        // (c) Name tags render over both avatars in a harness session
        //     (host-only harness: host avatar + a local remote proxy).
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator NameTagsRenderOverHostAvatarAndRemoteProxy()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            // Host player avatar spawns with the session.
            PlayerAvatarNetwork hostAvatar = null;
            var deadline = Time.realtimeSinceStartup + 8f;
            while (Time.realtimeSinceStartup < deadline && hostAvatar == null)
            {
                hostAvatar = Object.FindFirstObjectByType<PlayerAvatarNetwork>();
                yield return null;
            }

            Assert.That(hostAvatar, Is.Not.Null, "The host player avatar should spawn in the harness session.");

            var hostTag = hostAvatar.GetComponent<AvatarNameTag>();
            Assert.That(hostTag, Is.Not.Null, "P16: the networked avatar must carry a name tag.");
            Assert.That(hostTag.Text, Does.Contain("(P1)"));
            Assert.That(hostTag.Text, Does.Contain(AvatarConfig.GetAvatar(hostAvatar.AvatarId).DisplayName));
            Assert.That(hostTag.Label, Is.Not.Null);
            Assert.That(hostTag.Label.transform.localPosition.y, Is.GreaterThan(0f), "The tag sits above the avatar.");

            var tagOrder = hostTag.Label.GetComponent<MeshRenderer>().sortingOrder;
            Assert.That(tagOrder, Is.InRange(300, 399), "Name tags live in the characters sorting band.");

            // Host-only harness: the second player is a local remote proxy with
            // the same identity-tag component the network layer attaches.
            var proxy = new GameObject("presence-remote-proxy", typeof(SpriteRenderer), typeof(AvatarRuntimeView));
            proxy.GetComponent<AvatarRuntimeView>().ApplyAvatar("care_captain");
            var proxyTag = proxy.AddComponent<AvatarNameTag>();
            proxyTag.Configure(AvatarNameTag.IdentityTextFor(1UL, AvatarConfig.GetAvatar("care_captain")));
            yield return null;

            Assert.That(proxyTag.Text, Is.EqualTo("Care Captain (P2)"));
            Assert.That(proxyTag.Label, Is.Not.Null);
            Assert.That(proxyTag.Label.gameObject.activeInHierarchy, Is.True);

            Object.Destroy(proxy);
            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        // ------------------------------------------------------------------
        // (d) First hub entry triggers the guide beat once; re-entry does not;
        //     the pointed door pulses.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator FirstHubEntryPlaysGuideBeatOnceAndPulsesNearestUnplayedDoor()
        {
            FirstRunGuideBeat.ResetSessionFlag();

            var appObject = new GameObject("presence-first-run-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            app.ShowAvatarSelectionForPlay();
            app.ChooseAvatar("care_captain");
            yield return null;

            var guideObject = GameObject.Find("CampusGuide");
            Assert.That(guideObject, Is.Not.Null);

            var beat = guideObject.GetComponent<FirstRunGuideBeat>();
            Assert.That(beat, Is.Not.Null);
            Assert.That(beat.DidPlay, Is.True, "P10: the first hub entry of the session plays the guide beat.");
            Assert.That(beat.GreetingLine, Does.Contain("Care Captain"), "The guide greets the chosen avatar by name.");

            var guide = guideObject.GetComponent<CampusGuideController>();
            Assert.That(guide.Bubble, Is.Not.Null, "The guide speaks through a SpeechBubble — no TextMesh remains.");
            Assert.That(guide.Bubble.IsVisible, Is.True);
            Assert.That(guide.Bubble.DisplayedText, Is.EqualTo(beat.GreetingLine));
            Assert.That(guideObject.GetComponentInChildren<TextMesh>(true), Is.Null,
                "The legacy GuidePrompt TextMesh must be gone.");

            // The pointed door is the nearest unplayed room and actually pulses.
            Assert.That(beat.PointedDoor, Is.Not.Null);
            Assert.That(beat.PointedRoute, Is.EqualTo(ActivityRoute.HealthHero),
                "From the player spawn, Health Hero is the nearest unplayed core room.");
            Assert.That(beat.PointedDoor.IsPulsing, Is.True);

            beat.PointedDoor.AutoTick = false;
            var scaleBefore = beat.PointedDoor.transform.localScale;
            beat.PointedDoor.Tick(0.37f);
            Assert.That((beat.PointedDoor.transform.localScale - scaleBefore).magnitude, Is.GreaterThan(0.001f),
                "The pointed door sign must pulse per DESIGN motion rules.");

            // The beat ends deterministically: pulse released, default prompt back.
            beat.AutoTick = false;
            beat.Tick(FirstRunGuideBeat.BeatDurationSeconds + 0.1f);
            Assert.That(beat.PointedDoor, Is.Null);
            Assert.That(guide.Bubble.DisplayedText, Is.EqualTo("Move to a door, then press E."));

            // Re-entry in the same session does not repeat the beat.
            app.ShowHealthHero();
            yield return null;
            app.ShowCampus();
            yield return null;

            var secondGuide = GameObject.Find("CampusGuide");
            Assert.That(secondGuide, Is.Not.Null);
            var secondBeat = secondGuide.GetComponent<FirstRunGuideBeat>();
            Assert.That(secondBeat.DidPlay, Is.False, "The beat plays once per session — re-entry must not repeat it.");
            Assert.That(secondGuide.GetComponent<CampusGuideController>().Bubble.DisplayedText,
                Is.EqualTo("Move to a door, then press E."));

            Object.Destroy(appObject);
        }

        // ------------------------------------------------------------------
        // (e) Speech bubble wraps/truncates at ≤ 2 lines on the longest line.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator SpeechBubbleKeepsLongestGuideLineWithinTwoLines()
        {
            var anchor = new GameObject("presence-bubble-anchor");
            var bubble = SpeechBubble.Attach(anchor.transform, new Vector3(0f, 1.5f, 0f));
            bubble.AutoTick = false;
            yield return null;

            // The longest REAL guide line: longest avatar name x longest core room label.
            var longestGreeting = AvatarConfig.Avatars
                .SelectMany(avatar => CareerQuestCatalog.All.Select(entry => FirstRunGuideBeat.GreetingFor(avatar, entry.BuildingName)))
                .OrderByDescending(line => line.Length)
                .First();

            bubble.Show(longestGreeting);
            Assert.That(bubble.IsVisible, Is.True);
            Assert.That(bubble.RenderedLineCount, Is.InRange(1, 2),
                $"DESIGN.md speech bubbles are one or two lines maximum — '{longestGreeting}' must wrap within 2.");

            // Pathologically long copy still truncates gracefully, never overflows.
            bubble.Show(string.Concat(System.Linq.Enumerable.Repeat("really very long guide sentence ", 12)));
            Assert.That(bubble.IsVisible, Is.True);
            Assert.That(bubble.RenderedLineCount, Is.InRange(1, 2), "Overlong text must truncate at 2 lines.");

            // Timed lines hide on the deterministic clock.
            bubble.Show("Hello!", 1f);
            bubble.Tick(0.5f);
            Assert.That(bubble.IsVisible, Is.True);
            bubble.Tick(0.6f);
            Assert.That(bubble.IsVisible, Is.False);

            Object.Destroy(anchor);
        }
    }
}
