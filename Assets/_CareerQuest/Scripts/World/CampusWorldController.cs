using UnityEngine;

namespace CareerQuest
{
    public class CampusWorldController : MonoBehaviour
    {
        private static Sprite _squareSprite;
        private static Sprite _circleSprite;

        private Transform _root;
        private Camera _camera;

        private const int WorldLabelFontSize = 28;
        private const float BuildingLabelSize = 0.033f;
        private const float SmallBuildingLabelSize = 0.027f;
        private const float CharacterLabelSize = 0.028f;
        private const float ItemLabelSize = 0.034f;

        public static CampusWorldController Ensure()
        {
            var existing = FindFirstObjectByType<CampusWorldController>();
            if (existing != null)
            {
                existing.EnsureSetup();
                return existing;
            }

            var world = new GameObject("CampusWorld", typeof(CampusWorldController));
            var controller = world.GetComponent<CampusWorldController>();
            controller.EnsureSetup();
            return controller;
        }

        public void ShowEntry(GameSession session)
        {
            BuildCampus("Entry");
            AddHeroCharacters(session, session?.PlayerCount > 1);
        }

        public void ShowConnection(GameSession session)
        {
            BuildCampus("Connection");
            AddNetworkProof(-2.2f, 0.1f, "Host", Colors.PlayerBlue);
            AddNetworkProof(2.2f, 0.1f, "Join", Colors.PlayerGold);
        }

        public void ShowCampus(GameSession session)
        {
            BuildCampus("Campus");
            AddHeroCharacters(session, session != null && (session.PlayerCount > 1 || session.Mode == AppMode.Showcase));
        }

        public void ShowProof(GameSession session)
        {
            ClearWorld();
            AddSky();
            AddGround();
            AddPath(new Vector2(0f, -0.9f), new Vector2(7.2f, 0.42f), 0f);
            AddBuilding("Shared Campus", 0f, 1.25f, 2.25f, 1.3f, Colors.Mint, Colors.TealRoof, 3);
            AddNetworkProof(-2.6f, -0.85f, "P1 Builder", Colors.PlayerBlue);
            AddNetworkProof(2.6f, -0.85f, "P2 Designer", Colors.PlayerGold);
            AddShape("ProofPulseA", SpriteKind.Circle, new Vector2(-2.6f, -0.85f), new Vector2(1.35f, 1.35f), Colors.PlayerBlueSoft, 1);
            AddShape("ProofPulseB", SpriteKind.Circle, new Vector2(2.6f, -0.85f), new Vector2(1.35f, 1.35f), Colors.PlayerGoldSoft, 1);
        }

        public void ShowDesignBuild(GameSession session)
        {
            ClearWorld();
            AddCatalogSprite("DesignBuildRoomBackdrop", "room.design_build", new Vector2(0f, 0.12f), new Vector2(7.4f, 4.16f), 0);
            AddPath(new Vector2(0f, -1.65f), new Vector2(8.6f, 0.36f), 0f);
            AddBuildTable();
            AddCatalogSprite("DesignBuildBlueprintProp", "prop.blueprint", new Vector2(-3.15f, -0.55f), new Vector2(0.62f, 0.62f), 7);
            AddCharacter(session?.SelectedAvatar.DisplayName ?? "Planner", -3.6f, -1.35f, session?.SelectedAvatar.ShirtColor ?? Colors.PlayerTeal, 0.2f, true, session?.SelectedAvatar.SpriteAssetId, false);
            AddCharacter("Builder", 3.65f, -1.33f, Colors.PlayerBlue, 1.7f, true, "npc.builder_partner", false);
        }

