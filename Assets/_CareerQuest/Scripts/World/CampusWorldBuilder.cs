using UnityEngine;

namespace CareerQuest
{
    internal sealed class CampusWorldBuilder
    {
        public const int WorldLabelFontSize = 28;
        public const float BuildingLabelSize = 0.033f;
        public const float SmallBuildingLabelSize = 0.027f;
        public const float CharacterLabelSize = 0.028f;
        public const float ItemLabelSize = 0.034f;

        public CampusWorldBuilder(Transform root)
        {
            Root = root;
        }

        public Transform Root { get; }

        public void ClearWorld()
        {
            for (var i = Root.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(Root.GetChild(i).gameObject);
            }
        }

        public GameObject AddFullScreenVeil()
        {
            return AddShape("RoomVeil", CampusSpriteKind.Square, Vector2.zero, new Vector2(12f, 9f), CampusWorldPalette.Veil, 100);
        }

        public void AddSky()
        {
            AddShape("Sun", CampusSpriteKind.Circle, new Vector2(4.75f, 2.75f), new Vector2(0.8f, 0.8f), CampusWorldPalette.Sun, 0);
            AddCloud(-4.35f, 2.75f);
            AddCloud(2.25f, 2.95f);
        }

        public void AddGround()
        {
            AddShape("CampusGrass", CampusSpriteKind.Square, new Vector2(0f, -2.25f), new Vector2(11.2f, 3.25f), CampusWorldPalette.Grass, 0);
        }

        public void AddPath(Vector2 position, Vector2 size, float rotation)
        {
            AddShape($"Path_{position.x}_{position.y}", CampusSpriteKind.Square, position, size, CampusWorldPalette.Path, 1, rotation);
        }

        public void AddPlaza(string name)
        {
            AddShape($"{name}PlazaShadow", CampusSpriteKind.Circle, new Vector2(0f, -0.72f), new Vector2(1.72f, 0.72f), CampusWorldPalette.Shadow, 1);
            AddShape($"{name}Plaza", CampusSpriteKind.Circle, new Vector2(0f, -0.62f), new Vector2(1.55f, 0.62f), CampusWorldPalette.Plaza, 2);
        }

        public void AddBuildTable()
        {
            AddShape("BuildTable", CampusSpriteKind.Square, new Vector2(0f, -0.45f), new Vector2(6.6f, 1.05f), CampusWorldPalette.Plaza, 3);
            AddSkylineLot("Clinic", -2.45f, -0.35f, CampusWorldPalette.Mint);
            AddSkylineLot("Court", -1.2f, -0.35f, CampusWorldPalette.Gold);
            AddSkylineLot("Studio", 0f, -0.35f, CampusWorldPalette.Coral);
            AddSkylineLot("Lab", 1.2f, -0.35f, CampusWorldPalette.SkyBlue);
            AddSkylineLot("Art", 2.45f, -0.35f, CampusWorldPalette.Lilac);
            AddShape("CraneMast", CampusSpriteKind.Square, new Vector2(4.35f, 0.65f), new Vector2(0.14f, 2.4f), CampusWorldPalette.TealRoof, 4);
            AddShape("CraneArm", CampusSpriteKind.Square, new Vector2(3.55f, 1.72f), new Vector2(1.8f, 0.14f), CampusWorldPalette.TealRoof, 4);
            AddShape("CraneHook", CampusSpriteKind.Square, new Vector2(2.78f, 1.35f), new Vector2(0.1f, 0.74f), CampusWorldPalette.CoralRoof, 4);
        }

        public void AddBuilding(string label, float x, float y, float width, float height, Color body, Color roof, int order)
        {
            AddShape($"{label}Shadow", CampusSpriteKind.Square, new Vector2(x + 0.08f, y - 0.08f), new Vector2(width + 0.25f, height + 0.15f), CampusWorldPalette.Shadow, order - 1);
            AddCatalogSprite($"{label}Sprite", CampusAssetIdFor(label), new Vector2(x, y + 0.02f), new Vector2(width + 0.28f, height + 0.28f), order);
            AddLabel($"{label}Label", ShortBuildingLabel(label), x, y - height * 0.66f, BuildingLabelSize, CampusWorldPalette.Ink, order + 5);
        }

