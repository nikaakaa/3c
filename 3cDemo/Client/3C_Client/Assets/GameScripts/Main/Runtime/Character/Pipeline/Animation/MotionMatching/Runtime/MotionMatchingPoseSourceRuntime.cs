using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct MotionMatchingClipSamplePlan
    {
        public MotionMatchingClipSamplePlan(
            MotionMatchingClipBindingPayload binding,
            int clipBindingIndex,
            MotionMatchingPoseTimePlan time)
        {
            if (binding == null || clipBindingIndex < 0 || !time.IsValid ||
                time.SampleTime > binding.DurationSeconds ||
                !IsLoopTimeValid(time) ||
                time.AnimatorStateSpeed != 0f)
                throw new ArgumentException("Motion Matching Clip Sample Plan is invalid.");
            SourceClipId = binding.SourceClipId;
            ClipBindingIndex = clipBindingIndex;
            Clip = binding.Clip;
            Time = time;
            DurationSeconds = binding.DurationSeconds;
            SourceIsLooping = binding.IsLooping;
            RootLocked = binding.RootLocked;
            Backend = binding.Backend;
            ResourceCatalogIndex = binding.ResourceCatalogIndex;
            GroupClipIndex = binding.GroupClipIndex;
        }

        public CharacterMotionMatchingSourceClipId SourceClipId { get; }
        public int ClipBindingIndex { get; }
        public AnimationClip Clip { get; }
        public MotionMatchingPoseTimePlan Time { get; }
        public float ClipTime => Time.SampleTime;
        public float DurationSeconds { get; }
        public float NormalizedTime => ClipTime / DurationSeconds;
        public double ContinuousVisualTime => Time.ContinuousVisualTime;
        public int Cycle => Time.Cycle;
        public float VisualTimeScale => Time.VisualTimeScale;
        public float AnimatorStateSpeed => Time.AnimatorStateSpeed;
        public bool IsLooping => Time.Looping;
        public bool SourceIsLooping { get; }
        public bool RootLocked { get; }
        public CharacterAnimationSamplingBackendKind Backend { get; }
        public int ResourceCatalogIndex { get; }
        public int GroupClipIndex { get; }
        public bool IsAcl => Backend == CharacterAnimationSamplingBackendKind.Acl;
        public bool IsValid =>
            SourceClipId.IsValid &&
            ClipBindingIndex >= 0 &&
            Time.IsValid &&
            IsLoopTimeValid(Time) &&
            RootLocked &&
            float.IsFinite(DurationSeconds) &&
            DurationSeconds > 0f &&
            ClipTime >= 0f &&
            ClipTime <= DurationSeconds &&
            AnimatorStateSpeed == 0f &&
            (Backend == CharacterAnimationSamplingBackendKind.NativeClip
                ? Clip && ResourceCatalogIndex == -1 && GroupClipIndex == -1 &&
                  DurationSeconds == Clip.length &&
                  SourceIsLooping == Clip.isLooping
                : Backend == CharacterAnimationSamplingBackendKind.Acl &&
                  !Clip &&
                  ResourceCatalogIndex >= 0 && GroupClipIndex >= 0);

        public void RequireValid()
        {
            if (!IsValid)
                throw new InvalidOperationException(
                    "Motion Matching Clip Sample Plan is invalid.");
        }

        static bool IsLoopTimeValid(MotionMatchingPoseTimePlan time) =>
            time.Looping
                ? time.Cycle == 0
                    ? time.ContinuousVisualTime == time.SampleTime
                    : time.ContinuousVisualTime > time.SampleTime
                : time.Cycle == 0 &&
                  time.ContinuousVisualTime == time.SampleTime;
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
            int projectionDatabaseIndex,
            PresentationPoseSourceProviderId providerId,
            PresentationPoseSourceIndex sourceIndex,
            PoseNodeId playerNodeId,
            MotionMatchingSelectionGeneration selectionGeneration,
            ulong frameSequence,
            MotionMatchingClipSamplePlan clipSamplePlan,
            MotionMatchingPoseParameterSample footPlacementWeight,
            AnimationFootPlacementSample footFeatures,
            CharacterMotionMatchingPlanId planId)
        {
            if (databaseIdentity == null || projectionDatabaseIndex < 0 ||
                !providerId.IsValid ||
                !sourceIndex.IsValid || !playerNodeId.IsValid ||
                !selectionGeneration.IsValid || frameSequence == 0 ||
                !clipSamplePlan.IsValid ||
                !footPlacementWeight.IsValid ||
                !footPlacementWeight.ParameterId.Equals(MotionMatchingPoseSourceRuntime.FootPlacementWeightParameterId) ||
                !footFeatures.IsValid || !planId.IsValid)
                throw new ArgumentException("Motion Matching Pose Source output is incomplete.");
            DatabaseIdentity = databaseIdentity;
            ProjectionDatabaseIndex = projectionDatabaseIndex;
            ProviderId = providerId;
            SourceIndex = sourceIndex;
            PlayerNodeId = playerNodeId;
            SelectionGeneration = selectionGeneration;
            SourcePoseContinuityIdentity = selectionGeneration.Value;
            FrameSequence = frameSequence;
            ClipSamplePlan = clipSamplePlan;
            FootPlacementWeight = footPlacementWeight;
            FootFeatures = footFeatures;
            PlanId = planId;
        }

        public CharacterMotionMatchingDatabaseArtifactIdentity DatabaseIdentity { get; }
        public int ProjectionDatabaseIndex { get; }
        public PresentationPoseSourceProviderId ProviderId { get; }
        public PresentationPoseSourceIndex SourceIndex { get; }
        public PoseNodeId PlayerNodeId { get; }
        public MotionMatchingPoseSourceKind PoseSourceKind => MotionMatchingPoseSourceKind.MotionMatching;
        public MotionMatchingSelectionGeneration SelectionGeneration { get; }
        public PresentationPoseSourceGeneration SourceGeneration =>
            new PresentationPoseSourceGeneration(SelectionGeneration.Value);
        public ulong SourcePoseContinuityIdentity { get; }
        public ulong FrameSequence { get; }
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
            PresentationPoseSourceProviderId providerId,
            PresentationPoseSourceIndex sourceIndex,
            PoseNodeId playerNodeId,
            ulong frameSequence)
        {
            if (!selection.IsValid)
                throw new InvalidOperationException($"Motion Matching Pose Source cannot lower invalid selection '{selection.InvalidReason}'.");
            MotionMatchingSamplePayload sample = m_Database.GetSample(selection.SampleIndex);
            MotionMatchingClipBindingPayload clip = m_Database.GetClipBinding(sample.ClipBindingIndex) ??
                throw new InvalidOperationException("Motion Matching selected sample has no Clip binding.");
            if (clip.FootPlacementWeightCurve == null ||
                !clip.FootPlacementWeightCurve.ParameterId.Equals(FootPlacementWeightParameterId))
                throw new InvalidOperationException($"Motion Matching Pose Source requires Projection parameter '{FootPlacementWeightParameterName}' for the selected Clip sample.");
            var clipSamplePlan = new MotionMatchingClipSamplePlan(
                clip,
                sample.ClipBindingIndex,
                selection.PoseTime);
            var footPlacementWeight = new MotionMatchingPoseParameterSample(
                FootPlacementWeightParameterId,
                clip.FootPlacementWeightCurve.Sample(clipSamplePlan.NormalizedTime));
            var footPlacement = new AnimationFootPlacementSample(
                footPlacementWeight.Value,
                sample.LeftFoot.BindPredictionSource(
                    AnimationPredictedFootStepSample.SourceIdentity(clip.SourceClipId.Value),
                    selection.PoseTime.Cycle),
                sample.RightFoot.BindPredictionSource(
                    AnimationPredictedFootStepSample.SourceIdentity(clip.SourceClipId.Value),
                    selection.PoseTime.Cycle));
            return new MotionMatchingPoseSourceOutput(
                m_Database.ArtifactIdentity,
                m_Database.ProjectionDatabaseIndex,
                providerId,
                sourceIndex,
                playerNodeId,
                selection.Generation,
                frameSequence,
                clipSamplePlan,
                footPlacementWeight,
                footPlacement,
                selection.Plan.PlanId);
        }
    }
}