        public void ShowClinic(GameSession session)
        {
            ClearWorld();
            AddCatalogSprite("HealthHeroRoomBackdrop", "room.health_hero", new Vector2(0f, 0.18f), new Vector2(8.35f, 4.7f), 0);
            AddShape("ClinicExamBedShadow", SpriteKind.Circle, new Vector2(-1.82f, -0.88f), new Vector2(2.25f, 0.36f), Colors.Shadow, 2);
            AddShape("ClinicExamBed", SpriteKind.Square, new Vector2(-1.82f, -0.66f), new Vector2(2.25f, 0.42f), Colors.Window, 3);
            AddShape("ClinicExamPillow", SpriteKind.Square, new Vector2(-2.58f, -0.43f), new Vector2(0.5f, 0.22f), Colors.Plaza, 4);
            AddShape("ClinicCareCounterShadow", SpriteKind.Circle, new Vector2(1.88f, -0.9f), new Vector2(2.5f, 0.36f), Colors.Shadow, 2);
            AddShape("ClinicCareCounter", SpriteKind.Square, new Vector2(1.88f, -0.62f), new Vector2(2.72f, 0.62f), Colors.Plaza, 3);
            AddCatalogSprite("Thermometer", "prop.thermometer", new Vector2(1.16f, -0.4f), new Vector2(0.64f, 0.72f), 5);
            AddCatalogSprite("CarePlan", "prop.care_plan", new Vector2(2.05f, -0.42f), new Vector2(0.72f, 0.72f), 5);
            AddShape("ClinicWallBadge", SpriteKind.Circle, new Vector2(-3.08f, 0.72f), new Vector2(0.55f, 0.55f), Colors.Mint, 3);
            AddShape("ClinicWallCrossA", SpriteKind.Square, new Vector2(-3.08f, 0.72f), new Vector2(0.12f, 0.4f), Color.white, 4);
            AddShape("ClinicWallCrossB", SpriteKind.Square, new Vector2(-3.08f, 0.72f), new Vector2(0.4f, 0.12f), Color.white, 4);
            AddCharacter("Patient", -1.82f, -1.04f, Colors.Mint, 1.2f, true, "npc.patient", false);
            AddCharacter(session?.SelectedAvatar.DisplayName ?? "Care Lead", 0.38f, -1.35f, session?.SelectedAvatar.ShirtColor ?? Colors.PlayerBlue, 0.3f, true, session?.SelectedAvatar.SpriteAssetId, false);
        }

        public void ShowCourt(GameSession session)
        {
            ClearWorld();
            AddCatalogSprite("LogicCourtRoomBackdrop", "room.logic_court", new Vector2(0f, 0.18f), new Vector2(8.35f, 4.7f), 0);
            AddShape("JudgeBenchShadow", SpriteKind.Circle, new Vector2(-2.05f, -0.62f), new Vector2(2.34f, 0.42f), Colors.Shadow, 2);
            AddShape("JudgeBench", SpriteKind.Square, new Vector2(-2.05f, -0.34f), new Vector2(2.3f, 0.72f), Colors.GoldRoof, 3);
            AddShape("JudgeBenchFront", SpriteKind.Square, new Vector2(-2.05f, -0.62f), new Vector2(2.48f, 0.22f), Colors.Gold, 4);
            AddShape("HelpfulZone", SpriteKind.Square, new Vector2(1.03f, -0.56f), new Vector2(1.35f, 0.78f), new Color(0.74f, 0.93f, 0.76f, 0.86f), 3);
            AddShape("ReviewZone", SpriteKind.Square, new Vector2(2.48f, -0.56f), new Vector2(1.35f, 0.78f), new Color(0.83f, 0.9f, 1f, 0.86f), 3);
            AddEvidence("Test", 0.72f, -0.42f, Colors.Mint);
            AddEvidence("Plan", 1.45f, -0.42f, Colors.SkyBlue);
            AddEvidence("Paint", 2.5f, -0.42f, Colors.Lilac);
            AddCatalogSprite("ArgumentMeter", "prop.argument_meter", new Vector2(3.3f, 0.42f), new Vector2(0.8f, 0.8f), 4);
            AddCharacter("Judge", -2.05f, -1.02f, Colors.PlayerGold, 1.6f, true, "npc.judge", false);
            AddCharacter(session?.SelectedAvatar.DisplayName ?? "Speaker", 0.04f, -1.35f, session?.SelectedAvatar.ShirtColor ?? Colors.PlayerGold, 0.8f, true, session?.SelectedAvatar.SpriteAssetId, false);
        }

