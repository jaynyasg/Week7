using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CareerQuest
{
    public static class AssetCatalog
    {
        public const int MaxFallbackTextureSize = 512;

        private static readonly AssetDefinition[] _definitions =
        {
            Avatar("avatar.sky_builder", "Sky Builder", new Color(0.12f, 0.43f, 0.86f), new Color(0.83f, 0.96f, 1f)),
            Avatar("avatar.sky_builder.walk", "Sky Builder Walk", new Color(0.12f, 0.43f, 0.86f), new Color(0.83f, 0.96f, 1f)),
            Avatar("avatar.care_captain", "Care Captain", new Color(0.05f, 0.55f, 0.5f), new Color(0.36f, 0.78f, 0.6f)),
            Avatar("avatar.care_captain.walk", "Care Captain Walk", new Color(0.05f, 0.55f, 0.5f), new Color(0.36f, 0.78f, 0.6f)),
            Avatar("avatar.logic_spark", "Logic Spark", new Color(0.93f, 0.55f, 0.12f), new Color(0.96f, 0.86f, 0.35f)),
            Avatar("avatar.logic_spark.walk", "Logic Spark Walk", new Color(0.93f, 0.55f, 0.12f), new Color(0.96f, 0.86f, 0.35f)),
            Avatar("avatar.art_inventor", "Art Inventor", new Color(0.62f, 0.52f, 0.86f), new Color(0.94f, 0.34f, 0.28f)),
            Avatar("avatar.art_inventor.walk", "Art Inventor Walk", new Color(0.62f, 0.52f, 0.86f), new Color(0.94f, 0.34f, 0.28f)),

            Npc("npc.campus_guide", "Campus Guide", new Color(0.05f, 0.55f, 0.5f), new Color(1f, 0.92f, 0.64f)),
            Npc("npc.builder_partner", "Builder Partner", new Color(0.12f, 0.43f, 0.86f), new Color(0.93f, 0.55f, 0.12f)),
            Npc("npc.patient", "Clinic Patient", new Color(0.36f, 0.78f, 0.6f), new Color(0.83f, 0.96f, 1f)),
            Npc("npc.judge", "Logic Judge", new Color(0.96f, 0.62f, 0.18f), new Color(0.68f, 0.36f, 0.03f)),

            Campus("campus.design_build_studio", "Design Build Studio", new Color(0.94f, 0.34f, 0.28f), new Color(0.55f, 0.12f, 0.12f)),
            Campus("campus.health_hero_clinic", "Health Hero Clinic", new Color(0.36f, 0.78f, 0.6f), new Color(0.04f, 0.3f, 0.32f)),
            Campus("campus.logic_court", "Logic Court", new Color(0.96f, 0.62f, 0.18f), new Color(0.68f, 0.36f, 0.03f)),
            Campus("campus.achievement_gallery", "Achievement Gallery", new Color(0.92f, 0.82f, 0.54f), new Color(0.13f, 0.55f, 0.58f)),
            Campus("campus.reveal_stage", "Career Reveal Stage", new Color(1f, 0.92f, 0.64f), new Color(0.28f, 0.66f, 0.94f)),
            Campus("campus.space_lab", "Space Lab", new Color(0.28f, 0.66f, 0.94f), new Color(0.08f, 0.26f, 0.55f), false),
            Campus("campus.music_studio", "Music Studio", new Color(0.62f, 0.52f, 0.86f), new Color(0.94f, 0.34f, 0.28f), false),
            Campus("campus.green_energy_center", "Green Energy Center", new Color(0.25f, 0.64f, 0.3f), new Color(0.48f, 0.78f, 0.36f), false),
            Campus("campus.robotics_garage", "Robotics Garage", new Color(0.13f, 0.55f, 0.58f), new Color(0.08f, 0.26f, 0.55f), false),
            Campus("campus.community_kitchen", "Community Kitchen", new Color(0.55f, 0.82f, 0.5f), new Color(0.96f, 0.62f, 0.18f), false),

            Room("room.design_build", "Future City Room", new Color(0.94f, 0.34f, 0.28f), new Color(0.9f, 0.72f, 0.42f)),
            Room("room.health_hero", "Health Hero Room", new Color(0.36f, 0.78f, 0.6f), new Color(0.83f, 0.96f, 1f)),
            Room("room.logic_court", "Logic Court Room", new Color(0.96f, 0.62f, 0.18f), new Color(0.62f, 0.52f, 0.86f)),
            Room("room.gallery", "Achievement Gallery Room", new Color(0.92f, 0.82f, 0.54f), new Color(0.13f, 0.55f, 0.58f)),
            Room("room.reveal", "Reveal Ceremony Room", new Color(1f, 0.92f, 0.64f), new Color(0.55f, 0.85f, 1f)),

            Prop("prop.blueprint", "Blueprint", new Color(0.83f, 0.96f, 1f), new Color(0.08f, 0.26f, 0.55f)),
            Prop("prop.city_piece_clinic", "Clinic City Piece", new Color(0.36f, 0.78f, 0.6f), new Color(0.04f, 0.3f, 0.32f)),
            Prop("prop.city_piece_court", "Court City Piece", new Color(0.96f, 0.62f, 0.18f), new Color(0.68f, 0.36f, 0.03f)),
            Prop("prop.city_piece_studio", "Studio City Piece", new Color(0.94f, 0.34f, 0.28f), new Color(0.55f, 0.12f, 0.12f)),
            Prop("prop.city_piece_lab", "Lab City Piece", new Color(0.28f, 0.66f, 0.94f), new Color(0.08f, 0.26f, 0.55f)),
            Prop("prop.city_piece_art_tower", "Art Tower City Piece", new Color(0.62f, 0.52f, 0.86f), new Color(0.94f, 0.34f, 0.28f)),
            Prop("prop.thermometer", "Thermometer", new Color(0.94f, 0.34f, 0.28f), new Color(1f, 1f, 1f)),
            Prop("prop.care_plan", "Care Plan", new Color(0.36f, 0.78f, 0.6f), new Color(1f, 0.92f, 0.64f)),
            Prop("prop.evidence_card", "Evidence Card", new Color(0.83f, 0.96f, 1f), new Color(0.96f, 0.62f, 0.18f)),
            Prop("prop.argument_meter", "Argument Meter", new Color(0.62f, 0.52f, 0.86f), new Color(1f, 0.92f, 0.64f)),

            Badge("badge.design_build", "Design Build Badge", new Color(0.94f, 0.34f, 0.28f), new Color(1f, 0.92f, 0.64f)),
            Badge("badge.health_hero", "Health Hero Badge", new Color(0.36f, 0.78f, 0.6f), new Color(0.83f, 0.96f, 1f)),
            Badge("badge.logic_court", "Logic Court Badge", new Color(0.96f, 0.62f, 0.18f), new Color(0.62f, 0.52f, 0.86f)),
            Badge("badge.reveal_ready", "Reveal Ready Badge", new Color(1f, 0.92f, 0.64f), new Color(0.28f, 0.66f, 0.94f)),

            Ui("ui.exit", "Exit Game Icon", new Color(0.09f, 0.31f, 0.42f), new Color(1f, 1f, 1f)),
            Ui("ui.gallery", "Gallery Icon", new Color(0.92f, 0.82f, 0.54f), new Color(0.13f, 0.55f, 0.58f)),
            Ui("ui.reveal_locked", "Reveal Locked Icon", new Color(0.42f, 0.42f, 0.46f), new Color(1f, 0.92f, 0.64f)),
            Ui("ui.reveal_unlocked", "Reveal Unlocked Icon", new Color(1f, 0.92f, 0.64f), new Color(0.28f, 0.66f, 0.94f)),
            Ui("ui.confirm", "Confirm Icon", new Color(0.13f, 0.55f, 0.58f), new Color(1f, 1f, 1f)),
            Ui("ui.back", "Back Icon", new Color(0.08f, 0.26f, 0.55f), new Color(1f, 1f, 1f))
        };

        private static readonly Dictionary<string, AssetDefinition> _definitionsById = _definitions.ToDictionary(definition => definition.Id);
        private static readonly Dictionary<string, SpriteResolution> _resolutionCache = new();

        public static IReadOnlyList<AssetDefinition> Definitions => _definitions;
        public static IReadOnlyList<AssetDefinition> RequiredDefinitions => _definitions.Where(definition => definition.RequiredInFirstPlayable).ToArray();
        public static IReadOnlyList<AssetDefinition> PlayerFacingDefinitions => _definitions.Where(definition => definition.RequiresFinalArtForPlayerFacingAcceptance).ToArray();

        public static bool TryGetDefinition(string id, out AssetDefinition definition)
        {
            definition = null;
            return !string.IsNullOrWhiteSpace(id) && _definitionsById.TryGetValue(id, out definition);
        }

        public static AssetDefinition GetDefinition(string id)
        {
            return TryGetDefinition(id, out var definition) ? definition : null;
        }

        public static Sprite SpriteFor(string id)
        {
            return ResolveSprite(id).Sprite;
        }

        public static string SpriteIdForLocomotion(string baseSpriteAssetId, bool isMoving)
        {
            if (!isMoving || string.IsNullOrWhiteSpace(baseSpriteAssetId))
            {
                return baseSpriteAssetId;
            }

            var walkId = baseSpriteAssetId.EndsWith(".walk") ? baseSpriteAssetId : $"{baseSpriteAssetId}.walk";
            return TryGetDefinition(walkId, out _) ? walkId : baseSpriteAssetId;
        }

        public static SpriteResolution ResolveSprite(string id)
        {
            var cacheKey = string.IsNullOrWhiteSpace(id) ? string.Empty : id;
            if (_resolutionCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            SpriteResolution resolution;
            if (string.IsNullOrWhiteSpace(id))
            {
                resolution = new SpriteResolution(id ?? string.Empty, null, SpriteFallbackFactory.CreateMissing("empty"), true, true);
                _resolutionCache[cacheKey] = resolution;
                return resolution;
            }

            if (!TryGetDefinition(id, out var definition))
            {
                var missing = SpriteFallbackFactory.CreateMissing(id);
                resolution = new SpriteResolution(id, null, missing, true, true);
                _resolutionCache[cacheKey] = resolution;
                return resolution;
            }

            var importedSprite = Resources.Load<Sprite>(definition.ResourcePath);
            var importedTexture = importedSprite == null ? Resources.Load<Texture2D>(definition.ResourcePath) : null;
            var isFallbackGenerated = importedSprite == null && importedTexture == null;
            var sprite = importedSprite ?? (importedTexture != null ? CreateSpriteFromTexture(definition, importedTexture) : SpriteFallbackFactory.Create(definition));
            resolution = new SpriteResolution(id, definition, sprite, isFallbackGenerated, false);
            _resolutionCache[cacheKey] = resolution;
            return resolution;
        }

        public static IReadOnlyList<SpriteResolution> ResolvePlayerFacingSprites()
        {
            return PlayerFacingDefinitions.Select(definition => ResolveSprite(definition.Id)).ToArray();
        }

        public static IReadOnlyList<SpriteResolution> PlayerFacingFallbackUsage()
        {
            return ResolvePlayerFacingSprites().Where(resolution => resolution.IsPlayerFacingFallback).ToArray();
        }

        public static bool IsFallbackSprite(Sprite sprite)
        {
            return SpriteFallbackFactory.IsFallbackSprite(sprite);
        }

        public static bool IsFinalArtSprite(Sprite sprite)
        {
            return TryGetDisplayedSpriteInfo(sprite, out var resolution) && resolution.IsFinalArt;
        }

        public static bool TryGetDisplayedSpriteInfo(Sprite sprite, out SpriteResolution resolution)
        {
            resolution = null;
            if (sprite == null)
            {
                return false;
            }

            resolution = _resolutionCache.Values.FirstOrDefault(candidate => ReferenceEquals(candidate.Sprite, sprite));
            if (resolution != null)
            {
                return true;
            }

            resolution = new SpriteResolution(sprite.name, null, sprite, SpriteFallbackFactory.IsFallbackSprite(sprite), false);
            return true;
        }

        private static AssetDefinition Avatar(string id, string displayName, Color primary, Color accent)
        {
            return new AssetDefinition(id, displayName, AssetCategory.Avatar, primary, accent, new Vector2Int(192, 256));
        }

        private static AssetDefinition Npc(string id, string displayName, Color primary, Color accent)
        {
            return new AssetDefinition(id, displayName, AssetCategory.Npc, primary, accent, new Vector2Int(192, 256));
        }

        private static AssetDefinition Campus(string id, string displayName, Color primary, Color accent, bool required = true)
        {
            return new AssetDefinition(id, displayName, AssetCategory.Campus, primary, accent, new Vector2Int(256, 192), required, required);
        }

        private static AssetDefinition Room(string id, string displayName, Color primary, Color accent)
        {
            return new AssetDefinition(id, displayName, AssetCategory.Room, primary, accent, new Vector2Int(384, 216));
        }

        private static AssetDefinition Prop(string id, string displayName, Color primary, Color accent)
        {
            return new AssetDefinition(id, displayName, AssetCategory.Prop, primary, accent, new Vector2Int(128, 128));
        }

        private static AssetDefinition Badge(string id, string displayName, Color primary, Color accent)
        {
            return new AssetDefinition(id, displayName, AssetCategory.Badge, primary, accent, new Vector2Int(128, 128));
        }

        private static AssetDefinition Ui(string id, string displayName, Color primary, Color accent)
        {
            return new AssetDefinition(id, displayName, AssetCategory.Ui, primary, accent, new Vector2Int(96, 96));
        }

        private static Sprite CreateSpriteFromTexture(AssetDefinition definition, Texture2D texture)
        {
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = definition.Id;
            return sprite;
        }

        public sealed class SpriteResolution
        {
            internal SpriteResolution(
                string requestedId,
                AssetDefinition definition,
                Sprite sprite,
                bool isFallbackGenerated,
                bool isMissingDefinition)
            {
                RequestedId = requestedId;
                Definition = definition;
                Sprite = sprite;
                IsFallbackGenerated = isFallbackGenerated;
                IsMissingDefinition = isMissingDefinition;
            }

            public string RequestedId { get; }
            public AssetDefinition Definition { get; }
            public Sprite Sprite { get; }
            public bool IsFallbackGenerated { get; }
            public bool IsMissingDefinition { get; }
            public bool IsCataloged => Definition != null;
            public bool IsFinalArt => IsCataloged && Sprite != null && !IsFallbackGenerated && !IsMissingDefinition;
            public bool IsPlayerFacingFallback => Definition != null && Definition.RequiresFinalArtForPlayerFacingAcceptance && IsFallbackGenerated;
            public string ResourcePath => Definition?.ResourcePath ?? string.Empty;
        }
    }
}
