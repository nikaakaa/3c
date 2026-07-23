using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationPresentationValidationCode
    {
        PresentationMissing,
        ProgramMissing,
        PosePayloadInvalid,
        SelectionInputInvalid,
        ProducerIdentityInvalid,
        ProducerDuplicate,
        ProducerChannelUnknown,
        BindingSourceInvalid,
        BindingDurationInvalid,
        BindingMarkerSyncInvalid,
        WorkspaceLayoutInvalid,
        ProjectionInvalid
    }

    public readonly struct AnimationPresentationValidationIssue
    {
        public AnimationPresentationValidationIssue(
            AnimationPresentationValidationCode code,
            string message,
            AnimationProducerId producerId = default,
            AnimationChannelId animationChannelId = default,
            PoseNodeId poseNodeId = default)
        {
            Code = code;
            Message = message ?? string.Empty;
            ProducerId = producerId;
            AnimationChannelId = animationChannelId;
            PoseNodeId = poseNodeId;
        }

        public AnimationPresentationValidationCode Code { get; }
        public string Message { get; }
        public AnimationProducerId ProducerId { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public PoseNodeId PoseNodeId { get; }
    }

    public readonly struct ResolvedAnimationSelectionInput
    {
        public ResolvedAnimationSelectionInput(CharacterPresentationSelectionInputEntry entry)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
            if (!IsValid)
                throw new ArgumentException("Resolved Animation Selection Input is invalid.");
        }

        public CharacterPresentationSelectionInputEntry Entry { get; }
        public int Index => Entry?.Index ?? -1;
        public PoseNodeId NodeId => Entry?.NodeId ?? default;
        public AnimationChannelId AnimationChannelId => Entry?.AnimationChannelId ?? default;
        public string ProgramProducerId => Entry?.ProgramProducerId ?? string.Empty;
        public bool MotionMatching => Entry?.MotionMatching == true;
        public AnimationSelectionAvailabilityPolicy Availability => Entry?.Availability ?? default;
        public bool IsValid => Entry != null && Index >= 0 && NodeId.IsValid && AnimationChannelId.IsValid &&
                               Enum.IsDefined(typeof(AnimationSelectionAvailabilityPolicy), Availability) &&
                               MotionMatching == !string.IsNullOrWhiteSpace(ProgramProducerId);
    }

    public readonly struct ResolvedAnimationProducerBinding
    {
        public ResolvedAnimationProducerBinding(
            int programProducerIndex,
            AnimationProducerId producerId,
            AnimationChannelId animationChannelId,
            CharacterPresentationAnimationBinding animation)
        {
            ProgramProducerIndex = programProducerIndex;
            ProducerId = producerId;
            AnimationChannelId = animationChannelId;
            Animation = animation ?? throw new ArgumentNullException(nameof(animation));
            SourceKind = AnimationPoseSourceKind.Timeline;
            BlendSpace = null;
            if (!IsValid)
                throw new ArgumentException("Resolved Animation Producer Binding is invalid.");
        }

        public ResolvedAnimationProducerBinding(
            int programProducerIndex,
            AnimationProducerId producerId,
            AnimationChannelId animationChannelId,
            CharacterAnimationBlendSpacePlan blendSpace)
        {
            ProgramProducerIndex = programProducerIndex;
            ProducerId = producerId;
            AnimationChannelId = animationChannelId;
            Animation = null;
            SourceKind = AnimationPoseSourceKind.BlendSpace;
            BlendSpace = blendSpace ?? throw new ArgumentNullException(nameof(blendSpace));
            if (!IsValid)
                throw new ArgumentException("Resolved Blend Space Producer Binding is invalid.");
        }

        public int ProgramProducerIndex { get; }
        public AnimationProducerId ProducerId { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public CharacterPresentationAnimationBinding Animation { get; }
        public AnimationPoseSourceKind SourceKind { get; }
        public CharacterAnimationBlendSpacePlan BlendSpace { get; }
        public TransitionAssetBase Source => Animation?.Source;
        public int AuthoredClipCount => Animation?.Clips.Count ?? BlendSpace?.Samples.Count ?? 0;
        public bool UsesMixer => AuthoredClipCount > 1;
        public bool IsValid => ProgramProducerIndex >= 0 && ProducerId.IsValid && AnimationChannelId.IsValid &&
                               (SourceKind == AnimationPoseSourceKind.Timeline && Animation != null && Source && Source.IsValid &&
                                AuthoredClipCount > 0 && float.IsFinite(Animation.DurationSeconds) && Animation.DurationSeconds > 0f ||
                                SourceKind == AnimationPoseSourceKind.BlendSpace && Animation == null && BlendSpace != null && AuthoredClipCount > 0);
    }

    public sealed class CharacterAnimationPresentationBindingIndex
    {
        readonly Dictionary<PoseNodeId, ResolvedAnimationSelectionInput> m_SelectionInputs =
            new Dictionary<PoseNodeId, ResolvedAnimationSelectionInput>();
        readonly Dictionary<AnimationChannelId, List<ResolvedAnimationSelectionInput>> m_SelectionInputsByChannel =
            new Dictionary<AnimationChannelId, List<ResolvedAnimationSelectionInput>>();
        readonly Dictionary<AnimationProducerId, ResolvedAnimationProducerBinding> m_Bindings =
            new Dictionary<AnimationProducerId, ResolvedAnimationProducerBinding>();
        readonly List<AnimationPresentationValidationIssue> m_Issues =
            new List<AnimationPresentationValidationIssue>();

        public bool IsValid { get; private set; }
        public CharacterPresentationProjection Projection { get; private set; }
        public AnimationPoseRequestWorkspaceLayout WorkspaceLayout { get; private set; }
        public IReadOnlyDictionary<PoseNodeId, ResolvedAnimationSelectionInput> SelectionInputs => m_SelectionInputs;
        public IReadOnlyDictionary<AnimationProducerId, ResolvedAnimationProducerBinding> Bindings => m_Bindings;
        public IReadOnlyList<AnimationPresentationValidationIssue> Issues => m_Issues;

        public bool TryGetSelectionInput(PoseNodeId nodeId, out ResolvedAnimationSelectionInput input) =>
            m_SelectionInputs.TryGetValue(nodeId, out input);

        public IReadOnlyList<ResolvedAnimationSelectionInput> RequireSelectionInputs(AnimationChannelId channelId) =>
            m_SelectionInputsByChannel.TryGetValue(channelId, out List<ResolvedAnimationSelectionInput> inputs)
                ? inputs
                : throw new KeyNotFoundException($"Animation Channel '{channelId}' has no Selection Input in the Pose Plan.");

        public bool TryGetBinding(AnimationProducerId producerId, out ResolvedAnimationProducerBinding binding) =>
            m_Bindings.TryGetValue(producerId, out binding);

        public static CharacterAnimationPresentationBindingIndex Build(
            CharacterPresentationProjection projection,
            CharacterPresentationSemanticContract contract,
            List<string> errors)
        {
            var index = new CharacterAnimationPresentationBindingIndex();
            bool valid = index.BuildInternal(projection, contract, errors);
            index.IsValid = valid;
            if (valid)
            {
                try
                {
                    index.WorkspaceLayout = AnimationPoseRequestWorkspaceLayoutFactory.Create(index);
                    valid = index.WorkspaceLayout.IsValid;
                }
                catch (Exception exception)
                {
                    index.Report(AnimationPresentationValidationCode.WorkspaceLayoutInvalid, exception.Message, errors);
                    valid = false;
                }
            }
            index.IsValid = valid;
            return index;
        }

        bool BuildInternal(
            CharacterPresentationProjection projection,
            CharacterPresentationSemanticContract contract,
            List<string> errors)
        {
            if (projection == null)
            {
                Report(AnimationPresentationValidationCode.PresentationMissing, "Character Presentation Projection is missing.", errors);
                return false;
            }
            if (contract == null)
            {
                Report(AnimationPresentationValidationCode.ProgramMissing, "Animation Presentation requires a compiled Character Program contract.", errors);
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
            bool valid = CollectSelectionInputs(projection.PosePlan, errors);
            if (projection.Producers.Count != contract.Producers.Count)
            {
                Report(AnimationPresentationValidationCode.ProjectionInvalid, "Presentation producer count does not match the Program manifest.", errors);
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
                    Report(AnimationPresentationValidationCode.ProjectionInvalid, $"Presentation producer #{i} does not match the Program manifest.", errors);
                    valid = false;
                    continue;
                }
                if (producer.Kind != CharacterPresentationProducerKind.Animation)
                    continue;
                AnimationProducerId producerId = producer.ProducerId;
                if (!producerId.IsValid || !producerIds.Add(producerId))
                {
                    Report(AnimationPresentationValidationCode.ProducerIdentityInvalid, $"Animation producer '{producer.ProgramProducerIdentity}' has an invalid or duplicate identity.", errors, producerId, producer.AnimationChannelId);
                    valid = false;
                    continue;
                }
                if (!m_SelectionInputsByChannel.ContainsKey(producer.AnimationChannelId))
                {
                    Report(AnimationPresentationValidationCode.ProducerChannelUnknown, $"Animation producer '{producerId}' channel '{producer.AnimationChannelId}' has no Pose Plan Selection Input.", errors, producerId, producer.AnimationChannelId);
                    valid = false;
                    continue;
                }
                if (producer.AnimationSourceKind == AnimationPoseSourceKind.MotionMatching)
                    continue;
                if (producer.AnimationSourceKind == AnimationPoseSourceKind.BlendSpace)
                {
                    if (producer.BlendSpacePlanIndex < 0 || producer.BlendSpacePlanIndex >= projection.BlendSpaces.Count)
                    {
                        Report(AnimationPresentationValidationCode.BindingSourceInvalid, $"Animation producer '{producerId}' has an invalid Blend Space plan binding.", errors, producerId, producer.AnimationChannelId);
                        valid = false;
                        continue;
                    }
                    CharacterAnimationBlendSpacePlan blendSpace = projection.BlendSpaces[producer.BlendSpacePlanIndex];
                    try
                    {
                        blendSpace.RequireValid(projection.FootAnalysis != null && projection.FootAnalysis.IsEnabled);
                        var resolved = new ResolvedAnimationProducerBinding(
                            producer.ProgramProducerIndex,
                            producerId,
                            producer.AnimationChannelId,
                            blendSpace);
                        if (!m_Bindings.TryAdd(producerId, resolved))
                        {
                            Report(AnimationPresentationValidationCode.ProducerDuplicate, $"Animation producer '{producerId}' cannot be indexed uniquely.", errors, producerId, producer.AnimationChannelId);
                            valid = false;
                        }
                    }
                    catch (Exception exception)
                    {
                        Report(AnimationPresentationValidationCode.BindingSourceInvalid, $"Animation producer '{producerId}' Blend Space is invalid: {exception.Message}", errors, producerId, producer.AnimationChannelId);
                        valid = false;
                    }
                    continue;
                }
                CharacterPresentationAnimationBinding animation = producer.Animation;
                if (animation == null || !animation.Source || !animation.Source.IsValid || animation.Clips.Count == 0)
                {
                    Report(AnimationPresentationValidationCode.BindingSourceInvalid, $"Animation producer '{producerId}' has an invalid source binding.", errors, producerId, producer.AnimationChannelId);
                    valid = false;
                    continue;
                }
                if (!float.IsFinite(animation.DurationSeconds) || animation.DurationSeconds <= 0f)
                {
                    Report(AnimationPresentationValidationCode.BindingDurationInvalid, $"Animation producer '{producerId}' has an invalid Timeline duration.", errors, producerId, producer.AnimationChannelId);
                    valid = false;
                    continue;
                }
                string markerError = string.Empty;
                if (animation.MarkerSync == null || !animation.MarkerSync.TryValidate(out markerError))
                {
                    Report(AnimationPresentationValidationCode.BindingMarkerSyncInvalid, $"Animation producer '{producerId}' marker sync is invalid: {markerError}.", errors, producerId, producer.AnimationChannelId);
                    valid = false;
                    continue;
                }
                if (animation.MarkerSync.IsMarkerGroup && !ValidateMarkerGroup(producer, animation.MarkerSync, markerPairSets, producerId, errors))
                {
                    valid = false;
                    continue;
                }
                var binding = new ResolvedAnimationProducerBinding(producer.ProgramProducerIndex, producerId, producer.AnimationChannelId, animation);
                if (!m_Bindings.TryAdd(producerId, binding))
                {
                    Report(AnimationPresentationValidationCode.ProducerDuplicate, $"Animation producer '{producerId}' cannot be indexed uniquely.", errors, producerId, producer.AnimationChannelId);
                    valid = false;
                }
            }
            return valid;
        }

        bool CollectSelectionInputs(CharacterPresentationPosePlan plan, List<string> errors)
        {
            bool valid = true;
            for (int i = 0; i < plan.SelectionInputs.Count; i++)
            {
                CharacterPresentationSelectionInputEntry entry = plan.SelectionInputs[i];
                try
                {
                    var resolved = new ResolvedAnimationSelectionInput(entry);
                    if (!m_SelectionInputs.TryAdd(resolved.NodeId, resolved))
                        throw new InvalidOperationException($"Pose Plan duplicates Selection Input '{resolved.NodeId}'.");
                    if (!m_SelectionInputsByChannel.TryGetValue(resolved.AnimationChannelId, out List<ResolvedAnimationSelectionInput> values))
                    {
                        values = new List<ResolvedAnimationSelectionInput>();
                        m_SelectionInputsByChannel.Add(resolved.AnimationChannelId, values);
                    }
                    values.Add(resolved);
                }
                catch (Exception exception)
                {
                    Report(AnimationPresentationValidationCode.SelectionInputInvalid, exception.Message, errors, poseNodeId: entry?.NodeId ?? default);
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
            List<string> errors)
        {
            string key = producer.AnimationChannelId.Value + "\0" + markerSync.CanonicalGroupId;
            var directedPairs = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < markerSync.Segments.Count; i++)
                directedPairs.Add(AnimationMarkerSyncAuthoring.PairKey(markerSync.Segments[i].PreviousMarkerId, markerSync.Segments[i].NextMarkerId));
            if (!markerPairSets.TryGetValue(key, out HashSet<string> expected))
            {
                markerPairSets.Add(key, directedPairs);
                return true;
            }
            if (expected.SetEquals(directedPairs))
                return true;
            Report(AnimationPresentationValidationCode.BindingMarkerSyncInvalid, $"Animation producer '{producerId}' marker pairs differ inside channel/group '{producer.AnimationChannelId}/{markerSync.CanonicalGroupId}'.", errors, producerId, producer.AnimationChannelId);
            return false;
        }

        void Report(
            AnimationPresentationValidationCode code,
            string message,
            List<string> errors,
            AnimationProducerId producerId = default,
            AnimationChannelId animationChannelId = default,
            PoseNodeId poseNodeId = default)
        {
            var issue = new AnimationPresentationValidationIssue(code, message, producerId, animationChannelId, poseNodeId);
            m_Issues.Add(issue);
            errors?.Add(issue.Message);
        }
    }
}