        public void ShowGallery(GameSession session)
        {
            ClearWorld();
            AddCatalogSprite("GalleryRoomBackdrop", "room.gallery", new Vector2(0f, 0.12f), new Vector2(7.4f, 4.16f), 0);
            AddShape("GalleryShelfA", SpriteKind.Square, new Vector2(0f, 1.08f), new Vector2(5.4f, 0.12f), Colors.TealRoof, 3);
            AddShape("GalleryShelfB", SpriteKind.Square, new Vector2(0f, 0f), new Vector2(5.4f, 0.12f), Colors.CoralRoof, 3);
            AddBadge("Build", -1.9f, 1.35f, Colors.Coral);
            AddBadge("Care", 0f, 1.35f, Colors.Mint);
            AddBadge("Logic", 1.9f, 1.35f, Colors.Gold);
            AddCharacter("Explorer", -3.25f, -1.45f, Colors.PlayerBlue, 0.4f, true, session?.SelectedAvatar.SpriteAssetId);
            AddCharacter("Guide", 3.25f, -1.45f, Colors.PlayerTeal, 1.2f, true, "npc.campus_guide");
        }

        public void ShowReveal(GameSession session)
        {
            ClearWorld();
            AddCatalogSprite("RevealRoomBackdrop", "room.reveal", new Vector2(0f, 0.12f), new Vector2(7.4f, 4.16f), 0);
            AddShape("RevealStageShadow", SpriteKind.Circle, new Vector2(0f, -0.9f), new Vector2(5.6f, 1.2f), Colors.Shadow, 1);
            AddShape("RevealStage", SpriteKind.Circle, new Vector2(0f, -0.76f), new Vector2(5.2f, 1f), Colors.Plaza, 2);
            AddShape("RevealBeamA", SpriteKind.Square, new Vector2(-1.1f, 0.65f), new Vector2(0.5f, 3.7f), Colors.LightBeamGold, 1, -12f);
            AddShape("RevealBeamB", SpriteKind.Square, new Vector2(1.1f, 0.65f), new Vector2(0.5f, 3.7f), Colors.LightBeamBlue, 1, 12f);
            AddCharacter(session?.SelectedAvatar.DisplayName ?? "Future Path", 0f, -1.25f, session?.SelectedAvatar.ShirtColor ?? Colors.PlayerGold, 0f, true, session?.SelectedAvatar.SpriteAssetId);
        }

        private void EnsureSetup()
        {
            if (_root == null)
            {
                _root = transform;
            }

            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                _camera = cameraObject.GetComponent<Camera>();
            }

            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Colors.Sky;
            _camera.orthographic = true;
            _camera.orthographicSize = 4.15f;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private void BuildCampus(string name)
        {
            ClearWorld();
            AddSky();
            AddGround();
            AddPath(new Vector2(0f, -0.96f), new Vector2(8.6f, 0.42f), 0f);
            AddPath(new Vector2(0f, -0.5f), new Vector2(0.46f, 3.2f), 0f);
            AddShape($"{name}PlazaShadow", SpriteKind.Circle, new Vector2(0f, -0.72f), new Vector2(1.72f, 0.72f), Colors.Shadow, 1);
            AddShape($"{name}Plaza", SpriteKind.Circle, new Vector2(0f, -0.62f), new Vector2(1.55f, 0.62f), Colors.Plaza, 2);

            AddBuilding("Design Build Studio", -3.0f, 0.8f, 2.15f, 1.55f, Colors.Coral, Colors.CoralRoof, 4);
            AddBuilding("Health Hero Clinic", 0f, 1f, 2.1f, 1.45f, Colors.Mint, Colors.TealRoof, 4);
            AddBuilding("Logic Court", 3.0f, 0.8f, 2.15f, 1.55f, Colors.Gold, Colors.GoldRoof, 4);

            AddSmallBuilding("AI Lab", -4.45f, -1.75f, Colors.SkyBlue);
            AddSmallBuilding("Music Studio", -2.05f, -2f, Colors.Lilac);
            AddSmallBuilding("Robotics", 2.05f, -2f, Colors.Teal);
            AddSmallBuilding("Kitchen", 4.45f, -1.75f, Colors.Leaf);
            AddTree(-4.8f, 1f);
            AddTree(4.8f, 1.1f);
            AddTree(-4.8f, -0.65f);
            AddTree(4.8f, -0.55f);
        }

