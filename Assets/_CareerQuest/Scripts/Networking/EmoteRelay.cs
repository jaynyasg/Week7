using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>P16: the fixed kid-safe emote vocabulary. IDs, never text.</summary>
    public enum EmoteId : byte
    {
        Heart = 0,
        Star = 1,
        Wave = 2
    }

    /// <summary>
    /// P16 synced one-button emote. Lives as a second NetworkBehaviour on the
    /// always-spawned CampusSessionState scene object (no new network prefab).
    ///
    /// Flow: client SendEmote → SubmitEmoteRpc (server) → host-side per-sender
    /// rate limit (excess is dropped GENTLY: no response, no error — never
    /// punish) → ShowEmoteRpc (everyone) → each client renders a sprite-only
    /// bubble above the SENDER's avatar.
    ///
    /// Privacy boundary (no chat): the wire carries one byte — a fixed emote ID
    /// from <see cref="EmoteId"/>. The render path is pure iconography
    /// (<see cref="EmoteBubble"/> holds no TMP/Text component anywhere).
    ///
    /// Session lifecycle: the rate-limit clocks reset on network despawn
    /// (disconnect/shutdown); bubbles ride avatar objects, which despawn with
    /// the session. Solo play never shows the emote UI (see CareerQuestApp —
    /// there is no partner to wave at; hub toys carry solo delight).
    ///
    /// Deterministic clock: Tick(deltaSeconds) drives the rate-limit window;
    /// Update forwards Time.deltaTime only when AutoTick is on (house idiom).
    /// </summary>
    public class EmoteRelay : NetworkBehaviour
    {
        /// <summary>Host-side per-sender minimum interval between accepted emotes.</summary>
        public const float MinSecondsBetweenEmotes = 1f;

        /// <summary>How long the rendered bubble stays before self-hiding.</summary>
        public const float BubbleSeconds = 1.9f;

        public static EmoteRelay Instance { get; private set; }

        private readonly Dictionary<ulong, float> _lastAcceptedClock = new();
        private float _clock;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        // Render seams (instance-scoped — tests assert deltas, never globals).
        public int RenderedEmoteCount { get; private set; }
        public EmoteId LastRenderedEmote { get; private set; }
        public ulong LastRenderedClientId { get; private set; }

        public override void OnNetworkSpawn()
        {
            Instance = this;
        }

        public override void OnNetworkDespawn()
        {
            // Session-scoped flags reset on disconnect (System-Wide Impact).
            _lastAcceptedClock.Clear();
            _clock = 0f;

            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Client entry point — the emote bar buttons call this.</summary>
        public void SendEmote(EmoteId emote)
        {
            if (!IsSpawned)
            {
                return;
            }

            SubmitEmoteRpc((byte)emote);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitEmoteRpc(byte emote, RpcParams rpcParams = default)
        {
            ApplyEmote((EmoteId)emote, rpcParams.Receive.SenderClientId);
        }

        /// <summary>
        /// Host-side core (also the host-only 2P test seam — mirror of the room
        /// states' ApplySubmission). Returns false when the sender is inside its
        /// rate-limit window; the excess emote is dropped silently — no reject
        /// response, no error (gentle by design).
        /// </summary>
        public bool ApplyEmote(EmoteId emote, ulong senderClientId)
        {
            if (!IsSpawned || !IsServer)
            {
                return false;
            }

            if (_lastAcceptedClock.TryGetValue(senderClientId, out var lastAccepted)
                && _clock - lastAccepted < MinSecondsBetweenEmotes)
            {
                return false; // gently dropped
            }

            _lastAcceptedClock[senderClientId] = _clock;
            ShowEmoteRpc((byte)emote, senderClientId);
            return true;
        }

        [Rpc(SendTo.Everyone)]
        private void ShowEmoteRpc(byte emote, ulong senderClientId, RpcParams rpcParams = default)
        {
            ShowEmoteFor(senderClientId, (EmoteId)emote);
        }

        /// <summary>
        /// Render seam (every peer): bubble above the SENDER's avatar. Returns
        /// false when that client has no spawned avatar here (e.g. a simulated
        /// partner id in host-only tests) — nothing renders, nothing breaks.
        /// </summary>
        public bool ShowEmoteFor(ulong senderClientId, EmoteId emote)
        {
            var avatar = FindAvatar(senderClientId);
            if (avatar == null)
            {
                return false;
            }

            EmoteBubble.For(avatar.transform).Show(emote, BubbleSeconds);
            RenderedEmoteCount++;
            LastRenderedEmote = emote;
            LastRenderedClientId = senderClientId;
            AudioCueCatalog.TryPlay(AudioCueIds.EmotePop);
            return true;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds > 0f)
            {
                _clock += deltaSeconds;
            }
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private static PlayerAvatarNetwork FindAvatar(ulong clientId)
        {
            foreach (var avatar in FindObjectsByType<PlayerAvatarNetwork>(FindObjectsSortMode.None))
            {
                if (avatar.IsSpawned && avatar.OwnerClientId == clientId)
                {
                    return avatar;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Sprite-only emote bubble above an avatar — Kenney Emotes Style1 (white
    /// bubble + tail baked into the sprite). NO text component exists anywhere
    /// in this hierarchy: the no-chat privacy boundary is structural, not
    /// behavioral. Reused per avatar; pop-in scale per DESIGN motion rules,
    /// timed self-hide. Tick/AutoTick house clock idiom.
    /// </summary>
    public class EmoteBubble : MonoBehaviour
    {
        private const float PopInSeconds = 0.16f;
        private const float LocalYOffset = 1.66f; // above the name tag (1.18)
        private const float WorldHeight = 0.62f;
        private const int SortingOrder = 356;     // above name tag (344) / speech (352)

        private static readonly Dictionary<EmoteId, Sprite> SpriteCache = new();

        private SpriteRenderer _icon;
        private float _hideRemaining;
        private float _popElapsed = PopInSeconds;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public bool IsVisible { get; private set; }
        public EmoteId ShownEmote { get; private set; }

        /// <summary>Finds or creates the avatar's single reusable emote bubble.</summary>
        public static EmoteBubble For(Transform avatar)
        {
            var existing = avatar.GetComponentInChildren<EmoteBubble>(true);
            if (existing != null)
            {
                return existing;
            }

            var bubbleObject = new GameObject("EmoteBubble", typeof(EmoteBubble));
            bubbleObject.transform.SetParent(avatar, false);
            bubbleObject.transform.localPosition = new Vector3(0f, LocalYOffset, 0f);
            var bubble = bubbleObject.GetComponent<EmoteBubble>();
            bubble.EnsureBuilt();
            bubble.HideImmediate();
            return bubble;
        }

        /// <summary>
        /// Curated Style1 sprite for an emote id (fallback: shared circle so a
        /// missing curation never breaks the render path or adds text).
        /// </summary>
        public static Sprite SpriteFor(EmoteId emote)
        {
            if (SpriteCache.TryGetValue(emote, out var cached) && cached != null)
            {
                return cached;
            }

            var sprite = Resources.Load<Sprite>($"CareerQuest/Emote/{ResourceNameFor(emote)}");
            if (sprite == null)
            {
                sprite = CampusWorldSprites.Circle; // visible-but-safe fallback
            }

            SpriteCache[emote] = sprite;
            return sprite;
        }

        /// <summary>White over curated art; accent tint over the circle fallback.</summary>
        public static Color IconTint(EmoteId emote)
        {
            if (Resources.Load<Sprite>($"CareerQuest/Emote/{ResourceNameFor(emote)}") != null)
            {
                return Color.white;
            }

            return emote switch
            {
                EmoteId.Heart => new Color(0.969f, 0.424f, 0.369f), // Creative Coral
                EmoteId.Star => new Color(0.953f, 0.769f, 0.357f),  // Path Gold
                _ => new Color(0.290f, 0.616f, 0.922f)              // Science Blue
            };
        }

        public void Show(EmoteId emote, float durationSeconds)
        {
            EnsureBuilt();
            ShownEmote = emote;
            _icon.sprite = SpriteFor(emote);
            _icon.color = IconTint(emote);
            ApplyWorldSize();

            _hideRemaining = Mathf.Max(0.1f, durationSeconds);
            _popElapsed = 0f;
            IsVisible = true;
            _icon.gameObject.SetActive(true);
            ApplyPopScale();
        }

        public void Hide()
        {
            HideImmediate();
        }

        public void Tick(float deltaSeconds)
        {
            if (!IsVisible || deltaSeconds <= 0f)
            {
                return;
            }

            if (_popElapsed < PopInSeconds)
            {
                _popElapsed += deltaSeconds;
                ApplyPopScale();
            }

            _hideRemaining -= deltaSeconds;
            if (_hideRemaining <= 0f)
            {
                HideImmediate();
            }
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void HideImmediate()
        {
            IsVisible = false;
            _hideRemaining = 0f;
            if (_icon != null)
            {
                _icon.gameObject.SetActive(false);
            }
        }

        private void ApplyPopScale()
        {
            var t = Mathf.Clamp01(_popElapsed / PopInSeconds);
            var eased = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, eased);
        }

        private void ApplyWorldSize()
        {
            var bounds = _icon.sprite != null ? _icon.sprite.bounds.size : Vector3.one;
            var height = Mathf.Approximately(bounds.y, 0f) ? 1f : bounds.y;
            var scale = WorldHeight / height;
            _icon.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void EnsureBuilt()
        {
            if (_icon != null)
            {
                return;
            }

            // ONE SpriteRenderer. No TMP, no Text — ever (privacy invariant).
            var iconObject = new GameObject("EmoteIcon", typeof(SpriteRenderer));
            iconObject.transform.SetParent(transform, false);
            _icon = iconObject.GetComponent<SpriteRenderer>();
            _icon.sortingOrder = SortingOrder;
        }

        private static string ResourceNameFor(EmoteId emote)
        {
            // Curated by CareerQuestEmoteArtCurator from Kenney Emotes Style1:
            // heart → emote_heart, star → emote_star, wave → emote_faceHappy
            // (the friendly hello — the pack has no literal hand-wave).
            return emote switch
            {
                EmoteId.Heart => "emote.heart",
                EmoteId.Star => "emote.star",
                _ => "emote.wave"
            };
        }
    }
}