        public void AddSmallBuilding(string label, float x, float y, Color body)
        {
            AddShape($"{label}SmallShadow", CampusSpriteKind.Square, new Vector2(x + 0.06f, y - 0.06f), new Vector2(1.28f, 0.82f), CampusWorldPalette.Shadow, 2);
            AddCatalogSprite($"{label}SmallSprite", CampusAssetIdFor(label), new Vector2(x, y + 0.04f), new Vector2(1.28f, 0.96f), 3);
            AddLabel($"{label}SmallLabel", label, x, y - 0.58f, SmallBuildingLabelSize, CampusWorldPalette.Ink, 8);
        }

        public void AddSkylineLot(string label, float x, float y, Color body)
        {
            AddShape($"{label}Pad", CampusSpriteKind.Circle, new Vector2(x, y - 0.52f), new Vector2(0.95f, 0.22f), CampusWorldPalette.Shadow, 4);
            AddCatalogSprite($"{label}PieceSprite", PropAssetIdFor(label), new Vector2(x, y + 0.08f), new Vector2(0.95f, 0.95f), 5);
            AddLabel($"{label}LotLabel", label, x, y - 0.86f, ItemLabelSize, CampusWorldPalette.Ink, 8);
        }

        public void AddNetworkProof(float x, float y, string label, Color color)
        {
            AddShape($"{label}Ring", CampusSpriteKind.Circle, new Vector2(x, y - 0.2f), new Vector2(1.4f, 0.46f), CampusWorldPalette.Plaza, 2);
            AddCharacter(label, x, y, color, x, true, label.Contains("Join") ? "avatar.logic_spark" : "avatar.sky_builder");
        }

        public void AddHeroCharacters(GameSession session, bool includeSecond)
        {
            AddCharacter(session?.SelectedAvatar.DisplayName ?? "Explorer", -0.6f, -1.2f, session?.SelectedAvatar.ShirtColor ?? CampusWorldPalette.PlayerBlue, 0.2f, true, session?.SelectedAvatar.SpriteAssetId);
            if (includeSecond)
            {
                AddCharacter("Designer", 0.75f, -1.25f, CampusWorldPalette.PlayerGold, 1.3f, true, "avatar.logic_spark");
            }

            AddCharacter("Campus Guide", 1.75f, -1.55f, CampusWorldPalette.PlayerTeal, 2.4f, true, "npc.campus_guide");
        }

        public void AddCharacter(string label, float x, float y, Color shirt, float phase, bool animated, string assetId = null, bool showLabel = true)
        {
            var group = new GameObject(label);
            group.transform.SetParent(Root, false);
            group.transform.position = new Vector3(x, y, 0f);

            AddShape($"{label}Shadow", CampusSpriteKind.Circle, new Vector2(0f, -0.52f), new Vector2(0.62f, 0.18f), CampusWorldPalette.Shadow, 7, 0f, group.transform);

            if (!string.IsNullOrWhiteSpace(assetId))
            {
                AddCatalogSprite($"{label}Sprite", assetId, new Vector2(0f, 0.02f), new Vector2(0.86f, 1.16f), 10, 0f, group.transform);
            }
            else
            {
                AddShape($"{label}LegA", CampusSpriteKind.Square, new Vector2(-0.1f, -0.34f), new Vector2(0.12f, 0.36f), CampusWorldPalette.Door, 8, 0f, group.transform);
                AddShape($"{label}LegB", CampusSpriteKind.Square, new Vector2(0.1f, -0.34f), new Vector2(0.12f, 0.36f), CampusWorldPalette.Door, 8, 0f, group.transform);
                AddShape($"{label}Body", CampusSpriteKind.Square, new Vector2(0f, 0f), new Vector2(0.48f, 0.55f), shirt, 9, 0f, group.transform);
                AddShape($"{label}Pack", CampusSpriteKind.Square, new Vector2(0.31f, 0.02f), new Vector2(0.16f, 0.38f), CampusWorldPalette.CoralRoof, 8, 0f, group.transform);
                AddShape($"{label}Head", CampusSpriteKind.Circle, new Vector2(0f, 0.48f), new Vector2(0.43f, 0.43f), CampusWorldPalette.Skin, 10, 0f, group.transform);
                AddShape($"{label}Hair", CampusSpriteKind.Circle, new Vector2(-0.03f, 0.62f), new Vector2(0.4f, 0.18f), CampusWorldPalette.Hair, 11, 0f, group.transform);
            }

            if (showLabel)
            {
                AddLabel($"{label}Label", label, 0f, -0.83f, CharacterLabelSize, CampusWorldPalette.Ink, 12, group.transform);
            }

            if (animated)
            {
                var character = group.AddComponent<CampusWorldCharacter>();
                character.Configure(phase, 0.055f);
            }
        }

