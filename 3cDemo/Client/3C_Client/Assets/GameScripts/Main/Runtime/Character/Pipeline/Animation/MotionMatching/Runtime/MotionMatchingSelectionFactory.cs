using System;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    internal static class MotionMatchingSelectionFactory
    {
        internal static PresentationPoseSourceSample Create(
            in MotionMatchingPoseSourceOutput output,
            CharacterPresentationProjection projection,
            AnimationPoseRequestWorkspace workspace)
        {
            if (output.DatabaseIdentity == null ||
                !output.ProviderId.IsValid ||
                !output.SourceIndex.IsValid ||
                !output.PlayerNodeId.IsValid ||
                output.PoseSourceKind != MotionMatchingPoseSourceKind.MotionMatching || !output.SelectionGeneration.IsValid ||
                output.SourcePoseContinuityIdentity == 0 || output.SourcePoseContinuityIdentity != output.SelectionGeneration.Value ||
                output.FrameSequence == 0 || !output.PlanId.IsValid)
                throw new ArgumentException("Motion Matching source output identity is invalid.", nameof(output));
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));
            projection.RequirePosePayload();
            CharacterPresentationPosePlan plan = projection.PosePlan;
            MotionMatchingProjectionPayload motionMatching =
                projection.MotionMatching;
            if (motionMatching == null ||
                (uint)output.ProjectionDatabaseIndex >=
                (uint)motionMatching.DatabaseCount)
            {
                throw new ArgumentException(
                    "Motion Matching source Database identity is invalid.",
                    nameof(output));
            }
            MotionMatchingDatabasePayload database =
                motionMatching.GetDatabase(
                    output.ProjectionDatabaseIndex);
            if (database == null ||
                !database.ArtifactIdentity.EqualsExact(
                    output.DatabaseIdentity))
            {
                throw new ArgumentException(
                    "Motion Matching source Database artifact does not match the Projection.",
                    nameof(output));
            }

            MotionMatchingClipSamplePlan sourceClip = output.ClipSamplePlan;
            float clipLength = sourceClip.Clip ? sourceClip.Clip.length : 0f;
            if (!sourceClip.SourceClipId.IsValid || sourceClip.ClipBindingIndex < 0 || !sourceClip.Clip ||
                !float.IsFinite(clipLength) || clipLength <= 0f || !sourceClip.Time.IsValid ||
                !float.IsFinite(sourceClip.ClipTime) || sourceClip.ClipTime < 0f || sourceClip.ClipTime > clipLength ||
                double.IsNaN(sourceClip.ContinuousVisualTime) || double.IsInfinity(sourceClip.ContinuousVisualTime) ||
                sourceClip.ContinuousVisualTime < sourceClip.ClipTime || sourceClip.Cycle < 0 ||
                !float.IsFinite(sourceClip.VisualTimeScale) || sourceClip.VisualTimeScale < 0f ||
                !float.IsFinite(sourceClip.NormalizedTime) || sourceClip.NormalizedTime < 0f || sourceClip.NormalizedTime > 1f ||
                sourceClip.AnimatorStateSpeed != 0f || !sourceClip.RootLocked)
                throw new ArgumentException("Motion Matching source Clip plan is invalid.", nameof(output));
            if ((uint)sourceClip.ClipBindingIndex >=
                (uint)database.ClipBindingCount)
            {
                throw new ArgumentException(
                    "Motion Matching source Clip binding exceeds the Projection Database.",
                    nameof(output));
            }
            MotionMatchingClipBindingPayload clipBinding =
                database.GetClipBinding(
                    sourceClip.ClipBindingIndex);
            if (clipBinding == null ||
                !clipBinding.SourceClipId.Equals(
                    sourceClip.SourceClipId) ||
                clipBinding.Clip != sourceClip.Clip)
            {
                throw new ArgumentException(
                    "Motion Matching source Clip binding does not match the Projection Database.",
                    nameof(output));
            }
            if (!output.FootPlacementWeight.IsValid ||
                !output.FootPlacementWeight.ParameterId.Equals(AnimationPoseParameterIds.FootPlacementWeight) ||
                !output.FootFeatures.IsValid || output.FootFeatures.Weight != output.FootPlacementWeight.Value)
                throw new ArgumentException("Motion Matching source Foot payload is invalid.", nameof(output));

            int parameterCount = plan.Parameters.Count;
            int footPlacementWeightIndex = plan.RequireParameterIndex(AnimationPoseParameterIds.FootPlacementWeight);
            var sourceId = new AnimationPoseSourceId(
                output.SourceIndex,
                AnimationPoseSourceKind.MotionMatching,
                new AnimationPoseSelectionGeneration(output.SelectionGeneration.Value));
            AnimationPoseRequestWorkspaceRow row = workspace.PrepareRow(sourceId);
            workspace.RequireCurrent(row);
            if (row.ClipCapacity < 1 || row.ParameterCount != parameterCount)
                throw new InvalidOperationException("Motion Matching Selection workspace does not match the Pose Plan.");
            row.Clips[row.ClipOffset] = new ClipSamplePlan(
                sourceClip.ClipBindingIndex,
                sourceClip.Clip,
                sourceClip.ClipTime,
                sourceClip.ContinuousVisualTime,
                sourceClip.NormalizedTime,
                1f,
                sourceClip.IsLooping);
            for (int i = 0; i < parameterCount; i++)
            {
                row.PoseParameters[row.ParameterOffset + i] = plan.Parameters[i].DefaultValue;
                row.PoseParameterAvailability[row.ParameterOffset + i] = 1;
            }
            row.PoseParameters[row.ParameterOffset + footPlacementWeightIndex] = output.FootPlacementWeight.Value;
            workspace.RequireCurrent(row);

            var sampleTime = new PresentationPoseSampleTime(
                sourceClip.ClipTime,
                sourceClip.ContinuousVisualTime,
                sourceClip.Cycle,
                sourceClip.IsLooping,
                sourceClip.VisualTimeScale);
            return PresentationPoseSourceSample.Ready(
                output.ProviderId,
                output.PlayerNodeId,
                output.SourceIndex,
                AnimationPoseSourceKind.MotionMatching,
                output.ProjectionDatabaseIndex,
                output.SourceGeneration,
                output.SourcePoseContinuityIdentity,
                output.FrameSequence,
                sampleTime,
                sampleTime,
                new AnimationReadOnlyBuffer<ClipSamplePlan>(row.Clips, row.ClipOffset, 1, workspace, row.LeaseGeneration),
                new PresentationParameterPageId(row.LeaseGeneration),
                new AnimationReadOnlyBuffer<float>(row.PoseParameters, row.ParameterOffset, parameterCount, workspace, row.LeaseGeneration),
                new AnimationReadOnlyBuffer<byte>(row.PoseParameterAvailability, row.ParameterOffset, parameterCount, workspace, row.LeaseGeneration),
                output.FootFeatures.Left,
                output.FootFeatures.Right,
                true);
        }
    }
}
