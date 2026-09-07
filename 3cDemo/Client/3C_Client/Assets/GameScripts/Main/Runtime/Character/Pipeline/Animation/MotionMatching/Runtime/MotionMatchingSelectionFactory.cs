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
            CharacterPoseProgramImage plan = projection.PosePlan;
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
            sourceClip.RequireValid();
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
            if (clipBinding == null)
                throw new ArgumentException(
                    "Motion Matching source Clip binding does not match the Projection Database.",
                    nameof(output));
            clipBinding.RequireValid();
            if (!clipBinding.SourceClipId.Equals(
                    sourceClip.SourceClipId) ||
                clipBinding.Backend != sourceClip.Backend ||
                clipBinding.DurationSeconds != sourceClip.DurationSeconds ||
                clipBinding.IsLooping != sourceClip.SourceIsLooping ||
                clipBinding.RootLocked != sourceClip.RootLocked ||
                clipBinding.ResourceCatalogIndex != sourceClip.ResourceCatalogIndex ||
                clipBinding.GroupClipIndex != sourceClip.GroupClipIndex ||
                clipBinding.IsAcl != sourceClip.IsAcl)
            {
                throw new ArgumentException(
                    "Motion Matching source Clip binding does not match the Projection Database.",
                    nameof(output));
            }
            if (clipBinding.IsAcl)
            {
                if (clipBinding.Clip != null || sourceClip.Clip != null)
                    throw new ArgumentException(
                        "Motion Matching source ACL binding contains a Native Clip.",
                        nameof(output));
            }
            else if (!clipBinding.Clip || clipBinding.Clip != sourceClip.Clip)
            {
                throw new ArgumentException(
                    "Motion Matching source Native Clip binding does not match the Projection Database.",
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
            row.Clips[row.ClipOffset] = sourceClip.IsAcl
                ? new ClipSamplePlan(
                    sourceClip.ClipBindingIndex,
                    sourceClip.ResourceCatalogIndex,
                    sourceClip.GroupClipIndex,
                    sourceClip.DurationSeconds,
                    sourceClip.ClipTime,
                    sourceClip.ContinuousVisualTime,
                    sourceClip.NormalizedTime,
                    1f,
                    sourceClip.IsLooping)
                : new ClipSamplePlan(
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
            if (!sourceClip.IsAcl && clipBinding.NativeScalarPage != null)
            {
                CharacterAnimationScalarCurvePage scalarPage =
                    clipBinding.NativeScalarPage;
                scalarPage.RequireValid();
                if (scalarPage.ParameterCount != parameterCount)
                    throw new InvalidOperationException(
                        "Motion Matching Native Clip scalar page does not match the Pose Plan.");
                for (int i = 0; i < scalarPage.Tracks.Count; i++)
                {
                    CharacterAnimationScalarCurveTrack track =
                        scalarPage.Tracks[i];
                    if (track.ParameterIndex < 0 ||
                        track.ParameterIndex >= parameterCount)
                        throw new InvalidOperationException(
                            "Motion Matching Native Clip scalar page parameter index is outside the Pose Plan.");
                    row.PoseParameters[row.ParameterOffset + track.ParameterIndex] =
                        track.Sample(sourceClip.NormalizedTime);
                    row.PoseParameterAvailability[
                        row.ParameterOffset + track.ParameterIndex] = 1;
                }
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
