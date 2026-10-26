using System.Collections.Generic;
using System.Linq;
using Mycelia.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Mycelia.Game.Tests
{
    // Design §8.3 slice 8: on "How to Grow" a finger draws a chain through the game's own spores, link by link, the
    // chain clears and grows back, then the next example plays; the demo loops.
    public class ChainDemoTests
    {
        // One second per phase, so the times below read as phase boundaries: an example of n spores lasts 1 (press)
        // + (n - 1) (drag) + 1 (hold) + 1 (clear) + 1 (gap) + 1 (grow back) + 1 (rest) = n + 5 seconds.
        static readonly ChainDemoTiming Seconds = new ChainDemoTiming(press: 1, step: 1, hold: 1, clear: 1, gap: 1, regrow: 1, rest: 1, chainedScale: 1.2f);
        static readonly int[] Lengths = { 3, 4 };

        static ChainDemoFrame At(float seconds) => ChainDemoTimeline.At(seconds, Lengths, Seconds);

        [Test]
        public void TheDemo_StartsWithTheFingerPressingTheFirstSporeOfTheFirstExample()
        {
            ChainDemoFrame frame = At(0f);

            Assert.That(frame.Example, Is.EqualTo(0));
            Assert.That(frame.Links, Is.EqualTo(1));
            Assert.That(frame.FingerShown, Is.True);
            Assert.That(frame.FingerFrom, Is.EqualTo(0));
            Assert.That(frame.FingerLerp, Is.EqualTo(0f));
            Assert.That(frame.ChainScale, Is.EqualTo(1.2f), "Chained spores are drawn larger, as on the board.");
        }

        [Test]
        public void TheFinger_GlidesToTheNextSpore_WhichJoinsTheChainWhenItArrives()
        {
            ChainDemoFrame halfway = At(1.5f);
            Assert.That(halfway.Links, Is.EqualTo(1));
            Assert.That(halfway.FingerFrom, Is.EqualTo(0));
            Assert.That(halfway.FingerLerp, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(halfway.LineShown, Is.True);

            ChainDemoFrame arrived = At(2.25f);
            Assert.That(arrived.Links, Is.EqualTo(2));
            Assert.That(arrived.FingerFrom, Is.EqualTo(1));

            ChainDemoFrame whole = At(3.5f);
            Assert.That(whole.Links, Is.EqualTo(3), "The finger holds on the last spore with the whole chain drawn.");
            Assert.That(whole.FingerFrom, Is.EqualTo(2));
            Assert.That(whole.FingerShown, Is.True);
        }

        [Test]
        public void TheFinger_EasesBetweenSpores_SlowAtBothEnds()
        {
            Assert.That(At(1.25f).FingerLerp, Is.LessThan(0.25f));
            Assert.That(At(1.75f).FingerLerp, Is.GreaterThan(0.75f));
        }

        [Test]
        public void OnRelease_TheChainClears_ThenGrowsBack()
        {
            ChainDemoFrame clearing = At(4.5f);
            Assert.That(clearing.FingerShown, Is.False);
            Assert.That(clearing.LineShown, Is.False);
            Assert.That(clearing.Links, Is.EqualTo(3));
            Assert.That(clearing.ChainScale, Is.EqualTo(0.6f).Within(1e-5f), "Halfway through clearing, half the chained size.");

            Assert.That(At(5.5f).ChainScale, Is.EqualTo(0f), "Cleared spores stay gone for a moment.");
            Assert.That(At(6.5f).ChainScale, Is.EqualTo(0.5f).Within(1e-5f), "Halfway grown back.");

            ChainDemoFrame resting = At(7.5f);
            Assert.That(resting.Links, Is.EqualTo(0));
            Assert.That(resting.FingerShown, Is.False);
            Assert.That(resting.LineShown, Is.False);
        }

        [Test]
        public void TheSecondExample_PlaysAfterTheFirst_AndTheDemoLoops()
        {
            ChainDemoFrame second = At(8f);
            Assert.That(second.Example, Is.EqualTo(1));
            Assert.That(second.Links, Is.EqualTo(1));

            Assert.That(At(8f + 1 + 3 + 0.5f).Links, Is.EqualTo(4), "The second example's chain is four long.");

            ChainDemoFrame again = At(17f + 1.5f);
            Assert.That(again.Example, Is.EqualTo(0));
            Assert.That(again.FingerLerp, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void TheDemo_FitsItsArea_AsLargeAsItCan_Centred()
        {
            (Vector3 centre, float scale) = ChainDemo.Fit(new Rect(0, 0, 10, 4), new Vector2(6, 3));

            Assert.That(centre.x, Is.EqualTo(5f).Within(1e-5f));
            Assert.That(centre.y, Is.EqualTo(2f).Within(1e-5f));
            Assert.That(scale, Is.EqualTo(4f / 3f).Within(1e-5f), "The area's height limits it.");
        }

        // The component, on scene objects: two examples, a straight chain over spores 0-2 and a bent one over 3-6.
        readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void DestroyAll()
        {
            foreach (GameObject obj in created) Object.DestroyImmediate(obj);
            created.Clear();
        }

        ChainDemo Demo(out SporeView[] spores)
        {
            var root = new GameObject("Chain Demo");
            created.Add(root);
            spores = new SporeView[8];
            for (int i = 0; i < spores.Length; i++)
            {
                GameObject spore = TestViews.SporeTemplate();
                spore.transform.SetParent(root.transform, false);
                spore.transform.localPosition = new Vector3(i, i % 2, 0);
                spores[i] = spore.GetComponent<SporeView>();
            }
            var line = new GameObject("Line").AddComponent<LineRenderer>();
            line.transform.SetParent(root.transform, false);
            var finger = new GameObject("Finger").AddComponent<SpriteRenderer>();
            finger.transform.SetParent(root.transform, false);
            var area = new GameObject("Area", typeof(RectTransform));
            created.Add(area);

            var demo = root.AddComponent<ChainDemo>();
            var serialized = new SerializedObject(demo);
            SetArray(serialized.FindProperty("spores"), spores);
            SetInts(serialized.FindProperty("sporeTypes"), new[] { 0, 0, 0, 3, 3, 3, 3, 1 });
            SetInts(serialized.FindProperty("chains"), new[] { 0, 1, 2, 3, 4, 5, 6 });
            SetInts(serialized.FindProperty("chainLengths"), Lengths);
            serialized.FindProperty("line").objectReferenceValue = line;
            serialized.FindProperty("finger").objectReferenceValue = finger;
            serialized.FindProperty("area").objectReferenceValue = area.GetComponent<RectTransform>();
            foreach (string phase in new[] { "press", "step", "hold", "clear", "gap", "regrow", "rest" })
                serialized.FindProperty($"timing.{phase}").floatValue = 1f;
            serialized.FindProperty("timing.chainedScale").floatValue = 1.2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            demo.Init(TestViews.ProjectSpores);
            return demo;
        }

        static void SetArray(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void SetInts(SerializedProperty array, int[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).intValue = values[i];
        }

        static LineRenderer Line(ChainDemo demo) => demo.GetComponentInChildren<LineRenderer>();
        static SpriteRenderer Finger(ChainDemo demo) => demo.transform.Find("Finger").GetComponent<SpriteRenderer>();

        [Test]
        public void TheDemoSpores_AreTheGamesOwn_InTheirCatalogLooks()
        {
            ChainDemo demo = Demo(out SporeView[] spores);
            SporeCatalog catalog = TestViews.ProjectSpores;

            Assert.That(spores[0].Renderer.sprite, Is.SameAs(catalog.Types[0].Sprite));
            Assert.That(spores[0].Renderer.color, Is.EqualTo(catalog.Types[0].Color));
            Assert.That(spores[3].Renderer.sprite, Is.SameAs(catalog.Types[3].Sprite));
            Assert.That(spores[7].Renderer.color, Is.EqualTo(catalog.Types[1].Color));
            Assert.That(spores[5].transform.localPosition, Is.EqualTo(new Vector3(5, 1, 0)), "Spores keep the places the scene gave them.");
            Assert.That(spores.All(spore => spore.Renderer.sortingOrder == SporeView.SortingOrder));
            Assert.That(Line(demo).sortingOrder, Is.EqualTo(ChainView.SortingOrder), "The line draws behind the spores, as in a level.");
            Assert.That(Finger(demo).sortingOrder, Is.GreaterThan(SporeView.SortingOrder), "The finger draws over them.");
        }

        [Test]
        public void WhileDragging_TheLineJoinsTheChainedSpores_InTheirColour_AndTheyGrow()
        {
            ChainDemo demo = Demo(out SporeView[] spores);

            demo.ShowAt(2.5f);

            LineRenderer line = Line(demo);
            Assert.That(line.positionCount, Is.EqualTo(2));
            Assert.That(line.GetPosition(0), Is.EqualTo(spores[0].transform.localPosition));
            Assert.That(line.GetPosition(1), Is.EqualTo(spores[1].transform.localPosition));
            Assert.That(line.startColor, Is.EqualTo(spores[0].Renderer.color));
            Assert.That(spores[0].transform.localScale.x, Is.EqualTo(1.2f).Within(1e-5f));
            Assert.That(spores[2].transform.localScale.x, Is.EqualTo(1f), "Not chained yet.");

            SpriteRenderer finger = Finger(demo);
            Assert.That(finger.enabled, Is.True);
            Assert.That(finger.transform.localPosition, Is.EqualTo(Vector3.Lerp(spores[1].transform.localPosition, spores[2].transform.localPosition, 0.5f)));
        }

        [Test]
        public void TheSecondExample_DrawsItsOwnSpores_AndTheFirstAreBackToNormal()
        {
            ChainDemo demo = Demo(out SporeView[] spores);
            demo.ShowAt(2.5f);

            demo.ShowAt(8f + 1 + 1);

            Assert.That(Line(demo).GetPosition(0), Is.EqualTo(spores[3].transform.localPosition));
            Assert.That(Line(demo).startColor, Is.EqualTo(spores[3].Renderer.color));
            Assert.That(spores[0].transform.localScale.x, Is.EqualTo(1f));
            Assert.That(spores[4].transform.localScale.x, Is.EqualTo(1.2f).Within(1e-5f));
        }

        [Test]
        public void ClearingAChain_HidesTheLineAndFinger_AndShrinksItsSpores()
        {
            ChainDemo demo = Demo(out SporeView[] spores);

            demo.ShowAt(4.5f);

            Assert.That(Line(demo).positionCount, Is.EqualTo(0));
            Assert.That(Finger(demo).enabled, Is.False);
            Assert.That(spores[1].transform.localScale.x, Is.EqualTo(0.6f).Within(1e-5f));
            Assert.That(spores[7].transform.localScale.x, Is.EqualTo(1f));
        }

        [Test]
        public void PlayingTheDemo_AllocatesNothing()
        {
            ChainDemo demo = Demo(out _);
            for (float t = 0; t < 17f; t += 0.25f) demo.ShowAt(t);

            Assert.That(() =>
            {
                for (float t = 0; t < 17f; t += 0.25f) demo.ShowAt(t);
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
