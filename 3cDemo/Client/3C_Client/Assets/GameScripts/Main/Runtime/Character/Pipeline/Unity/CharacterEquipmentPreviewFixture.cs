using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    [Serializable]
    public sealed class CharacterEquipmentPreviewSelection
    {
        [SerializeField] string m_SlotId = string.Empty;
        [SerializeField] string m_EquipmentId = string.Empty;
        [SerializeField] string m_VisualBindingId = string.Empty;
        [SerializeField, Min(1)] long m_EquipmentRevision = 1;
        [SerializeField, Min(0)] long m_SourceTick;

        internal EquipmentSlotId SlotId =>
            string.IsNullOrWhiteSpace(m_SlotId)
                ? default
                : new EquipmentSlotId(m_SlotId.Trim());
        internal EquipmentId EquipmentId =>
            string.IsNullOrWhiteSpace(m_EquipmentId)
                ? default
                : new EquipmentId(m_EquipmentId.Trim());
        internal EquipmentVisualBindingId VisualBindingId =>
            string.IsNullOrWhiteSpace(m_VisualBindingId)
                ? default
                : new EquipmentVisualBindingId(
                    m_VisualBindingId.Trim());
        internal ulong EquipmentRevision =>
            m_EquipmentRevision > 0
                ? (ulong)m_EquipmentRevision
                : 0;
        internal ulong SourceTick =>
            m_SourceTick >= 0
                ? (ulong)m_SourceTick
                : 0;
    }

    [DisallowMultipleComponent]
    public sealed class CharacterEquipmentPreviewFixture : MonoBehaviour
    {
        [SerializeField]
        CharacterEquipmentPreviewSelection[] m_CommittedSelections =
            Array.Empty<CharacterEquipmentPreviewSelection>();

        public IReadOnlyList<CharacterEquipmentPreviewSelection>
            CommittedSelections =>
                m_CommittedSelections ??
                Array.Empty<CharacterEquipmentPreviewSelection>();

        internal EquipmentVisualSelection[] BuildSelections(
            ActorId actorId,
            CharacterLinkedPoseProjectionPayload linkedPose)
        {
            if (!actorId.IsValid)
                throw new ArgumentException(
                    "Equipment Preview Actor identity is invalid.",
                    nameof(actorId));
            linkedPose = linkedPose ??
                throw new ArgumentNullException(nameof(linkedPose));
            linkedPose.RequireValid();

            var requiredSlots = new HashSet<EquipmentSlotId>();
            for (int i = 0;
                 i < linkedPose.EquipmentSelectors.Count;
                 i++)
            {
                requiredSlots.Add(
                    linkedPose.EquipmentSelectors[i].SlotId);
            }
            if (CommittedSelections.Count != requiredSlots.Count)
            {
                throw new InvalidOperationException(
                    "Equipment Preview fixture must declare exactly one committed selection for every Linked Pose selector Slot.");
            }

            var selections =
                new EquipmentVisualSelection[CommittedSelections.Count];
            var declaredSlots = new HashSet<EquipmentSlotId>();
            for (int i = 0; i < CommittedSelections.Count; i++)
            {
                CharacterEquipmentPreviewSelection entry =
                    CommittedSelections[i] ??
                    throw new InvalidOperationException(
                        $"Equipment Preview selection #{i} is missing.");
                EquipmentSlotId slotId = entry.SlotId;
                if (!slotId.IsValid ||
                    !requiredSlots.Contains(slotId) ||
                    !declaredSlots.Add(slotId) ||
                    entry.EquipmentRevision == 0 ||
                    entry.EquipmentId.IsValid !=
                    entry.VisualBindingId.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Equipment Preview selection #{i} is incomplete, duplicated, or outside the compiled selector closure.");
                }
                for (int selectorIndex = 0;
                     selectorIndex <
                     linkedPose.EquipmentSelectors.Count;
                     selectorIndex++)
                {
                    CharacterEquipmentLinkedPoseSelectorDescriptor selector =
                        linkedPose.EquipmentSelectors[selectorIndex];
                    if (selector.SlotId == slotId)
                        _ = selector.Resolve(entry.EquipmentId);
                }
                selections[i] = new EquipmentVisualSelection(
                    actorId,
                    slotId,
                    entry.EquipmentId,
                    entry.VisualBindingId,
                    entry.EquipmentRevision,
                    entry.SourceTick);
            }
            return selections;
        }
    }
}
