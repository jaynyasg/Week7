using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CareerQuest.Editor
{
    public static class CareerQuestSpriteKitGenerator
    {
        private const string ResourcesRoot = "Assets/Resources/CareerQuest";
        private const string ArtRoot = "Assets/_CareerQuest/Art";

        private static readonly Color Outline = new(0.06f, 0.08f, 0.1f, 1f);
        private static readonly Color Skin = new(0.78f, 0.52f, 0.34f, 1f);
        private static readonly Color SkinLight = new(0.92f, 0.68f, 0.46f, 1f);
        private static readonly Color HairDark = new(0.1f, 0.07f, 0.05f, 1f);
        private static readonly Color Shoe = new(0.16f, 0.14f, 0.12f, 1f);
        private static readonly Color Glass = new(0.83f, 0.96f, 1f, 1f);
        private static readonly Color Shadow = new(0.05f, 0.06f, 0.07f, 0.22f);
        private static readonly Color Paper = new(1f, 0.97f, 0.86f, 1f);
        private static readonly Color WarmLight = new(1f, 0.9f, 0.56f, 0.12f);

        [MenuItem("Career Quest/Generate Sprite Kit")]
        public static void Generate()
        {
            EnsureDirectories();

            foreach (var definition in AssetCatalog.Definitions)
            {
                var texture = DrawDefinition(definition);
                WritePng(texture, ResourcePath(definition));
                WritePng(texture, ArtPath(definition));
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.Refresh();

            foreach (var definition in AssetCatalog.Definitions)
            {
                ConfigureTextureImporter(ResourcePath(definition));
                ConfigureTextureImporter(ArtPath(definition));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Career Quest sprite kit generated: {AssetCatalog.Definitions.Count} sprites.");
        }

        private static void EnsureDirectories()
        {
            foreach (var category in Enum.GetValues(typeof(AssetCategory)).Cast<AssetCategory>())
            {
                Directory.CreateDirectory($"{ResourcesRoot}/{category}");
            }

            foreach (var folder in new[] { "Avatars", "Npcs", "Campus", "Rooms", "Badges", "UI" })
            {
                Directory.CreateDirectory($"{ArtRoot}/{folder}");
            }
        }

        private static Texture2D DrawDefinition(AssetDefinition definition)
        {
            var texture = NewTexture(definition.PixelSize.x, definition.PixelSize.y);
            var pixels = NewPixels(texture.width, texture.height);

            switch (definition.Category)
            {
                case AssetCategory.Avatar:
                    DrawCharacter(pixels, texture.width, texture.height, definition, false);
                    break;
                case AssetCategory.Npc:
                    DrawCharacter(pixels, texture.width, texture.height, definition, true);
                    break;
                case AssetCategory.Campus:
                    DrawBuilding(pixels, texture.width, texture.height, definition);
                    break;
                case AssetCategory.Room:
                    DrawRoom(pixels, texture.width, texture.height, definition);
                    break;
                case AssetCategory.Prop:
                    DrawProp(pixels, texture.width, texture.height, definition);
                    break;
                case AssetCategory.Badge:
                    DrawBadge(pixels, texture.width, texture.height, definition);
                    break;
                case AssetCategory.Ui:
                    DrawUiIcon(pixels, texture.width, texture.height, definition);
                    break;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D NewTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        private static Color[] NewPixels(int width, int height)
        {
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            return pixels;
        }

        private static void DrawCharacter(Color[] pixels, int width, int height, AssetDefinition definition, bool npc)
        {
            var primary = definition.PrimaryColor;
            var accent = definition.AccentColor;
            var variant = Math.Abs(definition.Id.GetHashCode());
            var hair = Color.Lerp(HairDark, primary, (variant % 5) * 0.06f);
            var skin = (variant % 4) switch
            {
                0 => new Color(0.68f, 0.43f, 0.27f, 1f),
                1 => Skin,
                2 => SkinLight,
                _ => new Color(0.55f, 0.35f, 0.24f, 1f)
            };
            var pants = Color.Lerp(new Color(0.12f, 0.18f, 0.24f, 1f), primary, 0.18f);
            var highlight = Color.Lerp(primary, Color.white, 0.32f);

            FillEllipse(pixels, width, height, width / 2, height / 12, width * 3 / 8, height / 22, Shadow);

            FillRect(pixels, width, height, width / 2 - width / 6, height / 8, width / 11, height / 4, Outline);
            FillRect(pixels, width, height, width / 2 + width / 14, height / 8, width / 11, height / 4, Outline);
            FillRect(pixels, width, height, width / 2 - width / 6 + 4, height / 8 + 4, width / 14, height / 4 - 8, pants);
            FillRect(pixels, width, height, width / 2 + width / 14 + 4, height / 8 + 4, width / 14, height / 4 - 8, pants);
            FillEllipse(pixels, width, height, width / 2 - width / 7, height / 9, width / 10, height / 30, Shoe);
            FillEllipse(pixels, width, height, width / 2 + width / 7, height / 9, width / 10, height / 30, Shoe);

            FillRect(pixels, width, height, width / 2 - width / 4, height / 3, width / 12, height / 4, Outline);
            FillRect(pixels, width, height, width / 2 + width / 6, height / 3, width / 12, height / 4, Outline);
            FillRect(pixels, width, height, width / 2 - width / 4 + 4, height / 3 + 4, width / 15, height / 4 - 8, skin);
            FillRect(pixels, width, height, width / 2 + width / 6 + 4, height / 3 + 4, width / 15, height / 4 - 8, skin);
            FillEllipse(pixels, width, height, width / 2 - width / 4, height / 3, width / 18, height / 18, skin);
            FillEllipse(pixels, width, height, width / 2 + width / 4, height / 3, width / 18, height / 18, skin);

            FillEllipse(pixels, width, height, width / 2, height / 2 - height / 18, width / 4, height / 4, Outline);
            FillEllipse(pixels, width, height, width / 2, height / 2 - height / 16, width / 5, height / 4 - 8, primary);
            FillRect(pixels, width, height, width / 2 - width / 9, height / 2 + height / 13, width * 2 / 9, height / 24, highlight);
            FillRect(pixels, width, height, width / 2 - width / 16, height / 2 - height / 8, width / 8, height / 6, Color.Lerp(primary, Outline, 0.42f));
            FillRect(pixels, width, height, width / 2 + width / 7, height / 2 - height / 14, width / 9, height / 5, accent);
            FillRect(pixels, width, height, width / 2 + width / 7 + 3, height / 2 - height / 14 + 4, width / 12, height / 13, Color.Lerp(accent, Color.white, 0.35f));

            FillRect(pixels, width, height, width / 2 - width / 18, height * 7 / 12, width / 9, height / 12, skin);
            FillEllipse(pixels, width, height, width / 2 - width / 5, height * 7 / 10, width / 18, height / 16, Outline);
            FillEllipse(pixels, width, height, width / 2 + width / 5, height * 7 / 10, width / 18, height / 16, Outline);
            FillEllipse(pixels, width, height, width / 2 - width / 5, height * 7 / 10, width / 22, height / 18, skin);
            FillEllipse(pixels, width, height, width / 2 + width / 5, height * 7 / 10, width / 22, height / 18, skin);
            FillEllipse(pixels, width, height, width / 2, height * 7 / 10, width / 5, height / 7, Outline);
            FillEllipse(pixels, width, height, width / 2, height * 7 / 10, width / 6, height / 8, skin);
            FillEllipse(pixels, width, height, width / 2, height * 31 / 40, width / 5, height / 10, hair);
            FillEllipse(pixels, width, height, width / 2 - width / 9, height * 3 / 4, width / 10, height / 12, hair);
            FillEllipse(pixels, width, height, width / 2 + width / 8, height * 3 / 4, width / 10, height / 12, hair);
            FillRect(pixels, width, height, width / 2 - width / 11, height * 13 / 20, width / 25, height / 34, Outline);
            FillRect(pixels, width, height, width / 2 + width / 15, height * 13 / 20, width / 25, height / 34, Outline);
            FillRect(pixels, width, height, width / 2 - width / 14, height * 3 / 5, width / 7, height / 44, new Color(0.42f, 0.15f, 0.11f, 1f));
            FillEllipse(pixels, width, height, width / 2 - width / 11, height * 25 / 40, width / 34, height / 48, new Color(0.96f, 0.45f, 0.4f, 0.42f));

            if (definition.Id.Contains("sky") || definition.Id.Contains("builder"))
            {
                FillRect(pixels, width, height, width / 2 - width / 5, height * 79 / 100, width * 2 / 5, height / 24, Outline);
                FillRect(pixels, width, height, width / 2 - width / 5 + 4, height * 79 / 100 + 3, width * 2 / 5 - 8, height / 32, accent);
                FillRect(pixels, width, height, width / 2 - width / 7, height / 2 + height / 15, width * 2 / 7, height / 18, Glass);
                FillRect(pixels, width, height, width / 2 - width / 7 + 4, height / 2 + height / 15 + 3, width * 2 / 7 - 8, height / 26, new Color(0.55f, 0.82f, 1f, 1f));
            }
            else if (definition.Id.Contains("care") || definition.Id.Contains("patient"))
            {
                FillRect(pixels, width, height, width / 2 - width / 28, height / 2, width / 14, height / 5, Color.white);
                FillRect(pixels, width, height, width / 2 - width / 10, height / 2 + height / 14, width / 5, height / 18, Color.white);
                FillRect(pixels, width, height, width / 2 - width / 10, height * 63 / 100, width / 5, height / 34, Glass);
            }
            else if (definition.Id.Contains("logic") || definition.Id.Contains("judge"))
            {
                FillRect(pixels, width, height, width / 2 - width / 4, height * 3 / 4, width / 2, height / 22, accent);
                FillRect(pixels, width, height, width / 2 - width / 5, height * 3 / 4 + 4, width * 2 / 5, height / 40, Color.Lerp(accent, Color.white, 0.4f));
                FillRect(pixels, width, height, width / 2 - width / 8, height * 13 / 20, width / 5, height / 38, Outline);
                FillRect(pixels, width, height, width / 2 - width / 8 + 3, height * 13 / 20 + 2, width / 5 - 6, height / 50, Glass);
            }
            else
            {
                FillEllipse(pixels, width, height, width / 2 + width / 4, height / 2 + height / 9, width / 10, height / 10, accent);
                FillEllipse(pixels, width, height, width / 2 + width / 4, height / 2 + height / 9, width / 18, height / 18, Color.Lerp(accent, Color.white, 0.45f));
                FillRect(pixels, width, height, width / 2 - width / 6, height / 2 + height / 14, width / 3, height / 16, Glass);
            }

            if (npc)
            {
                FillEllipse(pixels, width, height, width / 2 + width / 4, height / 2 - height / 14, width / 10, height / 10, accent);
                FillRect(pixels, width, height, width / 2 + width / 5, height / 2 - height / 10, width / 12, height / 26, Color.Lerp(accent, Color.white, 0.35f));
            }
        }

        private static void DrawBuilding(Color[] pixels, int width, int height, AssetDefinition definition)
        {
            var body = definition.PrimaryColor;
            var roof = definition.AccentColor;
            var side = Color.Lerp(body, Outline, 0.18f);
            FillEllipse(pixels, width, height, width / 2, height / 8, width * 2 / 5, height / 14, Shadow);
            FillRect(pixels, width, height, width / 6, height / 5 - 8, width * 2 / 3, height / 12, new Color(0.55f, 0.36f, 0.2f, 0.28f));
            FillRect(pixels, width, height, width / 5 - 6, height / 5 - 4, width * 3 / 5 + 12, height * 3 / 5 + 8, Outline);
            FillRect(pixels, width, height, width / 5 + 3, height / 5 + 3, width * 3 / 5 - 10, height * 3 / 5 - 8, Color.Lerp(body, Color.white, 0.08f));
            FillRect(pixels, width, height, width * 7 / 10, height / 5 + 3, width / 10, height * 3 / 5 - 8, side);
            FillRect(pixels, width, height, width / 5 + 3, height / 5 + height * 2 / 5, width * 3 / 5 - 10, height / 6, Color.Lerp(body, Color.white, 0.25f));
            FillRect(pixels, width, height, width / 8, height * 3 / 4, width * 3 / 4, height / 7, Outline);
            FillRect(pixels, width, height, width / 8 + 6, height * 3 / 4 + 5, width * 3 / 4 - 12, height / 7 - 10, roof);
            FillRect(pixels, width, height, width / 4, height * 69 / 100, width / 2, height / 12, Color.Lerp(roof, Color.white, 0.25f));
            FillRect(pixels, width, height, width / 2 - width / 10, height / 5, width / 5, height / 3, Outline);
            FillRect(pixels, width, height, width / 2 - width / 10 + 5, height / 5 + 5, width / 5 - 10, height / 3 - 10, Color.Lerp(body, Outline, 0.42f));
            FillEllipse(pixels, width, height, width / 2, height * 29 / 100, width / 12, height / 20, new Color(1f, 0.88f, 0.55f, 0.32f));
            for (var row = 0; row < 2; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    FillRect(pixels, width, height, width / 3 + column * width / 8, height / 2 + row * height / 8, width / 12, height / 11, Outline);
                    FillRect(pixels, width, height, width / 3 + column * width / 8 + 3, height / 2 + row * height / 8 + 3, width / 12 - 6, height / 11 - 6, Glass);
                    FillRect(pixels, width, height, width / 3 + column * width / 8 + 3, height / 2 + row * height / 8 + height / 13, width / 12 - 6, 2, new Color(1f, 1f, 1f, 0.45f));
                }
            }

            if (definition.Id.Contains("health"))
            {
                FillRect(pixels, width, height, width / 2 - width / 36, height * 79 / 100, width / 18, height / 12, Color.white);
                FillRect(pixels, width, height, width / 2 - width / 9, height * 82 / 100, width * 2 / 9, height / 30, Color.white);
            }
            else if (definition.Id.Contains("logic"))
            {
                FillRect(pixels, width, height, width / 2 - width / 4, height * 78 / 100, width / 2, height / 18, new Color(0.38f, 0.2f, 0.08f, 1f));
                FillEllipse(pixels, width, height, width / 2, height * 17 / 20, width / 12, height / 18, Color.Lerp(roof, Color.white, 0.25f));
            }
            else if (definition.Id.Contains("design"))
            {
                FillRect(pixels, width, height, width / 4, height / 5 + 4, width / 28, height * 3 / 5 - 8, Color.Lerp(Color.white, body, 0.5f));
                FillRect(pixels, width, height, width * 2 / 3, height * 4 / 5, width / 7, height / 22, Glass);
            }
            else
            {
                FillEllipse(pixels, width, height, width / 2, height * 17 / 20, width / 8, height / 12, Color.Lerp(roof, Color.white, 0.35f));
            }
        }

        private static void DrawRoom(Color[] pixels, int width, int height, AssetDefinition definition)
        {
            var wall = Color.Lerp(definition.PrimaryColor, Color.white, 0.42f);
            var floor = Color.Lerp(definition.AccentColor, Color.white, 0.18f);
            FillRect(pixels, width, height, 0, 0, width, height, wall);
            FillRect(pixels, width, height, 0, 0, width, height / 3, floor);
            FillRect(pixels, width, height, 0, height / 3 - 3, width, 6, Color.Lerp(definition.AccentColor, Outline, 0.24f));
            FillRect(pixels, width, height, 0, height / 3 + 4, width, height / 28, new Color(1f, 1f, 1f, 0.2f));

            for (var x = -width; x < width * 2; x += width / 7)
            {
                FillRect(pixels, width, height, x, height / 16, width / 36, height / 3, new Color(1f, 1f, 1f, 0.12f));
            }

            FillEllipse(pixels, width, height, width / 2, height / 6, width / 3, height / 14, Shadow);
            FillRect(pixels, width, height, width / 12, height / 2, width * 5 / 6, height / 10, Color.Lerp(definition.AccentColor, Outline, 0.16f));
            FillRect(pixels, width, height, width / 12, height / 2 + height / 10, width * 5 / 6, height / 45, Color.Lerp(definition.AccentColor, Color.white, 0.18f));
            FillRect(pixels, width, height, width / 7, height / 8, width * 5 / 7, height / 9, new Color(0.52f, 0.36f, 0.18f, 0.18f));
            FillRect(pixels, width, height, width / 4, height / 2 + height / 8, width / 5, height / 5, Outline);
            FillRect(pixels, width, height, width / 4 + 4, height / 2 + height / 8 + 4, width / 5 - 8, height / 5 - 8, Glass);
            FillRect(pixels, width, height, width * 11 / 20, height / 2 + height / 8, width / 5, height / 5, Outline);
            FillRect(pixels, width, height, width * 11 / 20 + 4, height / 2 + height / 8 + 4, width / 5 - 8, height / 5 - 8, Glass);
            FillRect(pixels, width, height, width / 12, height * 3 / 4, width * 5 / 6, height / 18, new Color(1f, 1f, 1f, 0.24f));

            if (definition.Id.Contains("design_build"))
            {
                FillRect(pixels, width, height, width / 16, height / 2 + height / 10, width / 4, height / 4, new Color(0.08f, 0.27f, 0.36f, 1f));
                FillRect(pixels, width, height, width / 16 + 5, height / 2 + height / 10 + 5, width / 4 - 10, height / 4 - 10, new Color(0.62f, 0.86f, 0.96f, 1f));
                for (var i = 0; i < 5; i++)
                {
                    FillRect(pixels, width, height, width / 16 + 16 + i * width / 28, height / 2 + height / 8, 3, height / 5, new Color(0.08f, 0.27f, 0.36f, 0.42f));
                }
                FillRect(pixels, width, height, width * 19 / 24, height / 3, width / 25, height * 11 / 24, Color.Lerp(definition.PrimaryColor, Outline, 0.42f));
                FillRect(pixels, width, height, width * 15 / 24, height * 3 / 4, width / 4, height / 24, Color.Lerp(definition.PrimaryColor, Outline, 0.42f));
                FillRect(pixels, width, height, width * 31 / 40, height * 5 / 8, width / 50, height / 8, definition.PrimaryColor);
                FillRect(pixels, width, height, width / 3, height / 5, width / 3, height / 7, Paper);
                FillRect(pixels, width, height, width / 3 + 6, height / 5 + 6, width / 3 - 12, height / 7 - 12, new Color(0.72f, 0.9f, 1f, 1f));
            }
            else if (definition.Id.Contains("health"))
            {
                FillRect(pixels, width, height, width / 12, height / 2 + height / 10, width / 4, height / 5, Color.white);
                FillRect(pixels, width, height, width / 12 + width / 11, height / 2 + height / 8, width / 28, height / 7, definition.PrimaryColor);
                FillRect(pixels, width, height, width / 12 + width / 18, height / 2 + height / 5, width / 9, height / 28, definition.PrimaryColor);
                FillRect(pixels, width, height, width * 13 / 24, height / 5, width / 3, height / 7, Paper);
                FillEllipse(pixels, width, height, width * 3 / 4, height / 4, width / 20, height / 12, new Color(0.94f, 0.34f, 0.28f, 1f));
                FillRect(pixels, width, height, width * 3 / 4 - width / 80, height / 4, width / 40, height / 6, new Color(0.94f, 0.34f, 0.28f, 1f));
            }
            else if (definition.Id.Contains("logic"))
            {
                FillRect(pixels, width, height, width * 2 / 3, height / 2 + height / 10, width / 5, height / 4, new Color(0.2f, 0.12f, 0.1f, 1f));
                FillRect(pixels, width, height, width * 2 / 3 + 5, height / 2 + height / 10 + 5, width / 5 - 10, height / 4 - 10, definition.AccentColor);
                FillRect(pixels, width, height, width / 5, height / 5, width / 5, height / 7, Paper);
                FillRect(pixels, width, height, width / 5 + 7, height / 5 + 8, width / 5 - 14, height / 34, definition.PrimaryColor);
                FillRect(pixels, width, height, width / 5 + 7, height / 5 + height / 16, width / 5 - 14, height / 34, definition.PrimaryColor);
            }

            FillRect(pixels, width, height, 0, 0, width, height, WarmLight);
        }

        private static void DrawProp(Color[] pixels, int width, int height, AssetDefinition definition)
        {
            if (definition.Id.Contains("thermometer"))
            {
                FillEllipse(pixels, width, height, width / 2, height / 6, width / 3, height / 12, Shadow);
                FillRect(pixels, width, height, width / 2 - width / 16, height / 5, width / 8, height * 3 / 5, Outline);
                FillRect(pixels, width, height, width / 2 - width / 18, height / 5, width / 9, height * 3 / 5, Color.white);
                FillEllipse(pixels, width, height, width / 2, height / 5, width / 7, height / 7, definition.PrimaryColor);
                FillRect(pixels, width, height, width / 2 - width / 32, height / 4, width / 16, height * 2 / 5, definition.PrimaryColor);
            }
            else if (definition.Id.Contains("evidence"))
            {
                DrawCardProp(pixels, width, height, definition.PrimaryColor, definition.AccentColor);
                FillRect(pixels, width, height, width / 3, height / 3, width / 3, height / 12, Color.white);
                FillRect(pixels, width, height, width / 3, height / 2, width / 3, height / 12, Color.white);
            }
            else if (definition.Id.Contains("blueprint"))
            {
                DrawCardProp(pixels, width, height, new Color(0.56f, 0.84f, 0.96f, 1f), definition.AccentColor);
                for (var i = 0; i < 4; i++)
                {
                    FillRect(pixels, width, height, width / 3 + i * width / 12, height / 3, 2, height / 3, new Color(0.08f, 0.27f, 0.36f, 0.42f));
                    FillRect(pixels, width, height, width / 3, height / 3 + i * height / 14, width / 3, 2, new Color(0.08f, 0.27f, 0.36f, 0.42f));
                }
            }
            else if (definition.Id.Contains("care_plan"))
            {
                DrawCardProp(pixels, width, height, definition.PrimaryColor, definition.AccentColor);
                FillRect(pixels, width, height, width / 2 - width / 32, height / 3, width / 16, height / 3, Color.white);
                FillRect(pixels, width, height, width / 3, height / 2 - height / 32, width / 3, height / 16, Color.white);
            }
            else if (definition.Id.Contains("city_piece"))
            {
                DrawCityPieceProp(pixels, width, height, definition);
            }
            else
            {
                DrawCardProp(pixels, width, height, definition.PrimaryColor, definition.AccentColor);
            }
        }

        private static void DrawCardProp(Color[] pixels, int width, int height, Color primary, Color accent)
        {
            FillEllipse(pixels, width, height, width / 2, height / 6, width / 3, height / 12, Shadow);
            FillRect(pixels, width, height, width / 4, height / 4, width / 2, height / 2, Outline);
            FillRect(pixels, width, height, width / 4 + 5, height / 4 + 5, width / 2 - 10, height / 2 - 10, primary);
            FillRect(pixels, width, height, width / 4 + 9, height / 2 + height / 8, width / 2 - 18, height / 12, Color.Lerp(primary, Color.white, 0.38f));
            FillRect(pixels, width, height, width / 3, height / 2, width / 3, height / 8, accent);
        }

        private static void DrawCityPieceProp(Color[] pixels, int width, int height, AssetDefinition definition)
        {
            var body = definition.PrimaryColor;
            var roof = definition.AccentColor;
            FillEllipse(pixels, width, height, width / 2, height / 7, width / 3, height / 13, Shadow);
            FillRect(pixels, width, height, width / 4, height / 5, width / 2, height / 2, Outline);
            FillRect(pixels, width, height, width / 4 + 5, height / 5 + 5, width / 2 - 10, height / 2 - 10, body);
            FillRect(pixels, width, height, width / 5, height * 7 / 10, width * 3 / 5, height / 10, Outline);
            FillRect(pixels, width, height, width / 5 + 4, height * 7 / 10 + 4, width * 3 / 5 - 8, height / 10 - 8, roof);
            FillRect(pixels, width, height, width / 3, height / 2, width / 8, height / 8, Glass);
            FillRect(pixels, width, height, width * 13 / 24, height / 2, width / 8, height / 8, Glass);
            FillRect(pixels, width, height, width / 2 - width / 12, height / 5, width / 6, height / 4, Color.Lerp(body, Outline, 0.36f));

            if (definition.Id.Contains("clinic"))
            {
                FillRect(pixels, width, height, width / 2 - width / 36, height * 5 / 8, width / 18, height / 8, Color.white);
                FillRect(pixels, width, height, width / 2 - width / 10, height * 2 / 3, width / 5, height / 22, Color.white);
            }
            else if (definition.Id.Contains("court"))
            {
                FillRect(pixels, width, height, width / 3, height * 3 / 5, width / 3, height / 16, Color.white);
                FillRect(pixels, width, height, width / 2 - width / 36, height * 3 / 5, width / 18, height / 6, Color.white);
            }
            else if (definition.Id.Contains("studio"))
            {
                FillEllipse(pixels, width, height, width / 2, height * 5 / 8, width / 9, height / 9, Color.white);
                FillRect(pixels, width, height, width / 2 + width / 10, height * 5 / 8, width / 8, height / 20, Color.white);
            }
            else if (definition.Id.Contains("lab"))
            {
                FillRect(pixels, width, height, width / 2 - width / 18, height * 3 / 5, width / 9, height / 6, Color.white);
                FillEllipse(pixels, width, height, width / 2, height * 3 / 5, width / 8, height / 18, Color.white);
            }
            else if (definition.Id.Contains("art_tower"))
            {
                FillEllipse(pixels, width, height, width / 2 - width / 10, height * 5 / 8, width / 12, height / 12, Color.white);
                FillEllipse(pixels, width, height, width / 2 + width / 12, height * 5 / 8, width / 12, height / 12, roof);
            }
        }

        private static void DrawBadge(Color[] pixels, int width, int height, AssetDefinition definition)
        {
            FillEllipse(pixels, width, height, width / 2, height / 2, width * 2 / 5, height * 2 / 5, Outline);
            FillEllipse(pixels, width, height, width / 2, height / 2, width * 7 / 20, height * 7 / 20, definition.PrimaryColor);
            FillEllipse(pixels, width, height, width / 2, height / 2, width / 4, height / 4, definition.AccentColor);
            FillRect(pixels, width, height, width / 2 - width / 20, height / 3, width / 10, height / 3, Color.white);
            FillRect(pixels, width, height, width / 3, height / 2 - height / 20, width / 3, height / 10, Color.white);
        }

        private static void DrawUiIcon(Color[] pixels, int width, int height, AssetDefinition definition)
        {
            FillEllipse(pixels, width, height, width / 2, height / 2, width * 2 / 5, height * 2 / 5, definition.PrimaryColor);
            FillEllipse(pixels, width, height, width / 2, height / 2, width / 4, height / 4, definition.AccentColor);

            if (definition.Id.Contains("back"))
            {
                FillRect(pixels, width, height, width / 3, height / 2 - height / 18, width / 3, height / 9, Color.white);
                FillRect(pixels, width, height, width / 3, height / 2, width / 8, height / 5, Color.white);
            }
            else if (definition.Id.Contains("exit"))
            {
                FillRect(pixels, width, height, width / 3, height / 3, width / 3, height / 10, Color.white);
                FillRect(pixels, width, height, width / 3, height / 2, width / 3, height / 10, Color.white);
            }
            else
            {
                FillRect(pixels, width, height, width / 2 - width / 18, height / 3, width / 9, height / 3, Color.white);
                FillRect(pixels, width, height, width / 3, height / 2 - height / 18, width / 3, height / 9, Color.white);
            }
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

        private static string ResourcePath(AssetDefinition definition)
        {
            return $"{ResourcesRoot}/{definition.Category}/{definition.Id}.png";
        }

        private static string ArtPath(AssetDefinition definition)
        {
            return $"{ArtFolder(definition.Category)}/{definition.Id}.png";
        }

        private static string ArtFolder(AssetCategory category)
        {
            return category switch
            {
                AssetCategory.Avatar => $"{ArtRoot}/Avatars",
                AssetCategory.Npc => $"{ArtRoot}/Npcs",
                AssetCategory.Campus => $"{ArtRoot}/Campus",
                AssetCategory.Room => $"{ArtRoot}/Rooms",
                AssetCategory.Badge => $"{ArtRoot}/Badges",
                AssetCategory.Ui => $"{ArtRoot}/UI",
                _ => $"{ArtRoot}/Rooms"
            };
        }

        private static void WritePng(Texture2D texture, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        private static void ConfigureTextureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
