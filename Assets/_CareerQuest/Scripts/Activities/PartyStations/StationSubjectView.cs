using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Design-review (2026-06-16, art pass): the drawn "scene subject" for a
    /// station — the character the seed copy names (the dragon, the sleepy
    /// robot, a guest). It was originally composed from flat
    /// <see cref="CampusWorldSprites"/> primitives, which read as low-quality
    /// next to the curated Kenney avatars. It now mounts real curated Kenney art
    /// routed through <see cref="AssetCatalog"/> (npc.subject_* ids, the same
    /// pipeline the room NPCs use), size-normalized so every subject reads at a
    /// consistent gameplay height regardless of its source art. Mounted above
    /// the toy playfield by <see cref="PartyStationController"/> and parented to
    /// the kit root, so kit teardown owns it. Pure presentation: it carries no
    /// interaction and never touches the rules.
    /// </summary>
    public static class StationSubjectView
    {
        public const string RootName = "StationSubject";
        public const string NameLabelName = "StationSubjectName";
        public const string BodyName = "StationSubjectBody";

        /// <summary>Rendered world height for every subject (normalizes mixed source art).</summary>
        public const float SubjectHeight = 1.6f;

        // Layering band: shadow under the subject, subject above it, name below.
        private const int OrderShadow = ToyInteractionKit.ZoneSortingOrder + 20;
        private const int OrderBody = ToyInteractionKit.ZoneSortingOrder + 22;
        private const int OrderLabel = ToyInteractionKit.ZoneSortingOrder + 24;

        private static readonly Color Shadow = new(0.05f, 0.07f, 0.09f, 0.18f);

        /// <summary>The curated Kenney catalog sprite id for each subject kind.</summary>
        public static string CatalogId(StationSubjectKind kind)
        {
            return kind switch
            {
                StationSubjectKind.Dragon => "npc.subject_dragon",
                StationSubjectKind.Critter => "npc.subject_critter",
                StationSubjectKind.Robot => "npc.subject_robot",
                StationSubjectKind.Cloud => "npc.subject_cloud",
                StationSubjectKind.Blob => "npc.subject_blob",
                _ => "npc.subject_person"
            };
        }

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

            // Soft contact shadow grounds the subject in the empty space.
            var shadow = new GameObject("Shadow", typeof(SpriteRenderer));
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, -SubjectHeight * 0.52f, 0f);
            shadow.transform.localScale = new Vector3(SubjectHeight * 0.6f, SubjectHeight * 0.17f, 1f);
            var shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            shadowRenderer.sprite = CampusWorldSprites.Circle;
            shadowRenderer.color = Shadow;
            shadowRenderer.sortingOrder = OrderShadow;

            // The curated subject sprite, size-normalized to SubjectHeight so the
            // Toon-art (robot/person) and the monster composites read at one scale.
            var body = new GameObject(BodyName, typeof(SpriteRenderer));
            body.transform.SetParent(root.transform, false);
            var renderer = body.GetComponent<SpriteRenderer>();
            renderer.sprite = AssetCatalog.SpriteFor(CatalogId(kind));
            renderer.sortingOrder = OrderBody;

            var spriteHeight = renderer.sprite != null ? renderer.sprite.bounds.size.y : 0f;
            var scale = spriteHeight > 0.0001f ? SubjectHeight / spriteHeight : 1f;
            body.transform.localScale = new Vector3(scale, scale, 1f);

            PartyStationRenderer.AddWorldLabel(
                root.transform, NameLabelName, name,
                new Vector3(0f, -SubjectHeight * 0.5f - 0.32f, 0f), 1.7f, OrderLabel);
            return root;
        }
    }
}
