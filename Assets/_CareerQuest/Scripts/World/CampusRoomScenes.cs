using UnityEngine;

namespace CareerQuest
{
    internal static class CampusRoomScenes
    {
        public static void ShowDesignBuild(CampusWorldBuilder builder, GameSession session)
        {
            // U6: the room route mounts the authored DesignBuildStudio prefab
            // (visual-only, no NetworkObject). When the prefab has not been built
            // yet, a code-built diorama with EMPTY lots keeps the room playable —
            // the controller's drag playfield supplies pieces in both paths.
            if (!TryMountDesignBuildStudio(builder))
            {
                builder.AddCatalogSprite("DesignBuildRoomBackdrop", "room.design_build", new Vector2(0f, 0.12f), new Vector2(7.4f, 4.16f), 0);
                builder.AddPath(new Vector2(0f, -1.65f), new Vector2(8.6f, 0.36f), 0f);
                BuildFallbackTable(builder);
                builder.AddCatalogSprite("DesignBuildBlueprintProp", "prop.blueprint", new Vector2(-3.15f, -0.55f), new Vector2(0.62f, 0.62f), 7);
            }

            builder.AddCharacter(session?.SelectedAvatar.DisplayName ?? "Planner", -3.6f, -1.35f, session?.SelectedAvatar.ShirtColor ?? CampusWorldPalette.PlayerTeal, 0.2f, true, session?.SelectedAvatar.SpriteAssetId, false);
            AddBuilderNpc(builder);
        }

        private static bool TryMountDesignBuildStudio(CampusWorldBuilder builder)
        {
            var prefab = Resources.Load<GameObject>(DesignBuildStudioLayout.PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"DesignBuildStudio prefab missing at Resources/{DesignBuildStudioLayout.PrefabResourcePath} — " +
                    "run 'Career Quest/World/Build Design Build Studio Prefab' " +
                    "(CareerQuestRoomPrefabBuilder.BuildDesignBuildStudio). Falling back to the code-built room.");
                return false;
            }

            var instance = Object.Instantiate(prefab, builder.Root);
            instance.name = "DesignBuildStudio";
            return true;
        }

        /// <summary>Fallback blueprint table with EMPTY lots (pieces live in the drag tray).</summary>
        private static void BuildFallbackTable(CampusWorldBuilder builder)
        {
            builder.AddShape("BuildTable", CampusSpriteKind.Square, new Vector2(0f, -0.45f), new Vector2(6.6f, 1.05f), CampusWorldPalette.Plaza, 3);
            var blueprint = FutureCityBlueprint.CreateDefault();
            for (var i = 0; i < blueprint.Pieces.Count; i++)
            {
                var lot = DesignBuildStudioLayout.SlotPosition(i);
                builder.AddShape($"{blueprint.Pieces[i].DisplayName}LotPad", CampusSpriteKind.Circle, new Vector2(lot.x, lot.y - 0.25f), new Vector2(0.95f, 0.22f), CampusWorldPalette.Shadow, 4);
            }

            builder.AddShape("TrayBoard", CampusSpriteKind.Square, new Vector2(0f, DesignBuildStudioLayout.TrayPosition(0).y - 0.05f), new Vector2(6.2f, 1.0f), CampusWorldPalette.Plaza, 2);
        }

        /// <summary>
        /// The builder partner NPC is created in code for both paths (prefab and
        /// fallback) so the controller's P14 cheer hook always finds the same
        /// AvatarRuntimeView + name.
        /// </summary>
        private static void AddBuilderNpc(CampusWorldBuilder builder)
        {
            var npcObject = new GameObject(DesignBuildStudioLayout.BuilderNpcName, typeof(SpriteRenderer), typeof(AvatarRuntimeView));
            npcObject.transform.SetParent(builder.Root, false);
            npcObject.transform.localPosition = new Vector3(
                DesignBuildStudioLayout.NpcPosition.x,
                DesignBuildStudioLayout.NpcPosition.y,
                0f);
            builder.AddShape("BuilderNpcShadow", CampusSpriteKind.Circle, new Vector2(0f, -0.52f), new Vector2(0.62f, 0.18f), CampusWorldPalette.Shadow, 307, 0f, npcObject.transform);
            npcObject.GetComponent<AvatarRuntimeView>().ApplySpriteAsset("npc.builder_partner");
        }

