using System;
using System.Globalization;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
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

    internal sealed class AnimationSequencePlayerRuntime : IDisposable
    {
        struct State
        {
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
            internal ulong MovementMarkerEpochIdentity;
            internal long MovementMarkerOrdinalOffset;
            internal ulong NextSourceGeneration;
            internal ulong ContinuityIdentity;
            internal ulong NextContinuityIdentity;
            internal ulong NextEventIdentity;
            internal ulong ResetSequence;
            internal ulong NextResetSequence;
            internal AnimationPoseSourceId SourceId;
            internal PoseDiscontinuityEndpoint Endpoint;
            internal PoseDiscontinuityResetReason PendingResetReason;
            internal bool Relevant;
            internal bool SourceRetained;
            internal bool HasCompletedFrame;
            internal bool HasMovementClockOrigin;
            internal bool HasContinuationAnchor;
            internal bool HasMovementMarkerEpoch;
            internal bool HasMovementMarkerAlignment;
            internal PoseSourceProviderDemandKind DemandKind;
        }

        readonly CharacterPresentationSequencePlayerDescriptor m_Descriptor;
        readonly CharacterPresentationPoseSourcePlan m_Source;
        readonly AnimationBlendSourcePoseWorkspace m_SourceWorkspace;
        readonly float[] m_Parameters;
        readonly byte[] m_ParameterAvailability;
        readonly ClipSamplePlan[] m_ClipSamples = new ClipSamplePlan[1];
        readonly AnimationPlayerReleaseJournal m_Releases;
        float m_PlayRate;
        State m_CommittedState;
        State m_PendingState;
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
        ulong m_MovementMarkerEpochIdentity { get => ActiveState.MovementMarkerEpochIdentity; set => ActiveState.MovementMarkerEpochIdentity = value; }
        long m_MovementMarkerOrdinalOffset { get => ActiveState.MovementMarkerOrdinalOffset; set => ActiveState.MovementMarkerOrdinalOffset = value; }
        ulong m_NextSourceGeneration { get => ActiveState.NextSourceGeneration; set => ActiveState.NextSourceGeneration = value; }
        ulong m_ContinuityIdentity { get => ActiveState.ContinuityIdentity; set => ActiveState.ContinuityIdentity = value; }
        ulong m_NextContinuityIdentity { get => ActiveState.NextContinuityIdentity; set => ActiveState.NextContinuityIdentity = value; }
        ulong m_NextEventIdentity { get => ActiveState.NextEventIdentity; set => ActiveState.NextEventIdentity = value; }
        ulong m_ResetSequence { get => ActiveState.ResetSequence; set => ActiveState.ResetSequence = value; }
        ulong m_NextResetSequence { get => ActiveState.NextResetSequence; set => ActiveState.NextResetSequence = value; }
        AnimationPoseSourceId m_SourceId { get => ActiveState.SourceId; set => ActiveState.SourceId = value; }
        PoseDiscontinuityEndpoint m_Endpoint { get => ActiveState.Endpoint; set => ActiveState.Endpoint = value; }
        PoseDiscontinuityResetReason m_PendingResetReason { get => ActiveState.PendingResetReason; set => ActiveState.PendingResetReason = value; }
        bool m_Relevant { get => ActiveState.Relevant; set => ActiveState.Relevant = value; }
        bool m_SourceRetained { get => ActiveState.SourceRetained; set => ActiveState.SourceRetained = value; }
        bool m_HasCompletedFrame { get => ActiveState.HasCompletedFrame; set => ActiveState.HasCompletedFrame = value; }
        bool m_HasMovementClockOrigin { get => ActiveState.HasMovementClockOrigin; set => ActiveState.HasMovementClockOrigin = value; }
        bool m_HasContinuationAnchor { get => ActiveState.HasContinuationAnchor; set => ActiveState.HasContinuationAnchor = value; }
        bool m_HasMovementMarkerEpoch { get => ActiveState.HasMovementMarkerEpoch; set => ActiveState.HasMovementMarkerEpoch = value; }
        bool m_HasMovementMarkerAlignment { get => ActiveState.HasMovementMarkerAlignment; set => ActiveState.HasMovementMarkerAlignment = value; }
        PoseSourceProviderDemandKind m_DemandKind { get => ActiveState.DemandKind; set => ActiveState.DemandKind = value; }

        internal AnimationSequencePlayerRuntime(
            CharacterPresentationSequencePlayerDescriptor descriptor,
            CharacterPresentationPoseSourcePlan source,
            CharacterPresentationPosePlan posePlan,
            CharacterAnimationRigPayload rig)
        {
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_PlayRate = descriptor.PlayRate;
            if (posePlan == null)
                throw new ArgumentNullException(nameof(posePlan));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            source.RequireValid();
            if (descriptor.PresentationPoseSourceIndex != source.SourceIndex ||
                !string.Equals(source.RigId, rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(source.RigRevision, rig.RigRevision, StringComparison.Ordinal) ||
                descriptor.Loop != source.Loop ||
                descriptor.InitialTime > source.Clip.length)
            {
                throw new InvalidOperationException($"Sequence Player '{descriptor.NodeId}' source binding does not match its compiled descriptor.");
            }
            source.LeftFootFeatures.RequireValid();
            source.RightFootFeatures.RequireValid();
            m_Parameters = new float[posePlan.Parameters.Count];
            m_ParameterAvailability = new byte[posePlan.Parameters.Count];
            for (int i = 0; i < posePlan.Parameters.Count; i++)
            {
                m_Parameters[i] = posePlan.Parameters[i].DefaultValue;
                m_ParameterAvailability[i] = 1;
            }
            FootPlacementWeightParameterIndex = posePlan.RequireParameterIndex(source.FootPlacementWeightParameterId);
            m_SourceWorkspace = new AnimationBlendSourcePoseWorkspace(
                rig,
                posePlan.Parameters.Count,
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

        internal PoseNodeId NodeId => m_Descriptor.NodeId;
        internal int PlayerIndex => m_Descriptor.PlayerIndex;
        internal PresentationPoseSourceIndex SourceIndex => m_Source.SourceIndex;
        internal int FootPlacementWeightParameterIndex { get; }
        internal AnimationPoseSourceId SourceId => m_SourceId;
        internal bool IsRelevant => m_Relevant;
        internal bool HasRetainedSource => m_SourceRetained;
        internal bool HasCompletedFrame => m_HasCompletedFrame;
        internal float SampleTime => m_SampleTime;
        internal double ContinuousTime => m_ContinuousTime;
        internal double RawContinuousTime => m_RawContinuousTime;
        internal int Cycle => m_Cycle;
        internal float RemainingTime => Math.Max(0f, m_Source.Clip.length - m_SampleTime);
        internal float Duration => m_Source.Clip.length;
        internal AnimationMarkerSyncBinding MarkerSync => m_Source.MarkerSync;
        internal CharacterSequencePlayerClockSource ClockSource => m_Descriptor.ClockSource;
        internal float PlayRate => m_PlayRate;

        internal string ApplyTuning(float playRate)
        {
            if (!float.IsFinite(playRate) || playRate <= 0f || playRate > 8f)
                return $"Sequence Player '{NodeId}' play rate is outside its published range.";
            m_PlayRate = playRate;
            return string.Empty;
        }
        internal AnimationReadOnlyBuffer<ClipSamplePlan> ClipSamples =>
            new AnimationReadOnlyBuffer<ClipSamplePlan>(m_ClipSamples, 0, 1);

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException($"Sequence Player '{NodeId}' frame is already open.");
            m_PendingState = m_CommittedState;
            m_Releases.BeginFrame();
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
                throw new InvalidOperationException($"Sequence Player '{NodeId}' frame is not open.");
            m_CommittedState = m_PendingState;
            m_Releases.CommitFrame();
            m_FrameOpen = false;
        }

        internal void SetRelevant(
            bool relevant,
            PoseSourceProviderDemandKind demandKind = PoseSourceProviderDemandKind.Active)
        {
            RequireAlive();
            if (relevant && !Enum.IsDefined(typeof(PoseSourceProviderDemandKind), demandKind))
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
                    throw new InvalidOperationException($"Sequence Player '{NodeId}' source generation was exhausted.");
                ulong sourceGeneration = m_NextSourceGeneration;
                m_NextSourceGeneration++;
                m_SourceId = new AnimationPoseSourceId(
                    m_Source.SourceIndex,
                    AnimationPoseSourceKind.Sequence,
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
            ClearMovementClockOrigin();
            SetRawClock(m_Descriptor.InitialTime);
            m_SourceWorkspace.ResetContinuity();
        }

        internal void ResetForStateEntry()
        {
            RequireAlive();
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

        internal void SetSynchronizedTime(double continuousTime)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!m_Relevant)
                throw new InvalidOperationException($"Sequence Player '{NodeId}' is not relevant.");
            if (m_Descriptor.Loop &&
                m_Descriptor.ClockSource == CharacterSequencePlayerClockSource.CommittedMovement &&
                m_HasMovementMarkerEpoch &&
                continuousTime < m_ContinuousTime)
            {
                double duration = m_Source.Clip.length;
                double cycleCount = Math.Ceiling(
                    (m_ContinuousTime - continuousTime) / duration);
                continuousTime += Math.Max(1d, cycleCount) * duration;
            }
            if (continuousTime < m_ContinuousTime)
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
                throw new InvalidOperationException($"Sequence Player '{NodeId}' is not relevant.");
            m_ContinuationAnchorRawTime = m_RawContinuousTime;
            m_ContinuationAnchorEffectiveTime = m_ContinuousTime;
            m_HasContinuationAnchor = true;
        }

        internal void SynchronizeMovementClock(
            double elapsedSeconds,
            CommittedMovementPlaybackClock clock,
            float presentationDeltaSeconds)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0d ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (!m_Relevant)
                return;
            if (!clock.IsValid)
            {
                ContinueMovementClock(presentationDeltaSeconds);
                return;
            }
            string ownerIdentity = clock.OwnerIdentity;
            ulong generation = clock.Generation;
            bool hadClockOrigin = m_HasMovementClockOrigin;
            bool changedClock = !hadClockOrigin ||
                                m_MovementClockGeneration != generation ||
                                !string.Equals(
                                    m_MovementClockOwnerIdentity,
                                    ownerIdentity,
                                    StringComparison.Ordinal);
            if (changedClock && hadClockOrigin &&
                m_DemandKind == PoseSourceProviderDemandKind.TransitionSource)
            {
                ContinueMovementClock(presentationDeltaSeconds);
                return;
            }
            if (changedClock)
            {
                double preservedContinuousTime = m_RawContinuousTime;
                m_MovementClockOriginSeconds = elapsedSeconds;
                m_MovementClockLastElapsedSeconds = elapsedSeconds;
                m_MovementClockOwnerIdentity = ownerIdentity;
                m_MovementClockGeneration = generation;
                m_HasMovementClockOrigin = true;
                if (!m_HasMovementMarkerEpoch &&
                    m_Source.MarkerSync != null &&
                    m_Source.MarkerSync.IsMarkerGroup)
                {
                    m_MovementMarkerEpochIdentity =
                        AnimationPredictedFootStepSample.SourceIdentity(
                            string.Concat(
                                m_Source.MarkerSync.CanonicalGroupId,
                                ":",
                                ownerIdentity,
                                ":",
                                generation.ToString(CultureInfo.InvariantCulture)));
                    m_MovementMarkerOrdinalOffset = 0;
                    m_HasMovementMarkerEpoch = true;
                    m_HasMovementMarkerAlignment = false;
                }
                else if (!m_HasMovementMarkerEpoch)
                {
                    m_MovementMarkerEpochIdentity = 0;
                    m_MovementMarkerOrdinalOffset = 0;
                    m_HasMovementMarkerEpoch = false;
                    m_HasMovementMarkerAlignment = false;
                }
                m_MovementClockOffsetSeconds =
                    preservedContinuousTime - m_Descriptor.InitialTime;
            }
            if (elapsedSeconds < m_MovementClockLastElapsedSeconds)
                throw new InvalidOperationException($"Sequence Player '{NodeId}' Movement clock regressed within one owner generation.");
            m_MovementClockLastElapsedSeconds = elapsedSeconds;
            double stateTime = m_Descriptor.InitialTime +
                               (elapsedSeconds - m_MovementClockOriginSeconds) * m_PlayRate +
                               m_MovementClockOffsetSeconds;
            SetRawClock(stateTime);
        }

        void ContinueMovementClock(float presentationDeltaSeconds)
        {
            if (presentationDeltaSeconds == 0f)
                return;
            SetRawClock(m_RawContinuousTime + presentationDeltaSeconds * m_PlayRate);
        }

        internal void AlignMovementMarkerEpoch(
            AnimationSequencePlayerRuntime source)
        {
            RequireAlive();
            RequireOpenFrame();
            AnimationMarkerSyncBinding sourceMarkerSync = source?.MarkerSync;
            AnimationMarkerSyncBinding targetMarkerSync = MarkerSync;
            if (source == null || !source.m_HasMovementMarkerEpoch ||
                sourceMarkerSync == null || !sourceMarkerSync.IsMarkerGroup ||
                targetMarkerSync == null || !targetMarkerSync.IsMarkerGroup ||
                !string.Equals(
                    sourceMarkerSync.CanonicalGroupId,
                    targetMarkerSync.CanonicalGroupId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Synchronized Movement marker epochs are incompatible.");
            }
            if (m_HasMovementMarkerAlignment &&
                m_MovementMarkerEpochIdentity == source.m_MovementMarkerEpochIdentity)
                return;
            long sourceOrdinal = source.ResolveCurrentMarkerOrdinal();
            long targetLocalOrdinal = ResolveCurrentLocalMarkerOrdinal();
            m_MovementMarkerEpochIdentity =
                source.m_MovementMarkerEpochIdentity;
            m_MovementMarkerOrdinalOffset = checked(
                sourceOrdinal - targetLocalOrdinal);
            m_HasMovementMarkerEpoch = true;
            m_HasMovementMarkerAlignment = true;
        }

        internal void Advance(float presentationDeltaSeconds)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            if (!m_Relevant || presentationDeltaSeconds == 0f)
                return;
            double next = m_RawContinuousTime + presentationDeltaSeconds * m_PlayRate;
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

        internal AnimationPoseSourceCaptureBinding PrepareCapture(float presentationDeltaSeconds)
        {
            RequireAlive();
            RequireOpenFrame();
            if (!m_Relevant)
                throw new InvalidOperationException($"Sequence Player '{NodeId}' is not relevant.");
            float normalizedTime = m_Source.Clip.length > 0f ? m_SampleTime / m_Source.Clip.length : 0f;
            m_Parameters[FootPlacementWeightParameterIndex] =
                m_Source.SampleFootPlacementWeightPrepared(normalizedTime);
            m_ClipSamples[0] = new ClipSamplePlan(
                0,
                m_Source.Clip,
                m_SampleTime,
                m_ContinuousTime,
                normalizedTime,
                1f,
                m_Descriptor.Loop);
            AnimationPoseSourceCaptureBinding binding = m_SourceWorkspace.PrepareCapture(
                m_SourceId,
                m_ContinuityIdentity,
                m_Descriptor.PlayerIndex,
                m_PlayRate,
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
            AnimationFootFeatureSample feature = curves.SamplePrepared(normalizedTime);
            return BindPredictionSource(feature, side);
        }

        AnimationFootFeatureSample BindPredictionSource(
            AnimationFootFeatureSample feature,
            CharacterFootSide side)
        {
            AnimationFootFeatureSample bound = feature.BindPredictionSource(
                AnimationPredictedFootStepSample.SourceIdentity(m_SourceId),
                m_Cycle,
                m_SampleTime,
                m_Source.Clip.length,
                m_Descriptor.Loop);
            AnimationPredictedFootStepSample step = bound.PredictedStep;
            if (!step.IsSourceBound || !m_HasMovementMarkerEpoch)
                return bound;
            AnimationPredictedFootStepSample incoming = bound.IncomingPredictedStep;
            return bound
                .WithPredictedStep(BindSynchronizedMarkerSource(side, in step))
                .WithIncomingPredictedStep(
                    incoming.IsSourceBound
                        ? BindSynchronizedMarkerSource(side, in incoming)
                        : incoming);
        }

        AnimationPredictedFootStepSample BindSynchronizedMarkerSource(
            CharacterFootSide side,
            in AnimationPredictedFootStepSample step)
        {
            int landingOrdinal = checked((int)ResolveLandingMarkerOrdinal(
                side,
                step.TimeToLandingSeconds));
            int opposingOrdinal = -1;
            if (step.OpposingEventOrdinal > 0)
            {
                opposingOrdinal = checked((int)ResolveLandingMarkerOrdinal(
                    side == CharacterFootSide.Left
                        ? CharacterFootSide.Right
                        : CharacterFootSide.Left,
                    step.OpposingLandingDelaySeconds));
            }
            return step.BindSynchronizedMarkerSource(
                m_MovementMarkerEpochIdentity,
                landingOrdinal,
                opposingOrdinal);
        }

        long ResolveLandingMarkerOrdinal(
            CharacterFootSide side,
            float delaySeconds)
        {
            if (side != CharacterFootSide.Left &&
                side != CharacterFootSide.Right ||
                !float.IsFinite(delaySeconds) || delaySeconds < 0f)
            {
                throw new ArgumentException(
                    "Locomotion landing marker request is invalid.");
            }
            AnimationMarkerSyncBinding binding = MarkerSync;
            string markerId = side == CharacterFootSide.Left
                ? "LeftFootContact"
                : "RightFootContact";
            double landingTime = binding.SequenceTopology ==
                                 BTSMTL.Timeline.AnimationMarkerSequenceTopology.Finite
                ? m_SampleTime + delaySeconds
                : m_ContinuousTime + delaySeconds;
            long bestOrdinal = long.MinValue;
            double bestDistance = double.MaxValue;
            if (binding.SequenceTopology ==
                BTSMTL.Timeline.AnimationMarkerSequenceTopology.Finite)
            {
                SelectLandingMarkerCandidate(
                    binding,
                    markerId,
                    landingTime,
                    0,
                    ref bestOrdinal,
                    ref bestDistance);
            }
            else
            {
                long centerCycle = (long)Math.Floor(
                    landingTime / binding.DurationSeconds);
                for (long cycle = Math.Max(0, centerCycle - 1);
                     cycle <= centerCycle + 1;
                     cycle++)
                {
                    SelectLandingMarkerCandidate(
                        binding,
                        markerId,
                        landingTime,
                        cycle,
                        ref bestOrdinal,
                        ref bestDistance);
                }
            }
            if (bestOrdinal == long.MinValue || bestDistance > 0.025d)
            {
                throw new InvalidOperationException(
                    $"Sequence Player '{NodeId}' predicted {markerId} landing does not resolve to its authored locomotion marker.");
            }
            return checked(bestOrdinal + m_MovementMarkerOrdinalOffset);
        }

        static void SelectLandingMarkerCandidate(
            AnimationMarkerSyncBinding binding,
            string markerId,
            double landingTime,
            long cycle,
            ref long bestOrdinal,
            ref double bestDistance)
        {
            for (int i = 0; i < binding.Markers.Count; i++)
            {
                AnimationMarkerSyncMarkerBinding marker = binding.Markers[i];
                if (!string.Equals(marker.MarkerId, markerId, StringComparison.Ordinal))
                    continue;
                double candidate = cycle * binding.DurationSeconds +
                                   marker.TimeSeconds;
                double distance = Math.Abs(candidate - landingTime);
                long ordinal = checked(cycle * binding.Markers.Count + i);
                if (distance < bestDistance - 0.0000001d ||
                    Math.Abs(distance - bestDistance) <= 0.0000001d &&
                    (bestOrdinal == long.MinValue || ordinal < bestOrdinal))
                {
                    bestOrdinal = ordinal;
                    bestDistance = distance;
                }
            }
        }

        long ResolveCurrentMarkerOrdinal() => checked(
            ResolveCurrentLocalMarkerOrdinal() +
            m_MovementMarkerOrdinalOffset);

        long ResolveCurrentLocalMarkerOrdinal()
        {
            AnimationMarkerSyncBinding binding = MarkerSync;
            if (binding == null || !binding.IsMarkerGroup ||
                binding.Markers.Count == 0)
            {
                throw new InvalidOperationException(
                    "Locomotion marker occurrence is unavailable.");
            }
            if (binding.SequenceTopology ==
                BTSMTL.Timeline.AnimationMarkerSequenceTopology.Finite)
            {
                int index = 0;
                for (int i = 0; i < binding.Markers.Count; i++)
                {
                    if (m_ContinuousTime + 0.0000001d <
                        binding.Markers[i].TimeSeconds)
                        break;
                    index = i;
                }
                return index;
            }
            long cycle = (long)Math.Floor(
                m_ContinuousTime / binding.DurationSeconds);
            double localTime = m_ContinuousTime -
                               cycle * binding.DurationSeconds;
            int markerIndex = -1;
            for (int i = 0; i < binding.Markers.Count; i++)
            {
                if (localTime + 0.0000001d <
                    binding.Markers[i].TimeSeconds)
                    break;
                markerIndex = i;
            }
            if (markerIndex >= 0)
                return checked(cycle * binding.Markers.Count + markerIndex);
            return checked(
                (cycle - 1) * binding.Markers.Count +
                binding.Markers.Count - 1);
        }

        internal AnimationSelectedPosePlayerJob PrepareJob(
            ulong completionIdentity,
            in AnimationPlayerPoseNativeWriteBinding output,
            AnimationPhysicalSourceIdentity physicalSource,
            int sourceIndex)
        {
            RequireAlive();
            RequireOpenFrame();
            return new AnimationSelectedPosePlayerJob(
                m_SourceWorkspace.RequireNativeReadBinding(completionIdentity),
                in output,
                physicalSource,
                sourceIndex,
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
            double duration = m_Source.Clip.length;
            m_ContinuousTime = continuousTime;
            if (m_Descriptor.Loop)
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

        void SetRawClock(double continuousTime)
        {
            if (!double.IsFinite(continuousTime) || continuousTime < 0d)
                throw new ArgumentOutOfRangeException(nameof(continuousTime));
            double effectiveTime = m_HasContinuationAnchor
                ? m_ContinuationAnchorEffectiveTime +
                  continuousTime -
                  m_ContinuationAnchorRawTime
                : continuousTime;
            if (!double.IsFinite(effectiveTime) || effectiveTime < 0d)
                throw new InvalidOperationException($"Sequence Player '{NodeId}' continuation anchor produced an invalid time.");
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
            m_MovementMarkerEpochIdentity = 0;
            m_MovementMarkerOrdinalOffset = 0;
            m_HasMovementClockOrigin = false;
            m_HasMovementMarkerEpoch = false;
            m_HasMovementMarkerAlignment = false;
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
                    $"Sequence Player '{NodeId}' continuity identity was exhausted.");
            }
            return m_NextContinuityIdentity++;
        }

        ulong AllocateResetSequence()
        {
            if (m_NextResetSequence == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Sequence Player '{NodeId}' reset sequence was exhausted.");
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
                    $"Sequence Player '{NodeId}' discontinuity identity was exhausted.");
            }
            return m_NextEventIdentity++;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationSequencePlayerRuntime));
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Sequence Player '{NodeId}' frame is not open.");
        }

        void RequireClosedFrame()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException(
                    $"Sequence Player '{NodeId}' frame must be closed.");
        }
    }
}