        public void AddTree(float x, float y)
        {
            AddShape($"TreeTrunk{x}_{y}", CampusSpriteKind.Square, new Vector2(x, y - 0.26f), new Vector2(0.16f, 0.5f), CampusWorldPalette.Door, 4);
            AddShape($"TreeTopA{x}_{y}", CampusSpriteKind.Circle, new Vector2(x - 0.18f, y + 0.1f), new Vector2(0.58f, 0.58f), CampusWorldPalette.Leaf, 5);
            AddShape($"TreeTopB{x}_{y}", CampusSpriteKind.Circle, new Vector2(x + 0.18f, y + 0.13f), new Vector2(0.58f, 0.58f), CampusWorldPalette.Leaf, 5);
            AddShape($"TreeTopC{x}_{y}", CampusSpriteKind.Circle, new Vector2(x, y + 0.38f), new Vector2(0.62f, 0.62f), CampusWorldPalette.LeafLight, 6);
        }

        public void AddCloud(float x, float y)
        {
            AddShape($"CloudA{x}", CampusSpriteKind.Circle, new Vector2(x - 0.25f, y), new Vector2(0.72f, 0.36f), CampusWorldPalette.Cloud, 0);
            AddShape($"CloudB{x}", CampusSpriteKind.Circle, new Vector2(x + 0.22f, y + 0.1f), new Vector2(0.82f, 0.46f), CampusWorldPalette.Cloud, 0);
            AddShape($"CloudC{x}", CampusSpriteKind.Circle, new Vector2(x + 0.72f, y), new Vector2(0.68f, 0.34f), CampusWorldPalette.Cloud, 0);
        }

        public void AddEvidence(string label, float x, float y, Color color)
        {
            AddCatalogSprite($"{label}Evidence", "prop.evidence_card", new Vector2(x, y), new Vector2(0.68f, 0.52f), 5);
            AddLabel($"{label}EvidenceLabel", label, x, y, ItemLabelSize, CampusWorldPalette.Ink, 6);
        }

        public void AddBadge(string label, float x, float y, Color color)
        {
            AddCatalogSprite($"{label}Badge", BadgeAssetIdFor(label), new Vector2(x, y), new Vector2(0.88f, 0.88f), 4);
            AddLabel($"{label}BadgeLabel", label, x, y, ItemLabelSize, CampusWorldPalette.Ink, 6);
        }

