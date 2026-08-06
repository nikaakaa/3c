using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [CreateAssetMenu(fileName = "CharacterEquipmentLinkedPoseSelection", menuName = "3C/Character/Linked Pose/Equipment Selector")]
    public sealed class CharacterEquipmentLinkedPoseSelectionBinding : CharacterLinkedPoseSelectorBindingAsset
    {
        [SerializeField] string m_SelectorId = string.Empty;
        [SerializeField] string m_GroupId = string.Empty;
        [SerializeField] string m_SlotId = string.Empty;
        [SerializeField] string m_EmptyImplementationId = string.Empty;
        [SerializeField] CharacterEquipmentLinkedPoseMapping[] m_Mappings = Array.Empty<CharacterEquipmentLinkedPoseMapping>();

        public override LinkedPoseSelectorId SelectorId => string.IsNullOrWhiteSpace(m_SelectorId) ? default : new LinkedPoseSelectorId(m_SelectorId);
        public override LinkedPoseGroupId GroupId => string.IsNullOrWhiteSpace(m_GroupId) ? default : new LinkedPoseGroupId(m_GroupId);
        public EquipmentSlotId SlotId => string.IsNullOrWhiteSpace(m_SlotId) ? default : new EquipmentSlotId(m_SlotId);
        public LinkedPoseImplementationId EmptyImplementationId => string.IsNullOrWhiteSpace(m_EmptyImplementationId) ? default : new LinkedPoseImplementationId(m_EmptyImplementationId);
        public IReadOnlyList<CharacterEquipmentLinkedPoseMapping> Mappings => m_Mappings ?? Array.Empty<CharacterEquipmentLinkedPoseMapping>();
        public override IReadOnlyList<LinkedPoseImplementationId> CandidateImplementationIds => CollectCandidates();

        public void Configure(
            LinkedPoseSelectorId selectorId,
            LinkedPoseGroupId groupId,
            EquipmentSlotId slotId,
            LinkedPoseImplementationId emptyImplementationId,
            CharacterEquipmentLinkedPoseMapping[] mappings)
        {
            m_SelectorId = selectorId.IsValid ? selectorId.Value : throw new ArgumentException("Linked Pose selector identity is invalid.", nameof(selectorId));
            m_GroupId = groupId.IsValid ? groupId.Value : throw new ArgumentException("Linked Pose Group identity is invalid.", nameof(groupId));
            m_SlotId = slotId.IsValid ? slotId.Value : throw new ArgumentException("Equipment Slot identity is invalid.", nameof(slotId));
            m_EmptyImplementationId = emptyImplementationId.IsValid
                ? emptyImplementationId.Value
                : throw new ArgumentException("Empty Linked Pose Implementation identity is invalid.", nameof(emptyImplementationId));
            m_Mappings = mappings ?? Array.Empty<CharacterEquipmentLinkedPoseMapping>();
        }

        public override CharacterLinkedPoseCompiledSelectorDescriptor CompileCore(CharacterLinkedPoseGroupBinding group)
        {
            group?.RequireValid();
            if (group == null || group.GroupId != GroupId)
                throw new InvalidOperationException($"Equipment Linked Pose selector '{SelectorId}' Group does not match.");
            return new CharacterLinkedPoseCompiledSelectorDescriptor(
                SelectorId,
                GroupId,
                group.Interface.InterfaceId,
                CandidateImplementationIds);
        }

        public CharacterEquipmentLinkedPoseSelectorDescriptor Compile(CharacterLinkedPoseGroupBinding group)
        {
            return new CharacterEquipmentLinkedPoseSelectorDescriptor(
                CompileCore(group),
                SlotId,
                EmptyImplementationId,
                Mappings);
        }

        public override void RequireValid(
            CharacterLinkedPoseGroupBinding group,
            IReadOnlyDictionary<LinkedPoseImplementationId, CharacterLinkedPoseImplementationAsset> implementations)
        {
            if (!SelectorId.IsValid || !GroupId.IsValid || !SlotId.IsValid || !EmptyImplementationId.IsValid || group == null || group.GroupId != GroupId)
                throw new InvalidOperationException($"Equipment Linked Pose selector '{name}' is incomplete.");
            var equipmentIds = new HashSet<EquipmentId>();
            for (int i = 0; i < Mappings.Count; i++)
            {
                CharacterEquipmentLinkedPoseMapping mapping = Mappings[i];
                mapping?.RequireValid();
                if (mapping == null || !equipmentIds.Add(mapping.EquipmentId))
                    throw new InvalidOperationException($"Equipment Linked Pose selector '{SelectorId}' mapping #{i} is missing or duplicated.");
            }
            RequireCandidateClosure(group, CandidateImplementationIds, implementations);
        }

        LinkedPoseImplementationId[] CollectCandidates()
        {
            if (!EmptyImplementationId.IsValid)
                return Array.Empty<LinkedPoseImplementationId>();
            var values = new HashSet<LinkedPoseImplementationId> { EmptyImplementationId };
            for (int i = 0; i < Mappings.Count; i++)
            {
                CharacterEquipmentLinkedPoseMapping mapping = Mappings[i];
                if (mapping != null && mapping.ImplementationId.IsValid)
                    values.Add(mapping.ImplementationId);
            }
            return values.OrderBy(value => value).ToArray();
        }
    }
}
