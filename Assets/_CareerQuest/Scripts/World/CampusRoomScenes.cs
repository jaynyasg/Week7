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
            // U10: the room route mounts the authored HealthHeroClinic prefab
            // (visual-only, no NetworkObject). When the prefab has not been built
            // yet, a code-built diorama with an EMPTY patient zone keeps the room
            // playable — the controller's drag playfield supplies the care tools
            // in both paths.
            if (!TryMountRoomPrefab(
                builder,
                HealthHeroClinicLayout.PrefabResourcePath,
                "HealthHeroClinic",
                "Career Quest/World/Build Health Hero Clinic Prefab",
                "CareerQuestRoomPrefabBuilder.BuildHealthHeroClinic"))
            {
                BuildFallbackClinic(builder);
            }

            builder.AddCharacter(session?.SelectedAvatar.DisplayName ?? "Care Lead", 0.38f, -1.35f, session?.SelectedAvatar.ShirtColor ?? CampusWorldPalette.PlayerBlue, 0.3f, true, session?.SelectedAvatar.SpriteAssetId, false);
            AddPatientNpc(builder);
        }

        /// <summary>Fallback clinic with an EMPTY patient zone (care tools live in the drag tray).</summary>
        private static void BuildFallbackClinic(CampusWorldBuilder builder)
        {
            builder.AddCatalogSprite("HealthHeroRoomBackdrop", "room.health_hero", new Vector2(0f, 0.18f), new Vector2(8.35f, 4.7f), 0);
            builder.AddShape("ClinicExamBedShadow", CampusSpriteKind.Circle, new Vector2(-1.82f, -0.88f), new Vector2(2.25f, 0.36f), CampusWorldPalette.Shadow, 2);
            builder.AddShape("ClinicPatientZonePad", CampusSpriteKind.Circle, HealthHeroClinicLayout.PatientZonePosition, new Vector2(2.2f, 1.0f), new Color(0.74f, 0.93f, 0.82f, 0.55f), 2);
            builder.AddShape("ClinicExamBed", CampusSpriteKind.Square, new Vector2(-1.82f, -0.66f), new Vector2(2.25f, 0.42f), CampusWorldPalette.Window, 3);
            builder.AddShape("ClinicExamPillow", CampusSpriteKind.Square, new Vector2(-2.58f, -0.43f), new Vector2(0.5f, 0.22f), CampusWorldPalette.Plaza, 4);
            builder.AddShape("ClinicCareBoard", CampusSpriteKind.Square, new Vector2(2.6f, 0.55f), new Vector2(1.2f, 1.0f), CampusWorldPalette.Plaza, 3);
            builder.AddShape("ClinicCareCounterShadow", CampusSpriteKind.Circle, new Vector2(1.88f, -0.9f), new Vector2(2.5f, 0.36f), CampusWorldPalette.Shadow, 2);
            builder.AddShape("ClinicCareCounter", CampusSpriteKind.Square, new Vector2(1.88f, -0.62f), new Vector2(2.72f, 0.62f), CampusWorldPalette.Plaza, 3);
            builder.AddShape("ClinicToolTrayBoard", CampusSpriteKind.Square, new Vector2(0f, HealthHeroClinicLayout.TrayPosition(0).y - 0.05f), new Vector2(6.2f, 1.0f), CampusWorldPalette.Plaza, 2);
            builder.AddShape("ClinicWallBadge", CampusSpriteKind.Circle, new Vector2(-3.08f, 0.72f), new Vector2(0.55f, 0.55f), CampusWorldPalette.Mint, 3);
            builder.AddShape("ClinicWallCrossA", CampusSpriteKind.Square, new Vector2(-3.08f, 0.72f), new Vector2(0.12f, 0.4f), Color.white, 4);
            builder.AddShape("ClinicWallCrossB", CampusSpriteKind.Square, new Vector2(-3.08f, 0.72f), new Vector2(0.4f, 0.12f), Color.white, 4);
        }

        /// <summary>
        /// The patient NPC is created in code for both paths (prefab and
        /// fallback) so the controller's P14 brighten hook always finds the same
        /// AvatarRuntimeView + name (mirrors the BuilderNpc convention).
        /// </summary>
        private static void AddPatientNpc(CampusWorldBuilder builder)
        {
            var npcObject = new GameObject(HealthHeroClinicLayout.PatientNpcName, typeof(SpriteRenderer), typeof(AvatarRuntimeView));
            npcObject.transform.SetParent(builder.Root, false);
            npcObject.transform.localPosition = new Vector3(
                HealthHeroClinicLayout.NpcPosition.x,
                HealthHeroClinicLayout.NpcPosition.y,
                0f);
            builder.AddShape("PatientNpcShadow", CampusSpriteKind.Circle, new Vector2(0f, -0.52f), new Vector2(0.62f, 0.18f), CampusWorldPalette.Shadow, 307, 0f, npcObject.transform);
            npcObject.GetComponent<AvatarRuntimeView>().ApplySpriteAsset("npc.patient");
        }

        public static void ShowCourt(CampusWorldBuilder builder, GameSession session)
        {
            // U10: the room route mounts the authored LogicCourt prefab
            // (visual-only, no NetworkObject). When the prefab has not been built
            // yet, a code-built diorama with EMPTY sorting zones keeps the room
            // playable — the controller's drag playfield supplies the cards in
            // both paths.
            if (!TryMountRoomPrefab(
                builder,
                LogicCourtLayout.PrefabResourcePath,
                "LogicCourt",
                "Career Quest/World/Build Logic Court Prefab",
                "CareerQuestRoomPrefabBuilder.BuildLogicCourt"))
            {
                BuildFallbackCourt(builder);
            }

            builder.AddCharacter(session?.SelectedAvatar.DisplayName ?? "Speaker", 0.04f, -1.35f, session?.SelectedAvatar.ShirtColor ?? CampusWorldPalette.PlayerGold, 0.8f, true, session?.SelectedAvatar.SpriteAssetId, false);
            AddJudgeNpc(builder);
        }

        /// <summary>Fallback court with EMPTY sorting zones (evidence cards live in the drag tray).</summary>
        private static void BuildFallbackCourt(CampusWorldBuilder builder)
        {
            builder.AddCatalogSprite("LogicCourtRoomBackdrop", "room.logic_court", new Vector2(0f, 0.18f), new Vector2(8.35f, 4.7f), 0);
            builder.AddShape("JudgeBenchShadow", CampusSpriteKind.Circle, new Vector2(-2.05f, -0.62f), new Vector2(2.34f, 0.42f), CampusWorldPalette.Shadow, 2);
            builder.AddShape("JudgeBench", CampusSpriteKind.Square, new Vector2(-2.05f, -0.34f), new Vector2(2.3f, 0.72f), CampusWorldPalette.GoldRoof, 3);
            builder.AddShape("JudgeBenchFront", CampusSpriteKind.Square, new Vector2(-2.05f, -0.62f), new Vector2(2.48f, 0.22f), CampusWorldPalette.Gold, 4);
            builder.AddShape(LogicCourtLayout.StampPropName, CampusSpriteKind.Square, LogicCourtLayout.StampPosition, new Vector2(0.45f, 0.5f), CampusWorldPalette.Gold, 6);
            builder.AddShape("CourtPodium", CampusSpriteKind.Square, new Vector2(LogicCourtLayout.PodiumZonePosition.x, LogicCourtLayout.PodiumZonePosition.y - 0.2f), new Vector2(0.85f, 0.7f), CampusWorldPalette.GoldRoof, 3);
            builder.AddShape("HelpfulZone", CampusSpriteKind.Square, LogicCourtLayout.HelpfulZonePosition, new Vector2(1.35f, 0.85f), new Color(0.74f, 0.93f, 0.76f, 0.86f), 3);
            builder.AddShape("NotHelpfulZone", CampusSpriteKind.Square, LogicCourtLayout.NotHelpfulZonePosition, new Vector2(1.35f, 0.85f), new Color(0.9f, 0.86f, 0.96f, 0.86f), 3);
            builder.AddShape("CourtTrayBoard", CampusSpriteKind.Square, new Vector2(0f, LogicCourtLayout.TrayPosition(0).y - 0.05f), new Vector2(6.2f, 1.0f), CampusWorldPalette.Plaza, 2);
            builder.AddCatalogSprite("ArgumentMeter", "prop.argument_meter", new Vector2(3.3f, 0.42f), new Vector2(0.8f, 0.8f), 4);
        }

        /// <summary>
        /// The judge NPC is created in code for both paths (prefab and fallback)
        /// so the controller's P14 stamp/cheer hook always finds the same
        /// AvatarRuntimeView + name (mirrors the BuilderNpc convention).
        /// </summary>
        private static void AddJudgeNpc(CampusWorldBuilder builder)
        {
            var npcObject = new GameObject(LogicCourtLayout.JudgeNpcName, typeof(SpriteRenderer), typeof(AvatarRuntimeView));
            npcObject.transform.SetParent(builder.Root, false);
            npcObject.transform.localPosition = new Vector3(
                LogicCourtLayout.NpcPosition.x,
                LogicCourtLayout.NpcPosition.y,
                0f);
            builder.AddShape("JudgeNpcShadow", CampusSpriteKind.Circle, new Vector2(0f, -0.52f), new Vector2(0.62f, 0.18f), CampusWorldPalette.Shadow, 307, 0f, npcObject.transform);
            npcObject.GetComponent<AvatarRuntimeView>().ApplySpriteAsset("npc.judge");
        }

        /// <summary>
        /// Generic prefab mount for authored room dioramas (U10 — name-matched
        /// fallback with LogWarning, the Design Build convention).
        /// </summary>
        private static bool TryMountRoomPrefab(
            CampusWorldBuilder builder,
            string resourcePath,
            string instanceName,
            string menuItem,
            string headlessMethod)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"{instanceName} prefab missing at Resources/{resourcePath} — " +
                    $"run '{menuItem}' ({headlessMethod}). Falling back to the code-built room.");
                return false;
            }

            var instance = Object.Instantiate(prefab, builder.Root);
            instance.name = instanceName;
            return true;
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
            // U7: the reveal route mounts the authored RevealStage prefab
            // (visual-only, no NetworkObject). When the prefab has not been
            // built yet, a code-built stage with the SAME anchor/glow names
            // keeps the cinematic playable — RevealCinematicDirector resolves
            // everything by name with RevealStageLayout fallbacks.
            if (!TryMountRevealStage(builder))
            {
                BuildFallbackRevealStage(builder);
            }

            AddRevealHeroAvatar(builder, session);
        }

        private static bool TryMountRevealStage(CampusWorldBuilder builder)
        {
            var prefab = Resources.Load<GameObject>(RevealStageLayout.PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"RevealStage prefab missing at Resources/{RevealStageLayout.PrefabResourcePath} — " +
                    "run 'Career Quest/World/Build Reveal Stage Prefab' " +
                    "(CareerQuestRoomPrefabBuilder.BuildRevealStage). Falling back to the code-built stage.");
                return false;
            }

            var instance = Object.Instantiate(prefab, builder.Root);
            instance.name = RevealStageLayout.StageRootName;
            return true;
        }

        /// <summary>
        /// Fallback stage mirroring the prefab structure: platform, three token
        /// slot pedestals + anchors, faked stage lighting (glow sprites — P7,
        /// no URP). Sorting uses the world band 200-299; light wash sits in the
        /// 300-399 band so the sweep reads over the stage and characters.
        /// </summary>
        private static void BuildFallbackRevealStage(CampusWorldBuilder builder)
        {
            builder.AddCatalogSprite("RevealRoomBackdrop", "room.reveal", new Vector2(0f, 0.12f), new Vector2(7.4f, 4.16f), 200);
            builder.AddShape("RevealStageShadow", CampusSpriteKind.Circle, new Vector2(0f, -0.9f), new Vector2(5.6f, 1.2f), CampusWorldPalette.Shadow, 206);
            builder.AddShape("RevealStagePlatform", CampusSpriteKind.Circle, new Vector2(0f, -0.76f), new Vector2(5.2f, 1f), CampusWorldPalette.Plaza, 208);

            // Faked stage lighting (P7): a warm spot pool plus two angled beams.
            // Beams start dim and angled wide; the light-sweep beat ramps alpha
            // and rotates them toward vertical.
            var spot = builder.AddShape(RevealStageLayout.GlowSpotName, CampusSpriteKind.Circle, new Vector2(RevealStageLayout.StageCenter.x, RevealStageLayout.StageCenter.y - 0.35f), new Vector2(4.2f, 2.1f), CampusWorldPalette.LightBeamGold, 212);
            SetSpriteAlpha(spot, 0.3f);
            var beamLeft = builder.AddShape(RevealStageLayout.GlowBeamLeftName, CampusSpriteKind.Square, new Vector2(-1.1f, 0.65f), new Vector2(0.5f, 3.7f), CampusWorldPalette.LightBeamGold, 360, -26f);
            SetSpriteAlpha(beamLeft, 0.25f);
            var beamRight = builder.AddShape(RevealStageLayout.GlowBeamRightName, CampusSpriteKind.Square, new Vector2(1.1f, 0.65f), new Vector2(0.5f, 3.7f), CampusWorldPalette.LightBeamBlue, 360, 26f);
            SetSpriteAlpha(beamRight, 0.25f);

            for (var i = 0; i < RevealStageLayout.SlotCount; i++)
            {
                var slot = RevealStageLayout.SlotPosition(i);
                builder.AddShape($"RevealSlotPedestal{i}", CampusSpriteKind.Square, new Vector2(slot.x, slot.y - 0.52f), new Vector2(0.72f, 0.34f), CampusWorldPalette.Plaza, 214);
                var ring = builder.AddShape($"RevealSlotRing{i}", CampusSpriteKind.Circle, slot, new Vector2(0.78f, 0.78f), CampusWorldPalette.LightBeamGold, 215);
                SetSpriteAlpha(ring, 0.35f);

                var anchor = new GameObject(RevealStageLayout.SlotAnchorPrefix + i);
                anchor.transform.SetParent(builder.Root, false);
                anchor.transform.localPosition = new Vector3(slot.x, slot.y, 0f);
            }
        }

        /// <summary>
        /// The hero avatar is created in code for both paths (prefab and
        /// fallback) so the P15 celebrate hook always finds the same
        /// AvatarRuntimeView + name (mirrors the BuilderNpc convention).
        /// </summary>
        private static void AddRevealHeroAvatar(CampusWorldBuilder builder, GameSession session)
        {
            var heroObject = new GameObject(RevealStageLayout.HeroAvatarName, typeof(SpriteRenderer), typeof(AvatarRuntimeView));
            heroObject.transform.SetParent(builder.Root, false);
            heroObject.transform.localPosition = new Vector3(
                RevealStageLayout.HeroAvatarPosition.x,
                RevealStageLayout.HeroAvatarPosition.y,
                0f);
            builder.AddShape("RevealHeroShadow", CampusSpriteKind.Circle, new Vector2(0f, -0.52f), new Vector2(0.62f, 0.18f), CampusWorldPalette.Shadow, 307, 0f, heroObject.transform);
            heroObject.GetComponent<AvatarRuntimeView>().ApplySpriteAsset(
                session?.SelectedAvatar?.SpriteAssetId ?? AvatarConfig.DefaultAvatar.SpriteAssetId);
        }

        private static void SetSpriteAlpha(GameObject spriteObject, float alpha)
        {
            var renderer = spriteObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            var color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
