using System;
using UnityEngine;

namespace CareerQuest
{
    public static class SpriteFallbackFactory
    {
        public const string FallbackSpriteSuffix = ".fallback";
        public const string FallbackTextureSuffix = ".fallback.texture";

        public static Sprite Create(AssetDefinition definition)
        {
            if (definition == null)
            {
                return CreateMissing("null");
            }

            var width = Mathf.Clamp(definition.PixelSize.x, 32, AssetCatalog.MaxFallbackTextureSize);
            var height = Mathf.Clamp(definition.PixelSize.y, 32, AssetCatalog.MaxFallbackTextureSize);
            var pixels = ClearPixels(width, height);

            switch (definition.Category)
            {
                case AssetCategory.Avatar:
                case AssetCategory.Npc:
                    DrawCharacter(pixels, width, height, definition.PrimaryColor, definition.AccentColor);
                    break;
                case AssetCategory.Campus:
                    DrawBuilding(pixels, width, height, definition.PrimaryColor, definition.AccentColor);
                    break;
                case AssetCategory.Room:
                    DrawRoom(pixels, width, height, definition.PrimaryColor, definition.AccentColor);
                    break;
                case AssetCategory.Badge:
                    DrawBadge(pixels, width, height, definition.PrimaryColor, definition.AccentColor);
                    break;
                case AssetCategory.Ui:
                    DrawUiIcon(pixels, width, height, definition.PrimaryColor, definition.AccentColor);
                    break;
                default:
                    DrawProp(pixels, width, height, definition.PrimaryColor, definition.AccentColor);
                    break;
            }

            return BuildSprite(definition.Id, width, height, pixels);
        }

        public static Sprite CreateMissing(string id)
        {
            const int size = 96;
            var pixels = ClearPixels(size, size);
            var magenta = new Color(0.95f, 0.05f, 0.85f, 1f);
            var dark = new Color(0.05f, 0.05f, 0.08f, 1f);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[y * size + x] = ((x / 16) + (y / 16)) % 2 == 0 ? magenta : dark;
                }
            }

