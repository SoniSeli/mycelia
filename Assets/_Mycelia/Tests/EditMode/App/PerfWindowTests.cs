using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Mycelia.App.Tests
{
    public class PerfWindowTests
    {
        PerfWindow window;

        [SetUp]
        public void CreateWindow() => window = new PerfWindow(frames: 10);

        static PerfFrame Frame(double frameMs = 16, double mainMs = 2, double renderMs = 3, double gpuMs = 4, long gcBytes = 0,
            long batches = 12, long setPassCalls = 5) =>
            new PerfFrame(frameMs, mainMs, renderMs, gpuMs, gcBytes, batches, setPassCalls);

        bool AddQuiet(int count = 1)
        {
            bool full = false;
            for (int i = 0; i < count; i++) full = window.Add(Frame());
            return full;
        }

        [Test]
        public void AWindow_IsFull_AfterItsFrameCount()
        {
            Assert.That(AddQuiet(9), Is.False);
            Assert.That(AddQuiet(), Is.True);
            Assert.That(window.Count, Is.EqualTo(10));
        }

        [Test]
        public void TheSummary_GivesTheMedianP95AndLongestFrame()
        {
            // 1..10 ms: median 5.5, p95 by nearest rank is the 10th, longest 10.
            for (int i = 1; i <= 10; i++) window.Add(Frame(frameMs: i));

            PerfStat frame = window.Summarise().Frame;

            Assert.That(frame.Median, Is.EqualTo(5.5).Within(1e-9));
            Assert.That(frame.P95, Is.EqualTo(10).Within(1e-9));
            Assert.That(frame.Max, Is.EqualTo(10).Within(1e-9));
        }

        [Test]
        public void TheSummary_GivesMainThreadRenderThreadAndGpuTimes_EachOnItsOwn()
        {
            // Descending main times, so a summary that took the last value or mixed series up would show it.
            for (int i = 0; i < 10; i++) window.Add(Frame(mainMs: 10 - i, renderMs: 20 + i, gpuMs: 100 + 2 * i));

            PerfSummary summary = window.Summarise();

            Assert.That(summary.Main.Median, Is.EqualTo(5.5).Within(1e-9));
            Assert.That(summary.Main.Max, Is.EqualTo(10).Within(1e-9));
            Assert.That(summary.Render.Median, Is.EqualTo(24.5).Within(1e-9));
            Assert.That(summary.Render.Max, Is.EqualTo(29).Within(1e-9));
            Assert.That(summary.Gpu.Median, Is.EqualTo(109).Within(1e-9));
            Assert.That(summary.Gpu.Max, Is.EqualTo(118).Within(1e-9));
        }

        [Test]
        public void TheSummary_GivesTheMostBatches_AndTheMostSetPassCalls()
        {
            AddQuiet(3);
            window.Add(Frame(batches: 31, setPassCalls: 6));
            window.Add(Frame(batches: 20, setPassCalls: 9));
            AddQuiet(5);

            PerfSummary summary = window.Summarise();
            Assert.That(summary.MostBatches, Is.EqualTo(31));
            Assert.That(summary.MostSetPassCalls, Is.EqualTo(9));
        }

        [Test]
        public void TheSummary_CountsAllocatedBytes_AndTheFramesThatAllocated()
        {
            AddQuiet(); // The window's first sample; see the next test.
            window.Add(Frame(gcBytes: 40));
            AddQuiet(3);
            window.Add(Frame(gcBytes: 100));

            PerfSummary summary = window.Summarise();

            Assert.That(summary.GcBytes, Is.EqualTo(140));
            Assert.That(summary.FramesThatAllocated, Is.EqualTo(2));
        }

        [Test]
        public void TheFirstSampleOfAWindow_IsNotCountedForGc_BecauseItHoldsThePreviousReport()
        {
            // Counters report the frame before; that frame logged the last window's summary.
            window.Add(Frame(gcBytes: 900));
            AddQuiet(9);

            Assert.That(window.Summarise().GcBytes, Is.Zero);
        }

        [Test]
        public void Clearing_StartsANewWindow()
        {
            window.Add(Frame(frameMs: 40, mainMs: 30, gcBytes: 0, batches: 99, setPassCalls: 50));
            window.Add(Frame(gcBytes: 500));
            AddQuiet(8);

            window.Clear();
            AddQuiet(10);

            PerfSummary summary = window.Summarise();
            Assert.That(window.Count, Is.EqualTo(10));
            Assert.That(summary.Frame.Max, Is.EqualTo(16).Within(1e-9));
            Assert.That(summary.Main.Max, Is.EqualTo(2).Within(1e-9));
            Assert.That(summary.MostBatches, Is.EqualTo(12));
            Assert.That(summary.MostSetPassCalls, Is.EqualTo(5));
            Assert.That(summary.GcBytes, Is.Zero);
            Assert.That(summary.FramesThatAllocated, Is.Zero);
        }

        [Test]
        public void TheReport_IsOneLine_WithEveryFigure()
        {
            window.Add(Frame(16, 2, 3, 4, 0, 12, 4));
            window.Add(Frame(18, 3, 5, 6, 64, 14, 7));

            string report = window.Summarise().ToString();

            Assert.That(report, Does.Not.Contain("\n"));
            Assert.That(report, Does.StartWith("[Perf] frames=2"));
            Assert.That(report, Does.Contain("frame ms median=17.0 p95=18.0 max=18.0"));
            Assert.That(report, Does.Contain("main ms median=2.5 max=3.0"));
            Assert.That(report, Does.Contain("render ms median=4.0 max=5.0"));
            Assert.That(report, Does.Contain("gpu ms median=5.0 max=6.0"));
            Assert.That(report, Does.Contain("gc B=64 in 1 frames"));
            Assert.That(report, Does.Contain("batches max=14"));
            Assert.That(report, Does.Contain("setpass max=7"));
        }

        [Test]
        public void AddingAFrame_AllocatesNothing()
        {
            Assert.That(() => { window.Add(Frame()); }, Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Summarising_AllocatesNothing()
        {
            AddQuiet(10);
            window.Summarise();

            Assert.That(() => { window.Summarise(); }, Is.Not.AllocatingGCMemory());
        }
    }
}
