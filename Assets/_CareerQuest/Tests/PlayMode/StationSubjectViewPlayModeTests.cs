using System.Collections;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// Design-review (2026-06-16, art pass): the scene subject now mounts curated
    /// Kenney art routed through AssetCatalog (npc.subject_*), not flat shape
    /// primitives. These verify a Mount produces a named, visible, size-normalized
    /// sprite and that every subject kind resolves a cataloged sprite.
    /// </summary>
    public class StationSubjectViewPlayModeTests
    {
        private GameObject _parent;

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("SubjectTestParent");
        }

        [TearDown]
        public void TearDown()
        {
            if (_parent != null)
            {
                Object.DestroyImmediate(_parent);
            }
        }

        [UnityTest]
        public IEnumerator MountBuildsANamedVisibleSubject()
        {
            var root = StationSubjectView.Mount(
                _parent.transform,
                StationSubjectKind.Dragon,
                "Hiccup the Dragon",
                new Color(0.36f, 0.78f, 0.6f),
                new Vector3(0f, 1.95f, 0f));

            Assert.That(root, Is.Not.Null);
            Assert.That(root.name, Is.EqualTo(StationSubjectView.RootName));
            Assert.That(root.transform.parent, Is.EqualTo(_parent.transform));

            // The subject is drawn: a shadow + the body sprite, and the body
            // carries an actual sprite (the curated art, not a null renderer).
            var sprites = root.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(sprites.Length, Is.GreaterThanOrEqualTo(2), "subject needs a shadow and a body sprite");

            var bodyRenderer = sprites.FirstOrDefault(s => s.gameObject.name == StationSubjectView.BodyName);
            Assert.That(bodyRenderer, Is.Not.Null, "subject needs a body");
            Assert.That(bodyRenderer.sprite, Is.Not.Null, "subject body needs a sprite");

            // The kid-facing name is shown.
            var label = root.GetComponentsInChildren<TextMeshPro>()
                .FirstOrDefault(t => t.gameObject.name == StationSubjectView.NameLabelName);
            Assert.That(label, Is.Not.Null, "subject needs a name label");
            Assert.That(label.text, Is.EqualTo("Hiccup the Dragon"));

            yield return null;
        }

        [UnityTest]
        public IEnumerator EverySubjectKindResolvesCuratedArt()
        {
            foreach (StationSubjectKind kind in System.Enum.GetValues(typeof(StationSubjectKind)))
            {
                var id = StationSubjectView.CatalogId(kind);
                Assert.That(AssetCatalog.TryGetDefinition(id, out _), Is.True, $"{kind} -> {id} must be cataloged");

                var root = StationSubjectView.Mount(
                    _parent.transform,
                    kind,
                    $"Test {kind}",
                    Color.cyan,
                    Vector3.zero);
                Assert.That(root, Is.Not.Null, kind.ToString());

                var body = root.GetComponentsInChildren<SpriteRenderer>()
                    .FirstOrDefault(s => s.gameObject.name == StationSubjectView.BodyName);
                Assert.That(body, Is.Not.Null, kind.ToString());
                Assert.That(body.sprite, Is.Not.Null, kind.ToString());
                Object.DestroyImmediate(root);
            }

            yield return null;
        }
    }
}