        public static void ShowClinic(CampusWorldBuilder builder, GameSession session)
        {
            builder.AddCatalogSprite("HealthHeroRoomBackdrop", "room.health_hero", new Vector2(0f, 0.18f), new Vector2(8.35f, 4.7f), 0);
            builder.AddShape("ClinicExamBedShadow", CampusSpriteKind.Circle, new Vector2(-1.82f, -0.88f), new Vector2(2.25f, 0.36f), CampusWorldPalette.Shadow, 2);
            builder.AddShape("ClinicExamBed", CampusSpriteKind.Square, new Vector2(-1.82f, -0.66f), new Vector2(2.25f, 0.42f), CampusWorldPalette.Window, 3);
            builder.AddShape("ClinicExamPillow", CampusSpriteKind.Square, new Vector2(-2.58f, -0.43f), new Vector2(0.5f, 0.22f), CampusWorldPalette.Plaza, 4);
            builder.AddShape("ClinicCareCounterShadow", CampusSpriteKind.Circle, new Vector2(1.88f, -0.9f), new Vector2(2.5f, 0.36f), CampusWorldPalette.Shadow, 2);
            builder.AddShape("ClinicCareCounter", CampusSpriteKind.Square, new Vector2(1.88f, -0.62f), new Vector2(2.72f, 0.62f), CampusWorldPalette.Plaza, 3);
            builder.AddCatalogSprite("Thermometer", "prop.thermometer", new Vector2(1.16f, -0.4f), new Vector2(0.64f, 0.72f), 5);
            builder.AddCatalogSprite("CarePlan", "prop.care_plan", new Vector2(2.05f, -0.42f), new Vector2(0.72f, 0.72f), 5);
            builder.AddShape("ClinicWallBadge", CampusSpriteKind.Circle, new Vector2(-3.08f, 0.72f), new Vector2(0.55f, 0.55f), CampusWorldPalette.Mint, 3);
            builder.AddShape("ClinicWallCrossA", CampusSpriteKind.Square, new Vector2(-3.08f, 0.72f), new Vector2(0.12f, 0.4f), Color.white, 4);
            builder.AddShape("ClinicWallCrossB", CampusSpriteKind.Square, new Vector2(-3.08f, 0.72f), new Vector2(0.4f, 0.12f), Color.white, 4);
            builder.AddCharacter("Patient", -1.82f, -1.04f, CampusWorldPalette.Mint, 1.2f, true, "npc.patient", false);
            builder.AddCharacter(session?.SelectedAvatar.DisplayName ?? "Care Lead", 0.38f, -1.35f, session?.SelectedAvatar.ShirtColor ?? CampusWorldPalette.PlayerBlue, 0.3f, true, session?.SelectedAvatar.SpriteAssetId, false);
        }

        public static void ShowCourt(CampusWorldBuilder builder, GameSession session)
        {
            builder.AddCatalogSprite("LogicCourtRoomBackdrop", "room.logic_court", new Vector2(0f, 0.18f), new Vector2(8.35f, 4.7f), 0);
            builder.AddShape("JudgeBenchShadow", CampusSpriteKind.Circle, new Vector2(-2.05f, -0.62f), new Vector2(2.34f, 0.42f), CampusWorldPalette.Shadow, 2);
            builder.AddShape("JudgeBench", CampusSpriteKind.Square, new Vector2(-2.05f, -0.34f), new Vector2(2.3f, 0.72f), CampusWorldPalette.GoldRoof, 3);
            builder.AddShape("JudgeBenchFront", CampusSpriteKind.Square, new Vector2(-2.05f, -0.62f), new Vector2(2.48f, 0.22f), CampusWorldPalette.Gold, 4);
            builder.AddShape("HelpfulZone", CampusSpriteKind.Square, new Vector2(1.03f, -0.56f), new Vector2(1.35f, 0.78f), new Color(0.74f, 0.93f, 0.76f, 0.86f), 3);
            builder.AddShape("ReviewZone", CampusSpriteKind.Square, new Vector2(2.48f, -0.56f), new Vector2(1.35f, 0.78f), new Color(0.83f, 0.9f, 1f, 0.86f), 3);
            builder.AddEvidence("Test", 0.72f, -0.42f, CampusWorldPalette.Mint);
            builder.AddEvidence("Plan", 1.45f, -0.42f, CampusWorldPalette.SkyBlue);
            builder.AddEvidence("Paint", 2.5f, -0.42f, CampusWorldPalette.Lilac);
            builder.AddCatalogSprite("ArgumentMeter", "prop.argument_meter", new Vector2(3.3f, 0.42f), new Vector2(0.8f, 0.8f), 4);
            builder.AddCharacter("Judge", -2.05f, -1.02f, CampusWorldPalette.PlayerGold, 1.6f, true, "npc.judge", false);
            builder.AddCharacter(session?.SelectedAvatar.DisplayName ?? "Speaker", 0.04f, -1.35f, session?.SelectedAvatar.ShirtColor ?? CampusWorldPalette.PlayerGold, 0.8f, true, session?.SelectedAvatar.SpriteAssetId, false);
        }

