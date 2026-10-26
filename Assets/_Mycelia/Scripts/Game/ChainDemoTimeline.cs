using System;
using UnityEngine;

namespace Mycelia.Game
{
    /// <summary>How long each part of one chain demo lasts, in seconds, and how large chained spores are drawn.</summary>
    [Serializable]
    public struct ChainDemoTiming
    {
        [SerializeField, Tooltip("The finger rests on the first spore.")] float press;
        [SerializeField, Tooltip("The finger glides to the next spore.")] float step;
        [SerializeField, Tooltip("The whole chain shows before release.")] float hold;
        [SerializeField, Tooltip("The released chain shrinks away.")] float clear;
        [SerializeField, Tooltip("The cleared spores stay gone.")] float gap;
        [SerializeField, Tooltip("They grow back.")] float regrow;
        [SerializeField, Tooltip("Nothing happens before the next example.")] float rest;
        [SerializeField, Tooltip("Scale of a chained spore, as on the board (ChainView).")] float chainedScale;

        public ChainDemoTiming(float press, float step, float hold, float clear, float gap, float regrow, float rest, float chainedScale)
        {
            this.press = press;
            this.step = step;
            this.hold = hold;
            this.clear = clear;
            this.gap = gap;
            this.regrow = regrow;
            this.rest = rest;
            this.chainedScale = chainedScale;
        }

        public static ChainDemoTiming Default => new ChainDemoTiming(press: 0.35f, step: 0.4f, hold: 0.6f, clear: 0.25f, gap: 0.35f, regrow: 0.3f, rest: 0.5f, chainedScale: 1.15f);

        public float Press => press;
        public float Step => step;
        public float Hold => hold;
        public float Clear => clear;
        public float Gap => gap;
        public float Regrow => regrow;
        public float Rest => rest;
        public float ChainedScale => chainedScale;

        /// <summary>Seconds one example of <paramref name="length"/> spores takes, from press to the end of its rest.</summary>
        public float DurationOf(int length) => press + (length - 1) * step + hold + clear + gap + regrow + rest;
    }

    /// <summary>One moment of the chain demo.</summary>
    public readonly struct ChainDemoFrame
    {
        public ChainDemoFrame(int example, int links, bool lineShown, bool fingerShown, int fingerFrom, float fingerLerp, float chainScale)
        {
            Example = example;
            Links = links;
            LineShown = lineShown;
            FingerShown = fingerShown;
            FingerFrom = fingerFrom;
            FingerLerp = fingerLerp;
            ChainScale = chainScale;
        }

        /// <summary>Which example plays (0-based).</summary>
        public int Example { get; }

        /// <summary>How many of the example's chain spores are chained (and drawn at <see cref="ChainScale"/>).</summary>
        public int Links { get; }

        public bool LineShown { get; }

        public bool FingerShown { get; }

        /// <summary>The chain spore the finger last reached; it glides on toward the next by <see cref="FingerLerp"/>.</summary>
        public int FingerFrom { get; }

        public float FingerLerp { get; }

        public float ChainScale { get; }
    }

    /// <summary>
    /// The chain demo on "How to Grow" (design §8.3 slice 8), as a function of time: in each example the finger presses
    /// the first spore, glides from spore to spore (each joins the chain as it arrives), holds, and lets go; the chain
    /// shrinks away and grows back, and the next example plays. The demo loops. Pure, so it is tested without a scene.
    /// </summary>
    public static class ChainDemoTimeline
    {
        public static ChainDemoFrame At(float seconds, int[] lengths, in ChainDemoTiming timing)
        {
            float total = 0f;
            foreach (int length in lengths)
                total += timing.DurationOf(length);

            float t = Mathf.Repeat(seconds, total);
            int example = 0;
            while (example < lengths.Length - 1 && t >= timing.DurationOf(lengths[example]))
                t -= timing.DurationOf(lengths[example++]);

            int n = lengths[example];
            float chained = timing.ChainedScale;
            if (t < timing.Press)
                return new ChainDemoFrame(example, 1, true, true, 0, 0f, chained);

            t -= timing.Press;
            float drag = (n - 1) * timing.Step;
            if (t < drag)
            {
                int from = Mathf.Min((int)(t / timing.Step), n - 2);
                float along = t / timing.Step - from;
                return new ChainDemoFrame(example, from + 1, true, true, from, Ease(along), chained);
            }

            t -= drag;
            if (t < timing.Hold)
                return new ChainDemoFrame(example, n, true, true, n - 1, 0f, chained);

            t -= timing.Hold;
            if (t < timing.Clear)
                return new ChainDemoFrame(example, n, false, false, n - 1, 0f, chained * (1f - t / timing.Clear));

            t -= timing.Clear;
            if (t < timing.Gap)
                return new ChainDemoFrame(example, n, false, false, n - 1, 0f, 0f);

            t -= timing.Gap;
            if (t < timing.Regrow)
                return new ChainDemoFrame(example, n, false, false, n - 1, 0f, t / timing.Regrow);

            return new ChainDemoFrame(example, 0, false, false, 0, 0f, 1f);
        }

        // Smoothstep: a drag starts and ends gently, like a finger.
        static float Ease(float x) => x * x * (3f - 2f * x);
    }
}
