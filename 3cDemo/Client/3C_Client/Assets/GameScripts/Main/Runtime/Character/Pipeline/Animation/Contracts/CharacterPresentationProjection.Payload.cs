using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed partial class CharacterPresentationProjection
    {
        [SerializeField] CharacterPresentationPoseProgram m_PoseProgram;
        [SerializeField] AnimationBlendSlotPayload[] m_BlendSlots = Array.Empty<AnimationBlendSlotPayload>();
        [SerializeField] AnimationBlendCurveCatalogPayload m_BlendCurveCatalog;
        [SerializeField] AnimationBlendProfileCatalogPayload m_BlendProfileCatalog;
        [SerializeField] CharacterAnimationRigPayload m_Rig;
        [NonSerialized] MotionMatchingProjectionPayload m_MotionMatching;
        [SerializeField] byte[] m_MotionMatchingPayload = Array.Empty<byte>();
        [SerializeField] UnityEngine.AnimationClip[] m_MotionMatchingClips = Array.Empty<UnityEngine.AnimationClip>();

        public CharacterPresentationPoseProgram PoseProgram => m_PoseProgram;
        public IReadOnlyList<AnimationBlendSlotPayload> BlendSlots => m_BlendSlots ?? Array.Empty<AnimationBlendSlotPayload>();
        public AnimationBlendCurveCatalogPayload BlendCurveCatalog => m_BlendCurveCatalog;
        public AnimationBlendProfileCatalogPayload BlendProfileCatalog => m_BlendProfileCatalog;
        public CharacterAnimationRigPayload Rig => m_Rig;
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

        public AnimationBlendSlotPayload RequireBlendSlot(PoseSlotId poseSlotId)
        {
            AnimationBlendSlotPayload result = null;
            for (int i = 0; i < BlendSlots.Count; i++)
            {
                AnimationBlendSlotPayload candidate = BlendSlots[i];
                if (candidate == null || candidate.PoseSlotId != poseSlotId)
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"Presentation Projection duplicates Pose Slot '{poseSlotId}'.");
                result = candidate;
            }
            return result ?? throw new InvalidOperationException($"Presentation Projection has no Blend payload for Pose Slot '{poseSlotId}'.");
        }

        internal static CharacterPresentationProjection Create(
            CharacterPresentationSemanticContract contract,
            CharacterPresentationPoseProgram poseProgram,
            AnimationBlendSlotPayload[] blendSlots,
            AnimationBlendCurveCatalogPayload blendCurveCatalog,
            AnimationBlendProfileCatalogPayload blendProfileCatalog,
            CharacterAnimationRigPayload rig,
            MotionMatchingProjectionPayload motionMatching,
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
                m_PoseProgram = poseProgram ?? throw new ArgumentNullException(nameof(poseProgram)),
                m_BlendSlots = blendSlots ?? throw new ArgumentNullException(nameof(blendSlots)),
                m_BlendCurveCatalog = blendCurveCatalog ?? throw new ArgumentNullException(nameof(blendCurveCatalog)),
                m_BlendProfileCatalog = blendProfileCatalog ?? throw new ArgumentNullException(nameof(blendProfileCatalog)),
                m_Rig = rig ?? throw new ArgumentNullException(nameof(rig)),
                m_MotionMatching = motionMatching,
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
            PoseProgram?.RequireValid();
            Rig?.RequireValid();
            BlendCurveCatalog?.RequireValid();
            BlendProfileCatalog?.RequireValid(Rig?.Bones.Count ?? 0, Rig?.RigId, Rig?.RigRevision);
            if (PoseProgram == null || Rig == null || BlendCurveCatalog == null || BlendProfileCatalog == null ||
                BlendSlots.Count != PoseProgram.Slots.Count ||
                !string.Equals(PoseProgram.RigId, Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(PoseProgram.RigRevision, Rig.RigRevision, StringComparison.Ordinal) ||
                PoseProgram.BoneCount != Rig.Bones.Count)
            {
                throw new InvalidOperationException("Character Presentation Projection Pose payload is incomplete or inconsistent.");
            }

            var channels = new HashSet<ThirdPersonSimulation.AnimationChannelId>();
            var slots = new HashSet<PoseSlotId>();
            for (int i = 0; i < PoseProgram.Slots.Count; i++)
            {
                CharacterPresentationPoseSlotProgramEntry slot = PoseProgram.Slots[i];
                AnimationBlendSlotPayload blend = RequireBlendSlot(slot.PoseSlotId);
                if (blend.AnimationChannelId != slot.AnimationChannelId || blend.OutputPolicy != slot.OutputPolicy ||
                    !channels.Add(slot.AnimationChannelId) || !slots.Add(slot.PoseSlotId) || blend.StackPolicy == null)
                {
                    throw new InvalidOperationException($"Character Presentation Projection Pose Slot #{i} is inconsistent.");
                }
                blend.StackPolicy.RequireValid();
                for (int transitionIndex = 0; transitionIndex < blend.Transitions.Count; transitionIndex++)
                {
                    AnimationBlendTransitionPayload transition = blend.Transitions[transitionIndex];
                    if (transition == null)
                        throw new InvalidOperationException($"Character Presentation Projection Pose Slot '{slot.PoseSlotId}' transition #{transitionIndex} is missing.");
                    transition.RequireValid(BlendCurveCatalog.Entries.Count, BlendProfileCatalog.Entries.Count);
                }
            }
            RequireMotionMatchingPayload();
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
                    !resolved.Add(binding.ProgramProducerId) ||
                    producer.AnimationChannelId != binding.AnimationChannelId ||
                    PoseProgram.RequireSlot(binding.AnimationChannelId).PoseSlotId != binding.PoseSlotId)
                    throw new InvalidOperationException($"Motion Matching payload producer binding #{i} does not resolve uniquely to its declared producer, channel and Pose Slot.");
            }
        }
    }
}
