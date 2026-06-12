using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CareerQuest.Editor
{
    /// <summary>
    /// U6 authored activity-room pipeline, extending the CareerQuestHubPrefabBuilder
    /// convention: idempotent compose-and-save with headless entry points that
    /// EditorApplication.Exit(0/1).
    ///
    /// BuildDesignBuildStudio composes the visual-only DesignBuildStudio prefab
    /// (no NetworkObject): blueprint table with five lot pads + SlotAnchor_{pieceId}
    /// children, a piece tray with TrayAnchor_{i} children, blueprint prop, and
    /// craft-supply dressing. Positions come from the runtime
    /// <see cref="DesignBuildStudioLayout"/> single coordinate truth — the
    /// controller's fallback constants and the baked anchors can never diverge.
    /// The builder NPC is NOT baked: CampusRoomScenes creates it in code for both
    /// prefab and fallback paths so the P14 cheer hook is uniform.
    ///
    /// Canonical asset: Assets/_CareerQuest/Prefabs/Rooms/DesignBuildStudio.prefab
    /// Runtime copy:    Assets/Resources/CareerQuest/World/DesignBuildStudio.prefab
    /// (Resources.Load is the only runtime mount path for the AddComponent-built
    /// world controller.)
    /// </summary>
    public static class CareerQuestRoomPrefabBuilder
    {
        private const string RoomArtFolder = "Assets/_CareerQuest/Art/Rooms/DesignBuild";
        private const string PrefabFolder = "Assets/_CareerQuest/Prefabs/Rooms";
        private const string PrefabAssetPath = "Assets/_CareerQuest/Prefabs/Rooms/DesignBuildStudio.prefab";
        private const string PrefabResourcesFolder = "Assets/Resources/CareerQuest/World";
        private const string PrefabResourcesPath = "Assets/Resources/CareerQuest/World/DesignBuildStudio.prefab";
        private const string RoomBackdropPath = "Assets/Resources/CareerQuest/Room/room.design_build.png";
        private const string BlueprintPropPath = "Assets/Resources/CareerQuest/Prop/prop.blueprint.png";

        // DESIGN.md palette (Design Build identity: Creative Coral).
        private static readonly Color Coral = new(0.969f, 0.424f, 0.369f);
        private static readonly Color Mint = new(0.345f, 0.784f, 0.580f);
        private static readonly Color Amber = new(0.949f, 0.639f, 0.231f);
        private static readonly Color ScienceBlue = new(0.290f, 0.616f, 0.922f);
        private static readonly Color MusicLilac = new(0.620f, 0.522f, 0.863f);
        private static readonly Color PlazaCream = new(1f, 0.92f, 0.64f);
        private static readonly Color PaperWarm = new(1f, 0.969f, 0.878f);
        private static readonly Color SoftShadow = new(0.05f, 0.07f, 0.09f, 0.14f);

        private static readonly (string Id, Color Accent)[] Pieces =
        {
            ("clinic", Mint),
            ("court", Amber),
            ("studio", Coral),
            ("lab", ScienceBlue),
            ("art_tower", MusicLilac)
        };

        [MenuItem("Career Quest/World/Build Design Build Studio Prefab")]
        public static void BuildDesignBuildStudioInteractive()
        {
            BuildDesignBuildStudioCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: composes and saves the room prefab, then exits 0/1.</summary>
        public static void BuildDesignBuildStudio()
        {
            BuildDesignBuildStudioCore(exitWhenDone: true);
        }

        private static void BuildDesignBuildStudioCore(bool exitWhenDone)
        {
            GameObject root = null;
            try
            {
                GenerateRoomHelperArt();
                root = ComposeDesignBuildStudio();

                EnsureFolder(PrefabFolder);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);

                EnsureFolder(PrefabResourcesFolder);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabResourcesPath) != null)
                {
                    AssetDatabase.DeleteAsset(PrefabResourcesPath);
                }

                if (!AssetDatabase.CopyAsset(PrefabAssetPath, PrefabResourcesPath))
                {
                    throw new InvalidOperationException($"Failed to copy '{PrefabAssetPath}' to '{PrefabResourcesPath}'.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CQ_ROOM_PREFAB BuildDesignBuildStudio: saved '{PrefabAssetPath}' (+ runtime copy '{PrefabResourcesPath}').");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_ROOM_PREFAB BuildDesignBuildStudio failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        // ------------------------------------------------------------------
        // Helper art (idempotent — rebuild overwrites)
        // ------------------------------------------------------------------

        private static void GenerateRoomHelperArt()
        {
            Directory.CreateDirectory(RoomArtFolder);

            // Blueprint table: warm work surface with a lighter top lip.
            var table = DrawWithPixels(660, 110, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 0, 0, w, h, 18, PlazaCream);
                FillRect(pixels, w, h, 6, h - 14, w - 12, 8, Color.Lerp(PlazaCream, Color.white, 0.25f));
                FillRect(pixels, w, h, 6, 0, w - 12, 10, Color.Lerp(PlazaCream, Color.black, 0.12f));
            });
            WritePng(table, $"{RoomArtFolder}/room_table.png");
            UnityEngine.Object.DestroyImmediate(table);

            // Piece tray: paper board with a soft inner well.
            var tray = DrawWithPixels(640, 104, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 0, 0, w, h, 16, PaperWarm);
                FillRoundedRect(pixels, w, h, 8, 8, w - 16, h - 16, 12, Color.Lerp(PaperWarm, Color.black, 0.06f));
            });
            WritePng(tray, $"{RoomArtFolder}/room_tray.png");
            UnityEngine.Object.DestroyImmediate(tray);

            // Per-piece lot pads: soft accent plate with a lighter inset.
            foreach (var (id, accent) in Pieces)
            {
                var pad = DrawWithPixels(96, 88, (pixels, w, h) =>
                {
                    FillEllipse(pixels, w, h, w / 2, 10, w / 2 - 4, 9, SoftShadow);
                    FillRoundedRect(pixels, w, h, 4, 10, w - 8, h - 16, 12, Color.Lerp(accent, Color.white, 0.55f));
                    FillRoundedRect(pixels, w, h, 12, 18, w - 24, h - 32, 10, Color.Lerp(accent, Color.white, 0.72f));
                });
                WritePng(pad, $"{RoomArtFolder}/room_slot_pad_{id}.png");
                UnityEngine.Object.DestroyImmediate(pad);
            }

            // Craft supplies: pencil + ruler cluster for workshop dressing.
            var supplies = DrawWithPixels(120, 72, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 6, 10, 86, 16, 6, Amber);
                FillRect(pixels, w, h, 14, 14, 70, 3, Color.Lerp(Amber, Color.black, 0.25f));
                FillRoundedRect(pixels, w, h, 22, 38, 84, 12, 6, Coral);
                FillEllipse(pixels, w, h, 108, 44, 8, 6, Color.Lerp(Coral, Color.white, 0.4f));
            });
            WritePng(supplies, $"{RoomArtFolder}/room_supplies.png");
            UnityEngine.Object.DestroyImmediate(supplies);

            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------
        // Prefab composition
        // ------------------------------------------------------------------

        private static GameObject ComposeDesignBuildStudio()
        {
            var root = new GameObject("DesignBuildStudio");

            // World band 200-299 per the sorting decision; pieces (controller-
            // spawned) sit at 330 in the characters/props band.
            AddSprite(root.transform, "Backdrop", LoadSprite(RoomBackdropPath), new Vector2(0f, 0.12f), new Vector2(7.4f, 4.16f), 200);
            AddSprite(root.transform, "BuildTable", RoomSprite("room_table.png"), new Vector2(0f, -0.5f), new Vector2(6.6f, 1.05f), 210);
            AddSprite(root.transform, "TrayBoard", RoomSprite("room_tray.png"), new Vector2(0f, DesignBuildStudioLayout.TrayPosition(0).y - 0.02f), new Vector2(6.2f, 1.0f), 208);
            AddSprite(root.transform, "BlueprintProp", LoadSprite(BlueprintPropPath), new Vector2(-3.15f, -0.55f), new Vector2(0.62f, 0.62f), 218);
            AddSprite(root.transform, "CraftSupplies", RoomSprite("room_supplies.png"), new Vector2(3.1f, -0.18f), new Vector2(0.85f, 0.5f), 218);

            for (var i = 0; i < Pieces.Length; i++)
            {
                var (id, _) = Pieces[i];
                var slot = DesignBuildStudioLayout.SlotPosition(i);
                AddSprite(root.transform, $"SlotPad_{id}", RoomSprite($"room_slot_pad_{id}.png"), slot, new Vector2(1.0f, 0.92f), 215);
                AddAnchor(root.transform, DesignBuildStudioLayout.SlotAnchorPrefix + id, slot);
                AddAnchor(root.transform, DesignBuildStudioLayout.TrayAnchorPrefix + i, DesignBuildStudioLayout.TrayPosition(i));
            }

            return root;
        }

        private static void AddAnchor(Transform parent, string name, Vector2 position)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = new Vector3(position.x, position.y, 0f);
        }

        private static GameObject AddSprite(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 targetSize, int order)
        {
            var spriteObject = new GameObject(name, typeof(SpriteRenderer));
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = new Vector3(position.x, position.y, 0f);

            var renderer = spriteObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;

            var bounds = sprite.bounds.size;
            var width = Mathf.Approximately(bounds.x, 0f) ? 1f : bounds.x;
            var height = Mathf.Approximately(bounds.y, 0f) ? 1f : bounds.y;
            spriteObject.transform.localScale = new Vector3(targetSize.x / width, targetSize.y / height, 1f);
            return spriteObject;
        }

        private static Sprite RoomSprite(string fileName)
        {
            return LoadSprite($"{RoomArtFolder}/{fileName}");
        }

        private static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Sprite missing at '{assetPath}'. Generate room art first " +
                    "(it is written by this builder; for catalog art run the sprite kit pipeline).");
            }

            return sprite;
        }

        // ------------------------------------------------------------------
        // Texture drawing helpers (technique mirrors CareerQuestHubPrefabBuilder)
        // ------------------------------------------------------------------

        private static Texture2D DrawWithPixels(int width, int height, Action<Color[], int, int> draw)
        {
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            draw(pixels, width, height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void FillRect(Color[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, Color color)
        {
            var xMin = Mathf.Clamp(x, 0, width);
            var xMax = Mathf.Clamp(x + rectWidth, 0, width);
            var yMin = Mathf.Clamp(y, 0, height);
            var yMax = Mathf.Clamp(y + rectHeight, 0, height);

            for (var py = yMin; py < yMax; py++)
            {
                for (var px = xMin; px < xMax; px++)
                {
                    pixels[py * width + px] = Blend(pixels[py * width + px], color);
                }
            }
        }

        private static void FillRoundedRect(Color[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, int radius, Color color)
        {
            radius = Mathf.Clamp(radius, 0, Mathf.Min(rectWidth, rectHeight) / 2);
            var xMin = Mathf.Clamp(x, 0, width);
            var xMax = Mathf.Clamp(x + rectWidth, 0, width);
            var yMin = Mathf.Clamp(y, 0, height);
            var yMax = Mathf.Clamp(y + rectHeight, 0, height);

            for (var py = yMin; py < yMax; py++)
            {
                for (var px = xMin; px < xMax; px++)
                {
                    var localX = px - x;
                    var localY = py - y;
                    var insideX = Mathf.Clamp(localX, radius, rectWidth - radius);
                    var insideY = Mathf.Clamp(localY, radius, rectHeight - radius);
                    var dx = localX - insideX;
                    var dy = localY - insideY;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        pixels[py * width + px] = Blend(pixels[py * width + px], color);
                    }
                }
            }
        }

        private static void FillEllipse(Color[] pixels, int width, int height, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            var rx2 = Mathf.Max(1, radiusX * radiusX);
            var ry2 = Mathf.Max(1, radiusY * radiusY);

            for (var y = Mathf.Max(0, centerY - radiusY); y < Mathf.Min(height, centerY + radiusY); y++)
            {
                for (var x = Mathf.Max(0, centerX - radiusX); x < Mathf.Min(width, centerX + radiusX); x++)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    if (dx * dx * ry2 + dy * dy * rx2 <= rx2 * ry2)
                    {
                        pixels[y * width + x] = Blend(pixels[y * width + x], color);
                    }
                }
            }
        }

        private static Color Blend(Color background, Color foreground)
        {
            var alpha = foreground.a + background.a * (1f - foreground.a);
            if (alpha <= 0f)
            {
                return Color.clear;
            }

            return new Color(
                (foreground.r * foreground.a + background.r * background.a * (1f - foreground.a)) / alpha,
                (foreground.g * foreground.a + background.g * background.a * (1f - foreground.a)) / alpha,
                (foreground.b * foreground.a + background.b * background.a * (1f - foreground.a)) / alpha,
                alpha);
        }

        private static void WritePng(Texture2D texture, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            var segments = assetFolderPath.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private static void ExitIfHeadless(bool exitWhenDone, int code)
        {
            if (exitWhenDone)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
