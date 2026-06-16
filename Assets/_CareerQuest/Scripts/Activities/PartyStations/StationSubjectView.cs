using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Design-review (2026-06-16): the drawn "scene subject" for a station — the
    /// character the seed copy names (the dragon, the sleepy robot, a guest).
    /// Composed from <see cref="CampusWorldSprites"/> primitives (the same trick
    /// the hub avatars use), so no baked art or catalog entry is needed. Mounted
    /// above the toy playfield by <see cref="PartyStationController"/> and parented
    /// to the kit root, so kit teardown owns it. Pure presentation: it carries no
    /// interaction and never touches the rules.
    /// </summary>
    public static class StationSubjectView
    {
        public const string RootName = "StationSubject";
        public const string NameLabelName = "StationSubjectName";
        public const string BodyName = "StationSubjectBody";

        // Layering band (self-contained; the subject sits in empty space above
        // the toys). Back features < body < front features < face.
        private const int OrderShadow = ToyInteractionKit.ZoneSortingOrder + 20;
        private const int OrderBack = ToyInteractionKit.ZoneSortingOrder + 21;
        private const int OrderBody = ToyInteractionKit.ZoneSortingOrder + 22;
        private const int OrderFront = ToyInteractionKit.ZoneSortingOrder + 23;
        private const int OrderFace = ToyInteractionKit.ZoneSortingOrder + 24;
        private const int OrderPupil = ToyInteractionKit.ZoneSortingOrder + 25;

        private static readonly Color Ink = new(0.12f, 0.16f, 0.22f);
        private static readonly Color White = new(1f, 1f, 1f, 0.96f);
        private static readonly Color Shadow = new(0.05f, 0.07f, 0.09f, 0.16f);

        /// <summary>
        /// Mounts the subject creature + its kid-facing name at <paramref name="localPosition"/>
        /// under <paramref name="parent"/>. Returns the subject root.
        /// </summary>
        public static GameObject Mount(Transform parent, StationSubjectKind kind, string name, Color accent, Vector3 localPosition)
        {
            if (parent == null)
            {
                return null;
            }

            var root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            Part(root.transform, "Shadow", CampusWorldSprites.Circle, new Vector3(0f, -0.62f, 0f), new Vector3(1.05f, 0.3f, 1f), Shadow, OrderShadow);

            switch (kind)
            {
                case StationSubjectKind.Dragon:
                    DrawDragon(root.transform, accent);
                    break;
                case StationSubjectKind.Critter:
                    DrawCritter(root.transform, accent);
                    break;
                case StationSubjectKind.Robot:
                    DrawRobot(root.transform, accent);
                    break;
                case StationSubjectKind.Cloud:
                    DrawCloud(root.transform, accent);
                    break;
                case StationSubjectKind.Star:
                    DrawStar(root.transform, accent);
                    break;
                case StationSubjectKind.Blob:
                    DrawBlob(root.transform, accent);
                    break;
                default:
                    DrawPerson(root.transform, accent);
                    break;
            }

            PartyStationRenderer.AddWorldLabel(root.transform, NameLabelName, name, new Vector3(0f, -0.95f, 0f), 1.7f, OrderFace + 2);
            return root;
        }

        private static void DrawDragon(Transform root, Color accent)
        {
            var body = Tint(new Color(0.42f, 0.76f, 0.5f), accent);
            // Wings behind the body.
            Part(root, "WingL", CampusWorldSprites.Circle, new Vector3(-0.52f, 0.08f, 0f), new Vector3(0.5f, 0.62f, 1f), body * 0.86f, OrderBack);
            Part(root, "WingR", CampusWorldSprites.Circle, new Vector3(0.52f, 0.08f, 0f), new Vector3(0.5f, 0.62f, 1f), body * 0.86f, OrderBack);
            // Horns.
            Part(root, "HornL", CampusWorldSprites.Square, new Vector3(-0.16f, 0.46f, 0f), new Vector3(0.09f, 0.22f, 1f), new Color(0.97f, 0.92f, 0.78f), OrderFront, 18f);
            Part(root, "HornR", CampusWorldSprites.Square, new Vector3(0.16f, 0.46f, 0f), new Vector3(0.09f, 0.22f, 1f), new Color(0.97f, 0.92f, 0.78f), OrderFront, -18f);
            Body(root, body);
            // Snout.
            Part(root, "Snout", CampusWorldSprites.Circle, new Vector3(0f, -0.18f, 0f), new Vector3(0.5f, 0.34f, 1f), Lighter(body), OrderFront);
            Face(root, 0.12f, 0.2f);
        }

        private static void DrawCritter(Transform root, Color accent)
        {
            var body = Tint(new Color(0.86f, 0.72f, 0.52f), accent);
            // Round ears.
            Part(root, "EarL", CampusWorldSprites.Circle, new Vector3(-0.3f, 0.42f, 0f), new Vector3(0.32f, 0.32f, 1f), body, OrderBack);
            Part(root, "EarR", CampusWorldSprites.Circle, new Vector3(0.3f, 0.42f, 0f), new Vector3(0.32f, 0.32f, 1f), body, OrderBack);
            Part(root, "EarInL", CampusWorldSprites.Circle, new Vector3(-0.3f, 0.42f, 0f), new Vector3(0.16f, 0.16f, 1f), new Color(0.95f, 0.72f, 0.7f), OrderBody);
            Part(root, "EarInR", CampusWorldSprites.Circle, new Vector3(0.3f, 0.42f, 0f), new Vector3(0.16f, 0.16f, 1f), new Color(0.95f, 0.72f, 0.7f), OrderBody);
            Body(root, body);
            // Cheeks.
            Part(root, "CheekL", CampusWorldSprites.Circle, new Vector3(-0.32f, -0.05f, 0f), new Vector3(0.18f, 0.14f, 1f), new Color(0.96f, 0.7f, 0.68f, 0.85f), OrderFront);
            Part(root, "CheekR", CampusWorldSprites.Circle, new Vector3(0.32f, -0.05f, 0f), new Vector3(0.18f, 0.14f, 1f), new Color(0.96f, 0.7f, 0.68f, 0.85f), OrderFront);
            Face(root, 0.1f, 0.2f);
        }

        private static void DrawRobot(Transform root, Color accent)
        {
            var body = Tint(new Color(0.62f, 0.7f, 0.8f), accent);
            // Antenna.
            Part(root, "Antenna", CampusWorldSprites.Square, new Vector3(0f, 0.55f, 0f), new Vector3(0.05f, 0.28f, 1f), body * 0.8f, OrderBack);
            Part(root, "AntennaBall", CampusWorldSprites.Circle, new Vector3(0f, 0.72f, 0f), new Vector3(0.16f, 0.16f, 1f), accent, OrderFront);
            // Squarish head.
            Part(root, BodyName, CampusWorldSprites.Square, Vector3.zero, new Vector3(0.92f, 0.86f, 1f), body, OrderBody);
            Part(root, "Belly", CampusWorldSprites.Square, new Vector3(0f, -0.04f, 0f), new Vector3(0.62f, 0.4f, 1f), Lighter(body), OrderFront);
            FaceSquareEyes(root, 0.12f, 0.2f);
        }

        private static void DrawCloud(Transform root, Color accent)
        {
            var body = Tint(new Color(0.92f, 0.95f, 1f), accent * 0.5f + Color.white * 0.5f);
            Part(root, "PuffL", CampusWorldSprites.Circle, new Vector3(-0.38f, -0.02f, 0f), new Vector3(0.62f, 0.62f, 1f), body, OrderBody);
            Part(root, "PuffR", CampusWorldSprites.Circle, new Vector3(0.38f, -0.02f, 0f), new Vector3(0.62f, 0.62f, 1f), body, OrderBody);
            Part(root, BodyName, CampusWorldSprites.Circle, new Vector3(0f, 0.1f, 0f), new Vector3(0.86f, 0.78f, 1f), body, OrderBody);
            Part(root, "Base", CampusWorldSprites.Square, new Vector3(0f, -0.16f, 0f), new Vector3(0.95f, 0.4f, 1f), body, OrderBody);
            Face(root, 0.02f, 0.2f);
        }

        private static void DrawStar(Transform root, Color accent)
        {
            var body = Tint(new Color(0.96f, 0.82f, 0.4f), accent);
            // Five spikes.
            for (var i = 0; i < 5; i++)
            {
                var angle = 90f + i * 72f;
                var rad = angle * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(rad) * 0.46f, Mathf.Sin(rad) * 0.46f, 0f);
                Part(root, $"Spike{i}", CampusWorldSprites.Square, pos, new Vector3(0.22f, 0.22f, 1f), body, OrderBack, angle + 45f);
            }

            Body(root, body);
            Face(root, 0.06f, 0.18f);
        }

        private static void DrawBlob(Transform root, Color accent)
        {
            var body = Tint(new Color(0.7f, 0.85f, 0.95f), accent);
            // Little bubbles floating off it.
            Part(root, "BubbleA", CampusWorldSprites.Circle, new Vector3(-0.46f, 0.42f, 0f), new Vector3(0.2f, 0.2f, 1f), new Color(body.r, body.g, body.b, 0.7f), OrderBack);
            Part(root, "BubbleB", CampusWorldSprites.Circle, new Vector3(0.5f, 0.34f, 0f), new Vector3(0.14f, 0.14f, 1f), new Color(body.r, body.g, body.b, 0.7f), OrderBack);
            Body(root, body);
            Part(root, "Shine", CampusWorldSprites.Circle, new Vector3(-0.22f, 0.22f, 0f), new Vector3(0.22f, 0.22f, 1f), new Color(1f, 1f, 1f, 0.5f), OrderFront);
            Face(root, 0.05f, 0.2f);
        }

        private static void DrawPerson(Transform root, Color accent)
        {
            var shirt = Tint(new Color(0.55f, 0.62f, 0.86f), accent);
            var skin = new Color(0.96f, 0.8f, 0.66f);
            // Body.
            Part(root, "Torso", CampusWorldSprites.Square, new Vector3(0f, -0.34f, 0f), new Vector3(0.62f, 0.56f, 1f), shirt, OrderBody);
            // Head.
            Part(root, BodyName, CampusWorldSprites.Circle, new Vector3(0f, 0.24f, 0f), new Vector3(0.58f, 0.58f, 1f), skin, OrderBody);
            Part(root, "Hair", CampusWorldSprites.Circle, new Vector3(0f, 0.42f, 0f), new Vector3(0.56f, 0.28f, 1f), new Color(0.34f, 0.26f, 0.22f), OrderFront);
            Face(root, 0.24f, 0.16f);
        }

        // ---- shared parts -------------------------------------------------

        private static void Body(Transform root, Color color)
        {
            Part(root, BodyName, CampusWorldSprites.Circle, Vector3.zero, new Vector3(0.95f, 0.95f, 1f), color, OrderBody);
            Part(root, "Belly", CampusWorldSprites.Circle, new Vector3(0f, -0.08f, 0f), new Vector3(0.6f, 0.55f, 1f), Lighter(color), OrderFront);
        }

        /// <summary>Two round eyes + a smile centered on <paramref name="centerY"/>.</summary>
        private static void Face(Transform root, float centerY, float eyeSpread)
        {
            Part(root, "EyeWhiteL", CampusWorldSprites.Circle, new Vector3(-eyeSpread, centerY, 0f), new Vector3(0.19f, 0.19f, 1f), White, OrderFace);
            Part(root, "EyeWhiteR", CampusWorldSprites.Circle, new Vector3(eyeSpread, centerY, 0f), new Vector3(0.19f, 0.19f, 1f), White, OrderFace);
            Part(root, "PupilL", CampusWorldSprites.Circle, new Vector3(-eyeSpread, centerY - 0.01f, 0f), new Vector3(0.09f, 0.09f, 1f), Ink, OrderPupil);
            Part(root, "PupilR", CampusWorldSprites.Circle, new Vector3(eyeSpread, centerY - 0.01f, 0f), new Vector3(0.09f, 0.09f, 1f), Ink, OrderPupil);
            Smile(root, centerY - 0.2f);
        }

        /// <summary>Robot variant: square eyes, same smile.</summary>
        private static void FaceSquareEyes(Transform root, float centerY, float eyeSpread)
        {
            Part(root, "EyeL", CampusWorldSprites.Square, new Vector3(-eyeSpread, centerY, 0f), new Vector3(0.16f, 0.16f, 1f), White, OrderFace);
            Part(root, "EyeR", CampusWorldSprites.Square, new Vector3(eyeSpread, centerY, 0f), new Vector3(0.16f, 0.16f, 1f), White, OrderFace);
            Part(root, "PupilL", CampusWorldSprites.Square, new Vector3(-eyeSpread, centerY, 0f), new Vector3(0.07f, 0.07f, 1f), Ink, OrderPupil);
            Part(root, "PupilR", CampusWorldSprites.Square, new Vector3(eyeSpread, centerY, 0f), new Vector3(0.07f, 0.07f, 1f), Ink, OrderPupil);
            Smile(root, centerY - 0.2f);
        }

        /// <summary>A small upturned smile: three dots stepping down then up.</summary>
        private static void Smile(Transform root, float y)
        {
            Part(root, "SmileC", CampusWorldSprites.Circle, new Vector3(0f, y - 0.03f, 0f), new Vector3(0.07f, 0.07f, 1f), Ink, OrderPupil);
            Part(root, "SmileL", CampusWorldSprites.Circle, new Vector3(-0.1f, y + 0.02f, 0f), new Vector3(0.06f, 0.06f, 1f), Ink, OrderPupil);
            Part(root, "SmileR", CampusWorldSprites.Circle, new Vector3(0.1f, y + 0.02f, 0f), new Vector3(0.06f, 0.06f, 1f), Ink, OrderPupil);
        }

        private static Color Tint(Color baseColor, Color accent)
        {
            return Color.Lerp(baseColor, accent, 0.22f);
        }

        private static Color Lighter(Color color)
        {
            return Color.Lerp(color, Color.white, 0.28f);
        }

        private static void Part(Transform parent, string name, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int order, float rotation = 0f)
        {
            var part = new GameObject(name, typeof(SpriteRenderer));
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var renderer = part.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
        }
    }
}