        private void AddSky()
        {
            AddShape("Sun", SpriteKind.Circle, new Vector2(4.75f, 2.75f), new Vector2(0.8f, 0.8f), Colors.Sun, 0);
            AddCloud(-4.35f, 2.75f);
            AddCloud(2.25f, 2.95f);
        }

        private void AddGround()
        {
            AddShape("CampusGrass", SpriteKind.Square, new Vector2(0f, -2.25f), new Vector2(11.2f, 3.25f), Colors.Grass, 0);
        }

        private void AddPath(Vector2 position, Vector2 size, float rotation)
        {
            AddShape($"Path_{position.x}_{position.y}", SpriteKind.Square, position, size, Colors.Path, 1, rotation);
        }

        private void AddBuildTable()
        {
            AddShape("BuildTable", SpriteKind.Square, new Vector2(0f, -0.45f), new Vector2(6.6f, 1.05f), Colors.Plaza, 3);
            AddSkylineLot("Clinic", -2.45f, -0.35f, Colors.Mint);
            AddSkylineLot("Court", -1.2f, -0.35f, Colors.Gold);
            AddSkylineLot("Studio", 0f, -0.35f, Colors.Coral);
            AddSkylineLot("Lab", 1.2f, -0.35f, Colors.SkyBlue);
            AddSkylineLot("Art", 2.45f, -0.35f, Colors.Lilac);
            AddShape("CraneMast", SpriteKind.Square, new Vector2(4.35f, 0.65f), new Vector2(0.14f, 2.4f), Colors.TealRoof, 4);
            AddShape("CraneArm", SpriteKind.Square, new Vector2(3.55f, 1.72f), new Vector2(1.8f, 0.14f), Colors.TealRoof, 4);
            AddShape("CraneHook", SpriteKind.Square, new Vector2(2.78f, 1.35f), new Vector2(0.1f, 0.74f), Colors.CoralRoof, 4);
        }

        private void AddBuilding(string label, float x, float y, float width, float height, Color body, Color roof, int order)
        {
            AddShape($"{label}Shadow", SpriteKind.Square, new Vector2(x + 0.08f, y - 0.08f), new Vector2(width + 0.25f, height + 0.15f), Colors.Shadow, order - 1);
            AddCatalogSprite($"{label}Sprite", CampusAssetIdFor(label), new Vector2(x, y + 0.02f), new Vector2(width + 0.28f, height + 0.28f), order);
            AddLabel($"{label}Label", ShortBuildingLabel(label), x, y - height * 0.66f, BuildingLabelSize, Colors.Ink, order + 5);
        }

        private void AddSmallBuilding(string label, float x, float y, Color body)
        {
            AddShape($"{label}SmallShadow", SpriteKind.Square, new Vector2(x + 0.06f, y - 0.06f), new Vector2(1.28f, 0.82f), Colors.Shadow, 2);
            AddCatalogSprite($"{label}SmallSprite", CampusAssetIdFor(label), new Vector2(x, y + 0.04f), new Vector2(1.28f, 0.96f), 3);
            AddLabel($"{label}SmallLabel", label, x, y - 0.58f, SmallBuildingLabelSize, Colors.Ink, 8);
        }