            DrawRect(pixels, size, size, 10, 10, size - 20, size - 20, new Color(1f, 1f, 1f, 0.18f));
            return BuildSprite($"missing.{id}", size, size, pixels);
        }

        public static bool IsFallbackSprite(Sprite sprite)
        {
            return sprite != null && sprite.name.EndsWith(FallbackSpriteSuffix, StringComparison.Ordinal);
        }

        public static bool IsFallbackTexture(Texture2D texture)
        {
            return texture != null && texture.name.EndsWith(FallbackTextureSuffix, StringComparison.Ordinal);
        }

        private static Color[] ClearPixels(int width, int height)
        {
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            return pixels;
        }

        private static void DrawCharacter(Color[] pixels, int width, int height, Color shirt, Color accent)
        {
            DrawEllipse(pixels, width, height, width / 2, height / 6, width / 3, height / 18, new Color(0.06f, 0.08f, 0.1f, 0.18f));
            DrawRect(pixels, width, height, width / 2 - width / 10, height / 5, width / 14, height / 4, new Color(0.18f, 0.16f, 0.13f, 1f));
            DrawRect(pixels, width, height, width / 2 + width / 28, height / 5, width / 14, height / 4, new Color(0.18f, 0.16f, 0.13f, 1f));
            DrawRect(pixels, width, height, width / 2 - width / 5, height / 3, width * 2 / 5, height / 3, shirt);
            DrawRect(pixels, width, height, width / 2 + width / 6, height / 3, width / 8, height / 4, accent);
            DrawEllipse(pixels, width, height, width / 2, height * 3 / 4, width / 4, height / 7, new Color(0.78f, 0.52f, 0.34f, 1f));
            DrawEllipse(pixels, width, height, width / 2 - width / 24, height * 17 / 20, width / 4, height / 11, new Color(0.12f, 0.08f, 0.06f, 1f));
        }

        private static void DrawBuilding(Color[] pixels, int width, int height, Color body, Color roof)
        {
            DrawEllipse(pixels, width, height, width / 2, height / 7, width * 2 / 5, height / 12, new Color(0.06f, 0.08f, 0.1f, 0.18f));
            DrawRect(pixels, width, height, width / 5, height / 5, width * 3 / 5, height * 3 / 5, body);
            DrawRect(pixels, width, height, width / 6, height * 3 / 4, width * 2 / 3, height / 8, roof);
            DrawRect(pixels, width, height, width / 2 - width / 12, height / 5, width / 6, height / 4, new Color(0.18f, 0.16f, 0.13f, 1f));
            DrawRect(pixels, width, height, width / 3, height / 2, width / 8, height / 8, new Color(0.83f, 0.96f, 1f, 1f));
            DrawRect(pixels, width, height, width * 13 / 24, height / 2, width / 8, height / 8, new Color(0.83f, 0.96f, 1f, 1f));
        }

        private static void DrawRoom(Color[] pixels, int width, int height, Color wall, Color floor)
        {
            DrawRect(pixels, width, height, 0, height / 3, width, height * 2 / 3, wall);
            DrawRect(pixels, width, height, 0, 0, width, height / 3, floor);
            DrawRect(pixels, width, height, width / 8, height / 2, width * 3 / 4, height / 14, new Color(1f, 1f, 1f, 0.28f));
            DrawRect(pixels, width, height, width / 4, height / 7, width / 2, height / 12, new Color(0.06f, 0.08f, 0.1f, 0.15f));
        }

        private static void DrawProp(Color[] pixels, int width, int height, Color primary, Color accent)
        {
            DrawEllipse(pixels, width, height, width / 2, height / 6, width / 3, height / 12, new Color(0.06f, 0.08f, 0.1f, 0.18f));
            DrawRect(pixels, width, height, width / 4, height / 4, width / 2, height / 2, primary);
            DrawRect(pixels, width, height, width / 3, height / 2, width / 3, height / 8, accent);
        }

        private static void DrawBadge(Color[] pixels, int width, int height, Color primary, Color accent)
        {
            DrawEllipse(pixels, width, height, width / 2, height / 2, width * 2 / 5, height * 2 / 5, primary);
            DrawEllipse(pixels, width, height, width / 2, height / 2, width / 4, height / 4, accent);
            DrawRect(pixels, width, height, width / 2 - width / 18, height / 3, width / 9, height / 3, new Color(1f, 1f, 1f, 0.75f));
            DrawRect(pixels, width, height, width / 3, height / 2 - height / 18, width / 3, height / 9, new Color(1f, 1f, 1f, 0.75f));
        }

        private static void DrawUiIcon(Color[] pixels, int width, int height, Color primary, Color accent)
        {
            DrawEllipse(pixels, width, height, width / 2, height / 2, width * 2 / 5, height * 2 / 5, primary);
            DrawRect(pixels, width, height, width / 3, height / 3, width / 3, height / 3, accent);
        }

        private static void DrawRect(Color[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, Color color)
        {
            var xMin = Mathf.Clamp(x, 0, width);
            var xMax = Mathf.Clamp(x + rectWidth, 0, width);
            var yMin = Mathf.Clamp(y, 0, height);
            var yMax = Mathf.Clamp(y + rectHeight, 0, height);

            for (var py = yMin; py < yMax; py++)
            {
                for (var px = xMin; px < xMax; px++)
                {
                    pixels[py * width + px] = color;
                }
            }
        }

        private static void DrawEllipse(Color[] pixels, int width, int height, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            var radiusXSquared = Mathf.Max(1, radiusX * radiusX);
            var radiusYSquared = Mathf.Max(1, radiusY * radiusY);

            for (var y = Mathf.Max(0, centerY - radiusY); y < Mathf.Min(height, centerY + radiusY); y++)
            {
                for (var x = Mathf.Max(0, centerX - radiusX); x < Mathf.Min(width, centerX + radiusX); x++)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    if (dx * dx * radiusYSquared + dy * dy * radiusXSquared <= radiusXSquared * radiusYSquared)
                    {
                        pixels[y * width + x] = color;
                    }
                }
            }
        }

        private static Sprite BuildSprite(string name, int width, int height, Color[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"{name}{FallbackTextureSuffix}",
                hideFlags = HideFlags.HideAndDontSave
            };

            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = $"{name}{FallbackSpriteSuffix}";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
