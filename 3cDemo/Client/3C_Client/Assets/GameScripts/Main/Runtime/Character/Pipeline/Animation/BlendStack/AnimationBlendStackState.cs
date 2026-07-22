using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    public readonly struct AnimationBlendEntryId : IEquatable<AnimationBlendEntryId>
    {
        public AnimationBlendEntryId(
            PoseSlotId poseSlotId,
            AnimationPoseSourceId sourceId,
            bool emptyTarget,
            ulong presentationRequestSequence)
        {
            if (!poseSlotId.IsValid || presentationRequestSequence == 0 ||
                emptyTarget == sourceId.IsValid)
                throw new ArgumentException("Animation Blend Entry identity is invalid.");
            PoseSlotId = poseSlotId;
            SourceId = sourceId;
            EmptyTarget = emptyTarget;
            PresentationRequestSequence = presentationRequestSequence;
        }

        public PoseSlotId PoseSlotId { get; }
        public AnimationPoseSourceId SourceId { get; }
        public bool EmptyTarget { get; }
        public ulong PresentationRequestSequence { get; }
        public bool IsValid => PoseSlotId.IsValid && PresentationRequestSequence != 0 && EmptyTarget != SourceId.IsValid;

        public bool Equals(AnimationBlendEntryId other) =>
            PoseSlotId == other.PoseSlotId && SourceId.Equals(other.SourceId) &&
            EmptyTarget == other.EmptyTarget && PresentationRequestSequence == other.PresentationRequestSequence;

        public override bool Equals(object obj) => obj is AnimationBlendEntryId other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = PoseSlotId.GetHashCode();
                hash = hash * 397 ^ SourceId.GetHashCode();
                hash = hash * 397 ^ EmptyTarget.GetHashCode();
                return hash * 397 ^ PresentationRequestSequence.GetHashCode();
            }
        }

        public override string ToString() => EmptyTarget
            ? $"{PoseSlotId}/Empty#{PresentationRequestSequence}"
            : $"{PoseSlotId}/{SourceId}#{PresentationRequestSequence}";
    }

    internal struct AnimationBlendFadeClock
    {
        float m_ElapsedSeconds;

        public float ElapsedSeconds => m_ElapsedSeconds;

        public void Advance(float deltaSeconds)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            float elapsed = m_ElapsedSeconds + deltaSeconds;
            if (!float.IsFinite(elapsed))
                throw new InvalidOperationException("Animation Blend Fade Clock overflowed.");
            m_ElapsedSeconds = elapsed;
        }

        public void RebaseDepth(float durationScale)
        {
            if (!float.IsFinite(durationScale) || durationScale <= 0f)
                throw new ArgumentOutOfRangeException(nameof(durationScale));
            float elapsed = m_ElapsedSeconds * durationScale;
            if (!float.IsFinite(elapsed))
                throw new InvalidOperationException("Animation Blend Fade Clock depth rebase overflowed.");
            m_ElapsedSeconds = elapsed;
        }

        public float GetNormalizedTime(float durationSeconds)
        {
            if (!float.IsFinite(durationSeconds) || durationSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            return durationSeconds <= 0f ? 1f : Mathf.Clamp01(m_ElapsedSeconds / durationSeconds);
        }
    }

    internal struct AnimationBlendEntryState
    {
        AnimationBlendFadeClock m_Clock;
        float m_DepthDurationScale;

        public AnimationBlendEntryState(
            AnimationBlendEntryId entryId,
            int programProducerIndex,
            AnimationBlendTechnique technique,
            float baseDurationSeconds,
            int canonicalCurveIndex,
            int blendProfileIndex,
            ulong contributionContinuityIdentity)
        {
            if (!entryId.IsValid ||
                entryId.EmptyTarget == (programProducerIndex >= 0) ||
                !Enum.IsDefined(typeof(AnimationBlendTechnique), technique) ||
                !float.IsFinite(baseDurationSeconds) || baseDurationSeconds < 0f ||
                canonicalCurveIndex < 0 || blendProfileIndex < 0 ||
                contributionContinuityIdentity == 0)
                throw new ArgumentException("Animation Blend Entry state is invalid.");
            EntryId = entryId;
            ProgramProducerIndex = programProducerIndex;
            Technique = technique;
            BaseDurationSeconds = baseDurationSeconds;
            CanonicalCurveIndex = canonicalCurveIndex;
            BlendProfileIndex = blendProfileIndex;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            PushDepth = 0;
            m_DepthDurationScale = 1f;
            m_Clock = default;
        }

        public AnimationBlendEntryId EntryId { get; }
        public AnimationPoseSourceId SourceId => EntryId.SourceId;
        public bool IsEmpty => EntryId.EmptyTarget;
        public int ProgramProducerIndex { get; }
        public AnimationBlendTechnique Technique { get; }
        public float BaseDurationSeconds { get; }
        public int CanonicalCurveIndex { get; }
        public int BlendProfileIndex { get; }
        public ulong ContributionContinuityIdentity { get; }
        public int PushDepth { get; private set; }
        public float ElapsedSeconds => m_Clock.ElapsedSeconds;

        public void Advance(float deltaSeconds) => m_Clock.Advance(deltaSeconds);

        public void IncreasePushDepth(float depthBlendTimeMultiplier)
        {
            if (PushDepth == int.MaxValue)
                throw new InvalidOperationException("Animation Blend Entry push depth overflowed.");
            m_Clock.RebaseDepth(depthBlendTimeMultiplier);
            m_DepthDurationScale *= depthBlendTimeMultiplier;
            if (!float.IsFinite(m_DepthDurationScale) || m_DepthDurationScale <= 0f)
                throw new InvalidOperationException("Animation Blend Entry depth duration scale is invalid.");
            PushDepth++;
        }

        public float EvaluateBoneAlpha(
            int boneIndex,
            AnimationBlendCurvePayload curve,
            AnimationBlendProfilePayload blendProfile)
        {
            return AnimationBlendCurveEvaluator.Evaluate(
                curve,
                m_Clock.GetNormalizedTime(GetBoneDuration(boneIndex, blendProfile)));
        }

        public float EvaluateOutputAlpha(
            AnimationBlendCurvePayload curve,
            AnimationBlendProfilePayload blendProfile)
        {
            return AnimationBlendCurveEvaluator.Evaluate(
                curve,
                GetOutputNormalizedTime(blendProfile));
        }

        public float GetBoneNormalizedTime(int boneIndex, AnimationBlendProfilePayload blendProfile) =>
            m_Clock.GetNormalizedTime(GetBoneDuration(boneIndex, blendProfile));

        public float GetOutputNormalizedTime(AnimationBlendProfilePayload blendProfile) =>
            m_Clock.GetNormalizedTime(GetOutputDuration(blendProfile));

        public float GetBoneDuration(int boneIndex, AnimationBlendProfilePayload blendProfile)
        {
            if (blendProfile == null ||
                (uint)boneIndex >= (uint)blendProfile.DenseDurationMultipliers.Count)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            return BaseDurationSeconds *
                   blendProfile.DenseDurationMultipliers[boneIndex] *
                   m_DepthDurationScale;
        }

        public float GetOutputDuration(AnimationBlendProfilePayload blendProfile)
        {
            if (blendProfile == null)
                throw new ArgumentNullException(nameof(blendProfile));
            return BaseDurationSeconds * blendProfile.GlobalDurationMultiplier * m_DepthDurationScale;
        }

        public bool IsComplete(int boneCount, AnimationBlendProfilePayload blendProfile)
        {
            if (blendProfile == null || boneCount != blendProfile.DenseDurationMultipliers.Count)
                throw new ArgumentOutOfRangeException(nameof(boneCount));
            if (m_Clock.GetNormalizedTime(GetOutputDuration(blendProfile)) < 1f)
                return false;
            for (int boneIndex = 0; boneIndex < boneCount; boneIndex++)
            {
                if (m_Clock.GetNormalizedTime(GetBoneDuration(boneIndex, blendProfile)) < 1f)
                    return false;
            }
            return true;
        }
    }

    internal readonly struct AnimationBlendPushRequest
    {
        public AnimationBlendPushRequest(
            AnimationChannelId animationChannelId,
            PoseSlotId poseSlotId,
            AnimationPoseSourceId sourceId,
            bool targetEmpty,
            int programProducerIndex,
            ulong presentationRequestSequence,
            AnimationBlendTransitionPayload transition)
        {
            if (!animationChannelId.IsValid || !poseSlotId.IsValid || presentationRequestSequence == 0 ||
                targetEmpty == sourceId.IsValid || targetEmpty == (programProducerIndex >= 0) ||
                transition == null || transition.TargetEmpty != targetEmpty ||
                !targetEmpty && transition.TargetProducerIndex != programProducerIndex)
                throw new ArgumentException("Animation Blend push request is invalid.");
            AnimationChannelId = animationChannelId;
            PoseSlotId = poseSlotId;
            SourceId = sourceId;
            TargetEmpty = targetEmpty;
            ProgramProducerIndex = programProducerIndex;
            PresentationRequestSequence = presentationRequestSequence;
            Transition = transition;
        }

        public AnimationChannelId AnimationChannelId { get; }
        public PoseSlotId PoseSlotId { get; }
        public AnimationPoseSourceId SourceId { get; }
        public bool TargetEmpty { get; }
        public int ProgramProducerIndex { get; }
        public ulong PresentationRequestSequence { get; }
        public AnimationBlendTransitionPayload Transition { get; }
    }

    public readonly struct AnimationBlendStackRelease
    {
        public AnimationBlendStackRelease(
            PoseSlotId poseSlotId,
            AnimationPoseSourceId sourceId,
            ulong completionIdentity)
        {
            if (!poseSlotId.IsValid || !sourceId.IsValid || completionIdentity == 0)
                throw new ArgumentException("Animation Blend Stack release is invalid.");
            PoseSlotId = poseSlotId;
            SourceId = sourceId;
            CompletionIdentity = completionIdentity;
        }

        public PoseSlotId PoseSlotId { get; }
        public AnimationPoseSourceId SourceId { get; }
        public ulong CompletionIdentity { get; }
    }

    internal enum AnimationBlendPushResult : byte
    {
        ContinuedSource = 1,
        Pushed = 2,
        CapturedStoredPose = 3,
        RebasedInertial = 4
    }

    internal enum AnimationBlendStackInvalidReason : byte
    {
        None = 0,
        DuplicateCompletion = 1,
        SourceFrameNotPrepared = 2,
        MissingLiveSource = 3,
        InvalidSourcePose = 4,
        MissingRequiredOutput = 5,
        InvalidCaptureBoundary = 6
    }
}