        private void AddSkylineLot(string label, float x, float y, Color body)
        {
            AddShape($"{label}Pad", SpriteKind.Circle, new Vector2(x, y - 0.52f), new Vector2(0.95f, 0.22f), Colors.Shadow, 4);
            AddCatalogSprite($"{label}PieceSprite", PropAssetIdFor(label), new Vector2(x, y + 0.08f), new Vector2(0.95f, 0.95f), 5);
            AddLabel($"{label}LotLabel", label, x, y - 0.86f, ItemLabelSize, Colors.Ink, 8);
        }

        private void AddNetworkProof(float x, float y, string label, Color color)
        {
            AddShape($"{label}Ring", SpriteKind.Circle, new Vector2(x, y - 0.2f), new Vector2(1.4f, 0.46f), Colors.Plaza, 2);
            AddCharacter(label, x, y, color, x, true, label.Contains("Join") ? "avatar.logic_spark" : "avatar.sky_builder");
        }

        private void AddHeroCharacters(GameSession session, bool includeSecond)
        {
            AddCharacter(session?.SelectedAvatar.DisplayName ?? "Explorer", -0.6f, -1.2f, session?.SelectedAvatar.ShirtColor ?? Colors.PlayerBlue, 0.2f, true, session?.SelectedAvatar.SpriteAssetId);
            if (includeSecond)
            {
                AddCharacter("Designer", 0.75f, -1.25f, Colors.PlayerGold, 1.3f, true, "avatar.logic_spark");
            }

            AddCharacter("Campus Guide", 1.75f, -1.55f, Colors.PlayerTeal, 2.4f, true, "npc.campus_guide");
        }

        private void AddCharacter(string label, float x, float y, Color shirt, float phase, bool animated, string assetId = null, bool showLabel = true)
        {
            var group = new GameObject(label);
            group.transform.SetParent(_root, false);
            group.transform.position = new Vector3(x, y, 0f);

            AddShape($"{label}Shadow", SpriteKind.Circle, new Vector2(0f, -0.52f), new Vector2(0.62f, 0.18f), Colors.Shadow, 7, 0f, group.transform);

            if (!string.IsNullOrWhiteSpace(assetId))
            {
                AddCatalogSprite($"{label}Sprite", assetId, new Vector2(0f, 0.02f), new Vector2(0.86f, 1.16f), 10, 0f, group.transform);
            }
            else
            {
                AddShape($"{label}LegA", SpriteKind.Square, new Vector2(-0.1f, -0.34f), new Vector2(0.12f, 0.36f), Colors.Door, 8, 0f, group.transform);
                AddShape($"{label}LegB", SpriteKind.Square, new Vector2(0.1f, -0.34f), new Vector2(0.12f, 0.36f), Colors.Door, 8, 0f, group.transform);
                AddShape($"{label}Body", SpriteKind.Square, new Vector2(0f, 0f), new Vector2(0.48f, 0.55f), shirt, 9, 0f, group.transform);
                AddShape($"{label}Pack", SpriteKind.Square, new Vector2(0.31f, 0.02f), new Vector2(0.16f, 0.38f), Colors.CoralRoof, 8, 0f, group.transform);
                AddShape($"{label}Head", SpriteKind.Circle, new Vector2(0f, 0.48f), new Vector2(0.43f, 0.43f), Colors.Skin, 10, 0f, group.transform);
                AddShape($"{label}Hair", SpriteKind.Circle, new Vector2(-0.03f, 0.62f), new Vector2(0.4f, 0.18f), Colors.Hair, 11, 0f, group.transform);
            }

            if (showLabel)
            {
                AddLabel($"{label}Label", label, 0f, -0.83f, CharacterLabelSize, Colors.Ink, 12, group.transform);
            }

            if (animated)
            {
                var character = group.AddComponent<CampusWorldCharacter>();
                character.Configure(phase, 0.055f);
            }
        }

