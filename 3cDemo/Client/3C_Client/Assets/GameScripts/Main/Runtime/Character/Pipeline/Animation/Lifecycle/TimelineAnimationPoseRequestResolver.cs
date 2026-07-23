using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal static class TimelineAnimationPoseRequestResolver
    {
        internal static bool TryResolve(
            CharacterAnimationPresentationBindingIndex bindings,
            AnimationPoseRequestWorkspace workspace,
            AnimationChannelId animationChannelId,
            AnimationPoseSourceId sourceId,
            ulong sourcePoseContinuityIdentity,
            ulong presentationRequestSequence,
            int programProducerIndex,
            float visualSampleTime,
            double continuousVisualTime,
            int cycle,
            float visualTimeScale,
            bool isTrackLooping,
            bool holdTerminalPose,
            out AnimationSourcePoseSample sourceSample)
        {
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));
            if (!bindings.IsValid || bindings.Projection == null || !animationChannelId.IsValid || !sourceId.IsValid ||
                sourceId.SourceKind != AnimationPoseSourceKind.Timeline || sourcePoseContinuityIdentity == 0 ||
                presentationRequestSequence == 0 || programProducerIndex < 0 ||
                !float.IsFinite(visualSampleTime) || visualSampleTime < 0f ||
                double.IsNaN(continuousVisualTime) || double.IsInfinity(continuousVisualTime) || continuousVisualTime < 0d ||
                cycle < 0 || !float.IsFinite(visualTimeScale) || visualTimeScale < 0f)
                throw new ArgumentException("Timeline Animation Selection identity or time is invalid.");

            CharacterPresentationProjection projection = bindings.Projection;
            if ((uint)programProducerIndex >= (uint)projection.Producers.Count)
                throw new ArgumentOutOfRangeException(nameof(programProducerIndex));
            CharacterPresentationProducerEntry producer = projection.Producers[programProducerIndex];
            if (producer == null || !producer.IsValid || producer.ProgramProducerIndex != programProducerIndex ||
                producer.Kind != CharacterPresentationProducerKind.Animation ||
                producer.AnimationSourceKind != AnimationPoseSourceKind.Timeline ||
                producer.AnimationChannelId != animationChannelId ||
                !producer.ProducerId.Equals(sourceId.PlaybackId.ProducerId))
                throw new InvalidOperationException("Timeline Animation Selection does not match its Projection producer.");
            if (!bindings.TryGetBinding(producer.ProducerId, out ResolvedAnimationProducerBinding producerBinding) ||
                producerBinding.ProgramProducerIndex != programProducerIndex ||
                producerBinding.AnimationChannelId != animationChannelId)
                throw new InvalidOperationException("Timeline Animation Selection has no formal source binding.");

            CharacterPresentationAnimationBinding animation = producer.Animation;
            CharacterPresentationPosePlan plan = projection.PosePlan;
            int parameterCount = plan.Parameters.Count;
            AnimationPoseRequestWorkspaceRow row = workspace.PrepareRow(sourceId);
            workspace.RequireCurrent(row);
            if (row.ClipCapacity < animation.Clips.Count || row.ParameterCount != parameterCount)
                throw new InvalidOperationException("Timeline Animation Selection workspace does not match the Pose Plan.");
            for (int i = 0; i < parameterCount; i++)
            {
                row.PoseParameters[row.ParameterOffset + i] = plan.Parameters[i].DefaultValue;
                row.PoseParameterAvailability[row.ParameterOffset + i] = 1;
            }

            int footPlacementWeightIndex = plan.RequireParameterIndex(AnimationPoseParameterIds.FootPlacementWeight);
            float resolvedSampleTime = visualSampleTime;
            double resolvedContinuousTime = continuousVisualTime;
            int resolvedCycle = cycle;
            float resolvedTimeScale = visualTimeScale;
            bool resolvedLooping = isTrackLooping;
            int clipCount = animation.Sample(
                resolvedSampleTime,
                resolvedCycle,
                resolvedLooping,
                resolvedTimeScale,
                row.Clips,
                row.ClipOffset,
                out AnimationFootPlacementSample footPlacement);
            if (clipCount == 0 && holdTerminalPose)
            {
                resolvedSampleTime = Math.Max(0f, animation.DurationSeconds - 1f / 60000f);
                resolvedContinuousTime = resolvedSampleTime;
                resolvedCycle = 0;
                resolvedTimeScale = 0f;
                resolvedLooping = false;
                clipCount = animation.Sample(
                    resolvedSampleTime,
                    resolvedCycle,
                    resolvedLooping,
                    resolvedTimeScale,
                    row.Clips,
                    row.ClipOffset,
                    out footPlacement);
                if (clipCount == 0)
                    throw new InvalidOperationException("Retained Timeline source has no terminal pose to hold during Blend Stack exit.");
            }
            if (clipCount == 0)
            {
                sourceSample = default;
                return false;
            }
            if (clipCount < 0 || clipCount > row.ClipCapacity || !footPlacement.IsValid)
                throw new InvalidOperationException("Timeline source produced an invalid pose sample.");
            row.PoseParameters[row.ParameterOffset + footPlacementWeightIndex] = footPlacement.Weight;
            workspace.RequireCurrent(row);

            var selection = new AnimationSelectionFrame(
                animationChannelId,
                sourceId,
                sourcePoseContinuityIdentity,
                presentationRequestSequence,
                programProducerIndex,
                animation.MarkerBindingId,
                resolvedSampleTime,
                resolvedContinuousTime,
                resolvedCycle,
                resolvedLooping,
                resolvedTimeScale,
                new AnimationReadOnlyBuffer<ClipSamplePlan>(row.Clips, row.ClipOffset, clipCount, workspace, row.LeaseGeneration),
                new PresentationParameterPageId(row.LeaseGeneration),
                new AnimationReadOnlyBuffer<float>(row.PoseParameters, row.ParameterOffset, parameterCount, workspace, row.LeaseGeneration),
                new AnimationReadOnlyBuffer<byte>(row.PoseParameterAvailability, row.ParameterOffset, parameterCount, workspace, row.LeaseGeneration));
            sourceSample = new AnimationSourcePoseSample(selection, footPlacement.Left, footPlacement.Right, true);
            return true;
        }
    }
}
