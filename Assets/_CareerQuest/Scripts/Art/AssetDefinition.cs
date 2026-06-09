using UnityEngine;

namespace CareerQuest
{
    public sealed class AssetDefinition
    {
        public AssetDefinition(
            string id,
            string displayName,
            AssetCategory category,
            Color primaryColor,
            Color accentColor,
            Vector2Int pixelSize,
            bool requiredInFirstPlayable = true)
        {
            Id = id;
            DisplayName = displayName;
            Category = category;
            PrimaryColor = primaryColor;
            AccentColor = accentColor;
            PixelSize = pixelSize;
            RequiredInFirstPlayable = requiredInFirstPlayable;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public AssetCategory Category { get; }
        public Color PrimaryColor { get; }
        public Color AccentColor { get; }
        public Vector2Int PixelSize { get; }
        public bool RequiredInFirstPlayable { get; }
        public string ResourcePath => $"CareerQuest/{Category}/{Id}";
    }
}
