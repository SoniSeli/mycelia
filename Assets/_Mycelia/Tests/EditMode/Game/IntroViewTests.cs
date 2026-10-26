using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Mycelia.Game.Tests
{
    // Design §8.3 slice 8: the intro shows one page at a time under one round Next button; the chain demo, drawn in
    // the world behind the page, shows only with "How to Grow".
    public class IntroViewTests
    {
        readonly List<GameObject> created = new List<GameObject>();
        GameObject[] pages;
        GameObject demo;
        Button next;
        int finished, left;

        [TearDown]
        public void DestroyAll()
        {
            foreach (GameObject obj in created) Object.DestroyImmediate(obj);
            created.Clear();
        }

        IntroView View()
        {
            var root = new GameObject("Intro", typeof(RectTransform));
            created.Add(root);
            pages = Enumerable.Range(1, 3).Select(i =>
            {
                var page = new GameObject($"Page {i}", typeof(RectTransform));
                page.transform.SetParent(root.transform, false);
                return page;
            }).ToArray();
            next = new GameObject("Next", typeof(RectTransform)).AddComponent<Button>();
            next.transform.SetParent(root.transform, false);
            demo = new GameObject("Chain Demo");
            created.Add(demo);

            var view = root.AddComponent<IntroView>();
            var serialized = new SerializedObject(view);
            SerializedProperty array = serialized.FindProperty("pages");
            array.arraySize = pages.Length;
            for (int i = 0; i < pages.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = pages[i];
            serialized.FindProperty("next").objectReferenceValue = next;
            serialized.FindProperty("demo").objectReferenceValue = demo;
            serialized.FindProperty("demoPage").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            finished = left = 0;
            view.Init(() => finished++, () => left++);
            return view;
        }

        int[] Shown => Enumerable.Range(0, pages.Length).Where(i => pages[i].activeSelf).ToArray();

        [Test]
        public void TheIntro_ShowsOnlyItsFirstPage_AtFirst()
        {
            View();

            Assert.That(Shown, Is.EqualTo(new[] { 0 }));
            Assert.That(demo.activeSelf, Is.False);
        }

        [Test]
        public void Next_ShowsTheFollowingPage_AndTheDemoOnlyWithItsPage()
        {
            View();

            next.onClick.Invoke();
            Assert.That(Shown, Is.EqualTo(new[] { 1 }));
            Assert.That(demo.activeSelf, Is.True);

            next.onClick.Invoke();
            Assert.That(Shown, Is.EqualTo(new[] { 2 }));
            Assert.That(demo.activeSelf, Is.False);
            Assert.That(finished, Is.EqualTo(0));
        }

        [Test]
        public void Next_OnTheLastPage_FinishesTheIntro()
        {
            View();
            next.onClick.Invoke();
            next.onClick.Invoke();

            next.onClick.Invoke();

            Assert.That(finished, Is.EqualTo(1));
            Assert.That(left, Is.EqualTo(0));
            Assert.That(Shown, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void Back_ShowsThePreviousPage_AndOnTheFirstLeaves()
        {
            IntroView view = View();
            next.onClick.Invoke();

            view.Back();
            Assert.That(Shown, Is.EqualTo(new[] { 0 }));
            Assert.That(demo.activeSelf, Is.False);
            Assert.That(left, Is.EqualTo(0));

            view.Back();
            Assert.That(left, Is.EqualTo(1));
            Assert.That(finished, Is.EqualTo(0));
        }
    }
}
