using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct MotionMatchingClipSamplePlan
    {
        public MotionMatchingClipSamplePlan(
            CharacterMotionMatchingSourceClipId sourceClipId,
            int clipBindingIndex,
            AnimationClip clip,
            MotionMatchingPoseTimePlan time,
            bool rootLocked)
        {
            float clipLength = clip ? clip.length : 0f;
            bool loopTimeValid = time.Looping
                ? time.Cycle == 0
                    ? time.ContinuousVisualTime == time.SampleTime
                    : time.ContinuousVisualTime > time.SampleTime
                : time.Cycle == 0 && time.ContinuousVisualTime == time.SampleTime;
            if (!sourceClipId.IsValid || clipBindingIndex < 0 || !clip ||
                !float.IsFinite(clipLength) || clipLength <= 0f ||
                !time.IsValid || time.SampleTime > clipLength || !loopTimeValid || time.AnimatorStateSpeed != 0f || !rootLocked)
                throw new ArgumentException("Motion Matching Clip Sample Plan is invalid.");
            SourceClipId = sourceClipId;
            ClipBindingIndex = clipBindingIndex;
            Clip = clip;
            Time = time;
            RootLocked = rootLocked;
        }

        public CharacterMotionMatchingSourceClipId SourceClipId { get; }
        public int ClipBindingIndex { get; }
        public AnimationClip Clip { get; }
        public MotionMatchingPoseTimePlan Time { get; }
        public float ClipTime => Time.SampleTime;
        public float NormalizedTime => ClipTime / Clip.length;
        public double ContinuousVisualTime => Time.ContinuousVisualTime;
        public int Cycle => Time.Cycle;
        public float VisualTimeScale => Time.VisualTimeScale;
        public float AnimatorStateSpeed => Time.AnimatorStateSpeed;
        public bool IsLooping => Time.Looping;
        public bool RootLocked { get; }
    }

    public readonly struct MotionMatchingPoseParameterSample
    {
        public MotionMatchingPoseParameterSample(PoseParameterId parameterId, float value)
        {
            if (!parameterId.IsValid || !float.IsFinite(value) || value < 0f || value > 1f)
                throw new ArgumentException("Motion Matching Pose Parameter sample is invalid.");
            ParameterId = parameterId;
            Value = value;
        }

        public PoseParameterId ParameterId { get; }
        public float Value { get; }
        public bool IsValid => ParameterId.IsValid && float.IsFinite(Value) && Value >= 0f && Value <= 1f;
    }

    public readonly struct MotionMatchingPoseSourceOutput
    {
        public MotionMatchingPoseSourceOutput(
            CharacterMotionMatchingDatabaseArtifactIdentity databaseIdentity,
            AnimationChannelId animationChannelId,
            PoseNodeId poseNodeId,
            AnimationPlaybackId playbackId,
            MotionMatchingSelectionGeneration selectionGeneration,
            ulong presentationRequestSequence,
            int programProducerIndex,
            string programProducerId,
            MotionMatchingClipSamplePlan clipSamplePlan,
            MotionMatchingPoseParameterSample footPlacementWeight,
            AnimationFootPlacementSample footFeatures,
            CharacterMotionMatchingPlanId planId)
        {
            if (databaseIdentity == null || !animationChannelId.IsValid || !poseNodeId.IsValid || !playbackId.IsValid || !selectionGeneration.IsValid ||
                presentationRequestSequence == 0 || programProducerIndex < 0 || string.IsNullOrWhiteSpace(programProducerId) ||
                !footPlacementWeight.IsValid ||
                !footPlacementWeight.ParameterId.Equals(MotionMatchingPoseSourceRuntime.FootPlacementWeightParameterId) ||
                !footFeatures.IsValid || !planId.IsValid)
                throw new ArgumentException("Motion Matching Pose Source output is incomplete.");
            DatabaseIdentity = databaseIdentity;
            AnimationChannelId = animationChannelId;
            PoseNodeId = poseNodeId;
            PlaybackId = playbackId;
            SelectionGeneration = selectionGeneration;
            SourcePoseContinuityIdentity = selectionGeneration.Value;
            PresentationRequestSequence = presentationRequestSequence;
            ProgramProducerIndex = programProducerIndex;
            ProgramProducerId = programProducerId;
            ClipSamplePlan = clipSamplePlan;
            FootPlacementWeight = footPlacementWeight;
            FootFeatures = footFeatures;
            PlanId = planId;
        }

        public CharacterMotionMatchingDatabaseArtifactIdentity DatabaseIdentity { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public PoseNodeId PoseNodeId { get; }
        public AnimationPlaybackId PlaybackId { get; }
        public MotionMatchingPoseSourceKind PoseSourceKind => MotionMatchingPoseSourceKind.MotionMatching;
        public MotionMatchingSelectionGeneration SelectionGeneration { get; }
        public ulong SourcePoseContinuityIdentity { get; }
        public ulong PresentationRequestSequence { get; }
        public int ProgramProducerIndex { get; }
        public string ProgramProducerId { get; }
        public MotionMatchingClipSamplePlan ClipSamplePlan { get; }
        public MotionMatchingPoseParameterSample FootPlacementWeight { get; }
        public AnimationFootPlacementSample FootFeatures { get; }
        public CharacterMotionMatchingPlanId PlanId { get; }
    }

    public sealed class MotionMatchingPoseSourceRuntime
    {
        public const string FootPlacementWeightParameterName = MotionMatchingPoseSourceParameterContract.FootPlacementWeightName;
        public static readonly PoseParameterId FootPlacementWeightParameterId = MotionMatchingPoseSourceParameterContract.FootPlacementWeightId;

        readonly CharacterMotionMatchingRuntimeDatabase m_Database;

        public MotionMatchingPoseSourceRuntime(CharacterMotionMatchingRuntimeDatabase database)
        {
            m_Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public MotionMatchingPoseSourceOutput Resolve(
            MotionMatchingSelectionDecision selection,
            AnimationChannelId animationChannelId,
            PoseNodeId poseNodeId,
            AnimationPlaybackId playbackId,
            ulong presentationRequestSequence,
            int programProducerIndex,
            string programProducerId)
        {
            if (!selection.IsValid)
                throw new InvalidOperationException($"Motion Matching Pose Source cannot lower invalid selection '{selection.InvalidReason}'.");
            MotionMatchingSamplePayload sample = m_Database.GetSample(selection.SampleIndex);
            MotionMatchingClipBindingPayload clip = m_Database.GetClipBinding(sample.ClipBindingIndex);
            if (clip == null || !clip.RootLocked || !clip.Clip)
                throw new InvalidOperationException("Motion Matching selected sample has no valid root-locked Clip binding.");
            if (clip.FootPlacementWeightCurve == null ||
                !clip.FootPlacementWeightCurve.ParameterId.Equals(FootPlacementWeightParameterId))
                throw new InvalidOperationException($"Motion Matching Pose Source requires Projection parameter '{FootPlacementWeightParameterName}' for the selected Clip sample.");
            var clipSamplePlan = new MotionMatchingClipSamplePlan(
                clip.SourceClipId,
                sample.ClipBindingIndex,
                clip.Clip,
                selection.PoseTime,
                true);
            var footPlacementWeight = new MotionMatchingPoseParameterSample(
                FootPlacementWeightParameterId,
                clip.FootPlacementWeightCurve.Sample(clipSamplePlan.NormalizedTime));
            var footPlacement = new AnimationFootPlacementSample(
                footPlacementWeight.Value,
                sample.LeftFoot,
                sample.RightFoot);
            return new MotionMatchingPoseSourceOutput(
                m_Database.ArtifactIdentity,
                animationChannelId,
                poseNodeId,
                playbackId,
                selection.Generation,
                presentationRequestSequence,
                programProducerIndex,
                MotionMatchingIdentity.Require(programProducerId, nameof(programProducerId)),
                clipSamplePlan,
                footPlacementWeight,
                footPlacement,
                selection.Plan.PlanId);
        }
    }
}
