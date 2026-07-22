using System;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    internal static class MotionMatchingResolvedPoseRequestFactory
    {
        internal static ResolvedAnimationPoseRequest Create(
            in MotionMatchingPoseSourceOutput output,
            CharacterPresentationPoseProgram poseProgram,
            ClipSamplePlan[] clipWorkspace,
            int clipOffset,
            float[] parameterWorkspace,
            int parameterOffset,
            AnimationBlendTransitionIdentity exactTransitionIdentity)
        {
            if (!output.AnimationChannelId.IsValid || !output.PoseSlotId.IsValid || !output.PlaybackId.IsValid ||
                output.PoseSourceKind != MotionMatchingPoseSourceKind.MotionMatching || !output.SelectionGeneration.IsValid ||
                output.SourcePoseContinuityIdentity == 0 || output.SourcePoseContinuityIdentity != output.SelectionGeneration.Value ||
                output.PresentationRequestSequence == 0 || output.ProgramProducerIndex < 0 ||
                string.IsNullOrWhiteSpace(output.ProgramProducerId) || !output.PlanId.IsValid)
            {
                throw new ArgumentException("Motion Matching Pose Source output identity is invalid.", nameof(output));
            }

            MotionMatchingClipSamplePlan sourceClip = output.ClipSamplePlan;
            float clipLength = sourceClip.Clip ? sourceClip.Clip.length : 0f;
            bool loopTimeValid = sourceClip.IsLooping
                ? sourceClip.Cycle == 0
                    ? sourceClip.ContinuousVisualTime == sourceClip.ClipTime
                    : sourceClip.ContinuousVisualTime > sourceClip.ClipTime
                : sourceClip.Cycle == 0 && sourceClip.ContinuousVisualTime == sourceClip.ClipTime;
            if (!sourceClip.SourceClipId.IsValid || sourceClip.ClipBindingIndex < 0 || !sourceClip.Clip ||
                !float.IsFinite(clipLength) || clipLength <= 0f || !sourceClip.Time.IsValid ||
                !float.IsFinite(sourceClip.ClipTime) || sourceClip.ClipTime < 0f || sourceClip.ClipTime > clipLength ||
                double.IsNaN(sourceClip.ContinuousVisualTime) || double.IsInfinity(sourceClip.ContinuousVisualTime) ||
                sourceClip.ContinuousVisualTime < sourceClip.ClipTime || sourceClip.Cycle < 0 ||
                !float.IsFinite(sourceClip.VisualTimeScale) || sourceClip.VisualTimeScale < 0f ||
                !float.IsFinite(sourceClip.NormalizedTime) || sourceClip.NormalizedTime < 0f || sourceClip.NormalizedTime > 1f ||
                sourceClip.NormalizedTime != sourceClip.ClipTime / clipLength || !loopTimeValid ||
                sourceClip.AnimatorStateSpeed != 0f || !sourceClip.RootLocked)
            {
                throw new ArgumentException("Motion Matching Pose Source Clip plan is invalid.", nameof(output));
            }

            if (!output.FootPlacementWeight.IsValid ||
                !output.FootPlacementWeight.ParameterId.Equals(AnimationPoseParameterIds.FootPlacementWeight) ||
                !output.FootFeatures.IsValid || !output.FootFeatures.Left.IsValid || !output.FootFeatures.Right.IsValid ||
                output.FootFeatures.Weight != output.FootPlacementWeight.Value)
            {
                throw new ArgumentException("Motion Matching Pose Source Foot payload is invalid.", nameof(output));
            }

            if (poseProgram == null)
                throw new ArgumentNullException(nameof(poseProgram));
            int parameterCount = poseProgram.Parameters.Count;
            if (string.IsNullOrEmpty(poseProgram.PoseGraphId) || string.IsNullOrEmpty(poseProgram.ContentRevision) ||
                string.IsNullOrEmpty(poseProgram.ProgramHash) || string.IsNullOrEmpty(poseProgram.RigId) ||
                string.IsNullOrEmpty(poseProgram.RigRevision) || poseProgram.BoneCount <= 0 ||
                poseProgram.LeftFootBoneIndex < 0 || poseProgram.LeftFootBoneIndex >= poseProgram.BoneCount ||
                poseProgram.RightFootBoneIndex < 0 || poseProgram.RightFootBoneIndex >= poseProgram.BoneCount ||
                poseProgram.Slots.Count == 0 || parameterCount == 0 || poseProgram.Operations.Count == 0 ||
                poseProgram.OutputOperationIndex < 0 || poseProgram.OutputOperationIndex >= poseProgram.Operations.Count ||
                poseProgram.PoseValueWorkspaceCount <= 0 || poseProgram.ParameterWorkspaceCount < parameterCount ||
                poseProgram.ContributionWorkspaceCount <= 0 || poseProgram.FrameCacheCount <= 0)
            {
                throw new ArgumentException("Character Presentation Pose Program is invalid.", nameof(poseProgram));
            }

            CharacterPresentationPoseSlotProgramEntry slot = poseProgram.RequireSlot(output.AnimationChannelId);
            if (slot.Index < 0 || !slot.PoseSlotId.Equals(output.PoseSlotId))
                throw new InvalidOperationException("Motion Matching Pose Source Channel and Pose Slot do not match the Pose Program.");
            for (int i = 0; i < parameterCount; i++)
            {
                CharacterPresentationPoseParameterProgramEntry parameter = poseProgram.Parameters[i];
                if (parameter == null || parameter.Index != i || !parameter.ParameterId.IsValid || !float.IsFinite(parameter.DefaultValue))
                    throw new InvalidOperationException($"Pose Program parameter #{i} is invalid.");
                for (int previous = 0; previous < i; previous++)
                {
                    if (poseProgram.Parameters[previous].ParameterId.Equals(parameter.ParameterId))
                        throw new InvalidOperationException($"Pose Program parameter #{i} duplicates a stable identity.");
                }
            }

            int footPlacementWeightIndex = poseProgram.RequireParameterIndex(AnimationPoseParameterIds.FootPlacementWeight);
            if ((uint)footPlacementWeightIndex >= (uint)parameterCount)
                throw new InvalidOperationException("Pose Program Foot Placement Weight parameter index is outside the dense row.");
            if (clipWorkspace == null)
                throw new ArgumentNullException(nameof(clipWorkspace));
            if ((uint)clipOffset >= (uint)clipWorkspace.Length)
                throw new ArgumentOutOfRangeException(nameof(clipOffset));
            if (parameterWorkspace == null)
                throw new ArgumentNullException(nameof(parameterWorkspace));
            if (parameterOffset < 0 || parameterOffset > parameterWorkspace.Length - parameterCount)
                throw new ArgumentOutOfRangeException(nameof(parameterOffset));
            if (!exactTransitionIdentity.IsValid || !exactTransitionIdentity.PoseSlotId.Equals(output.PoseSlotId) ||
                exactTransitionIdentity.TargetEmpty || exactTransitionIdentity.TargetProducerIndex != output.ProgramProducerIndex)
            {
                throw new ArgumentException("Motion Matching exact transition does not target the selected Pose Slot producer.", nameof(exactTransitionIdentity));
            }

            clipWorkspace[clipOffset] = new ClipSamplePlan(
                sourceClip.ClipBindingIndex,
                sourceClip.Clip,
                sourceClip.ClipTime,
                sourceClip.ContinuousVisualTime,
                sourceClip.NormalizedTime,
                1f,
                sourceClip.IsLooping);
            for (int i = 0; i < parameterCount; i++)
                parameterWorkspace[parameterOffset + i] = poseProgram.Parameters[i].DefaultValue;
            parameterWorkspace[parameterOffset + footPlacementWeightIndex] = output.FootPlacementWeight.Value;

            var sourceId = new AnimationPoseSourceId(
                output.PlaybackId,
                AnimationPoseSourceKind.MotionMatching,
                new AnimationPoseSelectionGeneration(output.SelectionGeneration.Value));
            return new ResolvedAnimationPoseRequest(
                output.AnimationChannelId,
                output.PoseSlotId,
                sourceId,
                output.SourcePoseContinuityIdentity,
                output.PresentationRequestSequence,
                output.ProgramProducerIndex,
                sourceClip.ClipTime,
                sourceClip.ContinuousVisualTime,
                sourceClip.Cycle,
                sourceClip.VisualTimeScale,
                new AnimationReadOnlyBuffer<ClipSamplePlan>(clipWorkspace, clipOffset, 1),
                new AnimationReadOnlyBuffer<float>(parameterWorkspace, parameterOffset, parameterCount),
                output.FootFeatures.Left,
                output.FootFeatures.Right,
                true,
                exactTransitionIdentity);
        }
    }
}
