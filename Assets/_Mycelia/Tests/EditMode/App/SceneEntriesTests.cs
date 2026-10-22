using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mycelia.App.Tests
{
    public class SceneEntriesTests
    {
        readonly List<GameObject> roots = new List<GameObject>();

        [TearDown]
        public void DestroyRoots()
        {
            foreach (GameObject root in roots) Object.DestroyImmediate(root);
            roots.Clear();
        }

        GameObject Root()
        {
            var root = new GameObject("root");
            roots.Add(root);
            return root;
        }

        [Test]
        public void TheEntry_IsFoundAnywhereInTheScene()
        {
            Root();
            var child = new GameObject("child");
            child.transform.SetParent(Root().transform);
            var entry = child.AddComponent<GameSceneEntry>();

            Assert.That(SceneEntries.FindSingle("Test", roots), Is.SameAs(entry));
        }

        [Test]
        public void ASceneWithoutAnEntry_IsRejected()
        {
            Root();

            var error = Assert.Throws<InvalidOperationException>(() => SceneEntries.FindSingle("Test", roots));
            StringAssert.Contains("Test scene", error.Message);
        }

        [Test]
        public void ASceneWithTwoEntries_IsRejected()
        {
            Root().AddComponent<GameSceneEntry>();
            var inactive = new GameObject("inactive");
            inactive.transform.SetParent(Root().transform);
            inactive.AddComponent<GameSceneEntry>();
            inactive.SetActive(false);

            var error = Assert.Throws<InvalidOperationException>(() => SceneEntries.FindSingle("Test", roots));
            StringAssert.Contains("has 2", error.Message);
        }
    }
}
