using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed partial class CharacterPresentationProjection
    {
        [SerializeField] CharacterPresentationPosePlan m_PosePlan;
        [SerializeField] AnimationBlendCurveCatalogPayload m_BlendCurveCatalog;
        [SerializeField] AnimationBlendProfileCatalogPayload m_BlendProfileCatalog;
        [SerializeField] CharacterAnimationRigPayload m_Rig;
        [SerializeField] CharacterAnimationBlendSpacePlan[] m_BlendSpaces = Array.Empty<CharacterAnimationBlendSpacePlan>();
        [SerializeField] CharacterAnimationBlendSpacePlayerPlan[] m_BlendSpacePlayers = Array.Empty<CharacterAnimationBlendSpacePlayerPlan>();
        [NonSerialized] MotionMatchingProjectionPayload m_MotionMatching;
        [SerializeField] byte[] m_MotionMatchingPayload = Array.Empty<byte>();
        [SerializeField] UnityEngine.AnimationClip[] m_MotionMatchingClips = Array.Empty<UnityEngine.AnimationClip>();

        public CharacterPresentationPosePlan PosePlan => m_PosePlan;
        public AnimationBlendCurveCatalogPayload BlendCurveCatalog => m_BlendCurveCatalog;
        public AnimationBlendProfileCatalogPayload BlendProfileCatalog => m_BlendProfileCatalog;
        public CharacterAnimationRigPayload Rig => m_Rig;
        public IReadOnlyList<CharacterAnimationBlendSpacePlan> BlendSpaces => m_BlendSpaces ?? Array.Empty<CharacterAnimationBlendSpacePlan>();
        public IReadOnlyList<CharacterAnimationBlendSpacePlayerPlan> BlendSpacePlayers => m_BlendSpacePlayers ?? Array.Empty<CharacterAnimationBlendSpacePlayerPlan>();
        public MotionMatchingProjectionPayload MotionMatching => m_MotionMatching;

        public void OnBeforeSerialize()
        {
            m_MotionMatchingPayload = MotionMatchingProjectionPayloadCodec.Encode(
                m_MotionMatching,
                out m_MotionMatchingClips);
        }

        public void OnAfterDeserialize()
        {
            m_MotionMatching = MotionMatchingProjectionPayloadCodec.Decode(
                m_MotionMatchingPayload,
                m_MotionMatchingClips);
        }

        public AnimationBlendNodePayload RequireBlendNode(PoseNodeId nodeId) => PosePlan.RequireBlendNode(nodeId);

        internal static CharacterPresentationProjection Create(
            CharacterPresentationSemanticContract contract,
            CharacterPresentationPosePlan posePlan,
            AnimationBlendCurveCatalogPayload blendCurveCatalog,
            AnimationBlendProfileCatalogPayload blendProfileCatalog,
            CharacterAnimationRigPayload rig,
            MotionMatchingProjectionPayload motionMatching,
            CharacterAnimationBlendSpacePlan[] blendSpaces,
            CharacterAnimationBlendSpacePlayerPlan[] blendSpacePlayers,
            CharacterPresentationProducerEntry[] producers,
            AnimationFootAnalysisProjectionIdentity footAnalysis,
            string projectionRevision,
            EquipmentVisualProjectionBinding[] equipmentVisualBindings)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            var projection = new CharacterPresentationProjection
            {
                m_ProgramId = contract.ProgramId.Value,
                m_SourceRevision = contract.SourceRevision.Value,
                m_SemanticHash = contract.SemanticHash.ToString(),
                m_ContractHash = contract.ContractHash.ToString(),
                m_PosePlan = posePlan ?? throw new ArgumentNullException(nameof(posePlan)),
                m_BlendCurveCatalog = blendCurveCatalog ?? throw new ArgumentNullException(nameof(blendCurveCatalog)),
                m_BlendProfileCatalog = blendProfileCatalog ?? throw new ArgumentNullException(nameof(blendProfileCatalog)),
                m_Rig = rig ?? throw new ArgumentNullException(nameof(rig)),
                m_MotionMatching = motionMatching,
                m_BlendSpaces = blendSpaces ?? Array.Empty<CharacterAnimationBlendSpacePlan>(),
                m_BlendSpacePlayers = blendSpacePlayers ?? Array.Empty<CharacterAnimationBlendSpacePlayerPlan>(),
                m_Producers = producers ?? Array.Empty<CharacterPresentationProducerEntry>(),
                m_FootAnalysis = footAnalysis
            };
            projection.SetEquipmentProjection(
                projectionRevision,
                equipmentVisualBindings);
            projection.RequireContract(contract);
            projection.RequirePosePayload();
            return projection;
        }

        public void RequirePosePayload()
        {
            PosePlan?.RequireValid();
            Rig?.RequireValid();
            BlendCurveCatalog?.RequireValid();
            BlendProfileCatalog?.RequireValid(Rig?.Bones.Count ?? 0, Rig?.RigId, Rig?.RigRevision);
            if (PosePlan == null || Rig == null || BlendCurveCatalog == null || BlendProfileCatalog == null ||
                !string.Equals(PosePlan.RigId, Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(PosePlan.RigRevision, Rig.RigRevision, StringComparison.Ordinal) ||
                PosePlan.BoneCount != Rig.Bones.Count)
            {
                throw new InvalidOperationException("Character Presentation Projection Pose payload is incomplete or inconsistent.");
            }

            var blendNodes = new HashSet<PoseNodeId>();
            for (int i = 0; i < PosePlan.BlendNodes.Count; i++)
            {
                AnimationBlendNodePayload blend = PosePlan.BlendNodes[i];
                if (blend == null || !blendNodes.Add(blend.NodeId) || blend.StackPolicy == null)
                    throw new InvalidOperationException($"Character Presentation Projection Blend Stack #{i} is inconsistent.");
                blend.StackPolicy.RequireValid();
                for (int transitionIndex = 0; transitionIndex < blend.Transitions.Count; transitionIndex++)
                {
                    AnimationBlendTransitionPayload transition = blend.Transitions[transitionIndex];
                    if (transition == null)
                        throw new InvalidOperationException($"Character Presentation Projection Blend Stack '{blend.NodeId}' transition #{transitionIndex} is missing.");
                    transition.RequireValid(BlendCurveCatalog.Entries.Count, BlendProfileCatalog.Entries.Count);
                }
            }
            RequireMotionMatchingPayload();
            RequireBlendSpacePayload();
        }

        void RequireBlendSpacePayload()
        {
            var referencedPlans = new HashSet<int>();
            for (int i = 0; i < Producers.Count; i++)
            {
                CharacterPresentationProducerEntry producer = Producers[i];
                if (producer.Kind != CharacterPresentationProducerKind.Animation ||
                    producer.AnimationSourceKind != AnimationPoseSourceKind.BlendSpace)
                    continue;
                if (producer.BlendSpacePlanIndex < 0 || producer.BlendSpacePlanIndex >= BlendSpaces.Count)
                    throw new InvalidOperationException($"Blend Space producer '{producer.ProgramProducerIdentity}' has an invalid plan binding.");
                referencedPlans.Add(producer.BlendSpacePlanIndex);
                CharacterAnimationBlendSpacePlan plan = BlendSpaces[producer.BlendSpacePlanIndex];
                plan?.RequireValid(FootAnalysis != null && FootAnalysis.IsEnabled);
                if (plan == null || !string.Equals(plan.RigId, Rig.RigId, StringComparison.Ordinal) ||
                    !string.Equals(plan.RigRevision, Rig.RigRevision, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Blend Space producer '{producer.ProgramProducerIdentity}' plan Rig is inconsistent.");
                if (producer.AuthoredClipCount != plan.Samples.Count ||
                    !producer.SourceDurationSeconds.Equals(plan.ClockDurationSeconds) ||
                    !SameMarkerBinding(producer.MarkerSync, plan.MarkerSync))
                    throw new InvalidOperationException($"Blend Space producer '{producer.ProgramProducerIdentity}' source payload does not match its compiled plan.");
            }
            if (referencedPlans.Count != BlendSpaces.Count)
                throw new InvalidOperationException("Projection retains unreferenced or duplicated Blend Space plans.");
            int operationCount = 0;
            var playerNodes = new HashSet<PoseNodeId>();
            for (int i = 0; i < PosePlan.Operations.Count; i++)
            {
                if (PosePlan.Operations[i].Code == CharacterPoseOperationCode.BlendSpacePlayer)
                    operationCount++;
            }
            for (int i = 0; i < BlendSpacePlayers.Count; i++)
            {
                CharacterAnimationBlendSpacePlayerPlan player = BlendSpacePlayers[i];
                player?.RequireValid(this);
                if (player == null || !playerNodes.Add(player.NodeId))
                    throw new InvalidOperationException($"Projection Blend Space Player plan #{i} is missing or duplicated.");
            }
            if (operationCount != BlendSpacePlayers.Count)
                throw new InvalidOperationException("Projection Blend Space Player operation count is inconsistent.");
        }

        static bool SameMarkerBinding(AnimationMarkerSyncBinding left, AnimationMarkerSyncBinding right)
        {
            if (left == null || right == null || !left.TryValidate(out _) || !right.TryValidate(out _) ||
                left.Mode != right.Mode || !string.Equals(left.CanonicalGroupId, right.CanonicalGroupId, StringComparison.Ordinal) ||
                left.SequenceTopology != right.SequenceTopology || left.SyncRole != right.SyncRole ||
                left.DurationFrame != right.DurationFrame || !left.DurationSeconds.Equals(right.DurationSeconds) ||
                left.Markers.Count != right.Markers.Count || left.Segments.Count != right.Segments.Count)
                return false;
            for (int i = 0; i < left.Markers.Count; i++)
            {
                AnimationMarkerSyncMarkerBinding a = left.Markers[i];
                AnimationMarkerSyncMarkerBinding b = right.Markers[i];
                if (a == null || b == null || !string.Equals(a.AuthoringId, b.AuthoringId, StringComparison.Ordinal) ||
                    !string.Equals(a.MarkerId, b.MarkerId, StringComparison.Ordinal) || a.Frame != b.Frame ||
                    !a.TimeSeconds.Equals(b.TimeSeconds))
                    return false;
            }
            for (int i = 0; i < left.Segments.Count; i++)
            {
                AnimationMarkerSyncSegmentOccurrence a = left.Segments[i];
                AnimationMarkerSyncSegmentOccurrence b = right.Segments[i];
                if (a == null || b == null || a.OccurrenceIndex != b.OccurrenceIndex ||
                    a.PreviousMarkerIndex != b.PreviousMarkerIndex || a.NextMarkerIndex != b.NextMarkerIndex ||
                    !string.Equals(a.PreviousMarkerId, b.PreviousMarkerId, StringComparison.Ordinal) ||
                    !string.Equals(a.NextMarkerId, b.NextMarkerId, StringComparison.Ordinal) ||
                    !a.StartTimeSeconds.Equals(b.StartTimeSeconds) || !a.EndTimeSeconds.Equals(b.EndTimeSeconds) ||
                    a.Wraps != b.Wraps)
                    return false;
            }
            return true;
        }

        void RequireMotionMatchingPayload()
        {
            var motionMatchingProducers = new Dictionary<string, CharacterPresentationProducerEntry>(StringComparer.Ordinal);
            for (int i = 0; i < Producers.Count; i++)
            {
                CharacterPresentationProducerEntry producer = Producers[i];
                if (producer.Kind != CharacterPresentationProducerKind.Animation ||
                    producer.AnimationSourceKind != AnimationPoseSourceKind.MotionMatching)
                    continue;
                if (!motionMatchingProducers.TryAdd(producer.ProgramProducerIdentity, producer))
                    throw new InvalidOperationException($"Motion Matching producer '{producer.ProgramProducerIdentity}' is duplicated.");
            }
            if (motionMatchingProducers.Count == 0)
            {
                if (m_MotionMatching != null)
                    throw new InvalidOperationException("Projection has a Motion Matching payload without Motion Matching producers.");
                return;
            }
            if (m_MotionMatching == null)
                throw new InvalidOperationException("Projection Motion Matching producers require a Motion Matching payload.");
            if (m_MotionMatching.ProducerBindingCount != motionMatchingProducers.Count)
                throw new InvalidOperationException("Projection Motion Matching payload producer count does not match producer declarations.");
            var resolved = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_MotionMatching.ProducerBindingCount; i++)
            {
                MotionMatchingProducerBindingPayload binding = m_MotionMatching.GetProducerBinding(i);
                if (!motionMatchingProducers.TryGetValue(binding.ProgramProducerId, out CharacterPresentationProducerEntry producer) ||
                    !resolved.Add(binding.ProgramProducerId) || producer.AnimationChannelId != binding.AnimationChannelId ||
                    !HasMotionMatchingSelectionInput(binding))
                    throw new InvalidOperationException($"Motion Matching payload producer binding #{i} does not resolve uniquely to its Selection Input.");
            }
        }

        bool HasMotionMatchingSelectionInput(MotionMatchingProducerBindingPayload binding)
        {
            int count = 0;
            for (int i = 0; i < PosePlan.SelectionInputs.Count; i++)
            {
                CharacterPresentationSelectionInputEntry input = PosePlan.SelectionInputs[i];
                if (input.MotionMatching && input.NodeId == binding.PoseNodeId &&
                    input.AnimationChannelId == binding.AnimationChannelId &&
                    string.Equals(input.ProgramProducerId, binding.ProgramProducerId, StringComparison.Ordinal))
                    count++;
            }
            return count == 1;
        }
    }
}