        public static void ShowGallery(CampusWorldBuilder builder, GameSession session)
        {
            builder.AddCatalogSprite("GalleryRoomBackdrop", "room.gallery", new Vector2(0f, 0.12f), new Vector2(7.4f, 4.16f), 0);
            builder.AddShape("GalleryShelfA", CampusSpriteKind.Square, new Vector2(0f, 1.08f), new Vector2(5.4f, 0.12f), CampusWorldPalette.TealRoof, 3);
            builder.AddShape("GalleryShelfB", CampusSpriteKind.Square, new Vector2(0f, 0f), new Vector2(5.4f, 0.12f), CampusWorldPalette.CoralRoof, 3);
            builder.AddBadge("Build", -1.9f, 1.35f, CampusWorldPalette.Coral);
            builder.AddBadge("Care", 0f, 1.35f, CampusWorldPalette.Mint);
            builder.AddBadge("Logic", 1.9f, 1.35f, CampusWorldPalette.Gold);
            builder.AddCharacter("Explorer", -3.25f, -1.45f, CampusWorldPalette.PlayerBlue, 0.4f, true, session?.SelectedAvatar.SpriteAssetId);
            builder.AddCharacter("Guide", 3.25f, -1.45f, CampusWorldPalette.PlayerTeal, 1.2f, true, "npc.campus_guide");
        }

        public static void ShowOptionalRoom(CampusWorldBuilder builder, GameSession session, CatalogEntry entry)
        {
            builder.AddCatalogSprite(
                $"{entry.Id}RoomBackdrop",
                entry.CampusAssetId,
                new Vector2(0f, 0.12f),
                new Vector2(7.4f, 4.16f),
                0);
            builder.AddPath(new Vector2(0f, -1.65f), new Vector2(8.6f, 0.36f), 0f);
            builder.AddCatalogSprite(
                $"{entry.Id}RoomProp",
                "prop.evidence_card",
                new Vector2(0f, -0.35f),
                new Vector2(0.82f, 0.82f),
                6);
            builder.AddCharacter(
                session?.SelectedAvatar.DisplayName ?? "Explorer",
                -1.2f,
                -1.35f,
                session?.SelectedAvatar.ShirtColor ?? CampusWorldPalette.PlayerTeal,
                0.2f,
                true,
                session?.SelectedAvatar.SpriteAssetId,
                false);
            builder.AddCharacter("Guide", 1.65f, -1.35f, CampusWorldPalette.PlayerBlue, 1.4f, true, "npc.campus_guide", false);
        }

        public static void ShowReveal(CampusWorldBuilder builder, GameSession session)
        {
            builder.AddCatalogSprite("RevealRoomBackdrop", "room.reveal", new Vector2(0f, 0.12f), new Vector2(7.4f, 4.16f), 0);
            builder.AddShape("RevealStageShadow", CampusSpriteKind.Circle, new Vector2(0f, -0.9f), new Vector2(5.6f, 1.2f), CampusWorldPalette.Shadow, 1);
            builder.AddShape("RevealStage", CampusSpriteKind.Circle, new Vector2(0f, -0.76f), new Vector2(5.2f, 1f), CampusWorldPalette.Plaza, 2);
            builder.AddShape("RevealBeamA", CampusSpriteKind.Square, new Vector2(-1.1f, 0.65f), new Vector2(0.5f, 3.7f), CampusWorldPalette.LightBeamGold, 1, -12f);
            builder.AddShape("RevealBeamB", CampusSpriteKind.Square, new Vector2(1.1f, 0.65f), new Vector2(0.5f, 3.7f), CampusWorldPalette.LightBeamBlue, 1, 12f);
            builder.AddCharacter(session?.SelectedAvatar.DisplayName ?? "Future Path", 0f, -1.25f, session?.SelectedAvatar.ShirtColor ?? CampusWorldPalette.PlayerGold, 0f, true, session?.SelectedAvatar.SpriteAssetId);
        }
    }
}