        public GameObject AddCatalogSprite(
            string name,
            string assetId,
            Vector2 position,
            Vector2 targetSize,
            int order,
            float rotation = 0f,
            Transform parent = null)
        {
            var spriteObject = new GameObject(name, typeof(SpriteRenderer));
            spriteObject.transform.SetParent(parent != null ? parent : Root, false);
            spriteObject.transform.localPosition = new Vector3(position.x, position.y, 0f);
            spriteObject.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            var renderer = spriteObject.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetCatalog.SpriteFor(assetId);
            renderer.color = Color.white;
            renderer.sortingOrder = order;

            var bounds = renderer.sprite != null ? renderer.sprite.bounds.size : Vector3.one;
            var width = Mathf.Approximately(bounds.x, 0f) ? 1f : bounds.x;
            var height = Mathf.Approximately(bounds.y, 0f) ? 1f : bounds.y;
            spriteObject.transform.localScale = new Vector3(targetSize.x / width, targetSize.y / height, 1f);
            return spriteObject;
        }

        public GameObject AddShape(string name, CampusSpriteKind kind, Vector2 position, Vector2 size, Color color, int order, float rotation = 0f, Transform parent = null)
        {
            var shape = new GameObject(name, typeof(SpriteRenderer));
            shape.transform.SetParent(parent != null ? parent : Root, false);
            shape.transform.localPosition = new Vector3(position.x, position.y, 0f);
            shape.transform.localScale = new Vector3(size.x, size.y, 1f);
            shape.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            var renderer = shape.GetComponent<SpriteRenderer>();
            renderer.sprite = kind == CampusSpriteKind.Circle ? CampusWorldSprites.Circle : CampusWorldSprites.Square;
            renderer.color = color;
            renderer.sortingOrder = order;
            return shape;
        }

        public TextMesh AddLabel(string name, string text, float x, float y, float characterSize, Color color, int order, Transform parent = null)
        {
            var labelObject = new GameObject(name, typeof(TextMesh));
            labelObject.transform.SetParent(parent != null ? parent : Root, false);
            labelObject.transform.localPosition = new Vector3(x, y, 0f);

            var label = labelObject.GetComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = characterSize;
            label.fontSize = WorldLabelFontSize;
            label.color = color;

            var renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.sortingOrder = order;
            return label;
        }

        public static string ShortBuildingLabel(string label)
        {
            return label switch
            {
                "Design Build Studio" => "Design Build",
                "Health Hero Clinic" => "Health Hero",
                _ => label
            };
        }

        public static string CampusAssetIdFor(string label)
        {
            return label switch
            {
                "Design Build Studio" => "campus.design_build_studio",
                "Health Hero Clinic" => "campus.health_hero_clinic",
                "Logic Court" => "campus.logic_court",
                "Achievement Gallery" => "campus.achievement_gallery",
                "Career Reveal Stage" => "campus.reveal_stage",
                "AI Lab" => "campus.space_lab",
                "Music Studio" => "campus.music_studio",
                "Robotics" => "campus.robotics_garage",
                "Kitchen" => "campus.community_kitchen",
                _ => "campus.achievement_gallery"
            };
        }

        public static string PropAssetIdFor(string label)
        {
            return label switch
            {
                "Clinic" => "prop.city_piece_clinic",
                "Court" => "prop.city_piece_court",
                "Studio" => "prop.city_piece_studio",
                "Lab" => "prop.city_piece_lab",
                "Art" => "prop.city_piece_art_tower",
                _ => "prop.blueprint"
            };
        }

        public static string BadgeAssetIdFor(string label)
        {
            return label switch
            {
                "Build" => "badge.design_build",
                "Care" => "badge.health_hero",
                "Logic" => "badge.logic_court",
                _ => "badge.reveal_ready"
            };
        }
    }

    public class CampusWorldCharacter : MonoBehaviour
    {
        private float _phase;
        private float _amplitude;
        private Vector3 _start;

        public void Configure(float phase, float amplitude)
        {
            _phase = phase;
            _amplitude = amplitude;
            _start = transform.localPosition;
        }

        private void Start()
        {
            _start = transform.localPosition;
        }

        private void Update()
        {
            transform.localPosition = _start + new Vector3(0f, Mathf.Sin(Time.time * 2.6f + _phase) * _amplitude, 0f);
        }
    }
}
