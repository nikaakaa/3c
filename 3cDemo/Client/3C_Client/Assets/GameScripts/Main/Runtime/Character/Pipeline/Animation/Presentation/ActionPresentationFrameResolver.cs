using System;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    internal static class ActionPresentationSampleResolver
    {
        internal static ActionAnimationPlaybackFrame Resolve(
            ActionPresentationSampleWorkspace workspace,
            ActionAnimationBindingIndex bindings,
            in ResolvedActionAnimationBinding binding,
            in ActionAnimationPlaybackLifecycleFrame lifecycle,
            in ProjectedActionPresentationSample projected,
            int footPlacementWeightParameterIndex)
        {
            if (workspace == null ||
                bindings == null ||
                !binding.IsValid ||
                !projected.IsValid ||
                !lifecycle.PlaybackId.Equals(projected.PlaybackId) ||
                !binding.ProducerId.Equals(projected.PlaybackId.ProducerId) ||
                lifecycle.AnimationChannelId != binding.AnimationChannelId ||
                footPlacementWeightParameterIndex < 0)
            {
                throw new ArgumentException(
                    "Action presentation sample resolution is invalid.");
            }

            ActionPresentationSampleWorkspaceRow row =
                workspace.Prepare(projected.PlaybackId);
            CharacterPresentationPosePlan posePlan =
                bindings.Projection.PosePlan;
            int parameterCount = row.ParameterCount;
            if (posePlan == null ||
                parameterCount != posePlan.Parameters.Count ||
                parameterCount <= footPlacementWeightParameterIndex)
                throw new InvalidOperationException(
                    "Action sample workspace does not match the compiled Pose parameters.");

            for (int i = 0; i < parameterCount; i++)
            {
                row.PoseParameters[row.ParameterOffset + i] =
                    posePlan.Parameters[i].DefaultValue;
                row.PoseParameterAvailability[row.ParameterOffset + i] = 1;
            }

            PresentationPoseSampleTime sampleTime = projected.ProjectedRawSample;
            int clipCount = binding.Animation.Sample(
                sampleTime.SampleTime,
                sampleTime.Cycle,
                sampleTime.Loop,
                sampleTime.TimeScale,
                row.Clips,
                row.ClipOffset,
                out AnimationFootPlacementSample footPlacement);
            if (clipCount <= 0 ||
                clipCount > row.ClipCapacity ||
                !footPlacement.IsValid)
            {
                throw new InvalidOperationException(
                    $"Action playback '{projected.PlaybackId}' has no valid pose sample.");
            }
            row.PoseParameters[
                row.ParameterOffset + footPlacementWeightParameterIndex] =
                    footPlacement.Weight;
            row.FootFeatures[row.FootFeatureOffset] = footPlacement.Left;
            row.FootFeatures[row.FootFeatureOffset + 1] = footPlacement.Right;

            return new ActionAnimationPlaybackFrame(
                lifecycle.LatestEventId,
                lifecycle.PlaybackId,
                lifecycle.ActionInstanceId,
                lifecycle.SourcePoseContinuityIdentity,
                lifecycle.AnimationChannelId,
                lifecycle.ProgramProducerId,
                lifecycle.LatestCommandSequence,
                lifecycle.Phase,
                lifecycle.LatestCommittedRawSample,
                projected.ProjectedRawSample,
                projected.RetentionProjection,
                new AnimationReadOnlyBuffer<ClipSamplePlan>(
                    row.Clips,
                    row.ClipOffset,
                    clipCount,
                    workspace,
                    row.LeaseIdentity),
                new PresentationParameterPageId(row.LeaseIdentity),
                new AnimationReadOnlyBuffer<float>(
                    row.PoseParameters,
                    row.ParameterOffset,
                    parameterCount,
                    workspace,
                    row.LeaseIdentity),
                new AnimationReadOnlyBuffer<byte>(
                    row.PoseParameterAvailability,
                    row.ParameterOffset,
                    parameterCount,
                    workspace,
                    row.LeaseIdentity),
                new AnimationReadOnlyBuffer<AnimationFootFeatureSample>(
                    row.FootFeatures,
                    row.FootFeatureOffset,
                    2,
                    workspace,
                    row.LeaseIdentity));
        }
    }
}
