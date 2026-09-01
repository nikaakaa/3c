using System;
using System.Collections;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class FixedCapacityFrameBuffer<T> : IReadOnlyList<T>
    {
        readonly T[] m_Items;

        internal FixedCapacityFrameBuffer(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Items = capacity == 0
                ? Array.Empty<T>()
                : new T[capacity];
        }

        public int Count { get; private set; }
        internal int Capacity => m_Items.Length;
        public T this[int index] =>
            (uint)index < (uint)Count
                ? m_Items[index]
                : throw new ArgumentOutOfRangeException(nameof(index));

        internal void Add(in T item)
        {
            if (Count == m_Items.Length)
            {
                throw new InvalidOperationException(
                    $"Fixed frame buffer capacity '{m_Items.Length}' was exceeded.");
            }
            m_Items[Count++] = item;
        }

        internal void Clear()
        {
            if (RuntimeHelpers<T>.ContainsReferences)
                Array.Clear(m_Items, 0, Count);
            Count = 0;
        }

        internal void Sort(Comparison<T> comparison)
        {
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));
            for (int i = 1; i < Count; i++)
            {
                T value = m_Items[i];
                int index = i - 1;
                while (index >= 0 &&
                       comparison(m_Items[index], value) > 0)
                {
                    m_Items[index + 1] = m_Items[index];
                    index--;
                }
                m_Items[index + 1] = value;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal struct Enumerator : IEnumerator<T>
        {
            readonly FixedCapacityFrameBuffer<T> m_Owner;
            int m_Index;

            internal Enumerator(FixedCapacityFrameBuffer<T> owner)
            {
                m_Owner = owner;
                m_Index = -1;
            }

            public T Current => m_Owner[m_Index];
            object IEnumerator.Current => Current;
            public bool MoveNext() => ++m_Index < m_Owner.Count;
            public void Reset() => m_Index = -1;
            public void Dispose()
            {
            }
        }

        static class RuntimeHelpers<TValue>
        {
            internal static readonly bool ContainsReferences =
                System.Runtime.CompilerServices.RuntimeHelpers
                    .IsReferenceOrContainsReferences<TValue>();
        }
    }
}

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterPoseFrameTransaction
    {
        internal CharacterPoseFrameTransaction(
            int playbackCapacity,
            int backendReleaseCompletionCapacity)
        {
            if (playbackCapacity <= 0 ||
                backendReleaseCompletionCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playbackCapacity));
            }
            ConsumedReleaseCompletions =
                new FixedCapacityFrameBuffer<ActionBackendReleaseCompletion>(
                    backendReleaseCompletionCapacity);
            ActionSnapshots =
                new FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleSnapshot>(
                    playbackCapacity);
            TimeSnapshots =
                new FixedCapacityFrameBuffer<ActionPresentationTimeSnapshot>(
                    playbackCapacity);
            RetiredPlaybacks =
                new FixedCapacityFrameBuffer<AnimationPlaybackId>(
                    playbackCapacity);
            Closed = true;
        }

        internal CharacterPoseFrameLineage Lineage { get; private set; }
        internal PresentationFrameWorkspaceLease WorkspaceLease
        {
            get;
            private set;
        }
        internal CharacterActionPlaybackFrameTransaction ActionTransaction
        {
            get;
            private set;
        }
        internal ActionPresentationSamplingFrameTransaction
            SamplingTransaction { get; private set; }
        internal AnimationSlotMutationLease SlotLease { get; private set; }
        internal CharacterPoseProgramFrameLease PoseLease { get; private set; }
        internal CharacterPoseSourceFrameLease SourceLease { get; private set; }
        internal CharacterPoseConstraintFrameLease ConstraintLease { get; private set; }
        internal CharacterFinalPosePublicationFrameLease PublicationLease
        {
            get;
            private set;
        }
        internal CharacterPoseSourceDemand SourceDemand { get; private set; }
        internal CharacterPoseSourceFrameResult SourceFrame { get; private set; }
        internal bool HasSourceFrame { get; private set; }
        internal CharacterPoseProgramResult ProgramResult { get; private set; }
        internal CharacterPoseConstraintResult ConstraintResult
        {
            get;
            private set;
        }
        internal CharacterFinalPosePublicationResult PublicationResult
        {
            get;
            private set;
        }
        internal bool HasExecutionResults { get; private set; }
        internal MotionMatchingFrameMutationLease MotionMatchingLease
        {
            get;
            private set;
        }
        internal bool HasMotionMatchingLease { get; private set; }
        internal FixedCapacityFrameBuffer<ActionBackendReleaseCompletion>
            ConsumedReleaseCompletions { get; }
        internal FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleSnapshot>
            ActionSnapshots { get; }
        internal FixedCapacityFrameBuffer<ActionPresentationTimeSnapshot>
            TimeSnapshots { get; }
        internal FixedCapacityFrameBuffer<AnimationPlaybackId>
            RetiredPlaybacks { get; }
        internal bool Closed { get; private set; }
        internal AnimationPresentationFramePhase Phase { get; private set; }
        internal AnimationPresentationFrameOutcome Outcome { get; private set; }
        internal bool IsValid =>
            Lineage.IsValid &&
            WorkspaceLease.IsValid &&
            WorkspaceLease.Identity == Lineage.FrameIdentity &&
            WorkspaceLease.PresentationFrame == Lineage.PresentationFrame &&
            ActionTransaction?.IsValid == true &&
            ActionTransaction.Identity == Lineage.FrameIdentity &&
            ActionTransaction.PresentationFrame == Lineage.PresentationFrame &&
            SamplingTransaction?.IsValid == true &&
            SamplingTransaction.Identity == Lineage.FrameIdentity &&
            SamplingTransaction.PresentationFrame == Lineage.PresentationFrame &&
            SlotLease.IsValid &&
            SlotLease.FrameIdentity == Lineage.FrameIdentity &&
            PoseLease.IsValid &&
            PoseLease.Matches(Lineage) &&
            SourceLease.IsValid &&
            SourceLease.Matches(Lineage) &&
            ConstraintLease.IsValid &&
            ConstraintLease.Matches(Lineage) &&
            PublicationLease.IsValid &&
            PublicationLease.Matches(Lineage) &&
            HasSourceFrame &&
            SourceDemand.IsValid &&
            SourceFrame.IsReady &&
            SourceDemand.Lineage == Lineage &&
            SourceFrame.Lineage == Lineage &&
            HasExecutionResults &&
            ProgramResult.IsCompleted &&
            ConstraintResult.IsCompleted &&
            PublicationResult.IsPublished &&
            ProgramResult.Lineage == Lineage &&
            ConstraintResult.Lineage == Lineage &&
            PublicationResult.Lineage == Lineage &&
            HasMotionMatchingLease == MotionMatchingLease.IsValid &&
            (!HasMotionMatchingLease ||
             MotionMatchingLease.FrameIdentity == Lineage.FrameIdentity) &&
            !Closed;

        internal void Begin(
            in CharacterPoseFrameLineage lineage,
            PresentationFrameWorkspaceLease workspaceLease,
            CharacterActionPlaybackFrameTransaction actionTransaction,
            ActionPresentationSamplingFrameTransaction samplingTransaction,
            AnimationSlotMutationLease slotLease,
            CharacterPoseProgramFrameLease poseLease,
            CharacterPoseSourceFrameLease sourceLease,
            CharacterPoseConstraintFrameLease constraintLease,
            CharacterFinalPosePublicationFrameLease publicationLease,
            MotionMatchingFrameMutationLease motionMatchingLease,
            bool hasMotionMatchingLease)
        {
            if (!Closed ||
                !lineage.IsOpenValid ||
                !workspaceLease.IsValid ||
                workspaceLease.Identity != lineage.FrameIdentity ||
                workspaceLease.PresentationFrame != lineage.PresentationFrame ||
                actionTransaction == null ||
                !actionTransaction.IsValid ||
                actionTransaction.Identity != lineage.FrameIdentity ||
                actionTransaction.PresentationFrame != lineage.PresentationFrame ||
                samplingTransaction == null ||
                !samplingTransaction.IsValid ||
                samplingTransaction.Identity != lineage.FrameIdentity ||
                samplingTransaction.PresentationFrame != lineage.PresentationFrame ||
                !slotLease.IsValid ||
                slotLease.FrameIdentity != lineage.FrameIdentity ||
                !poseLease.IsValid ||
                !poseLease.Matches(lineage) ||
                !sourceLease.IsValid ||
                !sourceLease.Matches(lineage) ||
                !constraintLease.IsValid ||
                !constraintLease.Matches(lineage) ||
                !publicationLease.IsValid ||
                !publicationLease.Matches(lineage) ||
                hasMotionMatchingLease != motionMatchingLease.IsValid ||
                hasMotionMatchingLease &&
                motionMatchingLease.FrameIdentity != lineage.FrameIdentity)
            {
                throw new ArgumentException(
                    "Animation Presentation frame transaction is invalid.");
            }
            ClearBatches();
            Lineage = lineage;
            WorkspaceLease = workspaceLease;
            ActionTransaction = actionTransaction;
            SamplingTransaction = samplingTransaction;
            SlotLease = slotLease;
            PoseLease = poseLease;
            SourceLease = sourceLease;
            ConstraintLease = constraintLease;
            PublicationLease = publicationLease;
            MotionMatchingLease = motionMatchingLease;
            HasMotionMatchingLease = hasMotionMatchingLease;
            SourceDemand = default;
            SourceFrame = default;
            HasSourceFrame = false;
            ProgramResult = default;
            ConstraintResult = default;
            PublicationResult = default;
            HasExecutionResults = false;
            Outcome = AnimationPresentationFrameOutcome.None;
            Phase = AnimationPresentationFramePhase.Begin;
            Closed = false;
        }

        internal void BindSourceResults(
            in CharacterPoseSourceDemand demand,
            in CharacterPoseSourceFrameResult sourceFrame)
        {
            RequirePhase(AnimationPresentationFramePhase.Prepare);
            if (Lineage.CompletionIdentity != 0 ||
                HasSourceFrame ||
                !demand.IsValid ||
                !sourceFrame.IsReady ||
                !SourceLease.Matches(demand.Lineage) ||
                demand.Lineage != sourceFrame.Lineage ||
                Lineage.WithCompletion(
                    demand.Lineage.CompletionIdentity) != demand.Lineage)
            {
                throw new InvalidOperationException(
                    "Character Pose source results are invalid.");
            }
            Lineage = demand.Lineage;
            SourceDemand = demand;
            SourceFrame = sourceFrame;
            HasSourceFrame = true;
        }

        internal void BindExecutionResults(
            in CharacterPoseFrameExecutionResult results)
        {
            RequirePhase(AnimationPresentationFramePhase.EvaluateBarrier);
            if (HasExecutionResults ||
                !results.IsValid ||
                results.Lineage != Lineage)
            {
                throw new InvalidOperationException(
                    "Character Pose frame execution results are invalid.");
            }
            ProgramResult = results.Program;
            ConstraintResult = results.Constraint;
            PublicationResult = results.Publication;
            HasExecutionResults = true;
        }

        internal void BeginPrepare()
        {
            RequirePhase(AnimationPresentationFramePhase.Begin);
            Phase = AnimationPresentationFramePhase.Prepare;
        }

        internal void MarkValidated()
        {
            RequirePhase(AnimationPresentationFramePhase.Prepare);
            Phase = AnimationPresentationFramePhase.Validated;
        }

        internal void EnterEvaluateBarrier()
        {
            RequirePhase(AnimationPresentationFramePhase.Validated);
            Phase = AnimationPresentationFramePhase.EvaluateBarrier;
        }

        internal void MarkDiscarded()
        {
            if (Phase >= AnimationPresentationFramePhase.EvaluateBarrier ||
                Phase == AnimationPresentationFramePhase.Discarded)
            {
                throw new InvalidOperationException(
                    "Animation Presentation frame cannot be discarded from the current phase.");
            }
            Outcome = AnimationPresentationFrameOutcome.None;
            Phase = AnimationPresentationFramePhase.Discarded;
            Closed = true;
        }

        internal void MarkSealed()
        {
            if (Phase != AnimationPresentationFramePhase.EvaluateBarrier ||
                !HasExecutionResults ||
                !ProgramResult.IsCompleted ||
                !ConstraintResult.IsCompleted ||
                !PublicationResult.IsPublished)
            {
                throw new InvalidOperationException(
                    "Animation Presentation frame cannot seal before Evaluate barrier.");
            }
            Outcome = AnimationPresentationFrameOutcome.Committed;
            Phase = AnimationPresentationFramePhase.Sealed;
            Closed = true;
        }

        internal void MarkFaulted()
        {
            if (Phase < AnimationPresentationFramePhase.EvaluateBarrier)
            {
                throw new InvalidOperationException(
                    "Animation Presentation frame cannot fault before Evaluate barrier.");
            }
            Outcome = AnimationPresentationFrameOutcome.Faulted;
            Phase = AnimationPresentationFramePhase.Faulted;
            Closed = true;
        }

        internal void ResetAfterPublish()
        {
            if (!Closed)
            {
                throw new InvalidOperationException(
                    "Animation Presentation frame cannot reset before close.");
            }
            ClearBatches();
            Lineage = default;
            WorkspaceLease = default;
            ActionTransaction = null;
            SamplingTransaction = null;
            SlotLease = default;
            PoseLease = default;
            SourceLease = default;
            ConstraintLease = default;
            PublicationLease = default;
            SourceDemand = default;
            SourceFrame = default;
            HasSourceFrame = false;
            ProgramResult = default;
            ConstraintResult = default;
            PublicationResult = default;
            HasExecutionResults = false;
            MotionMatchingLease = default;
            HasMotionMatchingLease = false;
            Outcome = AnimationPresentationFrameOutcome.None;
            Phase = default;
        }

        void RequirePhase(AnimationPresentationFramePhase phase)
        {
            if (Phase != phase || Closed)
            {
                throw new InvalidOperationException(
                    $"Animation Presentation frame phase must be '{phase}'.");
            }
        }

        void ClearBatches()
        {
            ConsumedReleaseCompletions.Clear();
            ActionSnapshots.Clear();
            TimeSnapshots.Clear();
            RetiredPlaybacks.Clear();
        }
    }
}
