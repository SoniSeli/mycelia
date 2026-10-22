using System;
using System.Linq;
using NUnit.Framework;
using Mycelia.Domain;

namespace Mycelia.Content.Tests
{
    public class SporeCatalogTests
    {
        ContentFactory content;

        [SetUp]
        public void SetUp() => content = new ContentFactory();

        [TearDown]
        public void TearDown() => content.Dispose();

        [Test]
        public void Ids_StartAtOneInListOrder()
        {
            // ADR-0005: 0 is reserved for SporeTypeId.None.
            var circle = content.Spore("spore-circle");
            var triangle = content.Spore("spore-triangle");
            var catalog = content.Catalog(circle, triangle);

            Assert.That(catalog.IdOf(circle), Is.EqualTo(new SporeTypeId(1)));
            Assert.That(catalog.IdOf(triangle), Is.EqualTo(new SporeTypeId(2)));
        }

        [Test]
        public void TypeOf_ReturnsTheAssetForAnId()
        {
            var circle = content.Spore("spore-circle");
            var triangle = content.Spore("spore-triangle");
            var catalog = content.Catalog(circle, triangle);

            Assert.That(catalog.TypeOf(new SporeTypeId(2)), Is.SameAs(triangle));
        }

        [Test]
        public void TypeOf_RejectsNoneAndUnknownIds()
        {
            var catalog = content.Catalog(content.Spore("spore-circle"));

            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.TypeOf(SporeTypeId.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => catalog.TypeOf(new SporeTypeId(2)));
        }

        [Test]
        public void TryGetByKey_FindsTheId()
        {
            var catalog = content.Catalog(content.Spore("spore-circle"), content.Spore("spore-triangle"));

            Assert.That(catalog.TryGetByKey("spore-triangle", out SporeTypeId id), Is.True);
            Assert.That(id, Is.EqualTo(new SporeTypeId(2)));
            Assert.That(catalog.TryGetByKey("spore-missing", out _), Is.False);
        }

        [Test]
        public void IdOf_RejectsATypeNotInTheCatalog()
        {
            var catalog = content.Catalog(content.Spore("spore-circle"));

            Assert.Throws<ArgumentException>(() => catalog.IdOf(content.Spore("spore-other")));
        }

        [Test]
        public void AValidCatalog_HasNoProblems()
        {
            var catalog = content.Catalog(content.Spore("spore-circle"), content.Spore("spore-triangle"));

            Assert.That(catalog.FindProblems(), Is.Empty);
        }

        [Test]
        public void AnEmptyEntry_IsAProblem()
        {
            var catalog = content.Catalog(content.Spore("spore-circle"), null);

            Assert.That(catalog.FindProblems(), Has.Some.Contains("empty"));
        }

        [TestCase("")]
        [TestCase("Spore-Circle")]
        [TestCase("spore circle")]
        public void AKeyThatIsNotLowercaseLettersDigitsAndHyphens_IsAProblem(string key)
        {
            var catalog = content.Catalog(content.Spore(key));

            Assert.That(catalog.FindProblems(), Has.Some.Contains("key"));
        }

        [Test]
        public void ADuplicateKey_IsAProblem()
        {
            var catalog = content.Catalog(content.Spore("spore-circle"), content.Spore("spore-circle"));

            Assert.That(catalog.FindProblems(), Has.Some.Contains("more than one"));
        }

        [Test]
        public void TheSameAssetListedTwice_IsAProblem()
        {
            var circle = content.Spore("spore-circle");
            var catalog = content.Catalog(circle, circle);

            Assert.That(catalog.FindProblems(), Has.Some.Contains("listed more than once"));
        }

        [Test]
        public void AMissingSprite_IsAProblem()
        {
            var circle = content.Spore("spore-circle");
            circle.Set("spore-circle", "Circle", UnityEngine.Color.white, null, "circle");
            var catalog = content.Catalog(circle);

            Assert.That(catalog.FindProblems(), Has.Some.Contains("no sprite"));
        }

        [Test]
        public void TwoTypesSharingASprite_IsAProblem()
        {
            // ADR-0005: every spore type needs its own silhouette, so colour is never the only cue.
            var shared = content.Sprite();
            var catalog = content.Catalog(content.Spore("spore-circle", shared), content.Spore("spore-triangle", shared));

            Assert.That(catalog.FindProblems(), Has.Some.Contains("share a sprite"));
        }

        [Test]
        public void MoreThan255Types_IsAProblem()
        {
            var types = Enumerable.Range(0, SporeCatalog.MaxTypes + 1).Select(i => content.Spore($"spore-{i}")).ToArray();
            var catalog = content.Catalog(types);

            Assert.That(catalog.FindProblems(), Has.Some.Contains("255"));
        }
    }
}
