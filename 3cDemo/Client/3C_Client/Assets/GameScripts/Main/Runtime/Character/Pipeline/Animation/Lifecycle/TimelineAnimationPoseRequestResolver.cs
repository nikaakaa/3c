using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal static class TimelineAnimationPoseRequestResolver
    {
        internal static ResolvedAnimationPoseRequest Resolve(
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
            AnimationBlendTransitionIdentity exactTransitionIdentity)
        {
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));
            if (!bindings.IsValid || bindings.Projection == null)
                throw new ArgumentException("Animation Presentation Binding Index is invalid.", nameof(bindings));
            if (!animationChannelId.IsValid || !sourceId.IsValid ||
                sourceId.SourceKind != AnimationPoseSourceKind.Timeline ||
                sourcePoseContinuityIdentity == 0 || presentationRequestSequence == 0 ||
                programProducerIndex < 0 || !float.IsFinite(visualSampleTime) || visualSampleTime < 0f ||
                double.IsNaN(continuousVisualTime) || double.IsInfinity(continuousVisualTime) || continuousVisualTime < 0d ||
                cycle < 0 || !float.IsFinite(visualTimeScale) || visualTimeScale < 0f)
            {
                throw new ArgumentException("Timeline animation pose request identity or time is invalid.");
            }

            CharacterPresentationProjection projection = bindings.Projection;
            if ((uint)programProducerIndex >= (uint)projection.Producers.Count)
                throw new ArgumentOutOfRangeException(nameof(programProducerIndex));
            CharacterPresentationProducerEntry producer = projection.Producers[programProducerIndex];
            if (producer == null || !producer.IsValid || producer.ProgramProducerIndex != programProducerIndex ||
                producer.Kind != CharacterPresentationProducerKind.Animation ||
                !producer.AnimationChannelId.Equals(animationChannelId) ||
                !producer.ProducerId.Equals(sourceId.PlaybackId.ProducerId))
            {
                throw new InvalidOperationException("Timeline animation pose request does not match its Projection producer.");
            }

            CharacterPresentationAnimationBinding animation = producer.Animation;
            if (animation == null || !animation.Source || !animation.Source.IsValid || animation.Clips.Count == 0 ||
                !float.IsFinite(animation.DurationSeconds) || animation.DurationSeconds <= 0f)
            {
                throw new InvalidOperationException("Timeline animation Projection binding is invalid.");
            }
            if (!bindings.TryGetBinding(producer.ProducerId, out ResolvedAnimationProducerBinding producerBinding) ||
                !producerBinding.IsValid || producerBinding.ProgramProducerIndex != programProducerIndex ||
                !producerBinding.ProducerId.Equals(producer.ProducerId) ||
                !producerBinding.AnimationChannelId.Equals(animationChannelId) ||
                producerBinding.AuthoredClipCount != animation.Clips.Count ||
                !ReferenceEquals(producerBinding.Source, animation.Source))
            {
                throw new InvalidOperationException("Timeline animation Projection producer is not indexed by the formal binding.");
            }
            if (!bindings.TryGetSlot(animationChannelId, out ResolvedAnimationPoseSlot slot) ||
                !slot.PoseSlotId.Equals(producerBinding.PoseSlotId) || slot.BlendPayload == null)
            {
                throw new InvalidOperationException("Timeline Animation Channel does not resolve to its formal Pose Slot.");
            }

            CharacterPresentationPoseProgram poseProgram = projection.PoseProgram;
            if (poseProgram == null || poseProgram.Parameters.Count == 0)
                throw new InvalidOperationException("Timeline animation pose request requires a valid Pose Program parameter layout.");
            CharacterPresentationPoseSlotProgramEntry programSlot = poseProgram.RequireSlot(animationChannelId);
            if (programSlot.Index != slot.Index || !programSlot.PoseSlotId.Equals(slot.PoseSlotId) ||
                programSlot.OutputPolicy != slot.OutputPolicy)
            {
                throw new InvalidOperationException("Timeline Animation Channel and Pose Slot do not match the Pose Program.");
            }
            if (!exactTransitionIdentity.IsValid ||
                !exactTransitionIdentity.PoseSlotId.Equals(slot.PoseSlotId) ||
                exactTransitionIdentity.TargetEmpty ||
                exactTransitionIdentity.TargetProducerIndex != programProducerIndex)
            {
                throw new ArgumentException("Timeline exact transition does not target the selected Pose Slot producer.", nameof(exactTransitionIdentity));
            }
            AnimationBlendTransitionPayload transition = slot.BlendPayload.RequireTransition(
                exactTransitionIdentity.SourceProducerIndex,
                exactTransitionIdentity.SourceEmpty,
                exactTransitionIdentity.TargetProducerIndex,
                exactTransitionIdentity.TargetEmpty);
            if (transition.GetIdentity(slot.PoseSlotId) != exactTransitionIdentity)
                throw new InvalidOperationException("Timeline exact transition does not match the compiled Blend Slot transition.");

            AnimationPoseRequestWorkspaceRow row = workspace.PrepareRow(sourceId);
            workspace.RequireCurrent(row);
            int parameterCount = poseProgram.Parameters.Count;
            if (row.ClipCapacity < animation.Clips.Count || row.ParameterCount != parameterCount)
                throw new InvalidOperationException("Timeline animation pose request workspace row does not match the compiled layout.");
            for (int i = 0; i < parameterCount; i++)
            {
                CharacterPresentationPoseParameterProgramEntry parameter = poseProgram.Parameters[i];
                if (parameter == null || parameter.Index != i || !parameter.ParameterId.IsValid ||
                    !float.IsFinite(parameter.DefaultValue))
                {
                    throw new InvalidOperationException($"Pose Program parameter #{i} is invalid.");
                }
                row.PoseParameters[row.ParameterOffset + i] = parameter.DefaultValue;
            }

            int footPlacementWeightIndex = poseProgram.RequireParameterIndex(AnimationPoseParameterIds.FootPlacementWeight);
            if ((uint)footPlacementWeightIndex >= (uint)parameterCount)
                throw new InvalidOperationException("Pose Program Foot Placement Weight index is outside the dense parameter row.");
            int clipCount = animation.Sample(
                visualSampleTime,
                cycle,
                isTrackLooping,
                visualTimeScale,
                row.Clips,
                row.ClipOffset,
                out AnimationFootPlacementSample footPlacement);
            if (clipCount <= 0 || clipCount > row.ClipCapacity || !footPlacement.IsValid)
                throw new InvalidOperationException("Timeline animation Projection produced an invalid pose sample.");
            row.PoseParameters[row.ParameterOffset + footPlacementWeightIndex] = footPlacement.Weight;
            workspace.RequireCurrent(row);

            return new ResolvedAnimationPoseRequest(
                animationChannelId,
                slot.PoseSlotId,
                sourceId,
                sourcePoseContinuityIdentity,
                presentationRequestSequence,
                programProducerIndex,
                visualSampleTime,
                continuousVisualTime,
                cycle,
                visualTimeScale,
                new AnimationReadOnlyBuffer<ClipSamplePlan>(
                    row.Clips,
                    row.ClipOffset,
                    clipCount,
                    workspace,
                    row.LeaseGeneration),
                new AnimationReadOnlyBuffer<float>(
                    row.PoseParameters,
                    row.ParameterOffset,
                    parameterCount,
                    workspace,
                    row.LeaseGeneration),
                footPlacement.Left,
                footPlacement.Right,
                true,
                exactTransitionIdentity);
        }
    }
}
