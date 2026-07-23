using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonSimulation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Animations;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal sealed class AnimationSelectedPosePlayerRuntime : IDisposable
    {
        readonly AnimationBlendSourcePoseWorkspace m_Sources;
        readonly Queue<AnimationPoseSourceId> m_PendingReleases = new Queue<AnimationPoseSourceId>();
        AnimationPoseSourceId m_SourceId;
        PoseDiscontinuityEndpoint m_Endpoint;
        ulong m_SourcePoseContinuityIdentity;
        PoseDiscontinuityEndpoint m_PendingPreviousEndpoint;
        ulong m_PendingPreviousContinuityIdentity;
        PoseDiscontinuityReason m_PendingReason;
        PoseDiscontinuityResetReason m_PendingResetReason = PoseDiscontinuityResetReason.Initialization;
        ulong m_NextEventIdentity = 1;
        ulong m_ResetSequence = 1;
        bool m_HasPendingDiscontinuity;
        bool m_HasSelection;
        bool m_SourceRetained;
        bool m_HasCurrentSample;
        bool m_Empty;
        bool m_HasCompletedFrame;
        bool m_Disposed;

        internal AnimationSelectedPosePlayerRuntime(
            PoseNodeId nodeId,
            int playerIndex,
            AnimationChannelId channelId,
            AnimationSelectionAvailabilityPolicy availability,
            bool blendSpace,
            CharacterAnimationRigPayload rig,
            int parameterCount)
        {
            if (!nodeId.IsValid || playerIndex < 0 || !channelId.IsValid ||
                !Enum.IsDefined(typeof(AnimationSelectionAvailabilityPolicy), availability))
                throw new ArgumentException("Selected Pose Player configuration is invalid.");
            NodeId = nodeId;
            PlayerIndex = playerIndex;
            ChannelId = channelId;
            Availability = availability;
            BlendSpace = blendSpace;
            m_Sources = new AnimationBlendSourcePoseWorkspace(rig, parameterCount, 1);
        }

        internal PoseNodeId NodeId { get; }
        internal int PlayerIndex { get; }
        internal AnimationChannelId ChannelId { get; }
        internal AnimationSelectionAvailabilityPolicy Availability { get; }
        internal bool BlendSpace { get; }
        internal bool Accepts(AnimationPoseSourceKind sourceKind) =>
            BlendSpace ? sourceKind == AnimationPoseSourceKind.BlendSpace : sourceKind != AnimationPoseSourceKind.BlendSpace;
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

        internal void PushSelection(in AnimationSelectionFrame selection)
        {
            RequireAlive();
            if (!selection.IsValid || selection.AnimationChannelId != ChannelId || !Accepts(selection.SourceId.SourceKind))
                throw new ArgumentException("Selection does not belong to this Selected Pose Player.", nameof(selection));
            PoseDiscontinuityEndpoint endpoint = PoseDiscontinuityEndpoint.From(in selection);
            bool sourceChanged = m_HasSelection && !m_SourceId.Equals(selection.SourceId);
            bool continuityChanged = m_HasSelection && m_SourcePoseContinuityIdentity != selection.SourcePoseContinuityIdentity;
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
            m_SourceId = selection.SourceId;
            m_Endpoint = endpoint;
            m_SourcePoseContinuityIdentity = selection.SourcePoseContinuityIdentity;
            m_HasSelection = true;
            m_HasCurrentSample = true;
            m_Empty = false;
        }

        internal void PushUnavailable(AnimationPlaybackId playbackId)
        {
            RequireAlive();
            if (!playbackId.IsValid)
                throw new ArgumentException("Unavailable Selected Pose Player playback is invalid.", nameof(playbackId));
            if (m_HasSelection && !m_SourceId.PlaybackId.Equals(playbackId))
            {
                ReleaseRetainedSource();
                m_SourceId = default;
                m_Endpoint = default;
                m_SourcePoseContinuityIdentity = 0;
                m_HasSelection = false;
            }
            m_HasCurrentSample = false;
            m_Empty = false;
        }

        internal void PushEmpty()
        {
            RequireAlive();
            if (Availability == AnimationSelectionAvailabilityPolicy.RequireSelection)
                throw new InvalidOperationException($"Selected Pose Player '{NodeId}' requires a selection.");
            if (m_HasSelection)
                ReleaseRetainedSource();
            m_SourceId = default;
            m_Endpoint = default;
            m_SourcePoseContinuityIdentity = 0;
            m_HasSelection = false;
            m_HasCurrentSample = false;
            m_Empty = true;
        }

        internal void BeginFrame(ulong completionIdentity) => m_Sources.BeginFrame(completionIdentity);

        internal AnimationPoseSourceCaptureBinding PrepareCapture(
            in AnimationSourcePoseSample sample,
            float presentationDeltaSeconds)
        {
            RequireAlive();
            if (!m_HasSelection || !sample.IsValid || !sample.Selection.SourceId.Equals(m_SourceId))
                throw new InvalidOperationException($"Selected Pose Player '{NodeId}' has no matching source sample.");
            AnimationPoseSourceCaptureBinding binding = m_Sources.PrepareCapture(in sample, presentationDeltaSeconds);
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
            m_HasCompletedFrame = true;
            m_HasPendingDiscontinuity = false;
            m_PendingResetReason = PoseDiscontinuityResetReason.None;
        }

        internal bool TryDequeueRelease(out AnimationPoseSourceId sourceId)
        {
            RequireAlive();
            if (m_PendingReleases.Count > 0)
            {
                sourceId = m_PendingReleases.Dequeue();
                return true;
            }
            sourceId = default;
            return false;
        }

        internal void Reset(PoseDiscontinuityResetReason reason)
        {
            RequireAlive();
            if (reason == PoseDiscontinuityResetReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            if (m_SourceRetained)
                m_Sources.ReleaseSource(m_SourceId);
            m_SourceId = default;
            m_Endpoint = default;
            m_SourcePoseContinuityIdentity = 0;
            m_HasSelection = false;
            m_SourceRetained = false;
            m_HasCurrentSample = false;
            m_Empty = false;
            m_HasCompletedFrame = false;
            m_PendingReleases.Clear();
            m_HasPendingDiscontinuity = false;
            m_PendingResetReason = reason;
            m_ResetSequence = checked(m_ResetSequence + 1UL);
            m_Sources.ResetContinuity();
        }

        void ReleaseRetainedSource()
        {
            if (!m_SourceRetained)
                return;
            m_PendingReleases.Enqueue(m_SourceId);
            m_Sources.ReleaseSource(m_SourceId);
            m_SourceRetained = false;
        }

        PoseDiscontinuity BuildDiscontinuity(ulong completionIdentity)
        {
            if (m_PendingResetReason != PoseDiscontinuityResetReason.None)
                return PoseDiscontinuity.Reset(
                    m_NextEventIdentity++, completionIdentity, m_Endpoint, m_SourcePoseContinuityIdentity,
                    m_PendingResetReason, m_ResetSequence, m_HasSelection);
            if (!m_HasPendingDiscontinuity)
                return default;
            return PoseDiscontinuity.SourceJump(
                m_NextEventIdentity++, completionIdentity,
                m_PendingPreviousEndpoint, m_Endpoint,
                m_PendingPreviousContinuityIdentity, m_SourcePoseContinuityIdentity,
                m_PendingReason);
        }

        static PoseDiscontinuityReason ResolveReason(
            PoseDiscontinuityEndpoint previous,
            PoseDiscontinuityEndpoint current,
            bool sourceChanged)
        {
            if (previous.ProgramProducerIndex != current.ProgramProducerIndex ||
                previous.SourceKind != current.SourceKind || previous.PlaybackGeneration != current.PlaybackGeneration)
                return PoseDiscontinuityReason.SourceIdentityChanged;
            if (previous.SelectionGeneration != current.SelectionGeneration)
                return PoseDiscontinuityReason.SelectionGenerationChanged;
            return sourceChanged
                ? PoseDiscontinuityReason.SourceIdentityChanged
                : PoseDiscontinuityReason.SourcePoseContinuityChanged;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_PendingReleases.Clear();
            m_Sources.Dispose();
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationSelectedPosePlayerRuntime));
        }
    }

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
        [NativeDisableContainerSafetyRestriction] NativeSlice<PoseDiscontinuity> m_Discontinuity;
        [NativeDisableContainerSafetyRestriction] NativeSlice<AnimationPoseNativeInvalidReason> m_InvalidReason;
        [NativeDisableContainerSafetyRestriction] NativeSlice<ulong> m_CompletedAt;
        readonly AnimationPhysicalSourceIdentity m_PhysicalSource;
        readonly int m_SourceIndex;
        readonly int m_PlayerIndex;
        readonly ulong m_ContinuityIdentity;
        readonly PoseDiscontinuity m_PoseDiscontinuity;
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
            m_PoseDiscontinuity = discontinuity;
            m_CompletionIdentity = output.CompletionIdentity;
            m_AvailabilityPolicy = availabilityPolicy;
            m_HasSelection = hasSelection;
            m_Empty = empty;
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            ClearOutput();
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
            m_LeftFootFeatures[0] = m_Source.LeftFootFeatures[m_SourceIndex];
            m_RightFootFeatures[0] = m_Source.RightFootFeatures[m_SourceIndex];
            m_HasFootFeatures[0] = hasFeet ? (byte)1 : (byte)0;
            m_Contributions[0] = new AnimationPrimitivePoseContribution(
                m_PlayerIndex,
                m_PhysicalSource.Index.Value,
                m_PhysicalSource.Generation,
                AnimationPoseContributionKind.Live,
                m_Source.ProgramProducerIndices[m_SourceIndex],
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

        void ClearOutput()
        {
            for (int i = 0; i < m_Pose.Length; i++)
            {
                m_Pose[i] = default;
                m_Velocity[i] = default;
            }
            for (int i = 0; i < m_Parameters.Length; i++)
            {
                m_Parameters[i] = 0f;
                m_ParameterAvailability[i] = 0;
            }
            for (int i = 0; i < m_Contributions.Length; i++)
                m_Contributions[i] = default;
            for (int i = 0; i < m_DenseContributionWeights.Length; i++)
                m_DenseContributionWeights[i] = 0f;
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