        private void AddTree(float x, float y)
        {
            AddShape($"TreeTrunk{x}_{y}", SpriteKind.Square, new Vector2(x, y - 0.26f), new Vector2(0.16f, 0.5f), Colors.Door, 4);
            AddShape($"TreeTopA{x}_{y}", SpriteKind.Circle, new Vector2(x - 0.18f, y + 0.1f), new Vector2(0.58f, 0.58f), Colors.Leaf, 5);
            AddShape($"TreeTopB{x}_{y}", SpriteKind.Circle, new Vector2(x + 0.18f, y + 0.13f), new Vector2(0.58f, 0.58f), Colors.Leaf, 5);
            AddShape($"TreeTopC{x}_{y}", SpriteKind.Circle, new Vector2(x, y + 0.38f), new Vector2(0.62f, 0.62f), Colors.LeafLight, 6);
        }

        private void AddCloud(float x, float y)
        {
            AddShape($"CloudA{x}", SpriteKind.Circle, new Vector2(x - 0.25f, y), new Vector2(0.72f, 0.36f), Colors.Cloud, 0);
            AddShape($"CloudB{x}", SpriteKind.Circle, new Vector2(x + 0.22f, y + 0.1f), new Vector2(0.82f, 0.46f), Colors.Cloud, 0);
            AddShape($"CloudC{x}", SpriteKind.Circle, new Vector2(x + 0.72f, y), new Vector2(0.68f, 0.34f), Colors.Cloud, 0);
        }

        private void AddEvidence(string label, float x, float y, Color color)
        {
            AddCatalogSprite($"{label}Evidence", "prop.evidence_card", new Vector2(x, y), new Vector2(0.68f, 0.52f), 5);
            AddLabel($"{label}EvidenceLabel", label, x, y, ItemLabelSize, Colors.Ink, 6);
        }

        private void AddBadge(string label, float x, float y, Color color)
        {
            AddCatalogSprite($"{label}Badge", BadgeAssetIdFor(label), new Vector2(x, y), new Vector2(0.88f, 0.88f), 4);
            AddLabel($"{label}BadgeLabel", label, x, y, ItemLabelSize, Colors.Ink, 6);
        }

        private static string ShortBuildingLabel(string label)
        {
            return label switch
            {
                "Design Build Studio" => "Design Build",
                "Health Hero Clinic" => "Health Hero",
                _ => label
            };
        }

