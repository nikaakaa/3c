using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class AnimationPlayerReleaseJournal
    {
        readonly AnimationPoseSourceId[] m_Committed;
        readonly AnimationPoseSourceId[] m_PendingAppends;
        int m_CommittedHead;
        int m_CommittedCount;
        int m_PendingCommittedPopCount;
        int m_PendingAppendHead;
        int m_PendingAppendCount;
        int m_PreparedReleaseCount;
        int m_AppliedPreparedReleaseCount;
        bool m_FrameOpen;

        internal AnimationPlayerReleaseJournal(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Committed = new AnimationPoseSourceId[capacity];
            m_PendingAppends = new AnimationPoseSourceId[capacity];
        }

        internal int Count => m_FrameOpen
            ? m_CommittedCount - m_PendingCommittedPopCount + m_PendingAppendCount
            : m_CommittedCount;

        internal void BeginFrame()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Animation Player release journal frame is already open.");
            if (m_PreparedReleaseCount != 0 ||
                m_AppliedPreparedReleaseCount != 0)
            {
                throw new InvalidOperationException(
                    "Animation Player prepared releases were not applied.");
            }
            m_PendingCommittedPopCount = 0;
            m_PendingAppendHead = 0;
            m_PendingAppendCount = 0;
            m_FrameOpen = true;
        }

        internal void CommitFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Animation Player release journal frame is not open.");
            for (int i = 0; i < m_PendingCommittedPopCount; i++)
                PopCommitted();
            for (int i = 0; i < m_PendingAppendCount; i++)
                AppendCommitted(ReadPendingAppend(i));
            ClosePending();
        }

        internal void DiscardFrame()
        {
            if (!m_FrameOpen)
                return;
            ClosePending();
            DiscardPreparedReleases();
        }

        internal void Append(AnimationPoseSourceId sourceId)
        {
            if (!sourceId.IsValid)
                throw new ArgumentException("Animation Player release source is invalid.", nameof(sourceId));
            if (Count >= m_Committed.Length)
                throw new InvalidOperationException("Animation Player release capacity was exceeded.");
            if (!m_FrameOpen)
            {
                AppendCommitted(sourceId);
                return;
            }
            int index = (m_PendingAppendHead + m_PendingAppendCount) % m_PendingAppends.Length;
            m_PendingAppends[index] = sourceId;
            m_PendingAppendCount++;
        }

        internal AnimationPoseSourceId PrepareRelease(int releaseOrdinal)
        {
            if (releaseOrdinal < 0 ||
                releaseOrdinal >= Count ||
                releaseOrdinal != m_PreparedReleaseCount ||
                m_AppliedPreparedReleaseCount != 0)
            {
                throw new InvalidOperationException(
                    "Animation Player release ordinal is not current.");
            }
            AnimationPoseSourceId sourceId = Read(releaseOrdinal);
            m_PreparedReleaseCount++;
            return sourceId;
        }

        internal void CancelPreparedRelease(int releaseOrdinal)
        {
            if (releaseOrdinal != m_PreparedReleaseCount - 1 ||
                m_AppliedPreparedReleaseCount != 0)
            {
                throw new InvalidOperationException(
                    "Animation Player release preparation cannot be cancelled out of order.");
            }
            m_PreparedReleaseCount--;
        }

        internal void ApplyPreparedRelease(int releaseOrdinal)
        {
            PopCommitted();
            m_AppliedPreparedReleaseCount++;
            if (m_AppliedPreparedReleaseCount == m_PreparedReleaseCount)
            {
                m_PreparedReleaseCount = 0;
                m_AppliedPreparedReleaseCount = 0;
            }
        }

        internal void DiscardPreparedReleases()
        {
            m_PreparedReleaseCount = 0;
            m_AppliedPreparedReleaseCount = 0;
        }

        internal void Clear()
        {
            Array.Clear(m_Committed, 0, m_Committed.Length);
            Array.Clear(m_PendingAppends, 0, m_PendingAppends.Length);
            m_CommittedHead = 0;
            m_CommittedCount = 0;
            DiscardPreparedReleases();
            ClosePending();
        }

        AnimationPoseSourceId Read(int index)
        {
            int committedRemaining = m_FrameOpen
                ? m_CommittedCount - m_PendingCommittedPopCount
                : m_CommittedCount;
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (index < committedRemaining)
            {
                int committedOffset = m_FrameOpen
                    ? m_PendingCommittedPopCount + index
                    : index;
                return m_Committed[(m_CommittedHead + committedOffset) % m_Committed.Length];
            }
            return ReadPendingAppend(index - committedRemaining);
        }

        AnimationPoseSourceId ReadPendingAppend(int index) =>
            m_PendingAppends[(m_PendingAppendHead + index) % m_PendingAppends.Length];

        void AppendCommitted(AnimationPoseSourceId sourceId)
        {
            int index = (m_CommittedHead + m_CommittedCount) % m_Committed.Length;
            m_Committed[index] = sourceId;
            m_CommittedCount++;
        }

        void PopCommitted()
        {
            m_Committed[m_CommittedHead] = default;
            m_CommittedHead = (m_CommittedHead + 1) % m_Committed.Length;
            m_CommittedCount--;
        }

        void ClosePending()
        {
            m_PendingCommittedPopCount = 0;
            m_PendingAppendHead = 0;
            m_PendingAppendCount = 0;
            m_FrameOpen = false;
        }
    }

    internal readonly struct AnimationPlayerReleaseToken
    {
        internal AnimationPlayerReleaseToken(
            int releaseOrdinal,
            AnimationPoseSourceId sourceId,
            in AnimationBlendSourcePoseReleaseToken sourcePoseRelease)
        {
            if (releaseOrdinal < 0 ||
                !sourceId.IsValid ||
                !sourcePoseRelease.IsValid ||
                !sourcePoseRelease.SourceId.Equals(sourceId))
            {
                throw new ArgumentException("Animation Player release token is invalid.");
            }
            ReleaseOrdinal = releaseOrdinal;
            SourceId = sourceId;
            SourcePoseRelease = sourcePoseRelease;
        }

        internal int ReleaseOrdinal { get; }
        internal AnimationPoseSourceId SourceId { get; }
        internal AnimationBlendSourcePoseReleaseToken SourcePoseRelease { get; }
        internal bool IsValid =>
            ReleaseOrdinal >= 0 &&
            SourceId.IsValid &&
            SourcePoseRelease.IsValid;
    }

    internal sealed class AnimationClipPlayerRuntime : IDisposable
    {
        struct State
        {
            internal AnimationClipPlayerRuntime PhaseLeader;
            internal AnimationPoseSourceId PhaseLeaderSourceId;
            internal ulong PhaseLeaderContinuityIdentity;
            internal AnimationPhaseSynchronizationState PhaseSynchronization;
            internal double RawContinuousTime;
            internal double ContinuousTime;
            internal double ContinuationAnchorRawTime;
            internal double ContinuationAnchorEffectiveTime;
            internal float SampleTime;
            internal int Cycle;
            internal double MovementClockOriginSeconds;
            internal double MovementClockLastElapsedSeconds;
            internal double MovementClockOffsetSeconds;
            internal string MovementClockOwnerIdentity;
            internal ulong MovementClockGeneration;
            internal ulong NextSourceGeneration;
            internal ulong ContinuityIdentity;
            internal ulong NextContinuityIdentity;
            internal ulong NextEventIdentity;
            internal ulong ResetSequence;
            internal ulong NextResetSequence;
            internal AnimationPoseSourceId SourceId;
            internal ulong PredictionSourceIdentity;
            internal PoseDiscontinuityEndpoint Endpoint;
            internal PoseDiscontinuityResetReason PendingResetReason;
            internal bool Relevant;
            internal bool SourceRetained;
            internal bool HasCompletedFrame;
            internal bool HasMovementClockOrigin;
            internal bool HasContinuationAnchor;
            internal PoseSourceProviderDemandKind DemandKind;
        }

        readonly CharacterPresentationClipPlayerDescriptor m_Descriptor;
        readonly CharacterPresentationPoseSourcePlan m_Source;
        readonly AnimationBlendSourcePoseWorkspace m_SourceWorkspace;
        readonly float[] m_Parameters;
        readonly byte[] m_ParameterAvailability;
        readonly ClipSamplePlan[] m_ClipSamples = new ClipSamplePlan[1];
        readonly AnimationPlayerReleaseJournal m_Releases;
        State m_CommittedState;
        State m_PendingState;
        AnimationPhaseCoverage m_PhaseEntryCoverage;
        bool m_PhaseResolved;
        bool m_FrameOpen;
        bool m_Disposed;

        ref State ActiveState
        {
            get
            {
                if (m_FrameOpen)
                    return ref m_PendingState;
                return ref m_CommittedState;
            }
        }

        double m_ContinuousTime { get => ActiveState.ContinuousTime; set => ActiveState.ContinuousTime = value; }
        double m_RawContinuousTime { get => ActiveState.RawContinuousTime; set => ActiveState.RawContinuousTime = value; }
        double m_ContinuationAnchorRawTime { get => ActiveState.ContinuationAnchorRawTime; set => ActiveState.ContinuationAnchorRawTime = value; }
        double m_ContinuationAnchorEffectiveTime { get => ActiveState.ContinuationAnchorEffectiveTime; set => ActiveState.ContinuationAnchorEffectiveTime = value; }
        float m_SampleTime { get => ActiveState.SampleTime; set => ActiveState.SampleTime = value; }
        int m_Cycle { get => ActiveState.Cycle; set => ActiveState.Cycle = value; }
        double m_MovementClockOriginSeconds { get => ActiveState.MovementClockOriginSeconds; set => ActiveState.MovementClockOriginSeconds = value; }
        double m_MovementClockLastElapsedSeconds { get => ActiveState.MovementClockLastElapsedSeconds; set => ActiveState.MovementClockLastElapsedSeconds = value; }
        double m_MovementClockOffsetSeconds { get => ActiveState.MovementClockOffsetSeconds; set => ActiveState.MovementClockOffsetSeconds = value; }
        string m_MovementClockOwnerIdentity { get => ActiveState.MovementClockOwnerIdentity; set => ActiveState.MovementClockOwnerIdentity = value; }
        ulong m_MovementClockGeneration { get => ActiveState.MovementClockGeneration; set => ActiveState.MovementClockGeneration = value; }
        ulong m_NextSourceGeneration { get => ActiveState.NextSourceGeneration; set => ActiveState.NextSourceGeneration = value; }
        ulong m_ContinuityIdentity { get => ActiveState.ContinuityIdentity; set => ActiveState.ContinuityIdentity = value; }
        ulong m_NextContinuityIdentity { get => ActiveState.NextContinuityIdentity; set => ActiveState.NextContinuityIdentity = value; }
        ulong m_NextEventIdentity { get => ActiveState.NextEventIdentity; set => ActiveState.NextEventIdentity = value; }
        ulong m_ResetSequence { get => ActiveState.ResetSequence; set => ActiveState.ResetSequence = value; }
        ulong m_NextResetSequence { get => ActiveState.NextResetSequence; set => ActiveState.NextResetSequence = value; }
        AnimationPoseSourceId m_SourceId
        {
            get => ActiveState.SourceId;
            set
            {
                ref State state = ref ActiveState;
                state.SourceId = value;
                state.PredictionSourceIdentity = value.IsValid
                    ? AnimationPredictedFootStepSample.SourceIdentity(value)
                    : 0;
            }
        }
        PoseDiscontinuityEndpoint m_Endpoint { get => ActiveState.Endpoint; set => ActiveState.Endpoint = value; }
        PoseDiscontinuityResetReason m_PendingResetReason { get => ActiveState.PendingResetReason; set => ActiveState.PendingResetReason = value; }
        bool m_Relevant { get => ActiveState.Relevant; set => ActiveState.Relevant = value; }
        bool m_SourceRetained { get => ActiveState.SourceRetained; set => ActiveState.SourceRetained = value; }
        bool m_HasCompletedFrame { get => ActiveState.HasCompletedFrame; set => ActiveState.HasCompletedFrame = value; }
        bool m_HasMovementClockOrigin { get => ActiveState.HasMovementClockOrigin; set => ActiveState.HasMovementClockOrigin = value; }
        bool m_HasContinuationAnchor { get => ActiveState.HasContinuationAnchor; set => ActiveState.HasContinuationAnchor = value; }
        PoseSourceProviderDemandKind m_DemandKind { get => ActiveState.DemandKind; set => ActiveState.DemandKind = value; }

        internal AnimationClipPlayerRuntime(
            CharacterPresentationClipPlayerDescriptor descriptor,
            CharacterPresentationPoseSourcePlan source,
            IReadOnlyList<CharacterPoseParameterDeclaration> parameters,
            int footPlacementWeightParameterIndex,
            CharacterAnimationRigPayload rig)
        {
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            NodeId = descriptor.NodeId;
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            if (descriptor.PresentationPoseSourceIndex != source.SourceIndex ||
                !string.Equals(source.RigId, rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(source.RigRevision, rig.RigRevision, StringComparison.Ordinal) ||
                descriptor.InitialTime > source.SourceDurationSeconds ||
                (uint)footPlacementWeightParameterIndex >= (uint)parameters.Count ||
                !parameters[footPlacementWeightParameterIndex].ParameterId.Equals(source.FootPlacementWeightParameterId))
            {
                throw new InvalidOperationException($"Clip Player '{descriptor.NodeId}' source binding does not match its compiled descriptor.");
            }
            m_Parameters = new float[parameters.Count];
            m_ParameterAvailability = new byte[parameters.Count];
            for (int i = 0; i < parameters.Count; i++)
            {
                m_Parameters[i] = parameters[i].DefaultValue;
                m_ParameterAvailability[i] = 1;
            }
            FootPlacementWeightParameterIndex = footPlacementWeightParameterIndex;
            m_SourceWorkspace = new AnimationBlendSourcePoseWorkspace(
                rig,
                parameters.Count,
                AnimationBlendSourcePoseWorkspace.SinglePlayerHandoffCapacity);
            m_Releases = new AnimationPlayerReleaseJournal(
                AnimationBlendSourcePoseWorkspace.SinglePlayerHandoffCapacity);
            m_CommittedState = new State
            {
                NextSourceGeneration = 1,
                ContinuityIdentity = 1,
                NextContinuityIdentity = 2,
                NextEventIdentity = 1,
                ResetSequence = 1,
                NextResetSequence = 2,
                PendingResetReason = PoseDiscontinuityResetReason.Initialization
            };
            m_PendingState = m_CommittedState;
            SetRawClock(descriptor.InitialTime);
        }

        internal PoseNodeId NodeId { get; }
        internal string SyncGroupId => m_Source.SyncGroupId;
        internal AnimationClipPhasePlan PhasePlan => m_Source.PhasePlan;
        internal int PlayerIndex => m_Descriptor.PlayerIndex;
        internal PresentationPoseSourceIndex SourceIndex => m_Source.SourceIndex;
        internal CharacterAnimationSamplingBackendKind Backend => m_Source.Backend;
        internal int ResourceCatalogIndex => m_Source.ResourceCatalogIndex;
        internal int GroupClipIndex => m_Source.GroupClipIndex;
        internal int FootPlacementWeightParameterIndex { get; }
        internal AnimationPoseSourceId SourceId => m_SourceId;
        internal bool IsRelevant => m_Relevant;
        internal bool HasRetainedSource => m_SourceRetained;
        internal bool HasCompletedFrame => m_HasCompletedFrame;
        internal float SampleTime => m_SampleTime;
        internal double ContinuousTime => m_ContinuousTime;
        internal double RawContinuousTime => m_RawContinuousTime;
        internal int Cycle => m_Cycle;
        internal float RemainingTime => Math.Max(0f, m_Source.SourceDurationSeconds - m_SampleTime);
        internal float Duration => m_Source.SourceDurationSeconds;
        internal float PlayRate => m_Descriptor.PlayRate;

        internal void ConfigurePhaseEntry(float start, float end)
        {
            if (PhasePlan == null)
                return;
            if (PhasePlan.Loop != m_Descriptor.LoopAnimation)
                throw new InvalidOperationException($"Clip Player '{NodeId}' loop setting differs from its Phase resource.");
            if (PhasePlan.Loop)
                return;
            m_PhaseEntryCoverage = new AnimationPhaseCoverage(start, end);
            if (!PhasePlan.CurveCoverage.Contains(start) || !PhasePlan.CurveCoverage.Contains(end))
                throw new InvalidOperationException($"Clip Player '{NodeId}' Phase entry interval is outside its measured coverage.");
        }

        internal void SynchronizePhase(AnimationClipPlayerRuntime leader, float remainingBlendSeconds)
        {
            RequireOpenFrame();
            if (m_PhaseResolved)
                return;
            if (leader == null || ReferenceEquals(leader, this) || PhasePlan == null ||
                leader.PhasePlan == null || !string.Equals(SyncGroupId, leader.SyncGroupId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Clip Player '{NodeId}' Phase relation is invalid.");
            bool entering = !ReferenceEquals(m_PendingState.PhaseLeader, leader) ||
                !m_PendingState.PhaseLeaderSourceId.Equals(leader.SourceId) ||
                m_PendingState.PhaseLeaderContinuityIdentity != leader.m_ContinuityIdentity;
            double time = AnimationPhaseSynchronization.Map(
                leader.PhasePlan, leader.PhasePlan.Loop ? leader.ContinuousTime : leader.SampleTime,
                PhasePlan, ContinuousTime,
                m_PhaseEntryCoverage, remainingBlendSeconds, PlayRate,
                entering, ref m_PendingState.PhaseSynchronization);
            SetSynchronizedTime(time);
            m_PendingState.PhaseLeader = leader;
            m_PendingState.PhaseLeaderSourceId = leader.SourceId;
            m_PendingState.PhaseLeaderContinuityIdentity = leader.m_ContinuityIdentity;
            m_PhaseResolved = true;
        }
        internal void CreateFootMotionSamples(
            float sourceWeight,
            out AnimationFootMotionRuntimeSample left,
            out AnimationFootMotionRuntimeSample right) =>
            SampleFootMotion(
                sourceWeight,
                out _,
                out left,
                out right);

        void SampleFootMotion(
            float sourceWeight,
            out float normalizedTime,
            out AnimationFootMotionRuntimeSample left,
            out AnimationFootMotionRuntimeSample right)
        {
            RequireAlive();
            if (!IsRelevant || !HasCompletedFrame || !SourceId.IsValid ||
                !float.IsFinite(sourceWeight) || sourceWeight < 0f || sourceWeight > 1f)
            {
                throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' Foot Step observation is unavailable.");
            }
            normalizedTime = Duration > 0f
                ? Mathf.Clamp01(SampleTime / Duration)
                : 0f;
            left = m_Source.FootStepObservation.Left.Sample(
                normalizedTime,
                Cycle,
                Duration,
                m_Descriptor.LoopAnimation);
            right = m_Source.FootStepObservation.Right.Sample(
                normalizedTime,
                Cycle,
                Duration,
                m_Descriptor.LoopAnimation);
        }

        internal AnimationReadOnlyBuffer<ClipSamplePlan> ClipSamples =>
            new AnimationReadOnlyBuffer<ClipSamplePlan>(m_ClipSamples, 0, 1);

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException($"Clip Player '{NodeId}' frame is already open.");
            m_PendingState = m_CommittedState;
            m_Releases.BeginFrame();
            m_PhaseResolved = false;
            m_FrameOpen = true;
        }

        internal void DiscardFrame()
        {
            RequireAlive();
            if (!m_FrameOpen)
                return;
            DiscardSourceFrame();
            m_SourceWorkspace.DiscardPreparedReleases();
            m_Releases.DiscardFrame();
            m_PendingState = m_CommittedState;
            m_FrameOpen = false;
        }

        internal void CommitFrame()
        {
            RequireAlive();
            if (!m_FrameOpen)
                throw new InvalidOperationException($"Clip Player '{NodeId}' frame is not open.");
            if (!m_PhaseResolved)
                ClearPhaseRelation();
            m_CommittedState = m_PendingState;
            m_Releases.CommitFrame();
            m_FrameOpen = false;
        }

        internal void SetRelevant(
            bool relevant,
            PoseSourceProviderDemandKind demandKind = PoseSourceProviderDemandKind.Active)
        {
            RequireAlive();
            if (relevant &&
                (demandKind < PoseSourceProviderDemandKind.Entry ||
                 demandKind > PoseSourceProviderDemandKind.TransitionSource))
                throw new ArgumentOutOfRangeException(nameof(demandKind));
            if (m_Relevant == relevant)
            {
                if (relevant)
                    m_DemandKind = demandKind;
                return;
            }
            m_Relevant = relevant;
            m_HasCompletedFrame = false;
            if (relevant)
            {
                m_DemandKind = demandKind;
                if (m_NextSourceGeneration == ulong.MaxValue)
                    throw new InvalidOperationException($"Clip Player '{NodeId}' source generation was exhausted.");
                ulong sourceGeneration = m_NextSourceGeneration;
                m_NextSourceGeneration++;
                m_SourceId = new AnimationPoseSourceId(
                    m_Source.SourceIndex,
                    AnimationPoseSourceKind.Clip,
                    new AnimationPoseSelectionGeneration(sourceGeneration));
                m_Endpoint =
                    new PoseDiscontinuityEndpoint(m_SourceId);
                m_ContinuityIdentity =
                    AllocateContinuityIdentity();
                m_ResetSequence =
                    AllocateResetSequence();
                m_PendingResetReason = PoseDiscontinuityResetReason.BranchReplacement;
                return;
            }
            ReleaseRetainedSource();
            m_SourceId = default;
            m_Endpoint = default;
            m_DemandKind = default;
            ClearMovementClockOrigin();
        }

        internal void Reset(PoseDiscontinuityResetReason reason)
        {
            RequireAlive();
            RequireClosedFrame();
            if (reason == PoseDiscontinuityResetReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            ReleaseRetainedSource();
            m_Relevant = false;
            m_SourceRetained = false;
            m_HasCompletedFrame = false;
            m_SourceId = default;
            m_Endpoint = default;
            m_ContinuityIdentity =
                AllocateContinuityIdentity();
            m_ResetSequence = AllocateResetSequence();
            m_PendingResetReason = reason;
            ClearPhaseRelation();
            ClearMovementClockOrigin();
            SetRawClock(m_Descriptor.InitialTime);
            m_SourceWorkspace.ResetContinuity();
        }

        internal void ResetForStateEntry()
        {
            RequireAlive();
            ClearPhaseRelation();
            ClearMovementClockOrigin();
            SetRawClock(m_Descriptor.InitialTime);
            m_HasCompletedFrame = false;
            m_ContinuityIdentity =
                AllocateContinuityIdentity();
            m_ResetSequence = AllocateResetSequence();
            m_PendingResetReason = PoseDiscontinuityResetReason.BranchReplacement;
            if (!m_FrameOpen)
                m_SourceWorkspace.ResetContinuity();
        }

        void ClearPhaseRelation()
        {
            ActiveState.PhaseLeader = null;
            ActiveState.PhaseLeaderSourceId = default;
            ActiveState.PhaseLeaderContinuityIdentity = 0;
            ActiveState.PhaseSynchronization = default;
        }

        internal void SetSynchronizedTime(double continuousTime)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!m_Relevant)
                throw new InvalidOperationException($"Clip Player '{NodeId}' is not relevant.");
            if (m_CommittedState.HasCompletedFrame &&
                m_CommittedState.SourceId.Equals(m_SourceId) &&
                continuousTime < m_CommittedState.ContinuousTime)
            {
                m_HasCompletedFrame = false;
                m_ContinuityIdentity = AllocateContinuityIdentity();
                m_ResetSequence = AllocateResetSequence();
                m_PendingResetReason = PoseDiscontinuityResetReason.BranchReplacement;
            }
            m_ContinuationAnchorRawTime = m_RawContinuousTime;
            m_ContinuationAnchorEffectiveTime = continuousTime;
            m_HasContinuationAnchor = true;
            SetRawClock(m_RawContinuousTime);
        }

        internal void AnchorSynchronizedTime()
        {
            RequireAlive();
            RequireOpenFrame();
            if (!m_Relevant)
                throw new InvalidOperationException($"Clip Player '{NodeId}' is not relevant.");
            m_ContinuationAnchorRawTime = m_RawContinuousTime;
            m_ContinuationAnchorEffectiveTime = m_ContinuousTime;
            m_HasContinuationAnchor = true;
        }

        internal void SynchronizeMovementClock(
            double elapsedSeconds,
            CommittedMovementPlaybackClock clock,
            in CommittedLocomotionPlanarMotionTimeline locomotionTimeline,
            float presentationDeltaSeconds,
            float playRate)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0d ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (!m_Relevant)
                return;
            if (!clock.IsValid)
                throw new InvalidOperationException($"Clip Player '{NodeId}' requires a committed movement fact.");
            string ownerIdentity = clock.OwnerIdentity;
            ulong generation = clock.Generation;
            bool hadClockOrigin = m_HasMovementClockOrigin;
            bool changedClock = !hadClockOrigin ||
                                m_MovementClockGeneration != generation ||
                                !string.Equals(
                                    m_MovementClockOwnerIdentity,
                                    ownerIdentity,
                                    StringComparison.Ordinal);
            if (changedClock)
            {
                double preservedContinuousTime = m_RawContinuousTime;
                m_MovementClockOriginSeconds = elapsedSeconds;
                m_MovementClockLastElapsedSeconds = elapsedSeconds;
                m_MovementClockOwnerIdentity = ownerIdentity;
                m_MovementClockGeneration = generation;
                m_HasMovementClockOrigin = true;
                m_MovementClockOffsetSeconds =
                    preservedContinuousTime - m_Descriptor.InitialTime;
            }
            if (elapsedSeconds < m_MovementClockLastElapsedSeconds)
                throw new InvalidOperationException($"Clip Player '{NodeId}' Movement clock regressed within one owner generation.");
            m_MovementClockLastElapsedSeconds = elapsedSeconds;
            double stateTime = m_Descriptor.InitialTime +
                               (elapsedSeconds - m_MovementClockOriginSeconds) * playRate +
                               m_MovementClockOffsetSeconds;
            SetRawClock(stateTime);
        }

        internal void Advance(
            float presentationDeltaSeconds,
            float playRate)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            if (!m_Relevant || presentationDeltaSeconds == 0f)
                return;
            double next = m_RawContinuousTime + presentationDeltaSeconds * playRate;
            SetRawClock(next);
        }

        internal void SetPreviewTime(double continuousTime, bool resetContinuity)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!m_Relevant || !double.IsFinite(continuousTime) || continuousTime < 0d)
                throw new ArgumentOutOfRangeException(nameof(continuousTime));
            if (resetContinuity || continuousTime + 0.0000001d < m_RawContinuousTime)
            {
                m_HasCompletedFrame = false;
                m_ContinuityIdentity = AllocateContinuityIdentity();
                m_ResetSequence = AllocateResetSequence();
                m_PendingResetReason = PoseDiscontinuityResetReason.BranchReplacement;
            }
            ClearMovementClockOrigin();
            SetRawClock(continuousTime);
        }

        internal void BeginFrame(ulong completionIdentity)
        {
            RequireAlive();
            RequireOpenFrame();
            m_SourceWorkspace.BeginFrame(completionIdentity);
        }

        internal void CommitSourceFrame()
        {
            RequireAlive();
            if (m_SourceWorkspace.HasOpenFrame)
                m_SourceWorkspace.CommitFrame(m_SourceWorkspace.CompletionIdentity);
        }

        internal void DiscardSourceFrame()
        {
            RequireAlive();
            if (m_SourceWorkspace.HasOpenFrame)
                m_SourceWorkspace.DiscardFrame(m_SourceWorkspace.CompletionIdentity);
        }

        internal AnimationPoseSourceCaptureBinding PrepareCapture(
            float presentationDeltaSeconds,
            float playRate)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!m_Relevant)
                throw new InvalidOperationException($"Clip Player '{NodeId}' is not relevant.");
            float normalizedTime = m_Source.SourceDurationSeconds > 0f ? m_SampleTime / m_Source.SourceDurationSeconds : 0f;
            m_Source.SampleNativeProperties(
                normalizedTime,
                m_Parameters,
                m_ParameterAvailability);
            m_Parameters[FootPlacementWeightParameterIndex] =
                m_Source.SampleFootPlacementWeightPrepared(normalizedTime);
            m_ClipSamples[0] = m_Source.IsAcl
                ? new ClipSamplePlan(
                    0,
                    m_Source.ResourceCatalogIndex,
                    m_Source.GroupClipIndex,
                    m_Source.SourceDurationSeconds,
                    m_SampleTime,
                    m_ContinuousTime,
                    normalizedTime,
                    1f,
                    m_Descriptor.LoopAnimation)
                : new ClipSamplePlan(
                    0,
                    m_Source.Clip,
                    m_SampleTime,
                    m_ContinuousTime,
                    normalizedTime,
                    1f,
                    m_Descriptor.LoopAnimation);
            AnimationPoseSourceCaptureBinding binding = m_SourceWorkspace.PrepareCapture(
                m_SourceId,
                m_ContinuityIdentity,
                m_Descriptor.PlayerIndex,
                playRate,
                new AnimationReadOnlyBuffer<float>(m_Parameters, 0, m_Parameters.Length),
                new AnimationReadOnlyBuffer<byte>(m_ParameterAvailability, 0, m_ParameterAvailability.Length),
                SampleAndBindPredictionSource(
                    m_Source.LeftFootFeatures,
                    normalizedTime,
                    CharacterFootSide.Left),
                SampleAndBindPredictionSource(
                    m_Source.RightFootFeatures,
                    normalizedTime,
                    CharacterFootSide.Right),
                true,
                presentationDeltaSeconds);
            m_SourceRetained = true;
            return binding;
        }

        AnimationFootFeatureSample SampleAndBindPredictionSource(
            AnimationFootFeatureCurveSet curves,
            float normalizedTime,
            CharacterFootSide side)
        {
            try
            {
                AnimationFootFeatureSample feature = curves.SamplePrepared(normalizedTime);
                return BindPredictionSource(feature, side);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    $"Foot feature sampling failed. Source={m_Source.DisplayName}, Side={side}, " +
                    $"NormalizedTime={normalizedTime:R}, Cycle={m_Cycle}.",
                    exception);
            }
        }

        AnimationFootFeatureSample BindPredictionSource(
            AnimationFootFeatureSample feature,
            CharacterFootSide side)
        {
            AnimationFootFeatureSample bound = feature.BindPredictionSource(
                ActiveState.PredictionSourceIdentity,
                m_Cycle);
            return bound;
        }

        internal AnimationSelectedPosePlayerJob PrepareJob(
            ulong completionIdentity,
            in AnimationPlayerPoseNativeWriteBinding output,
            AnimationPhysicalSourceIdentity physicalSource,
            int sourceIndex,
            in CharacterPoseSourceScalarReadView scalarReadView)
        {
            RequireAlive();
            RequireOpenFrame();
            return new AnimationSelectedPosePlayerJob(
                m_SourceWorkspace.RequireNativeReadBinding(completionIdentity),
                in output,
                physicalSource,
                sourceIndex,
                in scalarReadView,
                m_ContinuityIdentity,
                BuildDiscontinuity(completionIdentity),
                m_Relevant
                    ? AnimationSelectionAvailabilityPolicy.RequireSelection
                    : AnimationSelectionAvailabilityPolicy.AllowEmpty,
                m_Relevant,
                !m_Relevant);
        }

        internal void CompleteFrame()
        {
            RequireAlive();
            RequireOpenFrame();
            CommitSourceFrame();
            m_HasCompletedFrame = m_Relevant;
            if (m_Relevant)
                m_PendingResetReason = PoseDiscontinuityResetReason.None;
        }

        internal int PendingReleaseCount
        {
            get
            {
                RequireAlive();
                return m_Releases.Count;
            }
        }

        internal AnimationPlayerReleaseToken PrepareRelease(
            int releaseOrdinal)
        {
            RequireAlive();
            AnimationPoseSourceId sourceId =
                m_Releases.PrepareRelease(releaseOrdinal);
            try
            {
                AnimationBlendSourcePoseReleaseToken sourcePoseRelease =
                    m_SourceWorkspace.PrepareRelease(sourceId);
                return new AnimationPlayerReleaseToken(
                    releaseOrdinal,
                    sourceId,
                    in sourcePoseRelease);
            }
            catch
            {
                m_Releases.CancelPreparedRelease(releaseOrdinal);
                throw;
            }
        }

        internal void ApplyPreparedRelease(
            in AnimationPlayerReleaseToken token)
        {
            AnimationBlendSourcePoseReleaseToken sourcePoseRelease =
                token.SourcePoseRelease;
            m_SourceWorkspace.ApplyPreparedRelease(
                in sourcePoseRelease);
            m_Releases.ApplyPreparedRelease(token.ReleaseOrdinal);
        }

        internal void DiscardPreparedReleases()
        {
            m_SourceWorkspace.DiscardPreparedReleases();
            m_Releases.DiscardPreparedReleases();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Releases.Clear();
            m_SourceWorkspace.Dispose();
        }

        void SetClock(double continuousTime)
        {
            if (double.IsNaN(continuousTime) || double.IsInfinity(continuousTime) || continuousTime < 0d)
                throw new ArgumentOutOfRangeException(nameof(continuousTime));
            double duration = m_Source.SourceDurationSeconds;
            m_ContinuousTime = continuousTime;
            if (m_Descriptor.LoopAnimation)
            {
                m_Cycle = checked((int)Math.Floor(continuousTime / duration));
                m_SampleTime = (float)(continuousTime - m_Cycle * duration);
                if (m_SampleTime >= duration)
                    m_SampleTime = 0f;
                return;
            }
            m_Cycle = 0;
            m_SampleTime = (float)Math.Min(continuousTime, duration);
        }

        internal void SetRawClock(double continuousTime)
        {
            if (!double.IsFinite(continuousTime) || continuousTime < 0d)
                throw new ArgumentOutOfRangeException(nameof(continuousTime));
            double effectiveTime = m_HasContinuationAnchor
                ? m_ContinuationAnchorEffectiveTime +
                  continuousTime -
                  m_ContinuationAnchorRawTime
                : continuousTime;
            if (!double.IsFinite(effectiveTime) || effectiveTime < 0d)
                throw new InvalidOperationException($"Clip Player '{NodeId}' continuation anchor produced an invalid time.");
            m_RawContinuousTime = continuousTime;
            SetClock(effectiveTime);
        }

        void ClearMovementClockOrigin()
        {
            m_MovementClockOriginSeconds = 0d;
            m_MovementClockLastElapsedSeconds = 0d;
            m_MovementClockOffsetSeconds = 0d;
            m_MovementClockOwnerIdentity = string.Empty;
            m_MovementClockGeneration = 0;
            m_HasMovementClockOrigin = false;
            ClearContinuationAnchor();
        }

        void ClearContinuationAnchor()
        {
            m_ContinuationAnchorRawTime = 0d;
            m_ContinuationAnchorEffectiveTime = 0d;
            m_HasContinuationAnchor = false;
        }

        void ReleaseRetainedSource()
        {
            if (!m_SourceRetained)
                return;
            m_Releases.Append(m_SourceId);
            m_SourceRetained = false;
        }

        ulong AllocateContinuityIdentity()
        {
            if (m_NextContinuityIdentity ==
                ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' continuity identity was exhausted.");
            }
            return m_NextContinuityIdentity++;
        }

        ulong AllocateResetSequence()
        {
            if (m_NextResetSequence == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' reset sequence was exhausted.");
            }
            return m_NextResetSequence++;
        }

        PoseDiscontinuity BuildDiscontinuity(ulong completionIdentity)
        {
            if (m_PendingResetReason == PoseDiscontinuityResetReason.None)
                return default;
            return PoseDiscontinuity.Reset(
                AllocateEventIdentity(),
                completionIdentity,
                m_Endpoint,
                m_ContinuityIdentity,
                m_PendingResetReason,
                m_ResetSequence,
                m_Relevant);
        }

        ulong AllocateEventIdentity()
        {
            if (m_NextEventIdentity == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' discontinuity identity was exhausted.");
            }
            return m_NextEventIdentity++;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationClipPlayerRuntime));
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' frame is not open.");
        }

        void RequireClosedFrame()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' frame must be closed.");
        }
    }
}
