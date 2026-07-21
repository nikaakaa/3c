using System;
using System.Collections.Generic;
using Animancer;
using Animancer.TransitionLibraries;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationPresentationValidationCode
    {
        PresentationMissing,
        ProgramMissing,
        PosePayloadInvalid,
        PoseSlotMissing,
        PoseSlotDuplicate,
        AnimationChannelDuplicate,
        ProducerIdentityInvalid,
        ProducerDuplicate,
        ProducerChannelUnknown,
        BindingSourceInvalid,
        BindingDurationInvalid,
        BindingMarkerSyncInvalid,
        ProjectionInvalid
    }

    public readonly struct AnimationPresentationValidationIssue
    {
        public AnimationPresentationValidationIssue(
            AnimationPresentationValidationCode code,
            string message,
            AnimationProducerId producerId = default,
            AnimationChannelId animationChannelId = default,
            PoseSlotId poseSlotId = default)
        {
            Code = code;
            Message = message ?? string.Empty;
            ProducerId = producerId;
            AnimationChannelId = animationChannelId;
            PoseSlotId = poseSlotId;
        }

        public AnimationPresentationValidationCode Code { get; }
        public string Message { get; }
        public AnimationProducerId ProducerId { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public PoseSlotId PoseSlotId { get; }
    }

    public readonly struct ResolvedAnimationPoseSlot
    {
        public ResolvedAnimationPoseSlot(
            int index,
            CharacterPresentationPoseSlotProgramEntry programEntry,
            AnimationBlendSlotPayload blendPayload)
        {
            if (index < 0 || programEntry == null || blendPayload == null ||
                programEntry.Index != index || programEntry.PoseSlotId != blendPayload.PoseSlotId ||
                programEntry.AnimationChannelId != blendPayload.AnimationChannelId ||
                programEntry.OutputPolicy != blendPayload.OutputPolicy)
            {
                throw new ArgumentException("Resolved Animation Pose Slot is invalid.");
            }
            Index = index;
            PoseSlotId = programEntry.PoseSlotId;
            AnimationChannelId = programEntry.AnimationChannelId;
            OutputPolicy = programEntry.OutputPolicy;
            BlendPayload = blendPayload;
        }

        public int Index { get; }
        public PoseSlotId PoseSlotId { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public PoseSlotOutputPolicy OutputPolicy { get; }
        public AnimationBlendSlotPayload BlendPayload { get; }
    }

    public readonly struct ResolvedAnimationProducerBinding
    {
        public ResolvedAnimationProducerBinding(
            int programProducerIndex,
            AnimationProducerId producerId,
            AnimationChannelId animationChannelId,
            PoseSlotId poseSlotId,
            TransitionAssetBase source,
            int authoredClipCount)
        {
            ProgramProducerIndex = programProducerIndex;
            ProducerId = producerId;
            AnimationChannelId = animationChannelId;
            PoseSlotId = poseSlotId;
            Source = source;
            AuthoredClipCount = authoredClipCount;
        }

        public int ProgramProducerIndex { get; }
        public AnimationProducerId ProducerId { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public PoseSlotId PoseSlotId { get; }
        public TransitionAssetBase Source { get; }
        public int AuthoredClipCount { get; }
        public bool UsesMixer => AuthoredClipCount > 1;
        public bool IsValid => ProgramProducerIndex >= 0 && ProducerId.IsValid && AnimationChannelId.IsValid &&
                               PoseSlotId.IsValid && Source && Source.IsValid && AuthoredClipCount > 0;
    }

    public sealed class CharacterAnimationPresentationBindingIndex
    {
        readonly Dictionary<PoseSlotId, ResolvedAnimationPoseSlot> m_Slots =
            new Dictionary<PoseSlotId, ResolvedAnimationPoseSlot>();
        readonly Dictionary<AnimationChannelId, ResolvedAnimationPoseSlot> m_Channels =
            new Dictionary<AnimationChannelId, ResolvedAnimationPoseSlot>();
        readonly Dictionary<AnimationProducerId, ResolvedAnimationProducerBinding> m_Bindings =
            new Dictionary<AnimationProducerId, ResolvedAnimationProducerBinding>();
        readonly List<AnimationPresentationValidationIssue> m_Issues =
            new List<AnimationPresentationValidationIssue>();

        public bool IsValid { get; private set; }
        public CharacterPresentationProjection Projection { get; private set; }
        public IReadOnlyDictionary<PoseSlotId, ResolvedAnimationPoseSlot> Slots => m_Slots;
        public IReadOnlyDictionary<AnimationChannelId, ResolvedAnimationPoseSlot> Channels => m_Channels;
        public IReadOnlyDictionary<AnimationProducerId, ResolvedAnimationProducerBinding> Bindings => m_Bindings;
        public IReadOnlyList<AnimationPresentationValidationIssue> Issues => m_Issues;

        public bool TryGetSlot(PoseSlotId poseSlotId, out ResolvedAnimationPoseSlot slot) =>
            m_Slots.TryGetValue(poseSlotId, out slot);

        public bool TryGetSlot(AnimationChannelId animationChannelId, out ResolvedAnimationPoseSlot slot) =>
            m_Channels.TryGetValue(animationChannelId, out slot);

        public bool TryGetBinding(AnimationProducerId producerId, out ResolvedAnimationProducerBinding binding) =>
            m_Bindings.TryGetValue(producerId, out binding);

        public static CharacterAnimationPresentationBindingIndex Build(
            CharacterPresentationProjection projection,
            CharacterPresentationSemanticContract contract,
            List<string> errors)
        {
            var index = new CharacterAnimationPresentationBindingIndex();
            index.IsValid = index.BuildInternal(projection, contract, errors);
            return index;
        }

        bool BuildInternal(
            CharacterPresentationProjection projection,
            CharacterPresentationSemanticContract contract,
            List<string> errors)
        {
            if (projection == null)
            {
                Report(AnimationPresentationValidationCode.PresentationMissing,
                    "Character Presentation Projection is missing.", errors);
                return false;
            }
            if (contract == null)
            {
                Report(AnimationPresentationValidationCode.ProgramMissing,
                    "Animation Presentation validation requires a compiled Character Simulation Program.", errors);
                return false;
            }
            try
            {
                projection.RequireContract(contract);
                projection.RequirePosePayload();
            }
            catch (Exception exception)
            {
                Report(AnimationPresentationValidationCode.ProjectionInvalid, exception.Message, errors);
                return false;
            }

            Projection = projection;
            bool valid = CollectPoseSlots(projection, errors);
            if (projection.Producers.Count != contract.Producers.Count)
            {
                Report(AnimationPresentationValidationCode.ProjectionInvalid,
                    "Character Presentation Projection producer count does not match the Program manifest.", errors);
                valid = false;
            }

            var producerIds = new HashSet<AnimationProducerId>();
            var markerPairSets = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            for (int i = 0; i < projection.Producers.Count; i++)
            {
                CharacterPresentationProducerEntry producer = projection.Producers[i];
                if (producer == null || producer.ProgramProducerIndex != i || i >= contract.Producers.Count ||
                    !string.Equals(producer.ProgramProducerIdentity, contract.Producers[i].Identity, StringComparison.Ordinal))
                {
                    Report(AnimationPresentationValidationCode.ProjectionInvalid,
                        $"Character Presentation Projection producer #{i} does not match the Program manifest.", errors);
                    valid = false;
                    continue;
                }
                if (producer.Kind != CharacterPresentationProducerKind.Animation)
                    continue;

                AnimationProducerId producerId = producer.ProducerId;
                CharacterPresentationAnimationBinding animation = producer.Animation;
                if (!producerId.IsValid || !producerIds.Add(producerId))
                {
                    Report(AnimationPresentationValidationCode.ProducerIdentityInvalid,
                        $"Animation producer '{producer.ProgramProducerIdentity}' has an invalid or duplicate identity.",
                        errors, producerId, producer.AnimationChannelId);
                    valid = false;
                    continue;
                }
                if (!m_Channels.TryGetValue(producer.AnimationChannelId, out ResolvedAnimationPoseSlot slot))
                {
                    Report(AnimationPresentationValidationCode.ProducerChannelUnknown,
                        $"Animation producer '{producerId}' references unknown Animation Channel '{producer.AnimationChannelId}'.",
                        errors, producerId, producer.AnimationChannelId);
                    valid = false;
                    continue;
                }
                if (animation == null || !animation.Source || !animation.Source.IsValid || animation.Clips.Count == 0)
                {
                    Report(AnimationPresentationValidationCode.BindingSourceInvalid,
                        $"Animation producer '{producerId}' has an invalid compiled source binding.",
                        errors, producerId, producer.AnimationChannelId, slot.PoseSlotId);
                    valid = false;
                    continue;
                }
                if (!float.IsFinite(animation.DurationSeconds) || animation.DurationSeconds <= 0f)
                {
                    Report(AnimationPresentationValidationCode.BindingDurationInvalid,
                        $"Animation producer '{producerId}' has an invalid compiled Timeline duration.",
                        errors, producerId, producer.AnimationChannelId, slot.PoseSlotId);
                    valid = false;
                    continue;
                }
                AnimationMarkerSyncBinding markerSync = animation.MarkerSync;
                string markerError = "binding is missing";
                if (markerSync == null || !markerSync.TryValidate(out markerError))
                {
                    Report(AnimationPresentationValidationCode.BindingMarkerSyncInvalid,
                        $"Animation producer '{producerId}' has invalid marker sync data: {markerError}.",
                        errors, producerId, producer.AnimationChannelId, slot.PoseSlotId);
                    valid = false;
                    continue;
                }
                if (markerSync.IsMarkerGroup &&
                    !ValidateMarkerGroup(producer, markerSync, markerPairSets, producerId, slot.PoseSlotId, errors))
                {
                    valid = false;
                    continue;
                }

                var binding = new ResolvedAnimationProducerBinding(
                    producer.ProgramProducerIndex,
                    producerId,
                    producer.AnimationChannelId,
                    slot.PoseSlotId,
                    animation.Source,
                    animation.Clips.Count);
                if (!binding.IsValid || !m_Bindings.TryAdd(producerId, binding))
                {
                    Report(AnimationPresentationValidationCode.ProducerDuplicate,
                        $"Animation producer '{producerId}' cannot be indexed uniquely.",
                        errors, producerId, producer.AnimationChannelId, slot.PoseSlotId);
                    valid = false;
                }
            }
            return valid;
        }

        bool CollectPoseSlots(CharacterPresentationProjection projection, List<string> errors)
        {
            bool valid = true;
            for (int i = 0; i < projection.PoseProgram.Slots.Count; i++)
            {
                CharacterPresentationPoseSlotProgramEntry programSlot = projection.PoseProgram.Slots[i];
                AnimationBlendSlotPayload blendSlot;
                try
                {
                    blendSlot = projection.RequireBlendSlot(programSlot.PoseSlotId);
                }
                catch (Exception exception)
                {
                    Report(AnimationPresentationValidationCode.PosePayloadInvalid, exception.Message, errors);
                    valid = false;
                    continue;
                }
                var resolved = new ResolvedAnimationPoseSlot(i, programSlot, blendSlot);
                if (!m_Slots.TryAdd(resolved.PoseSlotId, resolved))
                {
                    Report(AnimationPresentationValidationCode.PoseSlotDuplicate,
                        $"Animation Presentation duplicates Pose Slot '{resolved.PoseSlotId}'.",
                        errors, poseSlotId: resolved.PoseSlotId);
                    valid = false;
                }
                if (!m_Channels.TryAdd(resolved.AnimationChannelId, resolved))
                {
                    Report(AnimationPresentationValidationCode.AnimationChannelDuplicate,
                        $"Animation Presentation duplicates Animation Channel '{resolved.AnimationChannelId}'.",
                        errors, animationChannelId: resolved.AnimationChannelId, poseSlotId: resolved.PoseSlotId);
                    valid = false;
                }
            }
            return valid;
        }

        bool ValidateMarkerGroup(
            CharacterPresentationProducerEntry producer,
            AnimationMarkerSyncBinding markerSync,
            Dictionary<string, HashSet<string>> markerPairSets,
            AnimationProducerId producerId,
            PoseSlotId poseSlotId,
            List<string> errors)
        {
            string markerGroupKey = producer.AnimationChannelId.Value + "\0" + markerSync.CanonicalGroupId;
            var directedPairs = new HashSet<string>(StringComparer.Ordinal);
            for (int segmentIndex = 0; segmentIndex < markerSync.Segments.Count; segmentIndex++)
            {
                AnimationMarkerSyncSegmentOccurrence segment = markerSync.Segments[segmentIndex];
                directedPairs.Add(AnimationMarkerSyncAuthoring.PairKey(segment.PreviousMarkerId, segment.NextMarkerId));
            }
            if (!markerPairSets.TryGetValue(markerGroupKey, out HashSet<string> expectedPairs))
            {
                markerPairSets.Add(markerGroupKey, directedPairs);
                return true;
            }
            if (expectedPairs.SetEquals(directedPairs))
                return true;
            Report(AnimationPresentationValidationCode.BindingMarkerSyncInvalid,
                $"Animation producer '{producerId}' does not match directed marker pairs for Animation Channel/group '{producer.AnimationChannelId}/{markerSync.CanonicalGroupId}'.",
                errors, producerId, producer.AnimationChannelId, poseSlotId);
            return false;
        }

        void Report(
            AnimationPresentationValidationCode code,
            string message,
            List<string> errors,
            AnimationProducerId producerId = default,
            AnimationChannelId animationChannelId = default,
            PoseSlotId poseSlotId = default)
        {
            var issue = new AnimationPresentationValidationIssue(code, message, producerId, animationChannelId, poseSlotId);
            m_Issues.Add(issue);
            errors?.Add(issue.Message);
        }
    }
}
