using UnityEngine;

namespace CareerQuest
{
    public class AvatarDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Role { get; }
        public Color ShirtColor { get; }

        public AvatarDefinition(string id, string displayName, string role, Color shirtColor)
        {
            Id = id;
            DisplayName = displayName;
            Role = role;
            ShirtColor = shirtColor;
        }
    }

    public static class AvatarConfig
    {
        public const string DefaultAvatarId = "sky_builder";

        public static readonly AvatarDefinition[] Avatars =
        {
            new("sky_builder", "Sky Builder", "Future city maker", new Color(0.12f, 0.43f, 0.86f)),
            new("care_captain", "Care Captain", "Health helper", new Color(0.05f, 0.55f, 0.5f)),
            new("logic_spark", "Logic Spark", "Evidence solver", new Color(0.93f, 0.55f, 0.12f)),
            new("art_inventor", "Art Inventor", "Creative problem solver", new Color(0.62f, 0.52f, 0.86f))
        };

        public static AvatarDefinition DefaultAvatar => GetAvatar(DefaultAvatarId);

        public static AvatarDefinition GetAvatar(string id)
        {
            foreach (var avatar in Avatars)
            {
                if (avatar.Id == id)
                {
                    return avatar;
                }
            }

            return Avatars[0];
        }
    }
}
