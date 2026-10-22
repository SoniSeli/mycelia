using System;
using System.Collections.Generic;
using System.Threading;
using Mycelia.Domain.Sessions;

namespace Mycelia.Domain.Moves
{
    /// <summary>The fixed order a move resolves in (ADR-0004). Phases also group steps for playback.</summary>
    public enum MovePhase
    {
        Clear,
        Trigger,
        Damage,
        Gravity,
        Refill,
        Hazards,
        Settle,
        Evaluate
    }

    /// <summary>Points at one recorded step: which step type, and where it is in that type's buffer.</summary>
    public readonly struct StepRef
    {
        public readonly int TypeId;
        public readonly int Index;

        public StepRef(int typeId, int index)
        {
            TypeId = typeId;
            Index = index;
        }
    }

    /// <summary>The steps one phase recorded: <see cref="Count"/> step refs starting at <see cref="Start"/>.</summary>
    public readonly struct PhaseSteps
    {
        public readonly MovePhase Phase;
        public readonly int Start;
        public readonly int Count;

        public PhaseSteps(MovePhase phase, int start, int count)
        {
            Phase = phase;
            Start = start;
            Count = count;
        }
    }

    /// <summary>
    /// Everything one move did, as ordered phases of typed steps, for the view to play back (ADR-0003, ADR-0004).
    /// One instance is reused for every move, so a result is only valid until the next commit.
    /// Steps live in one reusable buffer per step type, so recording a move does not allocate after warm-up.
    /// </summary>
    public sealed class MoveResult
    {
        readonly int bufferCapacity;
        readonly List<StepRef> steps;
        readonly List<PhaseSteps> phases = new List<PhaseSteps>(8);
        IStepBuffer[] buffers = new IStepBuffer[8];
        MovePhase currentPhase;
        int phaseStart;

        public MoveResult(int bufferCapacity)
        {
            this.bufferCapacity = bufferCapacity;
            steps = new List<StepRef>(bufferCapacity * 4 + 16);

            // Built-in step types get their buffers up front; new mechanics' steps get theirs on first use.
            Buffer<SporeCleared>();
            Buffer<SporeFell>();
            Buffer<SporeSpawned>();
            Buffer<SporeRelocated>();
            Buffer<ObjectiveProgressed>();
            Buffer<SessionEnded>();
        }

        /// <summary>Phases that recorded at least one step, in resolution order.</summary>
        public int PhaseCount => phases.Count;
        public PhaseSteps GetPhase(int phaseIndex) => phases[phaseIndex];

        /// <summary>A step by its position across the whole move; a phase's steps are [Start, Start + Count).</summary>
        public StepRef GetStep(int stepIndex) => steps[stepIndex];

        public int MovesRemaining { get; internal set; }
        public SessionState Outcome { get; internal set; }

        /// <summary>1–3 when the move won the level, otherwise 0.</summary>
        public int Stars { get; internal set; }

        public bool Is<TStep>(StepRef step) where TStep : struct, IMoveStep => step.TypeId == StepType<TStep>.Id;

        public bool TryGet<TStep>(StepRef step, out TStep value) where TStep : struct, IMoveStep
        {
            if (!Is<TStep>(step))
            {
                value = default;
                return false;
            }

            value = Buffer<TStep>().Get(step.Index);
            return true;
        }

        internal void Reset()
        {
            steps.Clear();
            phases.Clear();
            for (int i = 0; i < buffers.Length; i++)
            {
                buffers[i]?.Clear();
            }
            MovesRemaining = 0;
            Outcome = SessionState.Playing;
            Stars = 0;
        }

        internal void BeginPhase(MovePhase phase)
        {
            currentPhase = phase;
            phaseStart = steps.Count;
        }

        internal void EndPhase()
        {
            int count = steps.Count - phaseStart;
            if (count > 0)
                phases.Add(new PhaseSteps(currentPhase, phaseStart, count));
        }

        internal void Record<TStep>(in TStep step) where TStep : struct, IMoveStep
        {
            int index = Buffer<TStep>().Add(step);
            steps.Add(new StepRef(StepType<TStep>.Id, index));
        }

        StepBuffer<TStep> Buffer<TStep>() where TStep : struct, IMoveStep
        {
            int id = StepType<TStep>.Id;
            if (id >= buffers.Length)
                Array.Resize(ref buffers, Math.Max(id + 1, buffers.Length * 2));
            if (buffers[id] == null)
                buffers[id] = new StepBuffer<TStep>(bufferCapacity);
            return (StepBuffer<TStep>)buffers[id];
        }

        interface IStepBuffer
        {
            void Clear();
        }

        sealed class StepBuffer<TStep> : IStepBuffer where TStep : struct
        {
            TStep[] items;
            int count;

            public StepBuffer(int capacity) => items = new TStep[Math.Max(capacity, 4)];

            public int Add(in TStep step)
            {
                if (count == items.Length)
                    Array.Resize(ref items, items.Length * 2);
                items[count] = step;
                return count++;
            }

            public TStep Get(int index) => items[index];

            public void Clear() => count = 0;
        }

        /// <summary>A small, stable integer per step type, assigned on first use.</summary>
        static class StepType<TStep> where TStep : struct, IMoveStep
        {
            public static readonly int Id = StepTypeIds.Next();
        }

        static class StepTypeIds
        {
            static int last = -1;
            public static int Next() => Interlocked.Increment(ref last);
        }
    }
}
