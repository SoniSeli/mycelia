using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using Object = UnityEngine.Object;

namespace Mycelia.Game.Tests
{
    public class SporeViewPoolTests
    {
        GameObject template;
        GameObject parent;

        [SetUp]
        public void CreateTemplate()
        {
            template = TestViews.SporeTemplate();
            parent = new GameObject("Pool");
        }

        [TearDown]
        public void DestroyObjects()
        {
            Object.DestroyImmediate(template);
            Object.DestroyImmediate(parent);
        }

        int ViewCount => parent.GetComponentsInChildren<SporeView>(includeInactive: true).Length;

        // Found on the phone: each view's C# Transform wrapper was created the first time a move showed it.
        [Test]
        public void ShowingViewsThatWereNeverRentedBefore_AllocatesNothing()
        {
            var type = ScriptableObject.CreateInstance<Content.SporeType>();
            try
            {
                // Another pool warms up what every pool shares, as earlier levels would.
                var earlier = new SporeViewPool(template.GetComponent<SporeView>(), parent.transform, capacity: 1);
                earlier.Rent().Show(type, Vector2.zero);

                var pool = new SporeViewPool(template.GetComponent<SporeView>(), parent.transform, capacity: 4);

                Assert.That(() =>
                {
                    for (int i = 0; i < 4; i++) pool.Rent().Show(type, Vector2.one);
                }, Is.Not.AllocatingGCMemory());
            }
            finally
            {
                Object.DestroyImmediate(type);
            }
        }

        [Test]
        public void ThePool_CreatesEveryViewUpFront_Hidden()
        {
            var pool = new SporeViewPool(template.GetComponent<SporeView>(), parent.transform, capacity: 8);

            Assert.That(ViewCount, Is.EqualTo(8));
            Assert.That(parent.GetComponentsInChildren<SporeView>(includeInactive: false), Is.Empty);
            Assert.That(pool.Available, Is.EqualTo(8));
        }

        [Test]
        public void RentingAndReturning_NeverCreatesOrDestroysViews()
        {
            var pool = new SporeViewPool(template.GetComponent<SporeView>(), parent.transform, capacity: 4);

            for (int round = 0; round < 3; round++)
            {
                var rented = new SporeView[4];
                for (int i = 0; i < rented.Length; i++) rented[i] = pool.Rent();
                Assert.That(pool.Available, Is.Zero);
                foreach (SporeView view in rented) pool.Return(view);
            }

            Assert.That(ViewCount, Is.EqualTo(4));
            Assert.That(pool.Available, Is.EqualTo(4));
        }

        [Test]
        public void ARentedView_IsShown_AndAReturnedViewIsHidden()
        {
            var pool = new SporeViewPool(template.GetComponent<SporeView>(), parent.transform, capacity: 1);

            SporeView view = pool.Rent();
            Assert.That(view.gameObject.activeSelf, Is.True);

            pool.Return(view);
            Assert.That(view.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void AnEmptyPool_RefusesToRent_InsteadOfCreatingAView()
        {
            var pool = new SporeViewPool(template.GetComponent<SporeView>(), parent.transform, capacity: 1);
            pool.Rent();

            Assert.Throws<InvalidOperationException>(() => pool.Rent());
            Assert.That(ViewCount, Is.EqualTo(1));
        }

        [Test]
        public void AViewReturnedTwice_IsRejected()
        {
            var pool = new SporeViewPool(template.GetComponent<SporeView>(), parent.transform, capacity: 2);
            SporeView view = pool.Rent();
            pool.Return(view);

            Assert.Throws<InvalidOperationException>(() => pool.Return(view));
            Assert.That(pool.Available, Is.EqualTo(2));
        }
    }
}
