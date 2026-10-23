using Unity.Profiling;
using UnityEngine;

namespace Mycelia.App
{
    /// <summary>
    /// Logs a <see cref="PerfSummary"/> line to the device log every few seconds in development builds (performance
    /// budget). Times come from the FrameTimingManager (Frame Timing Stats in Player Settings), which separates
    /// main-thread work from waiting for the next frame; GC and batches from ProfilerRecorders. Read it with
    /// <c>adb logcat -s Unity | findstr [Perf]</c>. Removes itself from release builds and from the Editor.
    /// </summary>
    public sealed class PerfProbe : MonoBehaviour
    {
        [SerializeField, Min(10), Tooltip("Frames per logged summary (300 is 5 s at 60 fps).")]
        int windowFrames = 300;

        readonly FrameTiming[] timings = new FrameTiming[1];
        PerfWindow window;
        ProfilerRecorder gcAllocated;
        ProfilerRecorder batches;
        ProfilerRecorder setPassCalls;

        void Awake()
        {
            if (!Debug.isDebugBuild || Application.isEditor)
            {
                Destroy(this);
                return;
            }

            window = new PerfWindow(windowFrames);
        }

        void OnEnable()
        {
            if (window == null)
                return;

            gcAllocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            setPassCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        }

        void OnDisable()
        {
            gcAllocated.Dispose();
            batches.Dispose();
            setPassCalls.Dispose();
        }

        void Update()
        {
            if (window == null)
                return;

            // Timings arrive a few frames late; each window still sees one per frame.
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, timings) < 1)
                return;

            FrameTiming timing = timings[0];
            var frame = new PerfFrame(
                frameMs: timing.cpuFrameTime,
                // Already without the wait for Present: on the Redmi, main + present wait adds up to the frame.
                mainMs: timing.cpuMainThreadFrameTime,
                renderMs: timing.cpuRenderThreadFrameTime,
                gpuMs: timing.gpuFrameTime,
                gcBytes: gcAllocated.LastValue,
                batches: batches.LastValue,
                setPassCalls: setPassCalls.LastValue);
            if (!window.Add(frame))
                return;

            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, this, "{0}", window.Summarise().ToString());
            window.Clear();
        }
    }
}
