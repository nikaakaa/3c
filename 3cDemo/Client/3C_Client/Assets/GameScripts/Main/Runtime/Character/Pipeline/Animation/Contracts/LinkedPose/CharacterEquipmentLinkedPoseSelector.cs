using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterEquipmentLinkedPoseInterface
    {
        public static readonly LinkedPoseInterfaceId InterfaceId = new LinkedPoseInterfaceId("character.equipment-pose");
        public static readonly LinkedPoseEntryId EquipmentPoseEntryId = new LinkedPoseEntryId("equipment.pose");
        public static readonly PoseInterfacePortId PoseInputPortId = new PoseInterfacePortId("input.pose");
        public static readonly PoseInterfacePortId PoseOutputPortId = new PoseInterfacePortId("output.pose");

        public static CharacterLinkedPoseInterfaceEntryDescriptor[] CreateEntries()
        {
            return new[]
            {
                new CharacterLinkedPoseInterfaceEntryDescriptor(
                    EquipmentPoseEntryId,
                    CharacterPoseExecutionDomain.PurePose,
                    new[]
                    {
                        new CharacterLinkedPoseInterfacePortDescriptor(PoseInputPortId, CharacterPosePortDirection.Input, CharacterPosePortKind.LocalPose, CharacterPoseSpace.Local, true, 0),
                        new CharacterLinkedPoseInterfacePortDescriptor(PoseOutputPortId, CharacterPosePortDirection.Output, CharacterPosePortKind.LocalPose, CharacterPoseSpace.Local, true, 1)
                    })
            };
        }

        public static void RequireFormalContract(CharacterLinkedPoseInterfaceAsset value)
        {
            value?.RequireValid();
            if (!value || value.InterfaceId != InterfaceId || value.Revision.Value != 2)
                throw new InvalidOperationException("Equipment Linked Pose Interface identity or revision is invalid.");
            CharacterLinkedPoseInterfaceEntryDescriptor[] expected = CreateEntries();
            if (value.Entries.Count != expected.Length)
                throw new InvalidOperationException("Equipment Linked Pose Interface Entry count is invalid.");
            for (int entryIndex = 0; entryIndex < expected.Length; entryIndex++)
            {
                CharacterLinkedPoseInterfaceEntryDescriptor actualEntry = value.RequireEntry(expected[entryIndex].EntryId);
                CharacterLinkedPoseInterfaceEntryDescriptor expectedEntry = expected[entryIndex];
                if (actualEntry.ExecutionDomain != expectedEntry.ExecutionDomain || actualEntry.Ports.Count != expectedEntry.Ports.Count)
                    throw new InvalidOperationException($"Equipment Linked Pose Entry '{expectedEntry.EntryId}' signature is invalid.");
                for (int portIndex = 0; portIndex < expectedEntry.Ports.Count; portIndex++)
                {
                    CharacterLinkedPoseInterfacePortDescriptor actual = actualEntry.Ports[portIndex];
                    CharacterLinkedPoseInterfacePortDescriptor required = expectedEntry.Ports[portIndex];
                    if (actual.PortId != required.PortId || actual.Direction != required.Direction || actual.Kind != required.Kind ||
                        actual.Space != required.Space || actual.Required != required.Required || actual.Order != required.Order)
                    {
                        throw new InvalidOperationException($"Equipment Linked Pose Entry '{expectedEntry.EntryId}' port #{portIndex} signature is invalid.");
                    }
                }
            }
        }
    }

    [Serializable]
    public sealed class CharacterEquipmentLinkedPoseMapping
    {
        [SerializeField] string m_EquipmentId = string.Empty;
        [SerializeField] string m_ImplementationId = string.Empty;

        public EquipmentId EquipmentId => string.IsNullOrWhiteSpace(m_EquipmentId) ? default : new EquipmentId(m_EquipmentId);
        public LinkedPoseImplementationId ImplementationId => string.IsNullOrWhiteSpace(m_ImplementationId) ? default : new LinkedPoseImplementationId(m_ImplementationId);

        public CharacterEquipmentLinkedPoseMapping() { }

        public CharacterEquipmentLinkedPoseMapping(EquipmentId equipmentId, LinkedPoseImplementationId implementationId)
        {
            m_EquipmentId = equipmentId.IsValid ? equipmentId.Value : throw new ArgumentException("Equipment identity is invalid.", nameof(equipmentId));
            m_ImplementationId = implementationId.IsValid ? implementationId.Value : throw new ArgumentException("Linked Pose Implementation identity is invalid.", nameof(implementationId));
        }

        public void RequireValid()
        {
            if (!EquipmentId.IsValid || !ImplementationId.IsValid)
                throw new InvalidOperationException("Equipment Linked Pose mapping is incomplete.");
        }
    }

    [Serializable]
    public sealed class CharacterEquipmentLinkedPoseCompiledMapping
    {
        [SerializeField] string m_EquipmentId = string.Empty;
        [SerializeField] string m_ImplementationId = string.Empty;

        public EquipmentId EquipmentId => string.IsNullOrWhiteSpace(m_EquipmentId) ? default : new EquipmentId(m_EquipmentId);
        public LinkedPoseImplementationId ImplementationId => string.IsNullOrWhiteSpace(m_ImplementationId) ? default : new LinkedPoseImplementationId(m_ImplementationId);

        public CharacterEquipmentLinkedPoseCompiledMapping() { }

        public CharacterEquipmentLinkedPoseCompiledMapping(CharacterEquipmentLinkedPoseMapping value)
        {
            value?.RequireValid();
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            m_EquipmentId = value.EquipmentId.Value;
            m_ImplementationId = value.ImplementationId.Value;
        }
    }

    [Serializable]
    public sealed class CharacterEquipmentLinkedPoseSelectorDescriptor
    {
        [SerializeField] CharacterLinkedPoseCompiledSelectorDescriptor m_Core;
        [SerializeField] string m_SlotId = string.Empty;
        [SerializeField] string m_EmptyImplementationId = string.Empty;
        [SerializeField] CharacterEquipmentLinkedPoseCompiledMapping[] m_Mappings = Array.Empty<CharacterEquipmentLinkedPoseCompiledMapping>();

        public CharacterLinkedPoseCompiledSelectorDescriptor Core => m_Core;
        public EquipmentSlotId SlotId => string.IsNullOrWhiteSpace(m_SlotId) ? default : new EquipmentSlotId(m_SlotId);
        public LinkedPoseImplementationId EmptyImplementationId => string.IsNullOrWhiteSpace(m_EmptyImplementationId) ? default : new LinkedPoseImplementationId(m_EmptyImplementationId);
        public IReadOnlyList<CharacterEquipmentLinkedPoseCompiledMapping> Mappings => m_Mappings ?? Array.Empty<CharacterEquipmentLinkedPoseCompiledMapping>();

        public CharacterEquipmentLinkedPoseSelectorDescriptor() { }

        public CharacterEquipmentLinkedPoseSelectorDescriptor(
            CharacterLinkedPoseCompiledSelectorDescriptor core,
            EquipmentSlotId slotId,
            LinkedPoseImplementationId emptyImplementationId,
            IEnumerable<CharacterEquipmentLinkedPoseMapping> mappings)
        {
            m_Core = core ?? throw new ArgumentNullException(nameof(core));
            m_SlotId = slotId.IsValid ? slotId.Value : throw new ArgumentException("Equipment Slot identity is invalid.", nameof(slotId));
            m_EmptyImplementationId = emptyImplementationId.IsValid
                ? emptyImplementationId.Value
                : throw new ArgumentException("Empty Linked Pose Implementation identity is invalid.", nameof(emptyImplementationId));
            m_Mappings = (mappings ?? throw new ArgumentNullException(nameof(mappings)))
                .OrderBy(value => value.EquipmentId)
                .Select(value => new CharacterEquipmentLinkedPoseCompiledMapping(value))
                .ToArray();
            RequireValid();
        }

        public void RequireValid()
        {
            Core?.RequireValid();
            if (Core == null || !SlotId.IsValid || !EmptyImplementationId.IsValid || !Core.Contains(EmptyImplementationId))
                throw new InvalidOperationException("Equipment Linked Pose selector descriptor is incomplete.");
            var equipmentIds = new HashSet<EquipmentId>();
            for (int i = 0; i < Mappings.Count; i++)
            {
                CharacterEquipmentLinkedPoseCompiledMapping mapping = Mappings[i];
                if (mapping == null || !mapping.EquipmentId.IsValid || !mapping.ImplementationId.IsValid ||
                    !equipmentIds.Add(mapping.EquipmentId) || !Core.Contains(mapping.ImplementationId))
                {
                    throw new InvalidOperationException($"Equipment Linked Pose selector mapping #{i} is invalid.");
                }
            }
        }

        public LinkedPoseImplementationId Resolve(EquipmentId equipmentId)
        {
            if (!equipmentId.IsValid)
                return EmptyImplementationId;
            for (int i = 0; i < Mappings.Count; i++)
            {
                CharacterEquipmentLinkedPoseCompiledMapping mapping = Mappings[i];
                if (mapping.EquipmentId == equipmentId)
                    return mapping.ImplementationId;
            }
            throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                CharacterLinkedPoseDiagnosticCode.MissingMapping,
                $"Equipment '{equipmentId}' has no mapping for Group '{Core.GroupId}'."));
        }
    }

    public sealed class CharacterEquipmentLinkedPoseSelectionAdapter : ICharacterLinkedPoseRuntimeSelectorAdapter
    {
        readonly CharacterEquipmentLinkedPoseSelectorDescriptor m_Descriptor;
        CharacterLinkedPoseSelectionFrame m_Frame;
        CharacterLinkedPoseSelectionFrame m_CommittedFrame;
        bool m_HasFrame;
        bool m_HasCommittedFrame;

        public CharacterEquipmentLinkedPoseSelectionAdapter(CharacterEquipmentLinkedPoseSelectorDescriptor descriptor)
        {
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_Descriptor.RequireValid();
        }

        public LinkedPoseGroupId GroupId => m_Descriptor.Core.GroupId;
        public EquipmentSlotId SlotId => m_Descriptor.SlotId;

        public void Capture(in EquipmentVisualSelection selection)
        {
            if (selection.SlotId != m_Descriptor.SlotId || selection.EquipmentRevision == 0)
                throw new InvalidOperationException($"Equipment selection does not match Linked Pose selector '{m_Descriptor.Core.SelectorId}'.");
            LinkedPoseImplementationId implementationId = m_Descriptor.Resolve(selection.EquipmentId);
            var revision = new LinkedPoseRevision(selection.EquipmentRevision);
            if (m_HasFrame)
            {
                int order = revision.CompareTo(m_Frame.SelectionRevision);
                if (order < 0 || order == 0 && implementationId != m_Frame.ImplementationId)
                    throw new InvalidOperationException($"Equipment Linked Pose selection for Group '{GroupId}' regressed or changed without a revision.");
                if (order == 0)
                    return;
            }
            m_Frame = new CharacterLinkedPoseSelectionFrame(
                GroupId,
                m_Descriptor.Core.InterfaceId,
                implementationId,
                revision);
            m_CommittedFrame = m_Frame;
            m_HasFrame = true;
            m_HasCommittedFrame = true;
        }

        public bool TryReadSelection(out CharacterLinkedPoseSelectionFrame frame)
        {
            frame = m_Frame;
            return m_HasFrame;
        }

        public void SetPreviewSelection(LinkedPoseImplementationId implementationId)
        {
            if (!implementationId.IsValid || !m_Descriptor.Core.Contains(implementationId))
                throw new InvalidOperationException(CharacterLinkedPoseDiagnostic.Format(
                    CharacterLinkedPoseDiagnosticCode.MissingMapping,
                    $"Preview Implementation '{implementationId}' is outside selector '{m_Descriptor.Core.SelectorId}' candidate closure."));
            if (!m_HasFrame)
                throw new InvalidOperationException("Linked Pose preview selection requires a captured selector frame.");
            if (m_Frame.ImplementationId == implementationId)
                return;
            if (m_Frame.SelectionRevision.Value == ulong.MaxValue)
                throw new InvalidOperationException("Linked Pose preview selection revision overflowed.");
            m_Frame = new CharacterLinkedPoseSelectionFrame(
                GroupId,
                m_Descriptor.Core.InterfaceId,
                implementationId,
                new LinkedPoseRevision(m_Frame.SelectionRevision.Value + 1UL));
        }

        public void ClearPreviewSelection()
        {
            if (m_HasCommittedFrame)
                m_Frame = m_CommittedFrame;
        }

        public void Reset()
        {
            m_Frame = default;
            m_CommittedFrame = default;
            m_HasFrame = false;
            m_HasCommittedFrame = false;
        }
    }
}
