using System;
using System.Globalization;

namespace Mycelia.App
{
    /// <summary>One frame's figures: times in milliseconds, GC allocated in the frame, batches and SetPass calls.</summary>
    public readonly struct PerfFrame
    {
        public PerfFrame(double frameMs, double mainMs, double renderMs, double gpuMs, long gcBytes, long batches, long setPassCalls)
        {
            FrameMs = frameMs;
            MainMs = mainMs;
            RenderMs = renderMs;
            GpuMs = gpuMs;
            GcBytes = gcBytes;
            Batches = batches;
            SetPassCalls = setPassCalls;
        }

        public double FrameMs { get; }
        /// <summary>Main-thread work, without waiting for the next frame.</summary>
        public double MainMs { get; }
        public double RenderMs { get; }
        public double GpuMs { get; }
        public long GcBytes { get; }
        public long Batches { get; }
        /// <summary>Shader pass changes: what the SRP Batcher keeps low even when every sprite is its own batch.</summary>
        public long SetPassCalls { get; }
    }

    /// <summary>
    /// Collects frames over a fixed window and summarises them against the performance budget: frame, main-thread,
    /// render-thread and GPU times, GC bytes, and the most batches and SetPass calls. Allocation-free; only
    /// <see cref="PerfSummary.ToString"/> allocates, once per window.
    /// </summary>
    public sealed class PerfWindow
    {
        readonly Series frame;
        readonly Series main;
        readonly Series render;
        readonly Series gpu;
        long gcBytes;
        int framesThatAllocated;
        long mostBatches;
        long mostSetPassCalls;

        public PerfWindow(int frames)
        {
            if (frames < 1) throw new ArgumentOutOfRangeException(nameof(frames), frames, "A window needs at least one frame.");
            frame = new Series(frames);
            main = new Series(frames);
            render = new Series(frames);
            gpu = new Series(frames);
        }

        public int Count { get; private set; }

        /// <returns>True once the window is full: summarise it, then clear it.</returns>
        public bool Add(PerfFrame sample)
        {
            if (Count == frame.Capacity)
                throw new InvalidOperationException("The window is full; clear it first.");

            // Counters report the previous frame, and the frame before a window logged the last window's summary.
            if (Count > 0 && sample.GcBytes > 0)
            {
                gcBytes += sample.GcBytes;
                framesThatAllocated++;
            }

            frame.Add(sample.FrameMs);
            main.Add(sample.MainMs);
            render.Add(sample.RenderMs);
            gpu.Add(sample.GpuMs);
            mostBatches = Math.Max(mostBatches, sample.Batches);
            mostSetPassCalls = Math.Max(mostSetPassCalls, sample.SetPassCalls);
            return ++Count == frame.Capacity;
        }

        public PerfSummary Summarise() =>
            new PerfSummary(Count, frame.Stat(), main.Stat(), render.Stat(), gpu.Stat(), gcBytes, framesThatAllocated,
                mostBatches, mostSetPassCalls);

        public void Clear()
        {
            Count = 0;
            frame.Clear();
            main.Clear();
            render.Clear();
            gpu.Clear();
            gcBytes = 0;
            framesThatAllocated = 0;
            mostBatches = 0;
            mostSetPassCalls = 0;
        }

        sealed class Series
        {
            readonly double[] values;
            readonly double[] sorted;
            int count;

            public Series(int capacity)
            {
                values = new double[capacity];
                sorted = new double[capacity];
            }

            public int Capacity => values.Length;

            public void Add(double value) => values[count++] = value;

            public void Clear() => count = 0;

            public PerfStat Stat()
            {
                if (count == 0)
                    return default;

                // Insertion sort: a few hundred values once per window, and unlike Array.Sort on Mono it never allocates.
                for (int i = 0; i < count; i++)
                {
                    double value = values[i];
                    int j = i - 1;
                    for (; j >= 0 && sorted[j] > value; j--)
                        sorted[j + 1] = sorted[j];
                    sorted[j + 1] = value;
                }

                double median = count % 2 == 1 ? sorted[count / 2] : (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
                double p95 = sorted[Math.Max(0, (int)Math.Ceiling(0.95 * count) - 1)];
                return new PerfStat(median, p95, sorted[count - 1]);
            }
        }
    }

    /// <summary>Median, 95th percentile (nearest rank) and maximum of one figure over a window, in milliseconds.</summary>
    public readonly struct PerfStat
    {
        public PerfStat(double median, double p95, double max)
        {
            Median = median;
            P95 = p95;
            Max = max;
        }

        public double Median { get; }
        public double P95 { get; }
        public double Max { get; }
    }

    public readonly struct PerfSummary
    {
        public PerfSummary(int frames, PerfStat frame, PerfStat main, PerfStat render, PerfStat gpu,
            long gcBytes, int framesThatAllocated, long mostBatches, long mostSetPassCalls)
        {
            MostSetPassCalls = mostSetPassCalls;
            Frames = frames;
            Frame = frame;
            Main = main;
            Render = render;
            Gpu = gpu;
            GcBytes = gcBytes;
            FramesThatAllocated = framesThatAllocated;
            MostBatches = mostBatches;
        }

        public int Frames { get; }
        public PerfStat Frame { get; }
        public PerfStat Main { get; }
        public PerfStat Render { get; }
        public PerfStat Gpu { get; }
        public long GcBytes { get; }
        public int FramesThatAllocated { get; }
        public long MostBatches { get; }
        public long MostSetPassCalls { get; }

        public override string ToString() => string.Format(CultureInfo.InvariantCulture,
            "[Perf] frames={0} | frame ms median={1:F1} p95={2:F1} max={3:F1} | main ms median={4:F1} max={5:F1} | " +
            "render ms median={6:F1} max={7:F1} | gpu ms median={8:F1} max={9:F1} | gc B={10} in {11} frames | " +
            "batches max={12} | setpass max={13}",
            Frames, Frame.Median, Frame.P95, Frame.Max, Main.Median, Main.Max, Render.Median, Render.Max,
            Gpu.Median, Gpu.Max, GcBytes, FramesThatAllocated, MostBatches, MostSetPassCalls);
    }
}
