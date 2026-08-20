using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Animations;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class AnimationSelectedPosePlayerRuntime : IDisposable
    {
        struct State
        {
            internal AnimationPoseSourceId SourceId;
            internal PoseDiscontinuityEndpoint Endpoint;
            internal ulong SourcePoseContinuityIdentity;
            internal PoseDiscontinuityEndpoint
                PendingPreviousEndpoint;
            internal ulong
                PendingPreviousContinuityIdentity;
            internal PoseDiscontinuityReason PendingReason;
            internal PoseDiscontinuityResetReason
                PendingResetReason;
            internal ulong NextEventIdentity;
            internal ulong ResetSequence;
            internal ulong NextResetSequence;
            internal bool HasPendingDiscontinuity;
            internal bool HasSelection;
            internal bool SourceRetained;
            internal bool HasCurrentSample;
            internal bool Empty;
            internal bool HasCompletedFrame;
        }

        readonly AnimationBlendSourcePoseWorkspace m_Sources;
        readonly AnimationPlayerReleaseJournal m_Releases;
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

        AnimationPoseSourceId m_SourceId { get => ActiveState.SourceId; set => ActiveState.SourceId = value; }
        PoseDiscontinuityEndpoint m_Endpoint { get => ActiveState.Endpoint; set => ActiveState.Endpoint = value; }
        ulong m_SourcePoseContinuityIdentity { get => ActiveState.SourcePoseContinuityIdentity; set => ActiveState.SourcePoseContinuityIdentity = value; }
        PoseDiscontinuityEndpoint m_PendingPreviousEndpoint { get => ActiveState.PendingPreviousEndpoint; set => ActiveState.PendingPreviousEndpoint = value; }
        ulong m_PendingPreviousContinuityIdentity { get => ActiveState.PendingPreviousContinuityIdentity; set => ActiveState.PendingPreviousContinuityIdentity = value; }
        PoseDiscontinuityReason m_PendingReason { get => ActiveState.PendingReason; set => ActiveState.PendingReason = value; }
        PoseDiscontinuityResetReason m_PendingResetReason { get => ActiveState.PendingResetReason; set => ActiveState.PendingResetReason = value; }
        ulong m_NextEventIdentity { get => ActiveState.NextEventIdentity; set => ActiveState.NextEventIdentity = value; }
        ulong m_ResetSequence { get => ActiveState.ResetSequence; set => ActiveState.ResetSequence = value; }
        ulong m_NextResetSequence { get => ActiveState.NextResetSequence; set => ActiveState.NextResetSequence = value; }
        bool m_HasPendingDiscontinuity { get => ActiveState.HasPendingDiscontinuity; set => ActiveState.HasPendingDiscontinuity = value; }
        bool m_HasSelection { get => ActiveState.HasSelection; set => ActiveState.HasSelection = value; }
        bool m_SourceRetained { get => ActiveState.SourceRetained; set => ActiveState.SourceRetained = value; }
        bool m_HasCurrentSample { get => ActiveState.HasCurrentSample; set => ActiveState.HasCurrentSample = value; }
        bool m_Empty { get => ActiveState.Empty; set => ActiveState.Empty = value; }
        bool m_HasCompletedFrame { get => ActiveState.HasCompletedFrame; set => ActiveState.HasCompletedFrame = value; }

        internal AnimationSelectedPosePlayerRuntime(
            PoseNodeId nodeId,
            int playerIndex,
            int sourceOwnerIndex,
            PresentationPoseSourceProviderId providerId,
            AnimationSelectionAvailabilityPolicy availability,
            CharacterAnimationRigPayload rig,
            int parameterCount)
        {
            if (!nodeId.IsValid || playerIndex < 0 ||
                sourceOwnerIndex < 0 ||
                !providerId.IsValid ||
                (byte)availability < (byte)AnimationSelectionAvailabilityPolicy.RequireSelection ||
                (byte)availability > (byte)AnimationSelectionAvailabilityPolicy.AllowEmpty)
                throw new ArgumentException("Selected Pose Player configuration is invalid.");
            NodeId = nodeId;
            PlayerIndex = playerIndex;
            SourceOwnerIndex = sourceOwnerIndex;
            ProviderId = providerId;
            Availability = availability;
            m_Sources = new AnimationBlendSourcePoseWorkspace(
                rig,
                parameterCount,
                AnimationBlendSourcePoseWorkspace.SinglePlayerHandoffCapacity);
            m_Releases = new AnimationPlayerReleaseJournal(
                AnimationBlendSourcePoseWorkspace.SinglePlayerHandoffCapacity);
            m_CommittedState = new State
            {
                PendingResetReason = PoseDiscontinuityResetReason.Initialization,
                NextEventIdentity = 1,
                ResetSequence = 1,
                NextResetSequence = 2
            };
            m_PendingState = m_CommittedState;
        }

        internal PoseNodeId NodeId { get; }
        internal int PlayerIndex { get; }
        internal int SourceOwnerIndex { get; }
        internal PresentationPoseSourceProviderId ProviderId { get; }
        internal AnimationSelectionAvailabilityPolicy Availability { get; }
        internal bool HasSelection => m_HasSelection;
        internal bool HasCurrentSample => m_HasSelection && m_HasCurrentSample;
        internal AnimationPoseSourceId SourceId => m_HasSelection ? m_SourceId : default;
        internal bool HasCompletedFrame => m_HasCompletedFrame;
        internal AnimationPoseAvailability LastAvailability => !m_HasCompletedFrame
            ? AnimationPoseAvailability.Invalid
            : HasCurrentSample
                ? AnimationPoseAvailability.Pose
                : m_Empty ? AnimationPoseAvailability.NoPose : AnimationPoseAvailability.Invalid;
        internal float LastOutputWeight => m_HasCompletedFrame && HasCurrentSample ? 1f : 0f;

        internal void BeginFrame()
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException($"Selected Pose Player '{NodeId}' frame is already open.");
            m_PendingState = m_CommittedState;
            m_Releases.BeginFrame();
            m_FrameOpen = true;
        }

        internal void DiscardFrame()
        {
            RequireAlive();
            if (!m_FrameOpen)
                return;
            if (m_Sources.HasOpenFrame)
                m_Sources.DiscardFrame(m_Sources.CompletionIdentity);
            m_Sources.DiscardPreparedReleases();
            m_Releases.DiscardFrame();
            m_PendingState = m_CommittedState;
            m_FrameOpen = false;
        }

        internal void CommitFrame()
        {
            RequireAlive();
            if (!m_FrameOpen)
                throw new InvalidOperationException($"Selected Pose Player '{NodeId}' frame is not open.");
            m_CommittedState = m_PendingState;
            m_Releases.CommitFrame();
            m_FrameOpen = false;
        }

        internal void PushSelection(
            in PresentationPoseSourceSample sample)
        {
            RequireAlive();
            if (sample == null || !sample.IsValid ||
                sample.Availability !=
                    PresentationPoseSourceAvailability.Ready ||
                sample.ProviderId != ProviderId ||
                sample.PlayerNodeId != NodeId ||
                sample.SourceKind !=
                    AnimationPoseSourceKind.MotionMatching)
            {
                throw new ArgumentException(
                    "Selection does not belong to this Selected Pose Player.",
                    nameof(sample));
            }
            var sourceId = new AnimationPoseSourceId(
                sample.SourceIndex,
                sample.SourceKind,
                new AnimationPoseSelectionGeneration(
                    sample.SourceGeneration.Value));
            PoseDiscontinuityEndpoint endpoint =
                PoseDiscontinuityEndpoint.From(in sample);
            bool sourceChanged =
                m_HasSelection &&
                !m_SourceId.Equals(sourceId);
            bool continuityChanged =
                m_HasSelection &&
                m_SourcePoseContinuityIdentity !=
                    sample.SourcePoseContinuityIdentity;
            if ((sourceChanged || continuityChanged) && m_HasPendingDiscontinuity)
                throw new InvalidOperationException($"Selected Pose Player '{NodeId}' received more than one discontinuity before frame completion.");
            if (sourceChanged)
            {
                ReleaseRetainedSource();
            }
            if (sourceChanged || continuityChanged)
            {
                m_PendingPreviousEndpoint = m_Endpoint;
                m_PendingPreviousContinuityIdentity = m_SourcePoseContinuityIdentity;
                m_PendingReason = ResolveReason(m_Endpoint, endpoint, sourceChanged);
                m_HasPendingDiscontinuity = true;
            }
            m_SourceId = sourceId;
            m_Endpoint = endpoint;
            m_SourcePoseContinuityIdentity =
                sample.SourcePoseContinuityIdentity;
            m_HasSelection = true;
            m_HasCurrentSample = true;
            m_Empty = false;
        }

        internal void BeginFrame(ulong completionIdentity) =>
            m_Sources.BeginFrame(completionIdentity);

        internal AnimationPoseSourceCaptureBinding PrepareCapture(
            in PresentationPoseSourceSample sample,
            float presentationDeltaSeconds)
        {
            RequireAlive();
            if (!m_HasSelection || sample == null || !sample.IsValid ||
                sample.ProviderId != ProviderId ||
                sample.PlayerNodeId != NodeId ||
                sample.Availability !=
                    PresentationPoseSourceAvailability.Ready)
            {
                throw new InvalidOperationException($"Selected Pose Player '{NodeId}' has no matching source sample.");
            }
            var sourceId = new AnimationPoseSourceId(
                sample.SourceIndex,
                sample.SourceKind,
                new AnimationPoseSelectionGeneration(
                    sample.SourceGeneration.Value));
            if (sourceId != m_SourceId)
            {
                throw new InvalidOperationException(
                    $"Selected Pose Player '{NodeId}' source identity does not match its current selection.");
            }
            PresentationPoseSampleTime time =
                sample.EffectiveSample;
            var request = new AnimationPoseSampleRequest(
                sourceId,
                sample.SourcePoseContinuityIdentity,
                sample.FrameSequence,
                SourceOwnerIndex,
                time.SampleTime,
                time.ContinuousTime,
                time.Cycle,
                time.Loop,
                time.TimeScale,
                sample.Clips,
                sample.ParameterPageId,
                sample.PoseParameters,
                sample.PoseParameterAvailability);
            var resolved = new AnimationResolvedPoseSourceSample(
                request,
                in sample.LeftFootFeatures,
                in sample.RightFootFeatures,
                sample.HasFootFeatures);
            AnimationPoseSourceCaptureBinding binding =
                m_Sources.PrepareCapture(
                    resolved,
                    presentationDeltaSeconds);
            m_SourceRetained = true;
            return binding;
        }

        internal AnimationSelectedPosePlayerJob PrepareJob(
            ulong completionIdentity,
            in AnimationPlayerPoseNativeWriteBinding output,
            AnimationPhysicalSourceIdentity physicalSource,
            int sourceIndex)
        {
            RequireAlive();
            return new AnimationSelectedPosePlayerJob(
                m_Sources.RequireNativeReadBinding(completionIdentity),
                in output,
                physicalSource,
                sourceIndex,
                m_HasSelection ? m_SourcePoseContinuityIdentity : completionIdentity,
                BuildDiscontinuity(completionIdentity),
                Availability,
                m_HasSelection,
                m_Empty);
        }

        internal void CompleteFrame()
        {
            RequireAlive();
            if (m_Sources.HasOpenFrame)
                m_Sources.CommitFrame(m_Sources.CompletionIdentity);
            m_HasCompletedFrame = true;
            m_HasPendingDiscontinuity = false;
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
                    m_Sources.PrepareRelease(sourceId);
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
            m_Sources.ApplyPreparedRelease(in sourcePoseRelease);
            m_Releases.ApplyPreparedRelease(token.ReleaseOrdinal);
        }

        internal void Reset(PoseDiscontinuityResetReason reason)
        {
            RequireAlive();
            if (reason == PoseDiscontinuityResetReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            ReleaseRetainedSource();
            m_SourceId = default;
            m_Endpoint = default;
            m_SourcePoseContinuityIdentity = 0;
            m_HasSelection = false;
            m_SourceRetained = false;
            m_HasCurrentSample = false;
            m_Empty = false;
            m_HasCompletedFrame = false;
            m_HasPendingDiscontinuity = false;
            m_PendingResetReason = reason;
            m_ResetSequence = AllocateResetSequence();
            m_Sources.ResetContinuity();
        }

        void ReleaseRetainedSource()
        {
            if (!m_SourceRetained)
                return;
            m_Releases.Append(m_SourceId);
            m_SourceRetained = false;
        }

        ulong AllocateResetSequence()
        {
            if (m_NextResetSequence == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Selected Pose Player '{NodeId}' reset sequence was exhausted.");
            }
            return m_NextResetSequence++;
        }

        PoseDiscontinuity BuildDiscontinuity(ulong completionIdentity)
        {
            if (m_PendingResetReason != PoseDiscontinuityResetReason.None)
                return PoseDiscontinuity.Reset(
                    AllocateEventIdentity(), completionIdentity, m_Endpoint, m_SourcePoseContinuityIdentity,
                    m_PendingResetReason, m_ResetSequence, m_HasSelection);
            if (!m_HasPendingDiscontinuity)
                return default;
            return PoseDiscontinuity.SourceJump(
                AllocateEventIdentity(), completionIdentity,
                m_PendingPreviousEndpoint, m_Endpoint,
                m_PendingPreviousContinuityIdentity, m_SourcePoseContinuityIdentity,
                m_PendingReason);
        }

        ulong AllocateEventIdentity()
        {
            if (m_NextEventIdentity == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    $"Selected Pose Player '{NodeId}' discontinuity identity was exhausted.");
            }
            return m_NextEventIdentity++;
        }

        static PoseDiscontinuityReason ResolveReason(
            PoseDiscontinuityEndpoint previous,
            PoseDiscontinuityEndpoint current,
            bool sourceChanged)
        {
            if (previous.SourceId.SourceKind !=
                    current.SourceId.SourceKind ||
                !previous.SourceId.PlaybackId.Equals(
                    current.SourceId.PlaybackId) ||
                previous.SourceId.PresentationPoseSourceIndex !=
                    current.SourceId.PresentationPoseSourceIndex ||
                previous.SourceId.SourceActionInstanceId !=
                    current.SourceId.SourceActionInstanceId)
            {
                return PoseDiscontinuityReason.SourceIdentityChanged;
            }
            if (previous.SourceId.SelectionGeneration !=
                current.SourceId.SelectionGeneration)
            {
                return PoseDiscontinuityReason.SelectionGenerationChanged;
            }
            return sourceChanged
                ? PoseDiscontinuityReason.SourceIdentityChanged
                : PoseDiscontinuityReason.SourcePoseContinuityChanged;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Releases.Clear();
            m_Sources.Dispose();
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationSelectedPosePlayerRuntime));
        }
    }

    [BurstCompile]
    internal struct AnimationSelectedPosePlayerJob : IAnimationJob
    {
        [ReadOnly] readonly AnimationBlendSourcePoseNativeReadBinding m_Source;
        [NativeDisableContainerSafetyRestriction] NativeSlice<AnimationLocalBonePose> m_Pose;
        [NativeDisableContainerSafetyRestriction] NativeSlice<AnimationBlendBoneVelocity> m_Velocity;
        [NativeDisableContainerSafetyRestriction] NativeSlice<float> m_Parameters;
        [NativeDisableContainerSafetyRestriction] NativeSlice<byte> m_ParameterAvailability;
        [NativeDisableContainerSafetyRestriction] NativeSlice<AnimationPrimitivePoseContribution> m_Contributions;
        [NativeDisableContainerSafetyRestriction] NativeSlice<float> m_DenseContributionWeights;
        [NativeDisableContainerSafetyRestriction] NativeSlice<int> m_ContributionCount;
        [NativeDisableContainerSafetyRestriction] NativeSlice<float> m_OutputWeight;
        [NativeDisableContainerSafetyRestriction] NativeSlice<AnimationFootFeatureSample> m_LeftFootFeatures;
        [NativeDisableContainerSafetyRestriction] NativeSlice<AnimationFootFeatureSample> m_RightFootFeatures;
        [NativeDisableContainerSafetyRestriction] NativeSlice<byte> m_HasFootFeatures;
        [NativeDisableContainerSafetyRestriction] NativeSlice<AnimationPoseAvailability> m_Availability;
        [NativeDisableContainerSafetyRestriction] NativeSlice<ulong> m_Continuity;
        [NativeDisableContainerSafetyRestriction] NativeSlice<PoseDiscontinuityNative> m_Discontinuity;
        [NativeDisableContainerSafetyRestriction] NativeSlice<AnimationPoseNativeInvalidReason> m_InvalidReason;
        [NativeDisableContainerSafetyRestriction] NativeSlice<ulong> m_CompletedAt;
        readonly AnimationPhysicalSourceIdentity m_PhysicalSource;
        readonly int m_SourceIndex;
        readonly int m_PlayerIndex;
        readonly ulong m_ContinuityIdentity;
        readonly PoseDiscontinuityNative m_PoseDiscontinuity;
        readonly ulong m_CompletionIdentity;
        readonly AnimationSelectionAvailabilityPolicy m_AvailabilityPolicy;
        readonly bool m_HasSelection;
        readonly bool m_Empty;

        internal AnimationSelectedPosePlayerJob(
            AnimationBlendSourcePoseNativeReadBinding source,
            in AnimationPlayerPoseNativeWriteBinding output,
            AnimationPhysicalSourceIdentity physicalSource,
            int sourceIndex,
            ulong continuityIdentity,
            PoseDiscontinuity discontinuity,
            AnimationSelectionAvailabilityPolicy availabilityPolicy,
            bool hasSelection,
            bool empty)
        {
            m_Source = source;
            m_Pose = output.DenseLocalPoses;
            m_Velocity = output.DenseVelocities;
            m_Parameters = output.PoseParameters;
            m_ParameterAvailability = output.PoseParameterAvailability;
            m_Contributions = output.Contributions;
            m_DenseContributionWeights = output.DenseContributionWeights;
            m_ContributionCount = output.ContributionCount;
            m_OutputWeight = output.OutputWeight;
            m_LeftFootFeatures = output.LeftFootFeatures;
            m_RightFootFeatures = output.RightFootFeatures;
            m_HasFootFeatures = output.HasFootFeatures;
            m_Availability = output.Availability;
            m_Continuity = output.ContinuityIdentity;
            m_Discontinuity = output.Discontinuity;
            m_InvalidReason = output.InvalidReason;
            m_CompletedAt = output.CompletedAt;
            m_PhysicalSource = physicalSource;
            m_SourceIndex = sourceIndex;
            m_PlayerIndex = output.Range.PhysicalPlayerIndex;
            m_ContinuityIdentity = continuityIdentity;
            m_PoseDiscontinuity =
                PoseDiscontinuityNative.From(in discontinuity);
            m_CompletionIdentity = output.CompletionIdentity;
            m_AvailabilityPolicy = availabilityPolicy;
            m_HasSelection = hasSelection;
            m_Empty = empty;
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            InvalidateOutput();
            if (!m_HasSelection)
            {
                if (m_AvailabilityPolicy == AnimationSelectionAvailabilityPolicy.AllowEmpty && m_Empty)
                    PublishNoPose();
                else
                    PublishInvalid(AnimationPoseNativeInvalidReason.RequiredPoseMissing);
                return;
            }
            if (!m_PhysicalSource.IsValid || m_SourceIndex < 0 || m_SourceIndex >= m_Source.SourceCapacity ||
                m_Source.CompletedAt[m_SourceIndex] != m_CompletionIdentity)
            {
                PublishInvalid(AnimationPoseNativeInvalidReason.SourceIncomplete);
                return;
            }
            int poseOffset = m_SourceIndex * m_Source.BoneCount;
            int parameterOffset = m_SourceIndex * m_Source.ParameterCount;
            for (int bone = 0; bone < m_Source.BoneCount; bone++)
            {
                AnimationLocalBonePose pose = m_Source.CurrentPose[poseOffset + bone];
                AnimationBlendBoneVelocity velocity = m_Source.Velocity[poseOffset + bone];
                if (!pose.IsValid || !velocity.IsValid)
                {
                    PublishInvalid(AnimationPoseNativeInvalidReason.SlotPoseInvalid);
                    return;
                }
                m_Pose[bone] = pose;
                m_Velocity[bone] = velocity;
                m_DenseContributionWeights[bone] = 1f;
            }
            for (int parameter = 0; parameter < m_Source.ParameterCount; parameter++)
            {
                float value = m_Source.PoseParameters[parameterOffset + parameter];
                byte available = m_Source.PoseParameterAvailability[parameterOffset + parameter];
                if (!float.IsFinite(value) || available > 1)
                {
                    PublishInvalid(AnimationPoseNativeInvalidReason.SlotParameterInvalid);
                    return;
                }
                m_Parameters[parameter] = value;
                m_ParameterAvailability[parameter] = available;
            }
            bool hasFeet = m_Source.HasFootFeatures[m_SourceIndex] != 0;
            m_LeftFootFeatures[0] = hasFeet
                ? m_Source.LeftFootFeatures[m_SourceIndex].BindPredictionContribution(
                    m_ContinuityIdentity,
                    CharacterFootSide.Left)
                : default;
            m_RightFootFeatures[0] = hasFeet
                ? m_Source.RightFootFeatures[m_SourceIndex].BindPredictionContribution(
                    m_ContinuityIdentity,
                    CharacterFootSide.Right)
                : default;
            m_HasFootFeatures[0] = hasFeet ? (byte)1 : (byte)0;
            m_Contributions[0] = new AnimationPrimitivePoseContribution(
                m_PlayerIndex,
                m_PhysicalSource.Index.Value,
                m_PhysicalSource.Generation,
                AnimationPoseContributionKind.Live,
                m_Source.SourceOwnerIndices[m_SourceIndex],
                m_ContinuityIdentity,
                1f,
                hasFeet ? 1f : 0f,
                hasFeet ? 1f : 0f);
            m_ContributionCount[0] = 1;
            m_OutputWeight[0] = 1f;
            m_Availability[0] = AnimationPoseAvailability.Pose;
            m_Continuity[0] = m_ContinuityIdentity;
            m_Discontinuity[0] = m_PoseDiscontinuity;
            m_InvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            m_CompletedAt[0] = m_CompletionIdentity;
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        void PublishNoPose()
        {
            m_Availability[0] = AnimationPoseAvailability.NoPose;
            m_Continuity[0] = m_ContinuityIdentity;
            m_Discontinuity[0] = m_PoseDiscontinuity;
            m_InvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            m_CompletedAt[0] = m_CompletionIdentity;
        }

        void PublishInvalid(AnimationPoseNativeInvalidReason reason)
        {
            m_Availability[0] = AnimationPoseAvailability.Invalid;
            m_Continuity[0] = m_ContinuityIdentity;
            m_Discontinuity[0] = m_PoseDiscontinuity;
            m_InvalidReason[0] = reason;
            m_CompletedAt[0] = m_CompletionIdentity;
        }

        void InvalidateOutput()
        {
            for (int i = 0; i < m_Parameters.Length; i++)
            {
                m_Parameters[i] = 0f;
                m_ParameterAvailability[i] = 0;
            }
            m_ContributionCount[0] = 0;
            m_OutputWeight[0] = 0f;
            m_LeftFootFeatures[0] = default;
            m_RightFootFeatures[0] = default;
            m_HasFootFeatures[0] = 0;
            m_Availability[0] = AnimationPoseAvailability.Invalid;
            m_Continuity[0] = 0;
            m_Discontinuity[0] = default;
            m_InvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            m_CompletedAt[0] = 0;
        }
    }
}
