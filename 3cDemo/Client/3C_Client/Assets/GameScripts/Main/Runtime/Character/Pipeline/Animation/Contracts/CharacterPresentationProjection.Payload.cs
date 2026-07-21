using System;
using System.Collections.Generic;
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

        public CharacterPresentationPoseProgram PoseProgram => m_PoseProgram;
        public IReadOnlyList<AnimationBlendSlotPayload> BlendSlots => m_BlendSlots ?? Array.Empty<AnimationBlendSlotPayload>();
        public AnimationBlendCurveCatalogPayload BlendCurveCatalog => m_BlendCurveCatalog;
        public AnimationBlendProfileCatalogPayload BlendProfileCatalog => m_BlendProfileCatalog;
        public CharacterAnimationRigPayload Rig => m_Rig;

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
        }
    }
}