        private GameObject AddCatalogSprite(
            string name,
            string assetId,
            Vector2 position,
            Vector2 targetSize,
            int order,
            float rotation = 0f,
            Transform parent = null)
        {
            var spriteObject = new GameObject(name, typeof(SpriteRenderer));
            spriteObject.transform.SetParent(parent != null ? parent : _root, false);
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

        private static string CampusAssetIdFor(string label)
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

        private static string PropAssetIdFor(string label)
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

        private static string BadgeAssetIdFor(string label)
        {
            return label switch
            {
                "Build" => "badge.design_build",
                "Care" => "badge.health_hero",
                "Logic" => "badge.logic_court",
                _ => "badge.reveal_ready"
            };
        }

        private GameObject AddShape(string name, SpriteKind kind, Vector2 position, Vector2 size, Color color, int order, float rotation = 0f, Transform parent = null)
        {
            var shape = new GameObject(name, typeof(SpriteRenderer));
            shape.transform.SetParent(parent != null ? parent : _root, false);
            shape.transform.localPosition = new Vector3(position.x, position.y, 0f);
            shape.transform.localScale = new Vector3(size.x, size.y, 1f);
            shape.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            var renderer = shape.GetComponent<SpriteRenderer>();
            renderer.sprite = kind == SpriteKind.Circle ? CircleSprite : SquareSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return shape;
        }

        private TextMesh AddLabel(string name, string text, float x, float y, float characterSize, Color color, int order, Transform parent = null)
        {
            var labelObject = new GameObject(name, typeof(TextMesh));
            labelObject.transform.SetParent(parent != null ? parent : _root, false);
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

        private void ClearWorld()
        {
            EnsureSetup();
            for (var i = _root.childCount - 1; i >= 0; i--)
            {
                Destroy(_root.GetChild(i).gameObject);
            }
        }

        private static Sprite SquareSprite
        {
            get
            {
                if (_squareSprite != null)
                {
                    return _squareSprite;
                }

                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                return _squareSprite;
            }
        }

        private static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite != null)
                {
                    return _circleSprite;
                }

                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                var center = (size - 1) * 0.5f;
                var radius = center;

                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var dx = x - center;
                        var dy = y - center;
                        var alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 1f);
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                _circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                return _circleSprite;
            }
        }

        private enum SpriteKind
        {
            Square,
            Circle
        }

        private static class Colors
        {
            public static readonly Color Sky = new(0.69f, 0.9f, 1f);
            public static readonly Color Cloud = new(1f, 1f, 1f, 0.86f);
            public static readonly Color Sun = new(1f, 0.84f, 0.27f);
            public static readonly Color Grass = new(0.55f, 0.82f, 0.5f);
            public static readonly Color Leaf = new(0.25f, 0.64f, 0.3f);
            public static readonly Color LeafLight = new(0.48f, 0.78f, 0.36f);
            public static readonly Color Path = new(0.9f, 0.72f, 0.42f);
            public static readonly Color Plaza = new(1f, 0.92f, 0.64f);
            public static readonly Color Shadow = new(0.06f, 0.08f, 0.1f, 0.18f);
            public static readonly Color Ink = new(0.05f, 0.09f, 0.11f);
            public static readonly Color Coral = new(0.94f, 0.34f, 0.28f);
            public static readonly Color CoralRoof = new(0.55f, 0.12f, 0.12f);
            public static readonly Color Mint = new(0.36f, 0.78f, 0.6f);
            public static readonly Color Teal = new(0.13f, 0.55f, 0.58f);
            public static readonly Color TealRoof = new(0.04f, 0.3f, 0.32f);
            public static readonly Color Gold = new(0.96f, 0.62f, 0.18f);
            public static readonly Color GoldRoof = new(0.68f, 0.36f, 0.03f);
            public static readonly Color SkyBlue = new(0.28f, 0.66f, 0.94f);
            public static readonly Color BlueRoof = new(0.08f, 0.26f, 0.55f);
            public static readonly Color Lilac = new(0.62f, 0.52f, 0.86f);
            public static readonly Color SoftGold = new(0.92f, 0.82f, 0.54f);
            public static readonly Color Window = new(0.83f, 0.96f, 1f);
            public static readonly Color Door = new(0.18f, 0.16f, 0.13f);
            public static readonly Color Skin = new(0.78f, 0.52f, 0.34f);
            public static readonly Color Hair = new(0.12f, 0.08f, 0.06f);
            public static readonly Color PlayerBlue = new(0.12f, 0.43f, 0.86f);
            public static readonly Color PlayerGold = new(0.93f, 0.55f, 0.12f);
            public static readonly Color PlayerTeal = new(0.05f, 0.55f, 0.5f);
            public static readonly Color PlayerBlueSoft = new(0.24f, 0.58f, 0.95f, 0.25f);
            public static readonly Color PlayerGoldSoft = new(0.95f, 0.62f, 0.16f, 0.25f);
            public static readonly Color LightBeamGold = new(1f, 0.9f, 0.34f, 0.35f);
            public static readonly Color LightBeamBlue = new(0.55f, 0.85f, 1f, 0.35f);
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
