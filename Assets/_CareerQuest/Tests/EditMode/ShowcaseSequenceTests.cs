using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    public class ShowcaseSequenceTests
    {
        [Test]
        public void DefaultShowcaseSequenceContainsRequiredBeatsInOrder()
        {
            var gameObject = new GameObject("showcase-test");
            var presenter = gameObject.AddComponent<ShowcasePresenter>();

            presenter.BuildDefaultSequence();

            Assert.That(presenter.Steps.Count, Is.EqualTo(6));
            Assert.That(presenter.Steps[0].Id, Is.EqualTo("connection"));
            Assert.That(presenter.Steps[1].Id, Is.EqualTo("campus"));
            Assert.That(presenter.Steps[2].Id, Is.EqualTo("stations"));
            Assert.That(presenter.Steps[3].Id, Is.EqualTo("build"));
            Assert.That(presenter.Steps[4].Id, Is.EqualTo("gallery"));
            Assert.That(presenter.Steps[5].Id, Is.EqualTo("reveal"));

            Object.DestroyImmediate(gameObject);
        }
    }
}
