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
    /// Design-review (2026-06-16): the scene subject is drawn from shape
    /// primitives. These verify a Mount produces a named, visible creature and
    /// that each subject kind composes without error.
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
        public IEnumerator MountBuildsANamedVisibleCreature()
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

            // The creature is actually drawn (multiple sprite parts incl. a body).
            var sprites = root.GetComponentsInChildren<SpriteRenderer>();
            Assert.That(sprites.Length, Is.GreaterThanOrEqualTo(6), "subject should be composed of several shape parts");
            Assert.That(sprites.Any(s => s.gameObject.name == StationSubjectView.BodyName), Is.True, "subject needs a body");

            // The kid-facing name is shown.
            var label = root.GetComponentsInChildren<TextMeshPro>()
                .FirstOrDefault(t => t.gameObject.name == StationSubjectView.NameLabelName);
            Assert.That(label, Is.Not.Null, "subject needs a name label");
            Assert.That(label.text, Is.EqualTo("Hiccup the Dragon"));

            yield return null;
        }

        [UnityTest]
        public IEnumerator EverySubjectKindComposesWithoutError()
        {
            foreach (StationSubjectKind kind in System.Enum.GetValues(typeof(StationSubjectKind)))
            {
                var root = StationSubjectView.Mount(
                    _parent.transform,
                    kind,
                    $"Test {kind}",
                    Color.cyan,
                    Vector3.zero);
                Assert.That(root, Is.Not.Null, kind.ToString());
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Length, Is.GreaterThan(3), kind.ToString());
                Object.DestroyImmediate(root);
            }

            yield return null;
        }
    }
}
